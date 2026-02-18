using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record TerminateDesktopSessionRequest(
    Guid CorrelationId,
    DesktopSessionViewModel? Session,
    ServerWorkspaceViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"TerminateDesktopSessionRequest {{ CorrelationId = {CorrelationId}, Session = {Session?.Title ?? "(null)"}, Workspace = {Workspace.CurrentServerLabel} }}";
}
