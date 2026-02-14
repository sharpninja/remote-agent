using System.Collections.ObjectModel;
using Grpc.Core;
using Grpc.Net.Client;
using RemoteAgent.Proto;

namespace RemoteAgent.App.Services;

public class AgentGatewayClientService
{
    private GrpcChannel? _channel;
    private AgentGateway.AgentGatewayClient? _client;
    private AsyncDuplexStreamingCall<ClientMessage, ServerMessage>? _call;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;

    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public bool IsConnected => _call != null;
    public event Action? ConnectionStateChanged;
    public event Action<ChatMessage>? MessageReceived;

    public async Task ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        Disconnect();
        var baseUrl = port == 443 ? $"https://{host}" : $"http://{host}:{port}";
        _channel = GrpcChannel.ForAddress(baseUrl, new GrpcChannelOptions { HttpHandler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true } });
        _client = new AgentGateway.AgentGatewayClient(_channel);
        _cts = new CancellationTokenSource();
        _call = _client.Connect(cancellationToken: _cts.Token);
        _receiveTask = ReceiveLoop(_cts.Token);
        ConnectionStateChanged?.Invoke();
        await SendControlAsync(SessionControl.Types.Action.Start, ct);
    }

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
        ConnectionStateChanged?.Invoke();
    }

    public async Task SendTextAsync(string text, CancellationToken ct = default)
    {
        if (_call?.RequestStream == null) return;
        await _call.RequestStream.WriteAsync(new ClientMessage { Text = text }, ct);
    }

    /// <summary>Sends a script run request (FR-9.1). Server returns stdout/stderr on completion.</summary>
    public async Task SendScriptRequestAsync(string pathOrCommand, ScriptType scriptType, CancellationToken ct = default)
    {
        if (_call?.RequestStream == null) return;
        await _call.RequestStream.WriteAsync(new ClientMessage
        {
            ScriptRequest = new ScriptRequest { PathOrCommand = pathOrCommand, ScriptType = scriptType }
        }, ct);
    }

    public async Task StopSessionAsync(CancellationToken ct = default)
    {
        await SendControlAsync(SessionControl.Types.Action.Stop, ct);
    }

    private async Task SendControlAsync(SessionControl.Types.Action action, CancellationToken ct)
    {
        if (_call?.RequestStream == null) return;
        await _call.RequestStream.WriteAsync(new ClientMessage { Control = new SessionControl { Action = action } }, ct);
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
                if (chat != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Messages.Add(chat);
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
}
