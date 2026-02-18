using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Logic.Requests;
using RemoteAgent.App.Logic.ViewModels;

namespace RemoteAgent.App.Logic.Handlers;

public sealed class LoadMcpServersHandler(IServerApiClient apiClient) : IRequestHandler<LoadMcpServersRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(LoadMcpServersRequest request, CancellationToken ct = default)
    {
        var vm = request.Workspace;
        var host = (vm.Host ?? "").Trim();
        var portText = (vm.Port ?? "5243").Trim();

        if (string.IsNullOrWhiteSpace(host))
        {
            vm.StatusText = "Host is required.";
            return CommandResult.Fail("Host is required.");
        }

        if (!int.TryParse(portText, out var port) || port <= 0 || port > 65535)
        {
            vm.StatusText = "Valid port required (1-65535).";
            return CommandResult.Fail("Valid port required (1-65535).");
        }

        vm.StatusText = "Loading MCP servers...";
        var response = await apiClient.ListMcpServersAsync(host, port, ct: ct);
        if (response == null)
        {
            vm.StatusText = "Failed to load MCP servers.";
            return CommandResult.Fail("Failed to load MCP servers.");
        }

        vm.Servers.Clear();
        foreach (var server in response.Servers.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
            vm.Servers.Add(server);

        vm.StatusText = $"Loaded {vm.Servers.Count} MCP server(s).";
        return CommandResult.Ok();
    }
}
