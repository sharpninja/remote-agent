using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class ApplyLocalServerActionHandler(ILocalServerManager localServerManager)
    : IRequestHandler<ApplyLocalServerActionRequest, CommandResult<LocalServerProbeResult>>
{
    public async Task<CommandResult<LocalServerProbeResult>> HandleAsync(
        ApplyLocalServerActionRequest request,
        CancellationToken cancellationToken = default)
    {
        LocalServerActionResult actionResult;
        if (request.IsCurrentlyRunning)
            actionResult = await localServerManager.StopAsync(cancellationToken);
        else
            actionResult = await localServerManager.StartAsync(cancellationToken);

        if (!actionResult.Success)
            return CommandResult<LocalServerProbeResult>.Fail(actionResult.Message);

        var probe = await localServerManager.ProbeAsync(cancellationToken);
        return CommandResult<LocalServerProbeResult>.Ok(probe);
    }
}
