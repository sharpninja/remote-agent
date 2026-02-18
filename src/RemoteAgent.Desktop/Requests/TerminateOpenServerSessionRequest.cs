using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record TerminateOpenServerSessionRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    string SessionId,
    string? ApiKey,
    ServerWorkspaceViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"TerminateOpenServerSessionRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, SessionId = {SessionId}, ApiKey = [REDACTED] }}";
}
