using Avalonia.Controls;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record OpenNewSessionRequest(
    Guid CorrelationId,
    Func<Window?> OwnerWindowFactory,
    ServerWorkspaceViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"OpenNewSessionRequest {{ CorrelationId = {CorrelationId}, Workspace = {Workspace.Host}:{Workspace.Port} }}";
}
