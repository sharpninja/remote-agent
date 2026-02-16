using System.Collections.ObjectModel;
using Grpc.Core;
using Grpc.Net.Client;
using RemoteAgent.Proto;

namespace RemoteAgent.App.Services;

/// <summary>Client for the AgentGateway gRPC service (FR-1.1, FR-2.1, FR-2.2, FR-2.4, TR-2.3, TR-5.2). Connects to the Linux service, sends text/control/script/media, and receives streamed output and events into <see cref="Messages"/>.</summary>
/// <remarks>Bind <see cref="Messages"/> to the chat UI (TR-5.1). Call <see cref="ConnectAsync"/> with host/port (e.g. 10.0.2.2:5243 for emulator). Optional <see cref="ILocalMessageStore"/> persists messages (TR-11.1). Notify-priority messages trigger <see cref="MessageReceived"/> so the app can show a notification (FR-3.2, FR-3.3).</remarks>
/// <example><code>
/// var client = new AgentGatewayClientService(store);
/// client.LoadFromStore();
/// await client.ConnectAsync("10.0.2.2", 5243, ct: ct);
/// await client.SendTextAsync("Hello", ct);
/// await client.StopSessionAsync(ct);
/// client.Disconnect();
/// </code></example>
/// <see href="https://sharpninja.github.io/remote-agent/functional-requirements.html">Functional requirements</see>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-2, TR-5)</see>
public class AgentGatewayClientService
{
    private readonly ILocalMessageStore? _store;
    private GrpcChannel? _channel;
    private AgentGateway.AgentGatewayClient? _client;
    private AsyncDuplexStreamingCall<ClientMessage, ServerMessage>? _call;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;

    /// <summary>Creates the client. Pass an <see cref="ILocalMessageStore"/> to persist messages (TR-11.1).</summary>
    public AgentGatewayClientService(ILocalMessageStore? store = null) => _store = store;

    /// <summary>Observable collection of chat messages (TR-5.1). Bind to the chat list.</summary>
    public ObservableCollection<ChatMessage> Messages { get; } = new();

    /// <summary>True when connected (duplex stream active).</summary>
    public bool IsConnected => _call != null;

    /// <summary>Last server info from GetServerInfo (version, capabilities, available agents). Set when connecting (TR-12.1.2).</summary>
    public ServerInfoResponse? ServerInfo { get; private set; }

    /// <summary>Current session id for this connection (FR-11.1, TR-12.1). Set by <see cref="ConnectAsync"/>; used when persisting messages.</summary>
    public string? CurrentSessionId { get; private set; }

    /// <summary>Raised when <see cref="IsConnected"/> changes (after Connect or Disconnect).</summary>
    public event Action? ConnectionStateChanged;

    /// <summary>Raised for each received message (e.g. to show a notification for Notify priority) (FR-3.2, FR-3.3).</summary>
    public event Action<ChatMessage>? MessageReceived;

    /// <summary>Loads messages from <see cref="ILocalMessageStore"/> into <see cref="Messages"/> for the given session (TR-11.1, TR-12.1.3). Call when switching session or on start.</summary>
    /// <param name="sessionId">Session to load; if null, loads all messages (backward compatible).</param>
    public void LoadFromStore(string? sessionId = null)
    {
        if (_store == null) return;
        Messages.Clear();
        foreach (var msg in _store.Load(sessionId))
            Messages.Add(msg);
    }

    /// <summary>Adds a user message to <see cref="Messages"/> and persists it if a store is configured (TR-11.1). Uses <see cref="CurrentSessionId"/> when connected.</summary>
    public void AddUserMessage(ChatMessage message)
    {
        Messages.Add(message);
        _store?.Add(message, CurrentSessionId);
    }

    /// <summary>Updates the archived state of a message in the store (FR-4.1, TR-5.5). Call when the user swipes to archive.</summary>
    public void SetArchived(ChatMessage message, bool archived)
    {
        if (message.Id is { } id)
            _store?.SetArchived(id, archived);
    }

