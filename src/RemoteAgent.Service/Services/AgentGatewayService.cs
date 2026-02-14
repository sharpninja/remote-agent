using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using Grpc.Core;
using Microsoft.Extensions.Options;
using RemoteAgent.Proto;

namespace RemoteAgent.Service.Services;

public class AgentGatewayService(
    ILogger<AgentGatewayService> logger,
    IOptions<AgentOptions> options) : AgentGateway.AgentGatewayBase
{
    public override async Task Connect(
        IAsyncStreamReader<ClientMessage> requestStream,
        IServerStreamWriter<ServerMessage> responseStream,
        ServerCallContext context)
    {
        var sessionId = Guid.NewGuid().ToString("N")[..8];
        var logPath = Path.Combine(
            options.Value.LogDirectory ?? Path.GetTempPath(),
            $"remote-agent-{sessionId}.log");
        using var logWriter = new StreamWriter(logPath, append: true, Encoding.UTF8) { AutoFlush = true };

        void Log(string line, string level = "INFO")
        {
            var entry = $"[{DateTime.UtcNow:O}] [{level}] {line}";
            logger.LogInformation("{Entry}", entry);
            try { logWriter.WriteLine(entry); } catch { /* ignore */ }
        }

        Process? agentProcess = null;
        var cts = new CancellationTokenSource();
        context.CancellationToken.Register(() => cts.Cancel());
        var requestTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var msg in requestStream.ReadAllAsync(cts.Token))
                {
                    if (msg.PayloadCase == ClientMessage.PayloadOneofCase.Control)
                    {
                        var action = msg.Control.Action;
                        if (action == SessionControl.Types.Action.Start)
                        {
                            if (agentProcess != null)
                            {
                                Log("Session already started");
                                continue;
                            }
                            var cmd = options.Value.Command;
                            if (string.IsNullOrWhiteSpace(cmd))
                            {
                                Log("Agent:Command not configured", "WARN");
                                await responseStream.WriteAsync(new ServerMessage
                                {
                                    Priority = MessagePriority.Normal,
                                    Event = new SessionEvent
                                    {
                                        Kind = SessionEvent.Types.Kind.SessionError,
                                        Message = "Agent command not configured. Set Agent:Command in appsettings."
                                    }
                                }, context.CancellationToken);
                                continue;
                            }
                            try
                            {
                                agentProcess = StartAgentProcess(cmd, options.Value.Arguments, logWriter, sessionId);
                                Log($"Agent started (PID {agentProcess.Id})");
                                await responseStream.WriteAsync(new ServerMessage
                                {
                                    Priority = MessagePriority.Normal,
                                    Event = new SessionEvent { Kind = SessionEvent.Types.Kind.SessionStarted }
                                }, context.CancellationToken);
                            }
                            catch (Exception ex)
                            {
                                Log($"Failed to start agent: {ex.Message}", "ERROR");
                                await responseStream.WriteAsync(new ServerMessage
                                {
                                    Priority = MessagePriority.Normal,
                                    Event = new SessionEvent
                                    {
                                        Kind = SessionEvent.Types.Kind.SessionError,
                                        Message = ex.Message
                                    }
                                }, context.CancellationToken);
                            }
                        }
                        else if (action == SessionControl.Types.Action.Stop)
                        {
                            if (agentProcess != null)
                            {
                                try { agentProcess.Kill(entireProcessTree: true); } catch { /* ignore */ }
                                agentProcess = null;
                                Log("Agent stopped");
                                await responseStream.WriteAsync(new ServerMessage
                                {
                                    Priority = MessagePriority.Normal,
                                    Event = new SessionEvent { Kind = SessionEvent.Types.Kind.SessionStopped }
                                }, context.CancellationToken);
                            }
                        }
                    }
                    else if (msg.PayloadCase == ClientMessage.PayloadOneofCase.Text && !string.IsNullOrEmpty(msg.Text))
                    {
                        Log($"→ {msg.Text}");
                        if (agentProcess?.StandardInput != null && !agentProcess.HasExited)
                        {
                            try
                            {
                                await agentProcess.StandardInput.WriteLineAsync(msg.Text);
                                await agentProcess.StandardInput.FlushAsync();
                            }
                            catch (Exception ex)
                            {
                                Log($"Write to agent failed: {ex.Message}", "ERROR");
                                await responseStream.WriteAsync(new ServerMessage { Priority = MessagePriority.Normal, Error = ex.Message }, context.CancellationToken);
                            }
                        }
                        else
                        {
                            await responseStream.WriteAsync(new ServerMessage
                            {
                                Priority = MessagePriority.Normal,
                                Error = "Agent not running. Send START first."
                            }, context.CancellationToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log($"Request stream error: {ex.Message}", "ERROR");
            }
            finally
            {
                if (agentProcess != null)
                {
                    try { agentProcess.Kill(entireProcessTree: true); } catch { }
                }
                cts.Cancel();
            }
        }, cts.Token);

        // Stream agent stdout/stderr to responseStream
        try
        {
            while (agentProcess == null && !context.CancellationToken.IsCancellationRequested)
                await Task.Delay(50, context.CancellationToken);
        }
        catch (OperationCanceledException) { }

        if (agentProcess != null)
        {
            var stdoutTask = StreamReaderToResponse(agentProcess.StandardOutput, responseStream, isError: false, context.CancellationToken, Log, "stdout");
            var stderrTask = StreamReaderToResponse(agentProcess.StandardError, responseStream, isError: true, context.CancellationToken, Log, "stderr");
            await Task.WhenAll(stdoutTask, stderrTask);
        }

        await requestTask;
        Log($"Session {sessionId} ended. Log: {logPath}");
    }

    private static Process StartAgentProcess(string command, string? arguments, StreamWriter logWriter, string sessionId)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments ?? "",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        var p = Process.Start(psi) ?? throw new InvalidOperationException("Process.Start returned null");
        return p;
    }

    private static async Task StreamReaderToResponse(
        StreamReader reader,
        IServerStreamWriter<ServerMessage> responseStream,
        bool isError,
        CancellationToken ct,
        Action<string, string> log,
        string streamName)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                log($"[{streamName}] {line}", isError ? "STDERR" : "INFO");
                var msg = new ServerMessage { Priority = MessagePriority.Normal };
                if (isError) msg.Error = line; else msg.Output = line;
                await responseStream.WriteAsync(msg, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            log($"Stream {streamName} error: {ex.Message}", "ERROR");
        }
    }
}
