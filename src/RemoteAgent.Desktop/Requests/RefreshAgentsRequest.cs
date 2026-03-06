using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record RefreshAgentsRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    string? ApiKey,
    string CurrentDefaultAgentId,
    AgentsViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"RefreshAgentsRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, ApiKey = [REDACTED] }}";
}
