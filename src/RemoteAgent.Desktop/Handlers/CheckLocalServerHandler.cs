using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class CheckLocalServerHandler(ILocalServerManager localServerManager)
    : IRequestHandler<CheckLocalServerRequest, CommandResult<LocalServerProbeResult>>
{
    public async Task<CommandResult<LocalServerProbeResult>> HandleAsync(
        CheckLocalServerRequest request,
        CancellationToken cancellationToken = default)
    {
        var probe = await localServerManager.ProbeAsync(cancellationToken);
        return CommandResult<LocalServerProbeResult>.Ok(probe);
    }
}
