using LiteDB;

namespace RemoteAgent.App.Services;

/// <summary>LiteDB document for a chat message (TR-11.1). Used by <see cref="LocalMessageStore"/>.</summary>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-11)</see>
public class StoredMessageRecord
{
#pragma warning disable CS8618
    /// <summary>LiteDB document id.</summary>
    public ObjectId Id { get; set; }
#pragma warning restore CS8618

    /// <summary>Public message id (maps to <see cref="ChatMessage.Id"/>).</summary>
    public Guid MessageId { get; set; }

    /// <summary>Message text.</summary>
    public string Text { get; set; } = "";

    /// <summary>True if sent by the user.</summary>
    public bool IsUser { get; set; }

    /// <summary>True if agent error/stderr.</summary>
    public bool IsError { get; set; }

    /// <summary>True if session event.</summary>
    public bool IsEvent { get; set; }

    /// <summary>Event message when <see cref="IsEvent"/> is true.</summary>
    public string? EventMessage { get; set; }

    /// <summary><see cref="ChatMessagePriority"/> as int.</summary>
    public int Priority { get; set; }

    /// <summary>True if archived (hidden from main list) (FR-4.2).</summary>
    public bool IsArchived { get; set; }

    /// <summary>When the message was stored.</summary>
    public DateTimeOffset Timestamp { get; set; }
}
