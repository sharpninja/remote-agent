using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.Handlers;

public sealed class StartLogMonitoringHandler(IStructuredLogClient logClient)
    : IRequestHandler<StartLogMonitoringRequest, CommandResult<StartLogMonitoringResult>>
{
    public async Task<CommandResult<StartLogMonitoringResult>> HandleAsync(
        StartLogMonitoringRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Host))
            return CommandResult<StartLogMonitoringResult>.Fail("Host is required.");

        if (request.Port <= 0 || request.Port > 65535)
            return CommandResult<StartLogMonitoringResult>.Fail("Port must be 1-65535.");

        StructuredLogsSnapshotResponse? snapshot;
        try
        {
            snapshot = await logClient.GetStructuredLogsSnapshotAsync(
                request.Host,
                request.Port,
                request.ReplayFromOffset,
                5000,
                request.ApiKey,
                cancellationToken,
                throwOnError: true);
        }
        catch (Exception ex)
        {
            return CommandResult<StartLogMonitoringResult>.Fail($"Log snapshot failed: {ex.Message}");
        }

        if (snapshot != null)
        {
            request.Workspace.IngestStructuredLogs(request.Host, request.Port, snapshot.Entries);
            request.Workspace.ReloadStructuredLogs();
        }

        var nextOffset = snapshot?.NextOffset ?? request.ReplayFromOffset;
        request.Workspace.LogMonitorStatus = $"Monitoring logs from {request.Host}:{request.Port} (offset {nextOffset}).";
        return CommandResult<StartLogMonitoringResult>.Ok(new StartLogMonitoringResult(nextOffset));
    }
}
