namespace RemoteAgent.App.Logic;

/// <summary>
/// Read-only provider of current session list, decoupling shell from storage details.
/// </summary>
public interface ISessionListProvider
{
    IReadOnlyList<SessionSummary> GetSessions();
}

public sealed record SessionSummary(string SessionId, string Title);
