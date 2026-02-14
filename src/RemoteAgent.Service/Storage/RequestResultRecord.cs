using LiteDB;

namespace RemoteAgent.Service.Storage;

/// <summary>Stored request or result for TR-11.1 (LiteDB local storage).</summary>
public class RequestResultRecord
{
#pragma warning disable CS8618
    public ObjectId Id { get; set; }
#pragma warning restore CS8618
    public string SessionId { get; set; } = "";
    public bool IsRequest { get; set; }  // true = from client, false = from server/agent
    public string Kind { get; set; } = "";  // Text, ScriptRequest, MediaUpload, Output, Error, Event, Media
    public string Summary { get; set; } = "";  // truncated or key content
    public DateTimeOffset Timestamp { get; set; }
    public string? MediaPath { get; set; }  // relative path under DataDirectory for media (TR-11.2)
}
