using LiteDB;

namespace RemoteAgent.App.Services;

/// <summary>LiteDB implementation of <see cref="ILocalMessageStore"/> (TR-11.1). Persists chat messages and archive state to a single database file.</summary>
/// <remarks>Uses collection <c>messages</c> and <see cref="StoredMessageRecord"/>. Best-effort: failures on Add or SetArchived do not throw.</remarks>
/// <example><code>
/// var store = new LocalMessageStore(Path.Combine(FileSystem.AppDataDirectory, "remote-agent.db"));
/// var client = new AgentGatewayClientService(store);
/// client.LoadFromStore();
/// </code></example>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-11)</see>
public sealed class LocalMessageStore : ILocalMessageStore
{
    private readonly string _dbPath;
    private const string CollectionName = "messages";

    /// <summary>Creates the store with the given database file path.</summary>
    /// <param name="dbPath">Full path to the LiteDB file (e.g. under <see cref="FileSystem.AppDataDirectory"/>).</param>
    public LocalMessageStore(string dbPath)
    {
        _dbPath = dbPath;
    }

    /// <inheritdoc />
    public IReadOnlyList<ChatMessage> Load(string? sessionId = null)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<StoredMessageRecord>(CollectionName);
            var query = sessionId != null ? col.Find(x => x.SessionId == sessionId) : col.FindAll();
            var all = query.OrderBy(x => x.Timestamp).ToList();
            return all.Select(ToChatMessage).ToList();
        }
        catch
        {
            return Array.Empty<ChatMessage>();
        }
    }

    /// <inheritdoc />
    public void Add(ChatMessage message, string? sessionId = null)
    {
        try
        {
            var id = Guid.NewGuid();
            message.Id = id;
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<StoredMessageRecord>(CollectionName);
            col.Insert(new StoredMessageRecord
            {
                MessageId = id,
                Text = message.Text,
                IsUser = message.IsUser,
                IsError = message.IsError,
                IsEvent = message.IsEvent,
                EventMessage = message.EventMessage,
                Priority = (int)message.Priority,
                IsArchived = message.IsArchived,
                Timestamp = DateTimeOffset.UtcNow,
                SessionId = sessionId
            });
        }
        catch
        {
            // best-effort
        }
    }

    /// <inheritdoc />
    public void SetArchived(Guid messageId, bool archived)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<StoredMessageRecord>(CollectionName);
            var doc = col.FindOne(x => x.MessageId == messageId);
            if (doc != null)
            {
                doc.IsArchived = archived;
                col.Update(doc);
            }
        }
        catch
        {
            // best-effort
        }
    }

    private static ChatMessage ToChatMessage(StoredMessageRecord r)
    {
        return new ChatMessage
        {
            Id = r.MessageId,
            Text = r.Text,
            IsUser = r.IsUser,
            IsError = r.IsError,
            IsEvent = r.IsEvent,
            EventMessage = r.EventMessage,
            Priority = (ChatMessagePriority)r.Priority,
            IsArchived = r.IsArchived
        };
    }
}
