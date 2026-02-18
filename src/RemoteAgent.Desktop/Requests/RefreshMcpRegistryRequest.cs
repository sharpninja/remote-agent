using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.ViewModels;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.Requests;

public sealed record McpRegistryData(
    IReadOnlyList<McpServerDefinition> Servers);

public sealed record RefreshMcpRegistryRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    string? ApiKey,
    McpRegistryDesktopViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"RefreshMcpRegistryRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, ApiKey = [REDACTED] }}";
}
