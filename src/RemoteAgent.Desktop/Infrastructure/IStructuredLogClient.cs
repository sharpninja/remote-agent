using RemoteAgent.App.Logic;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.Infrastructure;

public interface IStructuredLogClient
{
    Task<StructuredLogsSnapshotResponse?> GetStructuredLogsSnapshotAsync(
        string host,
        int port,
        long fromOffset = 0,
        int limit = 5000,
        string? apiKey = null,
        CancellationToken cancellationToken = default,
        bool throwOnError = false);

    Task MonitorStructuredLogsAsync(
        string host,
        int port,
        long fromOffset,
        Func<StructuredLogEntry, Task> onEntry,
        string? apiKey = null,
        CancellationToken cancellationToken = default);
}
