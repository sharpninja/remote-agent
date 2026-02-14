using System.Reflection;
using System.Text;
using Grpc.Core;
using Microsoft.Extensions.Options;
using RemoteAgent.Proto;
using RemoteAgent.Service.Agents;
using RemoteAgent.Service.Storage;

namespace RemoteAgent.Service.Services;

/// <summary>gRPC service implementing <see cref="Proto.AgentGateway"/> (FR-1.2–FR-1.5, TR-2.3, TR-3, TR-4). Handles GetServerInfo and Connect (duplex stream): forwards client messages to the agent, streams agent stdout/stderr and session events to the client.</summary>
/// <remarks>Connect implements the full session lifecycle (FR-7.1): START spawns the agent via <see cref="IAgentRunnerFactory"/>, text is forwarded to stdin (FR-1.3), output/error/events are streamed (FR-1.4). Supports script requests (FR-9), media upload (FR-10), and logs requests/responses (TR-11.1). Session logs are written per connection (TR-3.6).</remarks>
/// <see href="https://sharpninja.github.io/remote-agent/functional-requirements.html">Functional requirements</see>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-3, TR-4)</see>
public class AgentGatewayService(
    ILogger<AgentGatewayService> logger,
    IOptions<AgentOptions> options,
    IAgentRunnerFactory agentRunnerFactory,
    IReadOnlyDictionary<string, IAgentRunner> runnerRegistry,
    ILocalStorage localStorage,
    MediaStorageService mediaStorage) : AgentGateway.AgentGatewayBase
{
    /// <summary>Returns server version, capabilities (e.g. scripts, media_upload, agents), and list of available agent runner ids.</summary>
    public override Task<ServerInfoResponse> GetServerInfo(ServerInfoRequest request, ServerCallContext context)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        var response = new ServerInfoResponse { ServerVersion = version };
        response.Capabilities.AddRange(new[] { "scripts", "media_upload", "agents" });
        response.AvailableAgents.AddRange(runnerRegistry.Keys);
        return Task.FromResult(response);
    }

    /// <summary>Opens a duplex stream: reads ClientMessage (text, control, script, media), spawns agent on START, forwards text to agent stdin, streams stdout/stderr and SessionEvent to the client (FR-1.3, FR-1.4, FR-7.1, TR-4.4).</summary>
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

        IAgentSession? agentSession = null;
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
                        localStorage.LogRequest(sessionId, "Control", action.ToString());
                        if (action == SessionControl.Types.Action.Start)
                        {
                            if (agentSession != null)
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
                                localStorage.LogResponse(sessionId, "Event", "SessionError");
                                continue;
                            }
                            try
                            {
                                var agentRunner = agentRunnerFactory.GetRunner();
                                agentSession = await agentRunner.StartAsync(
                                    cmd,
                                    options.Value.Arguments,
                                    sessionId,
                                    logWriter,
                                    context.CancellationToken);
                                if (agentSession == null)
                                {
                                    await responseStream.WriteAsync(new ServerMessage
                                    {
                                        Priority = MessagePriority.Normal,
                                        Event = new SessionEvent
                                        {
                                            Kind = SessionEvent.Types.Kind.SessionError,
                                            Message = "Agent runner did not start a session."
                                        }
                                    }, context.CancellationToken);
                                    localStorage.LogResponse(sessionId, "Event", "SessionError");
                                    continue;
                                }
                                Log("Agent started");
                                await responseStream.WriteAsync(new ServerMessage
                                {
                                    Priority = MessagePriority.Normal,
                                    Event = new SessionEvent { Kind = SessionEvent.Types.Kind.SessionStarted }
                                }, context.CancellationToken);
                                localStorage.LogResponse(sessionId, "Event", "SessionStarted");
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
                                localStorage.LogResponse(sessionId, "Event", "SessionError");
                            }
                        }
                        else if (action == SessionControl.Types.Action.Stop)
                        {
                            if (agentSession != null)
                            {
                                agentSession.Stop();
                                try { agentSession.Dispose(); } catch { }
                                agentSession = null;
                                Log("Agent stopped");
                                await responseStream.WriteAsync(new ServerMessage
                                {
                                    Priority = MessagePriority.Normal,
                                    Event = new SessionEvent { Kind = SessionEvent.Types.Kind.SessionStopped }
                                }, context.CancellationToken);
                                localStorage.LogResponse(sessionId, "Event", "SessionStopped");
                            }
                        }
                    }
                    else if (msg.PayloadCase == ClientMessage.PayloadOneofCase.Text && !string.IsNullOrEmpty(msg.Text))
                    {
                        localStorage.LogRequest(sessionId, "Text", msg.Text);
                        Log($"→ {msg.Text}");
                        if (agentSession != null && !agentSession.HasExited)
                        {
                            try
                            {
                                await agentSession.SendInputAsync(msg.Text, context.CancellationToken);
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
                    else if (msg.PayloadCase == ClientMessage.PayloadOneofCase.ScriptRequest && msg.ScriptRequest != null)
                    {
                        var req = msg.ScriptRequest;
                        var pathOrCommand = req.PathOrCommand ?? "";
                        var scriptType = req.ScriptType == ScriptType.Unspecified ? ScriptType.Bash : req.ScriptType;
                        localStorage.LogRequest(sessionId, "ScriptRequest", $"{scriptType}: {pathOrCommand}");
                        Log($"Script run: {scriptType} {pathOrCommand}");
                        try
                        {
                            var (stdout, stderr) = await ScriptRunner.RunAsync(pathOrCommand, scriptType, context.CancellationToken);
                            if (!string.IsNullOrEmpty(stdout))
                            {
                                await responseStream.WriteAsync(new ServerMessage { Priority = MessagePriority.Normal, Output = stdout }, context.CancellationToken);
                                localStorage.LogResponse(sessionId, "Output", stdout);
                            }
                            if (!string.IsNullOrEmpty(stderr))
                            {
                                await responseStream.WriteAsync(new ServerMessage { Priority = MessagePriority.Normal, Error = stderr }, context.CancellationToken);
                                localStorage.LogResponse(sessionId, "Error", stderr);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"Script failed: {ex.Message}", "ERROR");
                            await responseStream.WriteAsync(new ServerMessage { Priority = MessagePriority.Normal, Error = ex.Message }, context.CancellationToken);
                            localStorage.LogResponse(sessionId, "Error", ex.Message);
                        }
                    }
                    else if (msg.PayloadCase == ClientMessage.PayloadOneofCase.MediaUpload && msg.MediaUpload != null)
                    {
                        var up = msg.MediaUpload;
                        if (up.Content == null || up.Content.Length == 0)
                        {
                            await responseStream.WriteAsync(new ServerMessage { Priority = MessagePriority.Normal, Error = "Media upload has no content." }, context.CancellationToken);
                            continue;
                        }
                        try
                        {
                            var (relativePath, fullPath) = mediaStorage.SaveUpload(sessionId, up.Content.ToByteArray(), up.ContentType ?? "application/octet-stream", up.FileName);
                            localStorage.LogRequest(sessionId, "MediaUpload", up.FileName ?? up.ContentType ?? "media", relativePath);
                            Log($"Media saved: {relativePath}");
                            if (agentSession != null && !agentSession.HasExited)
                                await agentSession.SendInputAsync($"[Attachment: {fullPath}]", context.CancellationToken);
                            await responseStream.WriteAsync(new ServerMessage
                            {
                                Priority = MessagePriority.Normal,
                                Output = $"[Saved attachment: {relativePath}]"
                            }, context.CancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Log($"Media save failed: {ex.Message}", "ERROR");
                            await responseStream.WriteAsync(new ServerMessage { Priority = MessagePriority.Normal, Error = ex.Message }, context.CancellationToken);
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
                if (agentSession != null)
                {
                    try { agentSession.Stop(); agentSession.Dispose(); } catch { }
                }
                cts.Cancel();
            }
        }, cts.Token);

        // Wait for session to start, then stream stdout/stderr
        try
        {
            while (agentSession == null && !context.CancellationToken.IsCancellationRequested)
                await Task.Delay(50, context.CancellationToken);
        }
        catch (OperationCanceledException) { }

        if (agentSession != null)
        {
            var stdoutTask = StreamReaderToResponse(agentSession.StandardOutput, responseStream, isError: false, context.CancellationToken, Log, "stdout");
            var stderrTask = StreamReaderToResponse(agentSession.StandardError, responseStream, isError: true, context.CancellationToken, Log, "stderr");
            await Task.WhenAll(stdoutTask, stderrTask);
        }

        await requestTask;
        Log($"Session {sessionId} ended. Log: {logPath}");
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
