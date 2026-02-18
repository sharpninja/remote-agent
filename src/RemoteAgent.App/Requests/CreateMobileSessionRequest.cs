using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.ViewModels;

namespace RemoteAgent.App.Requests;

public sealed record CreateMobileSessionRequest(
    Guid CorrelationId,
    MainPageViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"CreateMobileSessionRequest {{ CorrelationId = {CorrelationId} }}";
}
