using RemoteAgent.App.Logic;

namespace RemoteAgent.App.Services;

public sealed class SessionListProviderAdapter(ISessionStore sessionStore) : ISessionListProvider
{
    public IReadOnlyList<SessionSummary> GetSessions() =>
        sessionStore.GetAll()
            .Select(s => new SessionSummary(s.SessionId, string.IsNullOrWhiteSpace(s.Title) ? s.SessionId : s.Title))
            .ToList();
}
