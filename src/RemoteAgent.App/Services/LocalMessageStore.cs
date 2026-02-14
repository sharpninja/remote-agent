using LiteDB;

namespace RemoteAgent.App.Services;

/// <summary>LiteDB storage for chat messages (TR-11.1).</summary>
public sealed class LocalMessageStore : ILocalMessageStore
{
    private readonly string _dbPath;
    private const string CollectionName = "messages";

    public LocalMessageStore(string dbPath)
    {
        _dbPath = dbPath;
    }

    public IReadOnlyList<ChatMessage> Load()
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<StoredMessageRecord>(CollectionName);
            var all = col.FindAll().OrderBy(x => x.Timestamp).ToList();
            return all.Select(ToChatMessage).ToList();
        }
        catch
        {
            return Array.Empty<ChatMessage>();
        }
    }

    public void Add(ChatMessage message)
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
                Timestamp = DateTimeOffset.UtcNow
            });
        }
        catch
        {
            // best-effort
        }
    }

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
