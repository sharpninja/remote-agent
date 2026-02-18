using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Threading;
using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Logging;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.ViewModels;

public sealed class StructuredLogsViewModel : INotifyPropertyChanged
{
    private readonly IRequestDispatcher _dispatcher;
    private readonly IServerConnectionContext _context;
    private readonly IDesktopStructuredLogStore _structuredLogStore;
    private CancellationTokenSource? _logMonitorCts;
    private string _logLevelFilter = "";
    private string _logEventTypeFilter = "";
    private string _logSessionIdFilter = "";
    private string _logCorrelationIdFilter = "";
    private string _logComponentFilter = "";
    private string _logServerIdFilter = "";
    private string _logSearchFilter = "";
    private string _logFromUtcFilter = "";
    private string _logToUtcFilter = "";
    private string _logMonitorStatus = "Log monitor stopped.";

    public StructuredLogsViewModel(
        IRequestDispatcher dispatcher,
        IServerConnectionContext context,
        IDesktopStructuredLogStore structuredLogStore)
    {
        _dispatcher = dispatcher;
        _context = context;
        _structuredLogStore = structuredLogStore;
        _logServerIdFilter = context.ServerId;

        StartLogMonitoringCommand = new RelayCommand(
            () => _ = RunCommandAsync(StartLogMonitoringAsync));
        StopLogMonitoringCommand = new RelayCommand(
            () => RunCommand(StopLogMonitoring));
        ApplyLogFilterCommand = new RelayCommand(
            () => RunCommand(ReloadStructuredLogs));
        ClearLogFilterCommand = new RelayCommand(
            () => RunCommand(ClearLogFilter));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DesktopStructuredLogRecord> VisibleStructuredLogs { get; } = [];

    public string LogLevelFilter
    {
        get => _logLevelFilter;
        set
        {
            if (_logLevelFilter == value) return;
            _logLevelFilter = value;
            OnPropertyChanged();
        }
    }

    public string LogEventTypeFilter
    {
        get => _logEventTypeFilter;
        set
        {
            if (_logEventTypeFilter == value) return;
            _logEventTypeFilter = value;
            OnPropertyChanged();
        }
    }

    public string LogSessionIdFilter
    {
        get => _logSessionIdFilter;
        set
        {
            if (_logSessionIdFilter == value) return;
            _logSessionIdFilter = value;
            OnPropertyChanged();
        }
    }

    public string LogCorrelationIdFilter
    {
        get => _logCorrelationIdFilter;
        set
        {
            if (_logCorrelationIdFilter == value) return;
            _logCorrelationIdFilter = value;
            OnPropertyChanged();
        }
    }

    public string LogComponentFilter
    {
        get => _logComponentFilter;
        set
        {
            if (_logComponentFilter == value) return;
            _logComponentFilter = value;
            OnPropertyChanged();
        }
    }

    public string LogServerIdFilter
    {
        get => _logServerIdFilter;
        set
        {
            if (_logServerIdFilter == value) return;
            _logServerIdFilter = value;
            OnPropertyChanged();
        }
    }

    public string LogSearchFilter
    {
        get => _logSearchFilter;
        set
        {
            if (_logSearchFilter == value) return;
            _logSearchFilter = value;
            OnPropertyChanged();
        }
    }

    public string LogFromUtcFilter
    {
        get => _logFromUtcFilter;
        set
        {
            if (_logFromUtcFilter == value) return;
            _logFromUtcFilter = value;
            OnPropertyChanged();
        }
    }

    public string LogToUtcFilter
    {
        get => _logToUtcFilter;
        set
        {
            if (_logToUtcFilter == value) return;
            _logToUtcFilter = value;
            OnPropertyChanged();
        }
    }

    public string LogMonitorStatus
    {
        get => _logMonitorStatus;
        set
        {
            if (_logMonitorStatus == value) return;
            _logMonitorStatus = value;
            OnPropertyChanged();
        }
    }

    public ICommand StartLogMonitoringCommand { get; }
    public ICommand StopLogMonitoringCommand { get; }
    public ICommand ApplyLogFilterCommand { get; }
    public ICommand ClearLogFilterCommand { get; }

    public void IngestStructuredLogs(string host, int port, IEnumerable<StructuredLogEntry> entries)
    {
        var rows = entries
            .Select(x => ToDesktopStructuredLog(_context.ServerId, _context.ServerDisplayName, host, port, x))
            .ToList();
        if (rows.Count == 0) return;
        _structuredLogStore.UpsertBatch(rows);
    }

    public void ReloadStructuredLogs()
    {
        var rows = _structuredLogStore.Query(BuildLogFilter(), 5000);
        VisibleStructuredLogs.Clear();
        foreach (var row in rows)
            VisibleStructuredLogs.Add(row);
        LogMonitorStatus = $"Loaded {VisibleStructuredLogs.Count} log row(s).";
    }

    private async Task StartLogMonitoringAsync()
    {
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { LogMonitorStatus = "Host is required."; return; }
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { LogMonitorStatus = "Port must be 1-65535."; return; }

        StopLogMonitoring();
        _logMonitorCts = new CancellationTokenSource();
        var ct = _logMonitorCts.Token;

        var replayFromOffset = _structuredLogStore.GetMaxEventId(host, port, _context.ServerId);
        var result = await _dispatcher.SendAsync(new StartLogMonitoringRequest(Guid.NewGuid(), host, port, _context.ApiKey, _context.ServerId, replayFromOffset, Workspace: this));
        if (!result.Success) { LogMonitorStatus = result.ErrorMessage ?? "Log monitoring failed."; return; }
        var fromOffset = result.Data?.NextOffset ?? replayFromOffset;

        _ = Task.Run(async () =>
        {
            try
            {
                await ServerApiClient.MonitorStructuredLogsAsync(
                    host,
                    port,
                    fromOffset,
                    entry =>
                    {
                        IngestStructuredLogs(host, port, [entry]);
                        var row = ToDesktopStructuredLog(_context.ServerId, _context.ServerDisplayName, host, port, entry);
                        var filter = BuildLogFilter();
                        if (filter.Matches(row))
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                VisibleStructuredLogs.Insert(0, row);
                                while (VisibleStructuredLogs.Count > 5000)
                                    VisibleStructuredLogs.RemoveAt(VisibleStructuredLogs.Count - 1);
                            });
                        }

                        return Task.CompletedTask;
                    },
                    _context.ApiKey,
                    ct);
            }
            catch (OperationCanceledException)
            {
                // expected
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() => LogMonitorStatus = $"Log monitor error: {ex.Message}");
            }
        }, ct);
    }

    private void StopLogMonitoring()
    {
        try { _logMonitorCts?.Cancel(); } catch { }
        _logMonitorCts = null;
        LogMonitorStatus = "Log monitor stopped.";
    }

    private void ClearLogFilter()
    {
        LogLevelFilter = "";
        LogEventTypeFilter = "";
        LogSessionIdFilter = "";
        LogCorrelationIdFilter = "";
        LogComponentFilter = "";
        LogServerIdFilter = _context.ServerId;
        LogSearchFilter = "";
        LogFromUtcFilter = "";
        LogToUtcFilter = "";
        ReloadStructuredLogs();
    }

    private DesktopStructuredLogFilter BuildLogFilter()
    {
        return new DesktopStructuredLogFilter
        {
            Level = string.IsNullOrWhiteSpace(LogLevelFilter) ? null : LogLevelFilter.Trim(),
            EventType = string.IsNullOrWhiteSpace(LogEventTypeFilter) ? null : LogEventTypeFilter.Trim(),
            SessionId = string.IsNullOrWhiteSpace(LogSessionIdFilter) ? null : LogSessionIdFilter.Trim(),
            CorrelationId = string.IsNullOrWhiteSpace(LogCorrelationIdFilter) ? null : LogCorrelationIdFilter.Trim(),
            Component = string.IsNullOrWhiteSpace(LogComponentFilter) ? null : LogComponentFilter.Trim(),
            ServerId = string.IsNullOrWhiteSpace(LogServerIdFilter) ? _context.ServerId : LogServerIdFilter.Trim(),
            SearchText = string.IsNullOrWhiteSpace(LogSearchFilter) ? null : LogSearchFilter.Trim(),
            FromUtc = TryParseUtc(LogFromUtcFilter),
            ToUtc = TryParseUtc(LogToUtcFilter),
            SourceHost = string.IsNullOrWhiteSpace(_context.Host) ? null : _context.Host.Trim()
        };
    }

    private static DateTimeOffset? TryParseUtc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!DateTimeOffset.TryParse(value.Trim(), out var parsed))
            return null;

        return parsed.ToUniversalTime();
    }

    private static DesktopStructuredLogRecord ToDesktopStructuredLog(string serverId, string serverDisplayName, string host, int port, StructuredLogEntry row)
    {
        DateTimeOffset parsed;
        if (!DateTimeOffset.TryParse(row.TimestampUtc, out parsed))
            parsed = DateTimeOffset.UtcNow;
        return new DesktopStructuredLogRecord
        {
            ServerId = serverId ?? "",
            ServerDisplayName = serverDisplayName ?? "",
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

    private void RunCommand(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            LogMonitorStatus = $"Command failed: {ex.Message}";
        }
    }

    private async Task RunCommandAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            LogMonitorStatus = $"Command failed: {ex.Message}";
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
