namespace RemoteAgent.App.Logic;

/// <summary>
/// Abstracts session-related operations invoked from shell/flyout UI, decoupling 
/// AppShell from direct page references.
/// </summary>
public interface ISessionCommandBus
{
    void StartNewSession();
    bool SelectSession(string? sessionId);
    Task<bool> TerminateSessionAsync(string? sessionId);
    string? GetCurrentSessionId();
    bool IsConnected { get; }
    event Action? ConnectionStateChanged;
}
