using Grpc.Core;
using Grpc.Net.Client;
using RemoteAgent.App.Services;
using RemoteAgent.Proto;

namespace RemoteAgent.App.Logic;

public interface IAgentSessionClient : IAgentInteractionSession
{
    bool IsConnected { get; }
    string? CurrentSessionId { get; }
    ServerInfoResponse? ServerInfo { get; }
    string? PerRequestContext { get; set; }
    event Action? ConnectionStateChanged;
    event Action<ChatMessage>? MessageReceived;
    event Action<FileTransfer>? FileTransferReceived;

    Task ConnectAsync(
        string host,
        int port,
        string? sessionId = null,
        string? agentId = null,
        string? clientVersion = null,
        string? apiKey = null,
        CancellationToken ct = default);

    void Disconnect();
    Task SendTextAsync(string text, CancellationToken ct = default);
    Task SendScriptRequestAsync(string pathOrCommand, ScriptType scriptType, CancellationToken ct = default);
    Task SendMediaAsync(byte[] content, string contentType, string? fileName, CancellationToken ct = default);
    Task StopSessionAsync(CancellationToken ct = default);
}

/// <summary>Shared streaming session client used by desktop and mobile chat surfaces.</summary>
public sealed class AgentSessionClient(Func<MediaChunk, string>? mediaTextFormatter = null) : IAgentSessionClient
{
    private readonly Func<MediaChunk, string> _mediaTextFormatter = mediaTextFormatter ?? (_ => "Received media.");
    private GrpcChannel? _channel;
    private AgentGateway.AgentGatewayClient? _client;
    private AsyncDuplexStreamingCall<ClientMessage, ServerMessage>? _call;
    private CancellationTokenSource? _cts;

    public bool IsConnected => _call != null;
    public bool CanAcceptInput => IsConnected;
    public string? CurrentSessionId { get; private set; }
    public ServerInfoResponse? ServerInfo { get; private set; }
    public string? PerRequestContext { get; set; }

    public event Action? ConnectionStateChanged;
    public event Action<ChatMessage>? MessageReceived;
    public event Action<FileTransfer>? FileTransferReceived;

    public async Task ConnectAsync(
        string host,
        int port,
        string? sessionId = null,
        string? agentId = null,
        string? clientVersion = null,
        string? apiKey = null,
        CancellationToken ct = default)
    {
        Disconnect();

        var sid = string.IsNullOrWhiteSpace(sessionId)
            ? Guid.NewGuid().ToString("N")[..12]
            : sessionId.Trim();
        CurrentSessionId = sid;
        ServerInfo = null;

        var baseUrl = ServerApiClient.BuildBaseUrl(host, port);
        _channel = GrpcChannel.ForAddress(baseUrl);
        _client = new AgentGateway.AgentGatewayClient(_channel);

        try
        {
            ServerInfo = await _client.GetServerInfoAsync(
                new ServerInfoRequest { ClientVersion = clientVersion ?? "" },
                ServerApiClient.CreateHeaders(apiKey),
                deadline: null,
                cancellationToken: ct);
        }
        catch
        {
            // Ignore compatibility/readiness failures and continue opening the session stream.
        }

        _cts = new CancellationTokenSource();
        _call = _client.Connect(headers: ServerApiClient.CreateHeaders(apiKey), cancellationToken: _cts.Token);
        ConnectionStateChanged?.Invoke();

        _ = Task.Run(() => ReceiveLoop(_cts.Token));
        await SendControlAsync(SessionControl.Types.Action.Start, sid, agentId, ct);
    }

    public void Disconnect()
    {
        try
        {
            _cts?.Cancel();
            _call?.Dispose();
            _channel?.Dispose();
        }
        catch
        {
            // ignore cleanup exceptions
        }

        _call = null;
        _channel = null;
        _client = null;
        _cts = null;
        CurrentSessionId = null;
        ServerInfo = null;
        ConnectionStateChanged?.Invoke();
    }

    public Task SendInputAsync(string input, CancellationToken cancellationToken = default)
    {
        return SendTextAsync(input, cancellationToken);
    }

    public async Task SendTextAsync(string text, CancellationToken ct = default)
    {
        if (_call?.RequestStream == null || string.IsNullOrWhiteSpace(text))
            return;

        await _call.RequestStream.WriteAsync(
            new ClientMessage
            {
                Text = text,
                CorrelationId = Guid.NewGuid().ToString("N"),
                RequestContext = PerRequestContext ?? ""
            },
            ct);
    }

