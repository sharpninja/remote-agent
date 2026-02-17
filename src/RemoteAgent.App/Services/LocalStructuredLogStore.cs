using LiteDB;

namespace RemoteAgent.App.Services;

/// <summary>LiteDB storage for structured logs with filtering support.</summary>
public sealed class LocalStructuredLogStore(string dbPath) : ILocalStructuredLogStore
{
    private const string CollectionName = "structured_logs";

    public void UpsertBatch(IEnumerable<StructuredLogRecord> logs)
    {
        try
        {
            using var db = new LiteDatabase(dbPath);
            var col = db.GetCollection<StructuredLogRecord>(CollectionName);
            col.EnsureIndex(x => x.Id, unique: true);
            col.EnsureIndex(x => x.EventId);
            col.EnsureIndex(x => x.TimestampUtc);
            col.EnsureIndex(x => x.SessionId);
            col.EnsureIndex(x => x.CorrelationId);
            col.EnsureIndex(x => x.EventType);

            foreach (var row in logs)
            {
                row.Id = $"{row.SourceHost}:{row.SourcePort}:{row.EventId}";
                col.Upsert(row);
            }
        }
        catch
        {
            // best-effort
        }
    }

    public IReadOnlyList<StructuredLogRecord> Query(StructuredLogFilter? filter = null, int limit = 1000)
    {
        try
        {
            using var db = new LiteDatabase(dbPath);
            var col = db.GetCollection<StructuredLogRecord>(CollectionName);
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

    public long GetMaxEventId(string host, int port)
    {
        try
        {
            using var db = new LiteDatabase(dbPath);
            var col = db.GetCollection<StructuredLogRecord>(CollectionName);
            var latest = col.Find(x => x.SourceHost == host && x.SourcePort == port)
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