    /// <summary>Gets server info (including available agents) without opening a stream (TR-12.1.2). Use before showing agent picker, then call <see cref="ConnectAsync"/> with chosen session_id and agent_id.</summary>
    public static async Task<ServerInfoResponse?> GetServerInfoAsync(string host, int port, string? clientVersion = null, string? apiKey = null, CancellationToken ct = default)
    {
        var baseUrl = port == 443 ? $"https://{host}" : $"http://{host}:{port}";
        GrpcChannel? channel = null;
        try
        {
            channel = GrpcChannel.ForAddress(baseUrl);
            var client = new AgentGateway.AgentGatewayClient(channel);
            var headers = CreateHeaders(apiKey);
            return await client.GetServerInfoAsync(new ServerInfoRequest { ClientVersion = clientVersion ?? "" }, headers, deadline: null, cancellationToken: ct);
        }
        catch
        {
            return null;
        }
        finally
        {
            channel?.Dispose();
        }
    }

    /// <summary>Connects to the service at host:port (FR-2.4, TR-5.2, FR-11.1), calls GetServerInfo, opens the duplex stream, and sends START with session_id and agent_id (TR-12.1, TR-12.2).</summary>
    /// <param name="host">Host name or IP.</param>
    /// <param name="port">Port (e.g. 5243). Use 443 for TLS.</param>
    /// <param name="sessionId">Client-provided session id (FR-11.1). If null, a new guid-based id is used.</param>
    /// <param name="agentId">Optional agent runner id from server list (FR-11.1.2). If null, server uses default.</param>
    /// <param name="clientVersion">Optional app version for ServerInfoRequest.</param>
    /// <param name="apiKey">Optional API key sent as gRPC metadata header <c>x-api-key</c>.</param>
    /// <param name="ct">Cancellation.</param>
    public async Task ConnectAsync(string host, int port, string? sessionId = null, string? agentId = null, string? clientVersion = null, string? apiKey = null, CancellationToken ct = default)
    {
        Disconnect();
        ServerInfo = null;
        CurrentSessionId = null;
        var sid = sessionId ?? Guid.NewGuid().ToString("N")[..12];
        CurrentSessionId = sid;
        var baseUrl = port == 443 ? $"https://{host}" : $"http://{host}:{port}";
        _channel = GrpcChannel.ForAddress(baseUrl);
        _client = new AgentGateway.AgentGatewayClient(_channel);
        try
        {
            var headers = CreateHeaders(apiKey);
            ServerInfo = await _client.GetServerInfoAsync(new ServerInfoRequest { ClientVersion = clientVersion ?? "" }, headers, deadline: null, cancellationToken: ct);
        }
        catch
        {
            // proceed without server info (e.g. older server)
        }
        _cts = new CancellationTokenSource();
        _call = _client.Connect(headers: CreateHeaders(apiKey), cancellationToken: _cts.Token);
        _receiveTask = ReceiveLoop(_cts.Token);
        ConnectionStateChanged?.Invoke();
        await SendControlAsync(SessionControl.Types.Action.Start, sid, agentId, ct);
    }

    /// <summary>Closes the stream and channel (FR-2.4). Stops the session on the server.</summary>
    public void Disconnect()
    {
        try
        {
            _cts?.Cancel();
            _call?.Dispose();
            _channel?.Dispose();
        }
        catch { /* ignore */ }
        _call = null;
        _channel = null;
        _client = null;
        _cts = null;
        _receiveTask = null;
        ServerInfo = null;
        CurrentSessionId = null;
        ConnectionStateChanged?.Invoke();
    }

    /// <summary>Sends a text message to the agent (FR-2.1). Forwards to agent stdin on the server. Sets correlation ID for response matching (TR-4.5).</summary>
    public async Task SendTextAsync(string text, CancellationToken ct = default)
    {
        if (_call?.RequestStream == null) return;
        await _call.RequestStream.WriteAsync(new ClientMessage { Text = text, CorrelationId = Guid.NewGuid().ToString("N") }, ct);
    }

    /// <summary>Sends a script run request (FR-9.1, FR-9.2). Server runs the script and returns stdout/stderr as chat messages on completion (TR-4.5 correlation ID).</summary>
    /// <param name="pathOrCommand">Path to script file or command string.</param>
    /// <param name="scriptType">Bash or Pwsh.</param>
    /// <param name="ct">Cancellation.</param>
    public async Task SendScriptRequestAsync(string pathOrCommand, ScriptType scriptType, CancellationToken ct = default)
    {
        if (_call?.RequestStream == null) return;
        await _call.RequestStream.WriteAsync(new ClientMessage
        {
            ScriptRequest = new ScriptRequest { PathOrCommand = pathOrCommand, ScriptType = scriptType },
            CorrelationId = Guid.NewGuid().ToString("N")
        }, ct);
    }

