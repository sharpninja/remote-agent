using System.Collections.ObjectModel;
using Microsoft.Maui.ApplicationModel;
using RemoteAgent.App.Logic;
using RemoteAgent.Proto;

namespace RemoteAgent.App.Services;

/// <summary>MAUI-facing wrapper over the shared <see cref="AgentSessionClient"/> used by both mobile and desktop.</summary>
public sealed class AgentGatewayClientService
{
    private readonly ILocalMessageStore? _store;
    private readonly AgentSessionClient _sessionClient;

    public AgentGatewayClientService(ILocalMessageStore? store = null)
    {
        _store = store;
        _sessionClient = new AgentSessionClient(FormatReceivedMedia);
        _sessionClient.ConnectionStateChanged += () => ConnectionStateChanged?.Invoke();
        _sessionClient.MessageReceived += OnSessionClientMessageReceived;
    }

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public bool IsConnected => _sessionClient.IsConnected;
    public ServerInfoResponse? ServerInfo => _sessionClient.ServerInfo;
    public string? CurrentSessionId => _sessionClient.CurrentSessionId;

    public string? PerRequestContext
    {
        get => _sessionClient.PerRequestContext;
        set => _sessionClient.PerRequestContext = value;
    }

    public event Action? ConnectionStateChanged;
    public event Action<ChatMessage>? MessageReceived;

    public void LoadFromStore(string? sessionId = null)
    {
        if (_store == null) return;
        Messages.Clear();
        foreach (var msg in _store.Load(sessionId))
            Messages.Add(msg);
    }

    public void AddUserMessage(ChatMessage message)
    {
        Messages.Add(message);
        _store?.Add(message, CurrentSessionId);
    }

    public void SetArchived(ChatMessage message, bool archived)
    {
        if (message.Id is { } id)
            _store?.SetArchived(id, archived);
    }

    public static Task<ServerInfoResponse?> GetServerInfoAsync(string host, int port, string? clientVersion = null, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.GetServerInfoAsync(host, port, clientVersion, apiKey, ct);

    public static Task<GetPluginsResponse?> GetPluginsAsync(string host, int port, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.GetPluginsAsync(host, port, apiKey, ct);

    public static Task<UpdatePluginsResponse?> UpdatePluginsAsync(string host, int port, IEnumerable<string> assemblies, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.UpdatePluginsAsync(host, port, assemblies, apiKey, ct);

    public static Task<ListMcpServersResponse?> ListMcpServersAsync(string host, int port, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.ListMcpServersAsync(host, port, apiKey, ct);

    public static Task<UpsertMcpServerResponse?> UpsertMcpServerAsync(string host, int port, McpServerDefinition server, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.UpsertMcpServerAsync(host, port, server, apiKey, ct);

    public static Task<DeleteMcpServerResponse?> DeleteMcpServerAsync(string host, int port, string serverId, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.DeleteMcpServerAsync(host, port, serverId, apiKey, ct);

    public static Task<ListPromptTemplatesResponse?> ListPromptTemplatesAsync(string host, int port, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.ListPromptTemplatesAsync(host, port, apiKey, ct);

    public static Task<StructuredLogsSnapshotResponse?> GetStructuredLogsSnapshotAsync(string host, int port, long fromOffset = 0, int limit = 5000, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.GetStructuredLogsSnapshotAsync(host, port, fromOffset, limit, apiKey, ct);

    public static Task MonitorStructuredLogsAsync(string host, int port, long fromOffset, Func<StructuredLogEntry, Task> onEntry, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.MonitorStructuredLogsAsync(host, port, fromOffset, onEntry, apiKey, ct);

    public Task ConnectAsync(string host, int port, string? sessionId = null, string? agentId = null, string? clientVersion = null, string? apiKey = null, CancellationToken ct = default)
        => _sessionClient.ConnectAsync(host, port, sessionId, agentId, clientVersion, apiKey, ct);

    public void Disconnect()
        => _sessionClient.Disconnect();

    public Task SendTextAsync(string text, CancellationToken ct = default)
        => _sessionClient.SendTextAsync(text, ct);

    public Task SendScriptRequestAsync(string pathOrCommand, ScriptType scriptType, CancellationToken ct = default)
        => _sessionClient.SendScriptRequestAsync(pathOrCommand, scriptType, ct);

    public Task SendMediaAsync(byte[] content, string contentType, string? fileName, CancellationToken ct = default)
        => _sessionClient.SendMediaAsync(content, contentType, fileName, ct);

    public Task StopSessionAsync(CancellationToken ct = default)
        => _sessionClient.StopSessionAsync(ct);

    private void OnSessionClientMessageReceived(ChatMessage chat)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Messages.Add(chat);
            _store?.Add(chat, CurrentSessionId);
            MessageReceived?.Invoke(chat);
        });
    }

    private static string FormatReceivedMedia(MediaChunk media)
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
