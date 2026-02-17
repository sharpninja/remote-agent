namespace RemoteAgent.App.Services;

/// <summary>Local persistence of sessions (FR-11.1, TR-12.1.3). Session list with session_id, title, agent_id.</summary>
public interface ISessionStore
{
    /// <summary>All sessions, newest first.</summary>
    IReadOnlyList<SessionItem> GetAll();

    /// <summary>Get session by id, or null.</summary>
    SessionItem? Get(string sessionId);

    /// <summary>Add a session. SessionId must be set.</summary>
    void Add(SessionItem session);

    /// <summary>Update session title (FR-11.1.3, TR-12.2.1).</summary>
    void UpdateTitle(string sessionId, string title);

    /// <summary>Update session agent id (FR-11.1.2).</summary>
    void UpdateAgentId(string sessionId, string agentId);

    /// <summary>Update session connection mode ("direct" or "server").</summary>
    void UpdateConnectionMode(string sessionId, string connectionMode);

    /// <summary>Remove a session and its messages (optional).</summary>
    void Delete(string sessionId);
}