    /// <summary>Sends image or video as agent context (FR-10.1). Server stores under data/media and can pass path to the agent (TR-4.5 correlation ID).</summary>
    /// <param name="content">Raw file bytes.</param>
    /// <param name="contentType">MIME type (e.g. image/jpeg, video/mp4).</param>
    /// <param name="fileName">Optional original filename.</param>
    /// <param name="ct">Cancellation.</param>
    public async Task SendMediaAsync(byte[] content, string contentType, string? fileName, CancellationToken ct = default)
    {
        if (_call?.RequestStream == null) return;
        await _call.RequestStream.WriteAsync(new ClientMessage
        {
            MediaUpload = new MediaUpload
            {
                Content = Google.Protobuf.ByteString.CopyFrom(content),
                ContentType = contentType ?? "application/octet-stream",
                FileName = fileName ?? ""
            },
            CorrelationId = Guid.NewGuid().ToString("N")
        }, ct);
    }

    /// <summary>Sends STOP control to end the session and stop the agent (FR-2.4, FR-7.1).</summary>
    public async Task StopSessionAsync(CancellationToken ct = default)
    {
        await SendControlAsync(SessionControl.Types.Action.Stop, CurrentSessionId, null, ct);
    }

    private async Task SendControlAsync(SessionControl.Types.Action action, string? sessionId = null, string? agentId = null, CancellationToken ct = default)
    {
        if (_call?.RequestStream == null) return;
        var control = new SessionControl { Action = action };
        if (!string.IsNullOrEmpty(sessionId)) control.SessionId = sessionId;
        if (!string.IsNullOrEmpty(agentId)) control.AgentId = agentId;
        await _call.RequestStream.WriteAsync(new ClientMessage { Control = control, CorrelationId = Guid.NewGuid().ToString("N") }, ct);
    }

    private static Metadata? CreateHeaders(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return null;
        var headers = new Metadata { { "x-api-key", apiKey.Trim() } };
        return headers;
    }
    private async Task ReceiveLoop(CancellationToken ct)
    {
        if (_call?.ResponseStream == null) return;
        try
        {
            while (await _call.ResponseStream.MoveNext(ct))
            {
                var msg = _call.ResponseStream.Current;
                var priority = MapPriority(msg.Priority);
                ChatMessage? chat = null;
                if (msg.PayloadCase == ServerMessage.PayloadOneofCase.Output && !string.IsNullOrEmpty(msg.Output))
                    chat = new ChatMessage { IsUser = false, Text = msg.Output, Priority = priority };
                else if (msg.PayloadCase == ServerMessage.PayloadOneofCase.Error && !string.IsNullOrEmpty(msg.Error))
                    chat = new ChatMessage { IsUser = false, Text = msg.Error, IsError = true, Priority = priority };
                else if (msg.PayloadCase == ServerMessage.PayloadOneofCase.Event && msg.Event != null)
                    chat = new ChatMessage { IsEvent = true, EventMessage = msg.Event.Message ?? msg.Event.Kind.ToString(), Priority = priority };
                else if (msg.PayloadCase == ServerMessage.PayloadOneofCase.Media && msg.Media != null)
                {
                    var saved = SaveReceivedMedia(msg.Media);
                    chat = new ChatMessage { IsUser = false, Text = saved, Priority = priority };
                }
                if (chat != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Messages.Add(chat);
                        _store?.Add(chat, CurrentSessionId);
                        MessageReceived?.Invoke(chat);
                    });
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            MainThread.BeginInvokeOnMainThread(Disconnect);
        }
    }

    private static ChatMessagePriority MapPriority(MessagePriority p)
    {
        return p switch
        {
            MessagePriority.High => ChatMessagePriority.High,
            MessagePriority.Notify => ChatMessagePriority.Notify,
            _ => ChatMessagePriority.Normal,
        };
    }

    /// <summary>Saves received media to DCIM/Remote Agent (TR-11.3). Returns display text for the chat bubble.</summary>
    private static string SaveReceivedMedia(MediaChunk media)
    {
        try
        {
            var path = MediaSaveService.SaveToDcimRemoteAgent(media.Content.ToByteArray(), media.ContentType, media.FileName);
            return string.IsNullOrEmpty(path) ? "Received media." : $"Saved: {path}";
        }
        catch (Exception ex)
        {
            return $"Save failed: {ex.Message}";
        }
    }
}




