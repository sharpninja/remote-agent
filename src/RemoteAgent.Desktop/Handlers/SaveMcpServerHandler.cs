using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.Handlers;

public sealed class SaveMcpServerHandler(IServerCapacityClient client)
    : IRequestHandler<SaveMcpServerRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(
        SaveMcpServerRequest request,
        CancellationToken cancellationToken = default)
    {
        var saved = await client.UpsertMcpServerAsync(
            request.Host, request.Port, request.Server, request.ApiKey, cancellationToken);

        if (saved is null)
        {
            request.Workspace.McpStatus = "Failed to save MCP server.";
            return CommandResult.Fail("Failed to save MCP server.");
        }

        request.Workspace.McpStatus = $"Saved MCP server '{saved.ServerId}'.";

        // Refresh MCP inline
        var servers = await client.ListMcpServersAsync(request.Host, request.Port, request.ApiKey, cancellationToken);
        request.Workspace.McpServers.Clear();
        foreach (var row in servers)
            request.Workspace.McpServers.Add(row);

        var mapping = await client.GetAgentMcpServersAsync(
            request.Host, request.Port, request.Workspace.SelectedAgentId, request.ApiKey, cancellationToken);
        request.Workspace.AgentMcpServerIdsText = mapping == null
            ? ""
            : string.Join(Environment.NewLine, mapping.ServerIds);

        var savedId = saved.ServerId;
        request.Workspace.SelectedMcpServer =
            request.Workspace.McpServers.FirstOrDefault(x => string.Equals(x.ServerId, savedId, StringComparison.OrdinalIgnoreCase))
            ?? request.Workspace.McpServers.FirstOrDefault();

        request.Workspace.McpStatus = $"Loaded {request.Workspace.McpServers.Count} MCP server(s) for registry.";
        return CommandResult.Ok();
    }
}
