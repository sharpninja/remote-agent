using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using Grpc.Core;
using Microsoft.Extensions.Options;
using RemoteAgent.App.Logic;
using RemoteAgent.Proto;
using RemoteAgent.Service.Agents;
using RemoteAgent.Service.Logging;
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
    MediaStorageService mediaStorage,
    StructuredLogService structuredLogs,
    PluginConfigurationService pluginConfiguration,
    AgentMcpConfigurationService agentMcpConfiguration,
    PromptTemplateService promptTemplateService,
    ConnectionProtectionService connectionProtection,
    SessionCapacityService sessionCapacity,
    AuthUserService authUsers) : AgentGateway.AgentGatewayBase
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
        structuredLogs.Write(
            level: "INFO",
            eventType: "server_info",
            message: "Server info requested",
            component: nameof(AgentGatewayService),
            sessionId: null,
            correlationId: null,
            detailsJson: $"{{\"client_version\":\"{request.ClientVersion}\"}}");
        return Task.FromResult(response);
    }

    /// <summary>Returns structured logs from disk after a cursor offset.</summary>
    public override Task<StructuredLogsSnapshotResponse> GetStructuredLogsSnapshot(StructuredLogsSnapshotRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var rows = structuredLogs.GetSnapshot(request.FromOffset, request.Limit);
        var response = new StructuredLogsSnapshotResponse();
        long nextOffset = request.FromOffset;
        foreach (var row in rows)
        {
            response.Entries.Add(ToProto(row));
            if (row.EventId > nextOffset) nextOffset = row.EventId;
        }

        response.NextOffset = nextOffset;
        return Task.FromResult(response);
    }

    /// <summary>Streams structured logs in real time with initial replay from cursor offset.</summary>
    public override async Task StreamStructuredLogs(StructuredLogsStreamRequest request, IServerStreamWriter<StructuredLogEntry> responseStream, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var replay = structuredLogs.GetSnapshot(request.FromOffset, limit: 0);
        long currentOffset = request.FromOffset;

        foreach (var row in replay)
        {
            await responseStream.WriteAsync(ToProto(row), context.CancellationToken);
            if (row.EventId > currentOffset) currentOffset = row.EventId;
        }

        await foreach (var row in structuredLogs.StreamFromOffset(currentOffset, context.CancellationToken))
        {
            await responseStream.WriteAsync(ToProto(row), context.CancellationToken);
        }
    }

    /// <summary>Returns configured plugin assemblies and currently loaded runner IDs.</summary>
    public override Task<GetPluginsResponse> GetPlugins(GetPluginsRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var response = new GetPluginsResponse();
        response.ConfiguredAssemblies.AddRange(pluginConfiguration.GetAssemblies());
        response.LoadedRunnerIds.AddRange(runnerRegistry.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        structuredLogs.Write(
            level: "INFO",
            eventType: "plugins_listed",
            message: "Plugin configuration listed",
            component: nameof(AgentGatewayService),
            sessionId: null,
            correlationId: null,
            detailsJson: $"{{\"configured_count\":{response.ConfiguredAssemblies.Count},\"loaded_count\":{response.LoadedRunnerIds.Count}}}");
        return Task.FromResult(response);
    }

    /// <summary>Updates configured plugin assemblies. New plugins are loaded after service restart.</summary>
    public override Task<UpdatePluginsResponse> UpdatePlugins(UpdatePluginsRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var current = pluginConfiguration.UpdateAssemblies(request.Assemblies);
        var response = new UpdatePluginsResponse
        {
            Success = true,
            Message = "Plugin assembly configuration updated. Restart service to load new plugins."
        };
        response.ConfiguredAssemblies.AddRange(current);
        structuredLogs.Write(
            level: "INFO",
            eventType: "plugins_updated",
            message: "Plugin assembly configuration updated",
            component: nameof(AgentGatewayService),
            sessionId: null,
            correlationId: null,
            detailsJson: $"{{\"count\":{current.Count}}}");
        return Task.FromResult(response);
    }

    public override Task<SeedSessionContextResponse> SeedSessionContext(SeedSessionContextRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return Task.FromResult(new SeedSessionContextResponse
            {
                Success = false,
                Message = "session_id is required."
            });
        }

        var row = agentMcpConfiguration.AddSeedContext(request.SessionId, request.ContextType, request.Content, request.Source);
        structuredLogs.Write("INFO", "seed_context_added", "Session seed context added", nameof(AgentGatewayService), row.SessionId, request.CorrelationId, null);
        return Task.FromResult(new SeedSessionContextResponse
        {
            Success = true,
            Message = "Seed context added.",
            SeedId = row.SeedId
        });
    }

    public override Task<GetSessionSeedContextResponse> GetSessionSeedContext(GetSessionSeedContextRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var rows = agentMcpConfiguration.GetSeedContext(request.SessionId);
        var response = new GetSessionSeedContextResponse();
        response.Entries.AddRange(rows.Select(ToProto));
        return Task.FromResult(response);
    }

    public override Task<ClearSessionSeedContextResponse> ClearSessionSeedContext(ClearSessionSeedContextRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var removed = agentMcpConfiguration.ClearSeedContext(request.SessionId);
        structuredLogs.Write("INFO", "seed_context_cleared", "Session seed context cleared", nameof(AgentGatewayService), request.SessionId, null, $"{{\"removed\":{removed}}}");
        return Task.FromResult(new ClearSessionSeedContextResponse { RemovedCount = removed });
    }

    public override Task<ListMcpServersResponse> ListMcpServers(ListMcpServersRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var rows = agentMcpConfiguration.ListServers();
        var response = new ListMcpServersResponse();
        response.Servers.AddRange(rows.Select(ToProto));
        return Task.FromResult(response);
    }

    public override Task<UpsertMcpServerResponse> UpsertMcpServer(UpsertMcpServerRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        if (request.Server == null)
        {
            return Task.FromResult(new UpsertMcpServerResponse
            {
                Success = false,
                Message = "server is required."
            });
        }

        var row = agentMcpConfiguration.UpsertServer(new McpServerRecord
        {
            ServerId = request.Server.ServerId,
            DisplayName = request.Server.DisplayName,
            Transport = request.Server.Transport,
            Endpoint = request.Server.Endpoint,
            Command = request.Server.Command,
            Arguments = request.Server.Arguments.ToList(),
            AuthType = request.Server.AuthType,
            AuthConfigJson = request.Server.AuthConfigJson,
            Enabled = request.Server.Enabled,
            MetadataJson = request.Server.MetadataJson
        });

        structuredLogs.Write("INFO", "mcp_server_upserted", "MCP server upserted", nameof(AgentGatewayService), null, null, $"{{\"server_id\":\"{row.ServerId}\"}}");
        return Task.FromResult(new UpsertMcpServerResponse
        {
            Success = true,
            Message = "MCP server saved.",
            Server = ToProto(row)
        });
    }

    public override Task<DeleteMcpServerResponse> DeleteMcpServer(DeleteMcpServerRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var deleted = agentMcpConfiguration.DeleteServer(request.ServerId);
        structuredLogs.Write("INFO", "mcp_server_deleted", "MCP server delete requested", nameof(AgentGatewayService), null, null, $"{{\"server_id\":\"{request.ServerId}\",\"deleted\":{deleted.ToString().ToLowerInvariant()}}}");
        return Task.FromResult(new DeleteMcpServerResponse
        {
            Success = deleted,
            Message = deleted ? "MCP server deleted." : "MCP server not found."
        });
    }

    public override async Task<SetAgentMcpServersResponse> SetAgentMcpServers(SetAgentMcpServersRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var agentId = string.IsNullOrWhiteSpace(request.AgentId) ? "process" : request.AgentId.Trim();
        var previousIds = agentMcpConfiguration.GetAgentServerIds(agentId);
        var ids = agentMcpConfiguration.SetAgentServers(agentId, request.ServerIds);
        var enabled = ids.Except(previousIds, StringComparer.OrdinalIgnoreCase).ToList();
        var disabled = previousIds.Except(ids, StringComparer.OrdinalIgnoreCase).ToList();

        if (enabled.Count > 0 || disabled.Count > 0)
            await NotifyAgentMcpChangeAsync(agentId, enabled, disabled);

        structuredLogs.Write("INFO", "agent_mcp_set", "Agent MCP server mapping updated", nameof(AgentGatewayService), null, null, $"{{\"agent_id\":\"{agentId}\",\"count\":{ids.Count}}}");
        var response = new SetAgentMcpServersResponse
        {
            Success = true,
            Message = "Agent MCP servers updated.",
            AgentId = agentId
        };
        response.ServerIds.AddRange(ids);
        return response;
    }

    public override Task<GetAgentMcpServersResponse> GetAgentMcpServers(GetAgentMcpServersRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var agentId = string.IsNullOrWhiteSpace(request.AgentId) ? "process" : request.AgentId.Trim();
        var ids = agentMcpConfiguration.GetAgentServerIds(agentId);
        var rows = agentMcpConfiguration.GetAgentServers(agentId);
        var response = new GetAgentMcpServersResponse
        {
            AgentId = agentId
        };
        response.ServerIds.AddRange(ids);
        response.Servers.AddRange(rows.Select(ToProto));
        return Task.FromResult(response);
    }

    public override Task<ListPromptTemplatesResponse> ListPromptTemplates(ListPromptTemplatesRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var rows = promptTemplateService.List();
        var response = new ListPromptTemplatesResponse();
        response.Templates.AddRange(rows.Select(ToProto));
        return Task.FromResult(response);
    }

    public override Task<UpsertPromptTemplateResponse> UpsertPromptTemplate(UpsertPromptTemplateRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        if (request.Template == null)
        {
            return Task.FromResult(new UpsertPromptTemplateResponse
            {
                Success = false,
                Message = "template is required."
            });
        }

        var row = promptTemplateService.Upsert(new PromptTemplateRecord
        {
            TemplateId = request.Template.TemplateId,
            DisplayName = request.Template.DisplayName,
            Description = request.Template.Description,
            TemplateContent = request.Template.TemplateContent
        });
        structuredLogs.Write("INFO", "prompt_template_upserted", "Prompt template upserted", nameof(AgentGatewayService), null, null, $"{{\"template_id\":\"{row.TemplateId}\"}}");
        return Task.FromResult(new UpsertPromptTemplateResponse
        {
            Success = true,
            Message = "Prompt template saved.",
            Template = ToProto(row)
        });
    }

    public override Task<DeletePromptTemplateResponse> DeletePromptTemplate(DeletePromptTemplateRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var deleted = promptTemplateService.Delete(request.TemplateId);
        structuredLogs.Write("INFO", "prompt_template_deleted", "Prompt template delete requested", nameof(AgentGatewayService), null, null, $"{{\"template_id\":\"{request.TemplateId}\",\"deleted\":{deleted.ToString().ToLowerInvariant()}}}");
        return Task.FromResult(new DeletePromptTemplateResponse
        {
            Success = deleted,
            Message = deleted ? "Prompt template deleted." : "Prompt template not found."
        });
    }

    /// <summary>Opens a duplex stream: reads ClientMessage (text, control, script, media), spawns agent on START, forwards text to agent stdin, streams stdout/stderr and SessionEvent to the client (FR-1.3, FR-1.4, FR-7.1, TR-4.4).</summary>
    public override async Task Connect(
        IAsyncStreamReader<ClientMessage> requestStream,
        IServerStreamWriter<ServerMessage> responseStream,
        ServerCallContext context)
    {
        EnsureAuthorized(context);
        var connectionDecision = connectionProtection.TryOpenConnection(context.Peer, nameof(AgentGatewayService));
        if (!connectionDecision.Allowed)
            throw new RpcException(new Status(StatusCode.ResourceExhausted, connectionDecision.DeniedReason ?? "Connection rate limit exceeded."));

        var connectionPeer = connectionDecision.Peer;
        // Session id for this stream: use client-provided on START, else generate (TR-12.1, FR-11.1.1).
        var sessionId = Guid.NewGuid().ToString("N")[..8];
        var logPath = Path.Combine(
            options.Value.LogDirectory ?? Path.GetTempPath(),
            $"remote-agent-{sessionId}.log");
        using var logWriter = new StreamWriter(logPath, append: true, Encoding.UTF8) { AutoFlush = true };

        void Log(string line, string level = "INFO", string? correlationId = null, string eventType = "session_log")
        {
            var entry = $"[{DateTime.UtcNow:O}] [{level}] {line}";
            logger.LogInformation("{Entry}", entry);
            try { logWriter.WriteLine(entry); } catch { /* ignore */ }
            structuredLogs.Write(
                level: level,
                eventType: eventType,
                message: line,
                component: nameof(AgentGatewayService),
                sessionId: sessionId,
                correlationId: correlationId,
                detailsJson: null);
        }

        Log(
            $"Connection opened from {connectionPeer}. " +
            $"Session log: {logPath} | " +
            $"Structured log: {structuredLogs.FilePath} | " +
            $"Database: {localStorage.DbPath}",
            eventType: "connection_opened");

        IAgentSession? agentSession = null;
        string activeAgentId = "";
        var stopRequested = false;
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
                    if (!connectionProtection.TryRegisterClientMessage(connectionPeer, nameof(AgentGatewayService)))
                    {
                        structuredLogs.Write("WARN", "client_rate_limited", "Client stream exceeded inbound message rate limit", nameof(AgentGatewayService), sessionId, null, $"{{\"peer\":\"{connectionPeer}\"}}");
                        await responseStream.WriteAsync(new ServerMessage
                        {
                            Priority = MessagePriority.Normal,
                            Event = new SessionEvent
                            {
                                Kind = SessionEvent.Types.Kind.SessionError,
                                Message = "Inbound request rate limit exceeded."
                            }
                        }, context.CancellationToken);
                        break;
                    }

                    string corrId = msg.CorrelationId ?? "";
                    string requestContext = msg.RequestContext?.Trim() ?? "";
                    if (msg.PayloadCase == ClientMessage.PayloadOneofCase.Control)
                    {
                        var control = msg.Control;
                        var action = control.Action;
                        // Use client-provided session_id when present (TR-12.1, FR-11.1.1).
                        if (action == SessionControl.Types.Action.Start && !string.IsNullOrWhiteSpace(control.SessionId))
                            sessionId = SanitizeSessionId(control.SessionId);
                        var resumingSession = action == SessionControl.Types.Action.Start && localStorage.SessionExists(sessionId);
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
                                Log("Agent:Command is set to 'none' (no agent configured)", "WARN", corrId, "session_error");
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
                                activeAgentId = string.IsNullOrWhiteSpace(control.AgentId)
                                    ? (string.IsNullOrWhiteSpace(options.Value.RunnerId) ? "process" : options.Value.RunnerId.Trim())
                                    : control.AgentId.Trim();
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
                                if (!sessionCapacity.TryRegisterSession(activeAgentId, sessionId, agentSession, out var sessionLimitReason))
                                {
                                    try { agentSession.Stop(); agentSession.Dispose(); } catch { }
                                    agentSession = null;
                                    Log(sessionLimitReason, "WARN", corrId, "session_limit_exceeded");
                                    await responseStream.WriteAsync(new ServerMessage
                                    {
                                        Priority = MessagePriority.Normal,
                                        Event = new SessionEvent
                                        {
                                            Kind = SessionEvent.Types.Kind.SessionError,
                                            Message = sessionLimitReason
                                        },
                                        CorrelationId = corrId
                                    }, context.CancellationToken);
                                    localStorage.LogResponse(sessionId, "Event", "SessionError");
                                    continue;
                                }
                                Log("Agent started", "INFO", corrId, "session_started");
                                if (!string.IsNullOrWhiteSpace(requestContext))
                                    await ApplyRequestContextAsync(agentSession, requestContext, corrId, Log, context.CancellationToken);
                                var seedRows = agentMcpConfiguration.ConsumeSeedContext(sessionId);
                                var interactionSession = WrapSession(agentSession);
                                foreach (var seed in seedRows)
                                {
                                    var sent = await AgentInteractionDispatcher.TryIssueSeedContextAsync(interactionSession, seed.ContextType, seed.Content, context.CancellationToken);
                                    if (sent)
                                        structuredLogs.Write("INFO", "seed_context_applied", "Applied seed context to session", nameof(AgentGatewayService), sessionId, corrId, $"{{\"seed_id\":\"{seed.SeedId}\"}}");
                                }
                                await responseStream.WriteAsync(new ServerMessage
                                {
                                    Priority = MessagePriority.Normal,
                                    Event = new SessionEvent
                                    {
                                        Kind = SessionEvent.Types.Kind.SessionStarted,
                                        Message = resumingSession ? "Session resumed." : "Session started."
                                    },
                                    CorrelationId = corrId
                                }, context.CancellationToken);
                                if (resumingSession)
                                    structuredLogs.Write("INFO", "session_resumed", "Existing session resumed after reconnect", nameof(AgentGatewayService), sessionId, corrId, null);
                                localStorage.LogResponse(sessionId, "Event", "SessionStarted");
                            }
                            catch (Exception ex)
                            {
                                Log($"Failed to start agent: {ex.Message}", "ERROR", corrId, "session_error");
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
                            stopRequested = true;
                            if (agentSession != null)
                            {
                                agentSession.Stop();
                                try { agentSession.Dispose(); } catch { }
                                sessionCapacity.UnregisterSession(activeAgentId, sessionId);
                                agentSession = null;
                                Log("Agent stopped", "INFO", corrId, "session_stopped");
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
                        if (!string.IsNullOrWhiteSpace(requestContext))
                            await ApplyRequestContextAsync(agentSession, requestContext, corrId, Log, context.CancellationToken);
                        outputCorrelationId[0] = corrId;
                        localStorage.LogRequest(sessionId, "Text", msg.Text);
                        Log($"→ {msg.Text}", "INFO", corrId, "client_text");
                        if (agentSession != null && !agentSession.HasExited)
                        {
                            try
                            {
                                await agentSession.SendInputAsync(msg.Text, context.CancellationToken);
                            }
                            catch (Exception ex)
                            {
                                Log($"Write to agent failed: {ex.Message}", "ERROR", corrId, "agent_write_error");
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
                        if (!string.IsNullOrWhiteSpace(requestContext))
                            await ApplyRequestContextAsync(agentSession, requestContext, corrId, Log, context.CancellationToken);
                        var req = msg.ScriptRequest;
                        var pathOrCommand = req.PathOrCommand ?? "";
                        var scriptType = req.ScriptType == ScriptType.Unspecified ? ScriptType.Bash : req.ScriptType;
                        localStorage.LogRequest(sessionId, "ScriptRequest", $"{scriptType}: {pathOrCommand}");
                        Log($"Script run: {scriptType} {pathOrCommand}", "INFO", corrId, "script_request");
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
                            Log($"Script failed: {ex.Message}", "ERROR", corrId, "script_error");
                            await responseStream.WriteAsync(new ServerMessage { Priority = MessagePriority.Normal, Error = ex.Message, CorrelationId = corrId }, context.CancellationToken);
                            localStorage.LogResponse(sessionId, "Error", ex.Message);
                        }
                    }
                    else if (msg.PayloadCase == ClientMessage.PayloadOneofCase.MediaUpload && msg.MediaUpload != null)
                    {
                        if (!string.IsNullOrWhiteSpace(requestContext))
                            await ApplyRequestContextAsync(agentSession, requestContext, corrId, Log, context.CancellationToken);
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
                            Log($"Media saved: {relativePath}", "INFO", corrId, "media_saved");
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
                            Log($"Media save failed: {ex.Message}", "ERROR", corrId, "media_error");
                            await responseStream.WriteAsync(new ServerMessage { Priority = MessagePriority.Normal, Error = ex.Message, CorrelationId = corrId }, context.CancellationToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log($"Request stream error: {ex.Message}", "ERROR", null, "request_stream_error");
            }
            finally
            {
                connectionProtection.CloseConnection(connectionPeer);
                if (agentSession != null)
                {
                    try { agentSession.Stop(); agentSession.Dispose(); } catch { }
                    sessionCapacity.UnregisterSession(activeAgentId, sessionId);
                    if (!stopRequested)
                    {
                        sessionCapacity.MarkSessionAbandoned(sessionId, activeAgentId, "Streaming connection ended without STOP.");
                        structuredLogs.Write("WARN", "session_abandoned", "Session marked abandoned due to unexpected disconnect", nameof(AgentGatewayService), sessionId, null, $"{{\"agent_id\":\"{activeAgentId}\"}}");
                    }
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
            var stdoutTask = StreamReaderToResponse(agentSession.StandardOutput, responseStream, false, context.CancellationToken, Log, "stdout", () => outputCorrelationId[0]);
            var stderrTask = StreamReaderToResponse(agentSession.StandardError, responseStream, true, context.CancellationToken, Log, "stderr", () => outputCorrelationId[0]);
            await Task.WhenAll(stdoutTask, stderrTask);
        }

        await requestTask;
        Log($"Session {sessionId} ended. Log: {logPath}", "INFO", null, "session_end");
    }

    private const string ApiKeyHeader = "x-api-key";

    private void EnsureAuthorized(ServerCallContext context)
    {
        if (options.Value.AllowUnauthenticatedLoopback && IsLoopbackPeer(context.Peer))
            return;

        if (options.Value.AllowUnauthenticatedRemote)
            return;

        var configuredApiKey = options.Value.ApiKey?.Trim();
        if (!string.IsNullOrEmpty(configuredApiKey))
        {
            var provided = context.RequestHeaders?.FirstOrDefault(h => string.Equals(h.Key, ApiKeyHeader, StringComparison.OrdinalIgnoreCase))?.Value;
            if (!string.Equals(configuredApiKey, provided, StringComparison.Ordinal))
            {
                structuredLogs.Write("WARN", "auth_failed", "Invalid API key", nameof(AgentGatewayService), null, null, "{\"reason\":\"invalid_api_key\"}");
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid API key."));
            }
            return;
        }

        structuredLogs.Write("WARN", "auth_failed", "Unauthenticated remote access blocked", nameof(AgentGatewayService), null, null, "{\"reason\":\"loopback_required\"}");
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
        Action<string, string, string?, string> log,
        string streamName,
        Func<string?> getCorrelationId)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                var correlationId = getCorrelationId();
                log($"[{streamName}] {line}", isError ? "STDERR" : "INFO", correlationId, isError ? "agent_stderr" : "agent_stdout");
                var clean = StripAnsi(line);
                var msg = new ServerMessage { Priority = MessagePriority.Normal };
                if (isError) msg.Error = clean; else msg.Output = clean;
                if (!string.IsNullOrWhiteSpace(correlationId)) msg.CorrelationId = correlationId;
                await responseStream.WriteAsync(msg, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            log($"Stream {streamName} error: {ex.Message}", "ERROR", null, "agent_stream_error");
        }
    }

    private static StructuredLogEntry ToProto(StructuredLogEntryRecord row)
    {
        return new StructuredLogEntry
        {
            EventId = row.EventId,
            TimestampUtc = row.TimestampUtc.ToString("O"),
            Level = row.Level ?? "",
            EventType = row.EventType ?? "",
            Message = row.Message ?? "",
            Component = row.Component ?? "",
            SessionId = row.SessionId ?? "",
            CorrelationId = row.CorrelationId ?? "",
            DetailsJson = row.DetailsJson ?? ""
        };
    }

    private static SessionSeedContextEntry ToProto(SeedContextRecord row)
    {
        return new SessionSeedContextEntry
        {
            SeedId = row.SeedId ?? "",
            SessionId = row.SessionId ?? "",
            ContextType = row.ContextType ?? "",
            Content = row.Content ?? "",
            Source = row.Source ?? "",
            CreatedUtc = row.CreatedUtc.ToString("O")
        };
    }

    private static McpServerDefinition ToProto(McpServerRecord row)
    {
        var value = new McpServerDefinition
        {
            ServerId = row.ServerId ?? "",
            DisplayName = row.DisplayName ?? "",
            Transport = row.Transport ?? "",
            Endpoint = row.Endpoint ?? "",
            Command = row.Command ?? "",
            AuthType = row.AuthType ?? "",
            AuthConfigJson = row.AuthConfigJson ?? "",
            Enabled = row.Enabled,
            MetadataJson = row.MetadataJson ?? "",
            CreatedUtc = row.CreatedUtc.ToString("O"),
            UpdatedUtc = row.UpdatedUtc.ToString("O")
        };
        value.Arguments.AddRange(row.Arguments ?? []);
        return value;
    }

    private static PromptTemplateDefinition ToProto(PromptTemplateRecord row)
    {
        return new PromptTemplateDefinition
        {
            TemplateId = row.TemplateId ?? "",
            DisplayName = row.DisplayName ?? "",
            Description = row.Description ?? "",
            TemplateContent = row.TemplateContent ?? "",
            CreatedUtc = row.CreatedUtc.ToString("O"),
            UpdatedUtc = row.UpdatedUtc.ToString("O")
        };
    }

    private static async Task ApplyRequestContextAsync(
        IAgentSession? agentSession,
        string requestContext,
        string correlationId,
        Action<string, string, string?, string> log,
        CancellationToken ct)
    {
        var sent = await AgentInteractionDispatcher.TryIssueRequestContextAsync(WrapSession(agentSession), requestContext, ct);
        if (sent)
            log("Per-request context issued", "INFO", correlationId, "request_context_applied");
    }

    private async Task NotifyAgentMcpChangeAsync(string agentId, IReadOnlyList<string> enabled, IReadOnlyList<string> disabled)
    {
        var bySession = sessionCapacity.GetActiveSessionsForAgent(agentId);
        if (bySession.Count == 0)
            return;

        foreach (var kvp in bySession)
        {
            try
            {
                var sent = await AgentInteractionDispatcher.TryNotifyMcpUpdateAsync(WrapSession(kvp.Value), enabled, disabled, CancellationToken.None);
                if (sent)
                    structuredLogs.Write("INFO", "agent_mcp_notified", "Sent MCP mapping update to active session", nameof(AgentGatewayService), kvp.Key, null, $"{{\"agent_id\":\"{agentId}\"}}");
            }
            catch (Exception ex)
            {
                structuredLogs.Write("WARN", "agent_mcp_notify_failed", $"Failed to notify active session: {ex.Message}", nameof(AgentGatewayService), kvp.Key, null, $"{{\"agent_id\":\"{agentId}\"}}");
            }
        }
    }

    private static IAgentInteractionSession? WrapSession(IAgentSession? session)
    {
        return session == null ? null : new AgentSessionAdapter(session);
    }

    private sealed class AgentSessionAdapter(IAgentSession session) : IAgentInteractionSession
    {
        public bool CanAcceptInput => !session.HasExited;

        public Task SendInputAsync(string input, CancellationToken cancellationToken = default)
            => session.SendInputAsync(input, cancellationToken);
    }

    public override Task<CheckSessionCapacityResponse> CheckSessionCapacity(CheckSessionCapacityRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var status = sessionCapacity.GetStatus(request.AgentId);
        var response = new CheckSessionCapacityResponse
        {
            CanCreateSession = status.CanCreateSession,
            Reason = status.Reason,
            MaxConcurrentSessions = status.MaxConcurrentSessions,
            ActiveSessionCount = status.ActiveSessionCount,
            RemainingServerCapacity = status.RemainingServerCapacity,
            AgentId = status.AgentId,
            HasAgentLimit = status.AgentMaxConcurrentSessions.HasValue,
            AgentMaxConcurrentSessions = status.AgentMaxConcurrentSessions ?? 0,
            AgentActiveSessionCount = status.AgentActiveSessionCount,
            RemainingAgentCapacity = status.RemainingAgentCapacity ?? 0
        };
        return Task.FromResult(response);
    }

    public override Task<ListOpenSessionsResponse> ListOpenSessions(ListOpenSessionsRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var response = new ListOpenSessionsResponse();
        foreach (var s in sessionCapacity.ListOpenSessions())
            response.Sessions.Add(new OpenSessionEntry { SessionId = s.SessionId, AgentId = s.AgentId, CanAcceptInput = s.CanAcceptInput });
        return Task.FromResult(response);
    }

    public override Task<ListAbandonedSessionsResponse> ListAbandonedSessions(ListAbandonedSessionsRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var response = new ListAbandonedSessionsResponse();
        foreach (var s in sessionCapacity.ListAbandonedSessions())
            response.Sessions.Add(new AbandonedSessionEntry { SessionId = s.SessionId, AgentId = s.AgentId, Reason = s.Reason, AbandonedUtc = s.AbandonedUtc.ToString("O") });
        return Task.FromResult(response);
    }

    public override Task<TerminateSessionResponse> TerminateSession(TerminateSessionRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var success = sessionCapacity.TryTerminateSession(request.SessionId, out var reason);
        return Task.FromResult(new TerminateSessionResponse { Success = success, Message = success ? "Session terminated." : reason });
    }

    public override Task<ListConnectedPeersResponse> ListConnectedPeers(ListConnectedPeersRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var response = new ListConnectedPeersResponse();
        foreach (var p in connectionProtection.GetConnectedPeers())
            response.Peers.Add(new ConnectedPeerEntry
            {
                Peer = p.Peer,
                ActiveConnections = p.ActiveConnections,
                IsBlocked = p.IsBlocked,
                BlockedUntilUtc = p.BlockedUntilUtc?.ToString("O") ?? "",
                LastSeenUtc = p.LastSeenUtc.ToString("O")
            });
        return Task.FromResult(response);
    }

    public override Task<ListConnectionHistoryResponse> ListConnectionHistory(ListConnectionHistoryRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var limit = request.Limit > 0 ? request.Limit : 500;
        var response = new ListConnectionHistoryResponse();
        foreach (var e in connectionProtection.GetConnectionHistory(limit))
            response.Entries.Add(new global::RemoteAgent.Proto.ConnectionHistoryEntry
            {
                TimestampUtc = e.TimestampUtc.ToString("O"),
                Peer = e.Peer,
                Action = e.Action,
                Allowed = e.Allowed,
                Detail = e.Detail ?? ""
            });
        return Task.FromResult(response);
    }

    public override Task<ListBannedPeersResponse> ListBannedPeers(ListBannedPeersRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var response = new ListBannedPeersResponse();
        foreach (var b in connectionProtection.GetBannedPeers())
            response.Peers.Add(new global::RemoteAgent.Proto.BannedPeerEntry { Peer = b.Peer, Reason = b.Reason, BannedUtc = b.BannedUtc.ToString("O") });
        return Task.FromResult(response);
    }

    public override Task<BanPeerResponse> BanPeer(BanPeerRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var ok = connectionProtection.BanPeer(request.Peer, request.Reason, nameof(AgentGatewayService));
        return Task.FromResult(new BanPeerResponse { Success = ok, Message = ok ? "Peer banned." : "Invalid peer." });
    }

    public override Task<UnbanPeerResponse> UnbanPeer(UnbanPeerRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var ok = connectionProtection.UnbanPeer(request.Peer, nameof(AgentGatewayService));
        return Task.FromResult(new UnbanPeerResponse { Success = ok, Message = ok ? "Peer unbanned." : "Peer not found." });
    }

    public override Task<ListAuthUsersResponse> ListAuthUsers(ListAuthUsersRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var response = new ListAuthUsersResponse();
        foreach (var u in authUsers.List())
            response.Users.Add(new AuthUserEntry { UserId = u.UserId, DisplayName = u.DisplayName, Role = u.Role, Enabled = u.Enabled, CreatedUtc = u.CreatedUtc.ToString("O"), UpdatedUtc = u.UpdatedUtc.ToString("O") });
        return Task.FromResult(response);
    }

    public override Task<ListPermissionRolesResponse> ListPermissionRoles(ListPermissionRolesRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var response = new ListPermissionRolesResponse();
        response.Roles.AddRange(authUsers.ListRoles());
        return Task.FromResult(response);
    }

    public override Task<UpsertAuthUserResponse> UpsertAuthUser(UpsertAuthUserRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var entry = request.User ?? new AuthUserEntry();
        var record = new AuthUserRecord { UserId = entry.UserId, DisplayName = entry.DisplayName, Role = entry.Role, Enabled = entry.Enabled };
        var saved = authUsers.Upsert(record);
        var resultEntry = new AuthUserEntry { UserId = saved.UserId, DisplayName = saved.DisplayName, Role = saved.Role, Enabled = saved.Enabled, CreatedUtc = saved.CreatedUtc.ToString("O"), UpdatedUtc = saved.UpdatedUtc.ToString("O") };
        return Task.FromResult(new UpsertAuthUserResponse { Success = true, Message = "Auth user saved.", User = resultEntry });
    }

    public override Task<DeleteAuthUserResponse> DeleteAuthUser(DeleteAuthUserRequest request, ServerCallContext context)
    {
        EnsureAuthorized(context);
        var ok = authUsers.Delete(request.UserId);
        return Task.FromResult(new DeleteAuthUserResponse { Success = ok, Message = ok ? "Auth user deleted." : "Auth user not found." });
    }
}




