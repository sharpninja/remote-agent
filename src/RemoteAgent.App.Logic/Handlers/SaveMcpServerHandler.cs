using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Logic.Requests;
using RemoteAgent.App.Logic.ViewModels;
using RemoteAgent.Proto;

namespace RemoteAgent.App.Logic.Handlers;

public sealed class SaveMcpServerHandler(IServerApiClient apiClient) : IRequestHandler<SaveMcpServerRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(SaveMcpServerRequest request, CancellationToken ct = default)
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

        var server = new McpServerDefinition
        {
            ServerId = (vm.ServerId ?? "").Trim(),
            DisplayName = (vm.DisplayName ?? "").Trim(),
            Transport = (vm.Transport ?? "").Trim(),
            Endpoint = (vm.Endpoint ?? "").Trim(),
            Command = (vm.Command ?? "").Trim(),
            AuthType = (vm.AuthType ?? "").Trim(),
            AuthConfigJson = (vm.AuthConfigJson ?? "").Trim(),
            MetadataJson = (vm.MetadataJson ?? "").Trim(),
            Enabled = vm.Enabled,
        };

        foreach (var arg in McpRegistryPageViewModel.ParseArguments(vm.Arguments))
            server.Arguments.Add(arg);

        vm.StatusText = "Saving MCP server...";
        var saveResponse = await apiClient.UpsertMcpServerAsync(host, port, server, ct: ct);
        if (saveResponse == null)
        {
            vm.StatusText = "Failed to save MCP server.";
            return CommandResult.Fail("Failed to save MCP server.");
        }

        if (!saveResponse.Success)
        {
            vm.StatusText = saveResponse.Message;
            return CommandResult.Fail(saveResponse.Message);
        }

        var saved = saveResponse.Server;

        // Inline refresh after save
        var listResponse = await apiClient.ListMcpServersAsync(host, port, ct: ct);
        if (listResponse != null)
        {
            vm.Servers.Clear();
            foreach (var s in listResponse.Servers.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
                vm.Servers.Add(s);
        }

        if (saved != null)
            vm.PopulateFromServer(saved);

        vm.StatusText = $"Saved '{saved?.ServerId}'.";
        return CommandResult.Ok();
    }
}
