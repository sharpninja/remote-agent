namespace RemoteAgent.App.Services;

/// <summary>Persistent local storage for structured operational logs.</summary>
public interface ILocalStructuredLogStore
{
    void UpsertBatch(IEnumerable<StructuredLogRecord> logs);
    IReadOnlyList<StructuredLogRecord> Query(StructuredLogFilter? filter = null, int limit = 1000);
    long GetMaxEventId(string host, int port);
}
