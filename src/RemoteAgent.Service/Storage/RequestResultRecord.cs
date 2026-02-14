using LiteDB;

namespace RemoteAgent.Service.Storage;

/// <summary>Stored request or result row in LiteDB (TR-11.1). Used by <see cref="LiteDbLocalStorage"/> for session history and replay.</summary>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-11)</see>
public class RequestResultRecord
{
#pragma warning disable CS8618
    /// <summary>LiteDB document id.</summary>
    public ObjectId Id { get; set; }
#pragma warning restore CS8618

    /// <summary>Session identifier.</summary>
    public string SessionId { get; set; } = "";

    /// <summary>True if from client (request), false if from server/agent (response).</summary>
    public bool IsRequest { get; set; }

    /// <summary>Kind of entry: Text, ScriptRequest, MediaUpload, Output, Error, Event, Media.</summary>
    public string Kind { get; set; } = "";

    /// <summary>Truncated or key content (e.g. first 2000 chars).</summary>
    public string Summary { get; set; } = "";

    /// <summary>When the entry was logged.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Relative path under DataDirectory for media (TR-11.2).</summary>
    public string? MediaPath { get; set; }
}
