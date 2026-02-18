using System.Collections.ObjectModel;
using RemoteAgent.App.Services;
using RemoteAgent.Proto;

namespace RemoteAgent.App.Logic;

/// <summary>
/// Abstracts the gateway client for agent sessions, enabling testability and DI.
/// </summary>
public interface IAgentGatewayClient
{
    ObservableCollection<ChatMessage> Messages { get; }
    bool IsConnected { get; }
    string? PerRequestContext { get; set; }

    event Action? ConnectionStateChanged;
    event Action<ChatMessage>? MessageReceived;

    void LoadFromStore(string? sessionId = null);
    void AddUserMessage(ChatMessage message);
    void SetArchived(ChatMessage message, bool archived);

    Task ConnectAsync(string host, int port, string? sessionId = null, string? agentId = null,
        string? clientVersion = null, string? apiKey = null, CancellationToken ct = default);
    Task SendTextAsync(string text, CancellationToken ct = default);
    Task SendScriptRequestAsync(string pathOrCommand, ScriptType scriptType, CancellationToken ct = default);
    Task SendMediaAsync(byte[] content, string contentType, string? fileName, CancellationToken ct = default);
    Task StopSessionAsync(CancellationToken ct = default);
    void Disconnect();
}
