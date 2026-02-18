using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.Handlers;

public sealed class RefreshMcpRegistryHandler(IServerCapacityClient client)
    : IRequestHandler<RefreshMcpRegistryRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(
        RefreshMcpRegistryRequest request,
        CancellationToken cancellationToken = default)
    {
        var servers = await client.ListMcpServersAsync(
            request.Host, request.Port, request.ApiKey, cancellationToken);

        request.Workspace.McpServers.Clear();
        foreach (var row in servers)
            request.Workspace.McpServers.Add(row);
        request.Workspace.SelectedMcpServer = request.Workspace.McpServers.FirstOrDefault();

        var mapping = await client.GetAgentMcpServersAsync(
            request.Host, request.Port, request.Workspace.SelectedAgentId, request.ApiKey, cancellationToken);
        request.Workspace.AgentMcpServerIdsText = mapping == null
            ? ""
            : string.Join(Environment.NewLine, mapping.ServerIds);

        request.Workspace.McpStatus = $"Loaded {request.Workspace.McpServers.Count} MCP server(s) for registry.";
        return CommandResult.Ok();
    }
}
