namespace RemoteAgent.App.Services;

/// <summary>Represents a chat session (FR-11.1, TR-12.1.3). Persisted in local storage with session_id, title, and agent_id.</summary>
public class SessionItem
{
    /// <summary>Unique session id; used in SessionControl and for routing (FR-11.1.1).</summary>
    public string SessionId { get; set; } = "";

    /// <summary>User-definable title; defaults to first request text (FR-11.1.3, TR-12.2.1).</summary>
    public string Title { get; set; } = "New chat";

    /// <summary>Agent runner id (e.g. "process", "copilot-windows"); from server list (FR-11.1.2).</summary>
    public string AgentId { get; set; } = "";

    /// <summary>When the session was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
