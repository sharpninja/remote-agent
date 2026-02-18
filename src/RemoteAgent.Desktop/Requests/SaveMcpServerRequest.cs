using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.ViewModels;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.Requests;

public sealed record SaveMcpServerRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    McpServerDefinition Server,
    string? ApiKey,
    ServerWorkspaceViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"SaveMcpServerRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, ServerId = {Server.ServerId}, ApiKey = [REDACTED] }}";
}
