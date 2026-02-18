using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record SaveAgentMcpMappingRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    string AgentId,
    IReadOnlyList<string> ServerIds,
    string? ApiKey,
    ServerWorkspaceViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"SaveAgentMcpMappingRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, AgentId = {AgentId}, ServerIds = [{ServerIds.Count}], ApiKey = [REDACTED] }}";
}
