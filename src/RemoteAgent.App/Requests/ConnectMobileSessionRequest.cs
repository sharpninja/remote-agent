using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.ViewModels;

namespace RemoteAgent.App.Requests;

public sealed record ConnectMobileSessionRequest(
    Guid CorrelationId,
    MainPageViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"ConnectMobileSessionRequest {{ CorrelationId = {CorrelationId}, Host = {Workspace.Host}, Port = {Workspace.Port} }}";
}
