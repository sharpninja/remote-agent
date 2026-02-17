using LiteDB;

namespace RemoteAgent.App.Services;

/// <summary>LiteDB implementation of <see cref="ISessionStore"/> (TR-12.1.3).</summary>
public sealed class LocalSessionStore : ISessionStore
{
    private readonly string _dbPath;
    private const string CollectionName = "sessions";

    public LocalSessionStore(string dbPath)
    {
        _dbPath = dbPath;
    }

    public IReadOnlyList<SessionItem> GetAll()
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<StoredSessionRecord>(CollectionName);
            return col.FindAll().OrderByDescending(x => x.CreatedAt).Select(ToSession).ToList();
        }
        catch
        {
            return Array.Empty<SessionItem>();
        }
    }

    public SessionItem? Get(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId)) return null;
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<StoredSessionRecord>(CollectionName);
            var doc = col.FindOne(x => x.SessionId == sessionId);
            return doc != null ? ToSession(doc) : null;
        }
        catch
        {
            return null;
        }
    }

    public void Add(SessionItem session)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<StoredSessionRecord>(CollectionName);
            col.Insert(new StoredSessionRecord
            {
                SessionId = session.SessionId,
                Title = session.Title,
                AgentId = session.AgentId,
                CreatedAt = session.CreatedAt
            });
        }
        catch
        {
            // best-effort
        }
    }

    public void UpdateTitle(string sessionId, string title)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<StoredSessionRecord>(CollectionName);
            var doc = col.FindOne(x => x.SessionId == sessionId);
            if (doc != null)
            {
                doc.Title = title ?? "";
                col.Update(doc);
            }
        }
        catch
        {
            // best-effort
        }
    }

    public void UpdateAgentId(string sessionId, string agentId)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<StoredSessionRecord>(CollectionName);
            var doc = col.FindOne(x => x.SessionId == sessionId);
            if (doc != null)
            {
                doc.AgentId = agentId ?? "";
                col.Update(doc);
            }
        }
        catch
        {
            // best-effort
        }
    }

    public void Delete(string sessionId)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<StoredSessionRecord>(CollectionName);
            col.DeleteMany(x => x.SessionId == sessionId);
        }
        catch
        {
            // best-effort
        }
    }

    private static SessionItem ToSession(StoredSessionRecord r)
    {
        return new SessionItem
        {
            SessionId = r.SessionId,
            Title = r.Title,
            AgentId = r.AgentId,
            CreatedAt = r.CreatedAt
        };
    }

    private class StoredSessionRecord
    {
#pragma warning disable CS8618 // LiteDB sets Id on insert
        public ObjectId Id { get; set; }
#pragma warning restore CS8618
        public string SessionId { get; set; } = "";
        public string Title { get; set; } = "New chat";
        public string AgentId { get; set; } = "";
        public DateTimeOffset CreatedAt { get; set; }
    }
}
