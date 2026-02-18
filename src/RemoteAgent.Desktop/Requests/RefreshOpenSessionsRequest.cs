using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record RefreshOpenSessionsData(
    IReadOnlyList<OpenServerSessionSnapshot> Sessions);

public sealed record RefreshOpenSessionsRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    string? ApiKey,
    ServerWorkspaceViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"RefreshOpenSessionsRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, ApiKey = [REDACTED] }}";
}
