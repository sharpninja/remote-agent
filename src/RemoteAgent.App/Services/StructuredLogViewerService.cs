using System.Collections.ObjectModel;
using RemoteAgent.Proto;

namespace RemoteAgent.App.Services;

/// <summary>Realtime structured log monitoring with persistent ingestion and filterable view.</summary>
public sealed class StructuredLogViewerService
{
    private readonly ILocalStructuredLogStore _store;
    private CancellationTokenSource? _monitorCts;

    public StructuredLogViewerService(ILocalStructuredLogStore store)
    {
        _store = store;
    }

    public ObservableCollection<StructuredLogRecord> VisibleLogs { get; } = [];

    public StructuredLogFilter CurrentFilter { get; private set; } = new();

    public void SetFilter(StructuredLogFilter filter)
    {
        CurrentFilter = filter ?? new StructuredLogFilter();
        Reload();
    }

    public void Reload(int limit = 1000)
    {
        var rows = _store.Query(CurrentFilter, limit);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            VisibleLogs.Clear();
            foreach (var row in rows)
                VisibleLogs.Add(row);
        });
    }

    public async Task StartMonitoringAsync(string host, int port, string? apiKey = null, bool fullReplay = true, CancellationToken ct = default)
    {
        StopMonitoring();
        _monitorCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        long fromOffset = fullReplay ? 0 : _store.GetMaxEventId(host, port);
        if (fullReplay)
        {
            var snapshot = await AgentGatewayClientService.GetStructuredLogsSnapshotAsync(host, port, 0, limit: 0, apiKey, _monitorCts.Token);
            if (snapshot != null)
                Ingest(host, port, snapshot.Entries);
            Reload();
            fromOffset = snapshot?.NextOffset ?? 0;
        }

        await AgentGatewayClientService.MonitorStructuredLogsAsync(
            host,
            port,
            fromOffset,
            onEntry: entry =>
            {
                Ingest(host, port, [entry]);
                var row = ToRecord(host, port, entry);
                if (CurrentFilter.Matches(row))
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        VisibleLogs.Insert(0, row);
                        while (VisibleLogs.Count > 5000)
                            VisibleLogs.RemoveAt(VisibleLogs.Count - 1);
                    });
                }

                return Task.CompletedTask;
            },
            apiKey,
            _monitorCts.Token);
    }

    public void StopMonitoring()
    {
        try { _monitorCts?.Cancel(); } catch { }
        _monitorCts = null;
    }

    private void Ingest(string host, int port, IEnumerable<StructuredLogEntry> entries)
    {
        var rows = entries.Select(x => ToRecord(host, port, x)).ToList();
        if (rows.Count == 0) return;
        _store.UpsertBatch(rows);
    }

    private static StructuredLogRecord ToRecord(string host, int port, StructuredLogEntry row)
    {
        DateTimeOffset parsed;
        if (!DateTimeOffset.TryParse(row.TimestampUtc, out parsed))
            parsed = DateTimeOffset.UtcNow;
        return new StructuredLogRecord
        {
            EventId = row.EventId,
            TimestampUtc = parsed,
            Level = row.Level ?? "",
            EventType = row.EventType ?? "",
            Message = row.Message ?? "",
            Component = row.Component ?? "",
            SessionId = string.IsNullOrWhiteSpace(row.SessionId) ? null : row.SessionId,
            CorrelationId = string.IsNullOrWhiteSpace(row.CorrelationId) ? null : row.CorrelationId,
            DetailsJson = string.IsNullOrWhiteSpace(row.DetailsJson) ? null : row.DetailsJson,
            SourceHost = host,
            SourcePort = port
        };
    }
}
