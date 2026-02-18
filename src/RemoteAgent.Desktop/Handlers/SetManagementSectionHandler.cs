using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class SetManagementSectionHandler : IRequestHandler<SetManagementSectionRequest, Unit>
{
    public Task<Unit> HandleAsync(SetManagementSectionRequest request, CancellationToken cancellationToken = default)
    {
        return Unit.TaskValue;
    }
}
