using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record SecurityDataSnapshot(
    IReadOnlyList<AbandonedServerSessionSnapshot> Abandoned,
    IReadOnlyList<ConnectedPeerSnapshot> Peers,
    IReadOnlyList<ConnectionHistorySnapshot> History,
    IReadOnlyList<BannedPeerSnapshot> Banned);

public sealed record RefreshSecurityDataRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    string? ApiKey,
    ServerWorkspaceViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"RefreshSecurityDataRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, ApiKey = [REDACTED] }}";
}
