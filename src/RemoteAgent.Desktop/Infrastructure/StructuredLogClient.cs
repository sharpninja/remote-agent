using RemoteAgent.App.Logic;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.Infrastructure;

public sealed class StructuredLogClient : IStructuredLogClient
{
    public Task<StructuredLogsSnapshotResponse?> GetStructuredLogsSnapshotAsync(
        string host,
        int port,
        long fromOffset = 0,
        int limit = 5000,
        string? apiKey = null,
        CancellationToken cancellationToken = default,
        bool throwOnError = false)
        => ServerApiClient.GetStructuredLogsSnapshotAsync(host, port, fromOffset, limit, apiKey, cancellationToken, throwOnError);

    public Task MonitorStructuredLogsAsync(
        string host,
        int port,
        long fromOffset,
        Func<StructuredLogEntry, Task> onEntry,
        string? apiKey = null,
        CancellationToken cancellationToken = default)
        => ServerApiClient.MonitorStructuredLogsAsync(host, port, fromOffset, onEntry, apiKey, cancellationToken);
}
