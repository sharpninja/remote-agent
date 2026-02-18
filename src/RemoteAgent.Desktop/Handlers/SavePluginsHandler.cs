using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class SavePluginsHandler(IServerCapacityClient client)
    : IRequestHandler<SavePluginsRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(
        SavePluginsRequest request,
        CancellationToken cancellationToken = default)
    {
        var config = await client.UpdatePluginsAsync(
            request.Host, request.Port, request.Assemblies, request.ApiKey, cancellationToken);

        if (config is null)
        {
            request.Workspace.PluginStatus = "Failed to save plugin configuration.";
            return CommandResult.Fail("Failed to save plugin configuration.");
        }

        request.Workspace.PluginStatus = string.IsNullOrWhiteSpace(config.Message)
            ? "Plugin configuration updated."
            : config.Message;

        // Refresh plugins inline
        var refreshed = await client.GetPluginsAsync(request.Host, request.Port, request.ApiKey, cancellationToken);
        if (refreshed != null)
        {
            request.Workspace.ConfiguredPluginAssemblies.Clear();
            foreach (var assembly in refreshed.ConfiguredAssemblies)
                request.Workspace.ConfiguredPluginAssemblies.Add(assembly);

            request.Workspace.LoadedPluginRunnerIds.Clear();
            foreach (var runnerId in refreshed.LoadedRunnerIds)
                request.Workspace.LoadedPluginRunnerIds.Add(runnerId);

            request.Workspace.PluginAssembliesText = string.Join(Environment.NewLine, request.Workspace.ConfiguredPluginAssemblies);
            request.Workspace.PluginStatus = $"Loaded {request.Workspace.ConfiguredPluginAssemblies.Count} configured assembly(ies), {request.Workspace.LoadedPluginRunnerIds.Count} loaded runner(s).";
        }

        return CommandResult.Ok();
    }
}
