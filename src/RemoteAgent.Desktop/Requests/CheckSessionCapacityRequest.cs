using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record CheckSessionCapacityRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    string? AgentId,
    string? ApiKey,
    ServerWorkspaceViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"CheckSessionCapacityRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, AgentId = {AgentId}, ApiKey = [REDACTED] }}";
}
