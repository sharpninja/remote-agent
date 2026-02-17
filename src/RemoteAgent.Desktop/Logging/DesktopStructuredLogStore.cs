using LiteDB;

namespace RemoteAgent.Desktop.Logging;

public interface IDesktopStructuredLogStore
{
    void UpsertBatch(IEnumerable<DesktopStructuredLogRecord> logs);
    IReadOnlyList<DesktopStructuredLogRecord> Query(DesktopStructuredLogFilter? filter = null, int limit = 1000);
    long GetMaxEventId(string host, int port, string? serverId = null);
}

public sealed class DesktopStructuredLogStore(string dbPath) : IDesktopStructuredLogStore
{
    private const string CollectionName = "desktop_structured_logs";

    public void UpsertBatch(IEnumerable<DesktopStructuredLogRecord> logs)
    {
        try
        {
            using var db = new LiteDatabase(dbPath);
            var col = db.GetCollection<DesktopStructuredLogRecord>(CollectionName);
            col.EnsureIndex(x => x.Id, unique: true);
            col.EnsureIndex(x => x.EventId);
            col.EnsureIndex(x => x.TimestampUtc);
            col.EnsureIndex(x => x.SessionId);
            col.EnsureIndex(x => x.CorrelationId);
            col.EnsureIndex(x => x.EventType);
            col.EnsureIndex(x => x.ServerId);

            foreach (var row in logs)
            {
                row.Id = $"{row.ServerId}:{row.SourceHost}:{row.SourcePort}:{row.EventId}";
                col.Upsert(row);
            }
        }
        catch
        {
            // best-effort local storage path
        }
    }

    public IReadOnlyList<DesktopStructuredLogRecord> Query(DesktopStructuredLogFilter? filter = null, int limit = 1000)
    {
        try
        {
            using var db = new LiteDatabase(dbPath);
            var col = db.GetCollection<DesktopStructuredLogRecord>(CollectionName);
            var max = limit <= 0 ? 1000 : limit;
            var all = col.FindAll().OrderByDescending(x => x.TimestampUtc);
            if (filter == null) return all.Take(max).ToList();
            return all.Where(filter.Matches).Take(max).ToList();
        }
        catch
        {
            return [];
        }
    }

    public long GetMaxEventId(string host, int port, string? serverId = null)
    {
        try
        {
            using var db = new LiteDatabase(dbPath);
            var col = db.GetCollection<DesktopStructuredLogRecord>(CollectionName);
            var latest = col.Find(x =>
                    x.SourceHost == host
                    && x.SourcePort == port
                    && (string.IsNullOrWhiteSpace(serverId) || x.ServerId == serverId))
                .OrderByDescending(x => x.EventId)
                .FirstOrDefault();
            return latest?.EventId ?? 0;
        }
        catch
        {
            return 0;
        }
    }
}
