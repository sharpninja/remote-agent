using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
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
    /// <summary>Strips ANSI escape sequences (e.g. color codes) from text before sending to the client.</summary>
    private static string StripAnsi(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        // CSI: ESC [ parameters letter (colors, cursor, etc.)
        text = Regex.Replace(text, @"\x1b\[[0-9;]*[a-zA-Z]", "");
        // OSC: ESC ] ... BEL or ESC \
        text = Regex.Replace(text, @"\x1b\][^\x07\x1b]*(?:\x07|\x1b\\)?", "");
        return text;
    }

    /// <summary>Returns server version, capabilities (e.g. scripts, media_upload, agents), and list of available agent runner ids.</summary>
    public override Task<ServerInfoResponse> GetServerInfo(ServerInfoRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
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
        EnsureAuthorized(context);
        // Session id for this stream: use client-provided on START, else generate (TR-12.1, FR-11.1.1).
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
        // Correlation ID for agent stdout/stderr: set when forwarding text, so responses can be matched (TR-4.5).
        var outputCorrelationId = new[] { "" };
        var requestTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var msg in requestStream.ReadAllAsync(cts.Token))
                {
                    string corrId = msg.CorrelationId ?? "";
                    if (msg.PayloadCase == ClientMessage.PayloadOneofCase.Control)
                    {
                        var control = msg.Control;
                        var action = control.Action;
                        // Use client-provided session_id when present (TR-12.1, FR-11.1.1).
                        if (action == SessionControl.Types.Action.Start && !string.IsNullOrWhiteSpace(control.SessionId))
                            sessionId = SanitizeSessionId(control.SessionId);
                        localStorage.LogRequest(sessionId, "Control", action.ToString());
                        if (action == SessionControl.Types.Action.Start)
                        {
                            if (agentSession != null)
                            {
                                Log("Session already started");
                                continue;
                            }
                            // "none" = explicitly no agent (e.g. tests). null/empty = use runner default (e.g. copilot on Windows, agent on Linux).
                            var cmd = options.Value.Command;
                            if (string.Equals(cmd?.Trim(), "none", StringComparison.OrdinalIgnoreCase))
                            {
                                Log("Agent:Command is set to 'none' (no agent configured)", "WARN");
                                await responseStream.WriteAsync(new ServerMessage
                                {
                                    Priority = MessagePriority.Normal,
                                    Event = new SessionEvent
                                    {
                                        Kind = SessionEvent.Types.Kind.SessionError,
                                        Message = "Agent command not configured. Set Agent:Command in appsettings."
                                    },
                                    CorrelationId = corrId
                                }, context.CancellationToken);
                                localStorage.LogResponse(sessionId, "Event", "SessionError");
                                continue;
                            }
                            try
                            {
                                // Resolve runner by client agent_id when provided (FR-11.1.2, TR-12.1).
                                IAgentRunner agentRunner = string.IsNullOrWhiteSpace(control.AgentId)
                                    ? agentRunnerFactory.GetRunner()
                                    : (runnerRegistry.TryGetValue(control.AgentId.Trim(), out var r) ? r : agentRunnerFactory.GetRunner());
                                agentSession = await agentRunner.StartAsync(
                                    string.IsNullOrWhiteSpace(cmd) ? null : cmd,
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
                                        },
                                        CorrelationId = corrId
                                    }, context.CancellationToken);
                                    localStorage.LogResponse(sessionId, "Event", "SessionError");
                                    continue;
                                }
                                Log("Agent started");
                                await responseStream.WriteAsync(new ServerMessage
                                {
                                    Priority = MessagePriority.Normal,
                                    Event = new SessionEvent { Kind = SessionEvent.Types.Kind.SessionStarted },
                                    CorrelationId = corrId
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
                                    },
                                    CorrelationId = corrId
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
                                    Event = new SessionEvent { Kind = SessionEvent.Types.Kind.SessionStopped },
                                    CorrelationId = corrId
                                }, context.CancellationToken);
                                localStorage.LogResponse(sessionId, "Event", "SessionStopped");
                            }
                        }
                    }
                    else if (msg.PayloadCase == ClientMessage.PayloadOneofCase.Text && !string.IsNullOrEmpty(msg.Text))
                    {
                        outputCorrelationId[0] = corrId;
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
                                await responseStream.WriteAsync(new ServerMessage { Priority = MessagePriority.Normal, Error = StripAnsi(ex.Message), CorrelationId = corrId }, context.CancellationToken);
                            }
                        }
                        else
                        {
                            await responseStream.WriteAsync(new ServerMessage
                            {
                                Priority = MessagePriority.Normal,
                                Error = "Agent not running. Send START first.",
                                CorrelationId = corrId
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
                                var outClean = StripAnsi(stdout);
                                await responseStream.WriteAsync(new ServerMessage { Priority = MessagePriority.Normal, Output = outClean, CorrelationId = corrId }, context.CancellationToken);
                                localStorage.LogResponse(sessionId, "Output", outClean);
                            }
                            if (!string.IsNullOrEmpty(stderr))
                            {
                                var errClean = StripAnsi(stderr);
                                await responseStream.WriteAsync(new ServerMessage { Priority = MessagePriority.Normal, Error = errClean, CorrelationId = corrId }, context.CancellationToken);
                                localStorage.LogResponse(sessionId, "Error", errClean);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"Script failed: {ex.Message}", "ERROR");
                            await responseStream.WriteAsync(new ServerMessage { Priority = MessagePriority.Normal, Error = ex.Message, CorrelationId = corrId }, context.CancellationToken);
                            localStorage.LogResponse(sessionId, "Error", ex.Message);
                        }
                    }
                    else if (msg.PayloadCase == ClientMessage.PayloadOneofCase.MediaUpload && msg.MediaUpload != null)
                    {
                        var up = msg.MediaUpload;
                        if (up.Content == null || up.Content.Length == 0)
                        {
                            await responseStream.WriteAsync(new ServerMessage { Priority = MessagePriority.Normal, Error = "Media upload has no content.", CorrelationId = corrId }, context.CancellationToken);
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
                                Output = $"[Saved attachment: {relativePath}]",
                                CorrelationId = corrId
                            }, context.CancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Log($"Media save failed: {ex.Message}", "ERROR");
                            await responseStream.WriteAsync(new ServerMessage { Priority = MessagePriority.Normal, Error = ex.Message, CorrelationId = corrId }, context.CancellationToken);
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

        // Wait for session to start (or for request stream to end so we can finish when no agent was started, e.g. SessionError)
        try
        {
            while (agentSession == null && !requestTask.IsCompleted && !context.CancellationToken.IsCancellationRequested)
                await Task.Delay(50, context.CancellationToken);
        }
        catch (OperationCanceledException) { }

        if (agentSession != null)
        {
            var stdoutTask = StreamReaderToResponse(agentSession.StandardOutput, responseStream, false, context.CancellationToken, Log, "stdout");
            var stderrTask = StreamReaderToResponse(agentSession.StandardError, responseStream, true, context.CancellationToken, Log, "stderr");
            await Task.WhenAll(stdoutTask, stderrTask);
        }

        await requestTask;
        Log($"Session {sessionId} ended. Log: {logPath}");
    }

    private const string ApiKeyHeader = "x-api-key";

    private void EnsureAuthorized(ServerCallContext context)
    {
        var configuredApiKey = options.Value.ApiKey?.Trim();
        if (!string.IsNullOrEmpty(configuredApiKey))
        {
            var provided = context.RequestHeaders?.FirstOrDefault(h => string.Equals(h.Key, ApiKeyHeader, StringComparison.OrdinalIgnoreCase))?.Value;
            if (!string.Equals(configuredApiKey, provided, StringComparison.Ordinal))
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid API key."));
            return;
        }

        if (options.Value.AllowUnauthenticatedLoopback && IsLoopbackPeer(context.Peer))
            return;

        throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthenticated remote access is disabled. Configure Agent:ApiKey."));
    }

    private static bool IsLoopbackPeer(string? peer)
    {
        if (string.IsNullOrWhiteSpace(peer)) return false;
        if (peer.StartsWith("ipv4:", StringComparison.OrdinalIgnoreCase))
        {
            var value = peer[5..];
            var sep = value.LastIndexOf(':');
            var host = sep > 0 ? value[..sep] : value;
            return IPAddress.TryParse(host, out var ip) && IPAddress.IsLoopback(ip);
        }

        if (peer.StartsWith("ipv6:", StringComparison.OrdinalIgnoreCase))
        {
            var value = peer[5..];
            if (value.StartsWith("[", StringComparison.Ordinal))
            {
                var end = value.IndexOf(']');
                if (end > 1)
                {
                    var host = value[1..end];
                    return IPAddress.TryParse(host, out var ip) && IPAddress.IsLoopback(ip);
                }
            }

            var sep = value.LastIndexOf(':');
            var hostRaw = sep > 0 ? value[..sep] : value;
            return IPAddress.TryParse(hostRaw, out var ip2) && IPAddress.IsLoopback(ip2);
        }

        return false;
    }

    private static string SanitizeSessionId(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return Guid.NewGuid().ToString("N")[..8];

        var chars = sessionId.Trim().Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray();
        if (chars.Length == 0)
            return Guid.NewGuid().ToString("N")[..8];

        return new string(chars);
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
                var clean = StripAnsi(line);
                var msg = new ServerMessage { Priority = MessagePriority.Normal };
                if (isError) msg.Error = clean; else msg.Output = clean;
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




