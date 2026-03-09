namespace RemoteAgent.App.Logic;

/// <summary>
/// A saved server profile keyed by Host + Port.
/// Stores per-server configuration that applies to new connections and sessions.
/// </summary>
public sealed class ServerProfile
{
    /// <summary>Server hostname or IP address.</summary>
    public string Host { get; set; } = "";

    /// <summary>Server gRPC port.</summary>
    public int Port { get; set; } = 5244;

    /// <summary>API key for authentication (may be empty if not required).</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>Optional user-friendly display name for the server.</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>Text prepended to every chat message sent to the agent.</summary>
    public string PerRequestContext { get; set; } = "";

    /// <summary>Default context seeded into new sessions on this server.</summary>
    public string DefaultSessionContext { get; set; } = "";
}

/// <summary>
/// Persistence for saved server profiles. Implementations should treat
/// Host + Port as the unique key.
/// </summary>
public interface IServerProfileStore
{
    /// <summary>Return all saved profiles.</summary>
    IReadOnlyList<ServerProfile> GetAll();

    /// <summary>Find a profile by host and port, or null if not saved.</summary>
    ServerProfile? GetByHostPort(string host, int port);

    /// <summary>Insert or update a profile (matched by Host + Port).</summary>
    void Upsert(ServerProfile profile);

    /// <summary>Delete a profile by host and port.</summary>
    bool Delete(string host, int port);
}
