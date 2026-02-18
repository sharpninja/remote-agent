using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Logic.Requests;
using RemoteAgent.App.Logic.ViewModels;

namespace RemoteAgent.App.Logic.Handlers;

public sealed class DeleteMcpServerHandler(IServerApiClient apiClient, ISessionTerminationConfirmation deleteConfirmation)
    : IRequestHandler<DeleteMcpServerRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(DeleteMcpServerRequest request, CancellationToken ct = default)
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

        var serverId = (vm.ServerId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(serverId))
        {
            vm.StatusText = "Select a server or enter a server id to delete.";
            return CommandResult.Fail("Server id required.");
        }

        var confirmed = await deleteConfirmation.ConfirmAsync(serverId);
        if (!confirmed)
            return CommandResult.Fail("Cancelled.");

        vm.StatusText = "Deleting MCP server...";
        var response = await apiClient.DeleteMcpServerAsync(host, port, serverId, ct: ct);
        if (response == null)
        {
            vm.StatusText = "Failed to delete MCP server.";
            return CommandResult.Fail("Failed to delete MCP server.");
        }

        vm.StatusText = response.Message;

        if (response.Success)
        {
            vm.ClearForm();

            // Inline refresh after delete
            var listResponse = await apiClient.ListMcpServersAsync(host, port, ct: ct);
            if (listResponse != null)
            {
                vm.Servers.Clear();
                foreach (var s in listResponse.Servers.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
                    vm.Servers.Add(s);
                vm.StatusText = $"Deleted. {vm.Servers.Count} server(s) remain.";
            }
        }

        return CommandResult.Ok();
    }
}
