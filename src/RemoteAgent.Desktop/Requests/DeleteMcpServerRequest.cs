using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record DeleteMcpServerRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    string ServerId,
    string? ApiKey,
    ServerWorkspaceViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"DeleteMcpServerRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, ServerId = {ServerId}, ApiKey = [REDACTED] }}";
}
