using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class ExpandStatusLogPanelHandler : IRequestHandler<ExpandStatusLogPanelRequest, Unit>
{
    public Task<Unit> HandleAsync(ExpandStatusLogPanelRequest request, CancellationToken cancellationToken = default)
    {
        return Unit.TaskValue;
    }
}
