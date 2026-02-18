using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record UnbanPeerRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    string Peer,
    string? ApiKey,
    SecurityViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"UnbanPeerRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, Peer = {Peer}, ApiKey = [REDACTED] }}";
}