    public async Task SendScriptRequestAsync(string pathOrCommand, ScriptType scriptType, CancellationToken ct = default)
    {
        if (_call?.RequestStream == null || string.IsNullOrWhiteSpace(pathOrCommand))
            return;

        await _call.RequestStream.WriteAsync(
            new ClientMessage
            {
                ScriptRequest = new ScriptRequest
                {
                    PathOrCommand = pathOrCommand,
                    ScriptType = scriptType
                },
                CorrelationId = Guid.NewGuid().ToString("N"),
                RequestContext = PerRequestContext ?? ""
            },
            ct);
    }

    public async Task SendMediaAsync(byte[] content, string contentType, string? fileName, CancellationToken ct = default)
    {
        if (_call?.RequestStream == null || content.Length == 0)
            return;

        await _call.RequestStream.WriteAsync(
            new ClientMessage
            {
                MediaUpload = new MediaUpload
                {
                    Content = Google.Protobuf.ByteString.CopyFrom(content),
                    ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                    FileName = fileName ?? ""
                },
                CorrelationId = Guid.NewGuid().ToString("N"),
                RequestContext = PerRequestContext ?? ""
            },
            ct);
    }

    public Task StopSessionAsync(CancellationToken ct = default)
    {
        return SendControlAsync(SessionControl.Types.Action.Stop, CurrentSessionId, null, ct);
    }

    private async Task SendControlAsync(SessionControl.Types.Action action, string? sessionId, string? agentId, CancellationToken ct)
    {
        if (_call?.RequestStream == null)
            return;

        var control = new SessionControl { Action = action };
        if (!string.IsNullOrWhiteSpace(sessionId))
            control.SessionId = sessionId;
        if (!string.IsNullOrWhiteSpace(agentId))
            control.AgentId = agentId;

        await _call.RequestStream.WriteAsync(
            new ClientMessage
            {
                Control = control,
                CorrelationId = Guid.NewGuid().ToString("N"),
                RequestContext = PerRequestContext ?? ""
            },
            ct);
    }

    private async Task ReceiveLoop(CancellationToken ct)
    {
        if (_call?.ResponseStream == null)
            return;

        try
        {
            while (await _call.ResponseStream.MoveNext(ct))
            {
                var incoming = _call.ResponseStream.Current;

                // Handle file transfers separately: raise dedicated event + chat notification.
                if (incoming.PayloadCase == ServerMessage.PayloadOneofCase.FileTransfer && incoming.FileTransfer != null)
                {
                    var ft = incoming.FileTransfer;
                    FileTransferReceived?.Invoke(ft);
                    var sizeText = ft.TotalSize < 1024 ? $"{ft.TotalSize} B" : $"{ft.TotalSize / 1024.0:F1} KB";
                    var chatMsg = new ChatMessage
                    {
                        IsUser = false,
                        Text = $"\U0001F4C1 File received: {ft.RelativePath} ({sizeText})",
                        FileTransferPath = ft.RelativePath
                    };
                    MessageReceived?.Invoke(chatMsg);
                    continue;
                }

                var mapped = MapServerMessage(incoming);
                if (mapped != null)
                    MessageReceived?.Invoke(mapped);
            }
        }
        catch (OperationCanceledException)
        {
            // expected during disconnect
        }
        catch
        {
            Disconnect();
        }
    }

    private ChatMessage? MapServerMessage(ServerMessage message)
    {
        var priority = message.Priority switch
        {
            MessagePriority.High => ChatMessagePriority.High,
            MessagePriority.Notify => ChatMessagePriority.Notify,
            _ => ChatMessagePriority.Normal
        };

        return message.PayloadCase switch
        {
            ServerMessage.PayloadOneofCase.Output when !string.IsNullOrWhiteSpace(message.Output)
                => new ChatMessage { IsUser = false, Text = message.Output, Priority = priority },
            ServerMessage.PayloadOneofCase.Error when !string.IsNullOrWhiteSpace(message.Error)
                => new ChatMessage { IsUser = false, Text = message.Error, IsError = true, Priority = priority },
            ServerMessage.PayloadOneofCase.Event when message.Event != null
                => new ChatMessage
                {
                    IsEvent = true,
                    EventMessage = string.IsNullOrWhiteSpace(message.Event.Message)
                        ? message.Event.Kind.ToString()
                        : message.Event.Message,
                    Priority = priority
                },
            ServerMessage.PayloadOneofCase.Media when message.Media != null
                => new ChatMessage { IsUser = false, Text = _mediaTextFormatter(message.Media), Priority = priority },
            _ => null
        };
    }
}
