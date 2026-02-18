using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record SeedSessionContextRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    string SessionId,
    string ContextType,
    string Content,
    string? Source,
    string? ApiKey,
    ServerWorkspaceViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"SeedSessionContextRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, SessionId = {SessionId}, ContextType = {ContextType}, ApiKey = [REDACTED] }}";
}
