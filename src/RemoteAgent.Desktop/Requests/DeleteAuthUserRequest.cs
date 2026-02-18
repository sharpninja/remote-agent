using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record DeleteAuthUserRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    string UserId,
    string? ApiKey,
    ServerWorkspaceViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"DeleteAuthUserRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, UserId = {UserId}, ApiKey = [REDACTED] }}";
}
