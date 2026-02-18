using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record CreateDesktopSessionRequest(
    Guid CorrelationId,
    string Title,
    string Host,
    int Port,
    string ConnectionMode,
    string AgentId,
    string? ApiKey,
    string? PerRequestContext,
    ServerWorkspaceViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"CreateDesktopSessionRequest {{ CorrelationId = {CorrelationId}, Title = {Title}, Host = {Host}, Port = {Port}, ConnectionMode = {ConnectionMode}, AgentId = {AgentId}, ApiKey = [REDACTED] }}";
}
