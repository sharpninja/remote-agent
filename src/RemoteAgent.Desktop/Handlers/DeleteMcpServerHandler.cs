using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class DeleteMcpServerHandler(IServerCapacityClient client)
    : IRequestHandler<DeleteMcpServerRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(DeleteMcpServerRequest request, CancellationToken cancellationToken = default)
    {
        var ok = await client.DeleteMcpServerAsync(
            request.Host, request.Port, request.ServerId, request.ApiKey, cancellationToken);

        request.Workspace.McpStatus = ok
            ? $"Deleted MCP server '{request.ServerId}'."
            : $"Failed to delete MCP server '{request.ServerId}'.";

        // Refresh MCP inline
        var servers = await client.ListMcpServersAsync(request.Host, request.Port, request.ApiKey, cancellationToken);
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
        return ok ? CommandResult.Ok() : CommandResult.Fail($"Failed to delete MCP server '{request.ServerId}'.");
    }
}
