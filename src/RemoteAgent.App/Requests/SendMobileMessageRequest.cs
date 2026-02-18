using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.ViewModels;

namespace RemoteAgent.App.Requests;

public sealed record SendMobileMessageRequest(
    Guid CorrelationId,
    MainPageViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"SendMobileMessageRequest {{ CorrelationId = {CorrelationId} }}";
}
