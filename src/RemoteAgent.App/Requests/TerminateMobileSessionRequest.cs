using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Services;
using RemoteAgent.App.ViewModels;

namespace RemoteAgent.App.Requests;

public sealed record TerminateMobileSessionRequest(
    Guid CorrelationId,
    SessionItem? Session,
    MainPageViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"TerminateMobileSessionRequest {{ CorrelationId = {CorrelationId}, SessionId = {Session?.SessionId} }}";
}
