using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record ClearAppLogRequest(
    Guid CorrelationId,
    AppLogViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"ClearAppLogRequest {{ CorrelationId = {CorrelationId} }}";
}
