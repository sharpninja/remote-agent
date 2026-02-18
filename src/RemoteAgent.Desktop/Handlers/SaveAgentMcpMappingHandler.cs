using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class SaveAgentMcpMappingHandler(IServerCapacityClient client)
    : IRequestHandler<SaveAgentMcpMappingRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(SaveAgentMcpMappingRequest request, CancellationToken cancellationToken = default)
    {
        var ok = await client.SetAgentMcpServersAsync(
            request.Host, request.Port, request.AgentId, request.ServerIds, request.ApiKey, cancellationToken);

        request.Workspace.McpStatus = ok
            ? $"Saved MCP mapping for agent '{request.AgentId}'."
            : $"Failed to save MCP mapping for agent '{request.AgentId}'.";

        // Refresh MCP inline
        var servers = await client.ListMcpServersAsync(request.Host, request.Port, request.ApiKey, cancellationToken);
        request.Workspace.McpServers.Clear();
        foreach (var row in servers)
            request.Workspace.McpServers.Add(row);
        request.Workspace.SelectedMcpServer = request.Workspace.McpServers.FirstOrDefault();

        var mapping = await client.GetAgentMcpServersAsync(
            request.Host, request.Port, request.AgentId, request.ApiKey, cancellationToken);
        request.Workspace.AgentMcpServerIdsText = mapping == null
            ? ""
            : string.Join(Environment.NewLine, mapping.ServerIds);

        request.Workspace.McpStatus = $"Loaded {request.Workspace.McpServers.Count} MCP server(s) for registry.";
        return ok ? CommandResult.Ok() : CommandResult.Fail("Failed to save agent MCP mapping.");
    }
}
