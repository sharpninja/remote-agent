using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record BanPeerRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    string Peer,
    string? Reason,
    string? ApiKey,
    ServerWorkspaceViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"BanPeerRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, Peer = {Peer}, ApiKey = [REDACTED] }}";
}
