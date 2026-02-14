using LiteDB;

namespace RemoteAgent.App.Services;

/// <summary>Persistence record for chat messages (TR-11.1).</summary>
public class StoredMessageRecord
{
#pragma warning disable CS8618
    public ObjectId Id { get; set; }
#pragma warning restore CS8618
    public Guid MessageId { get; set; }
    public string Text { get; set; } = "";
    public bool IsUser { get; set; }
    public bool IsError { get; set; }
    public bool IsEvent { get; set; }
    public string? EventMessage { get; set; }
    public int Priority { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
