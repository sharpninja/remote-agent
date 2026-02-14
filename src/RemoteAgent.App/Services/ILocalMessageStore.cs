namespace RemoteAgent.App.Services;

/// <summary>Local persistence of chat messages for history and replay (TR-11.1).</summary>
/// <remarks>Implemented by <see cref="LocalMessageStore"/> (LiteDB). Used by <see cref="AgentGatewayClientService"/> to load messages on start and persist new messages and archive state (FR-4.2).</remarks>
/// <example><code>
/// var store = new LocalMessageStore(dbPath);
/// client.LoadFromStore();  // loads from store into Messages
/// client.AddUserMessage(msg);  // app adds and persists
/// </code></example>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-11)</see>
public interface ILocalMessageStore
{
    /// <summary>Loads all messages from storage (oldest first). Call on app start to restore chat.</summary>
    /// <returns>Ordered list of messages (including archived; filter by <see cref="ChatMessage.IsArchived"/> in UI).</returns>
    IReadOnlyList<ChatMessage> Load();

    /// <summary>Persists a new message. The store may set <see cref="ChatMessage.Id"/> after insert.</summary>
    /// <param name="message">The message to save (user, agent, event, or error).</param>
    void Add(ChatMessage message);

    /// <summary>Updates the archived state of a message (FR-4.1, TR-5.5). Call when the user swipes to archive.</summary>
    /// <param name="messageId">The message id (from <see cref="ChatMessage.Id"/>).</param>
    /// <param name="archived">True to archive (hide from list), false to unarchive.</param>
    void SetArchived(Guid messageId, bool archived);
}
