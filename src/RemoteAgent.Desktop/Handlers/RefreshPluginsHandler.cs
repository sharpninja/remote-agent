using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class RefreshPluginsHandler(IServerCapacityClient client)
    : IRequestHandler<RefreshPluginsRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(
        RefreshPluginsRequest request,
        CancellationToken cancellationToken = default)
    {
        var config = await client.GetPluginsAsync(
            request.Host, request.Port, request.ApiKey, cancellationToken);

        if (config is null)
        {
            request.Workspace.PluginStatus = "Failed to load plugin configuration.";
            return CommandResult.Fail("Plugin configuration endpoint returned no data.");
        }

        request.Workspace.ConfiguredPluginAssemblies.Clear();
        foreach (var assembly in config.ConfiguredAssemblies)
            request.Workspace.ConfiguredPluginAssemblies.Add(assembly);

        request.Workspace.LoadedPluginRunnerIds.Clear();
        foreach (var runnerId in config.LoadedRunnerIds)
            request.Workspace.LoadedPluginRunnerIds.Add(runnerId);

        request.Workspace.PluginAssembliesText = string.Join(Environment.NewLine, request.Workspace.ConfiguredPluginAssemblies);
        request.Workspace.PluginStatus = $"Loaded {request.Workspace.ConfiguredPluginAssemblies.Count} configured assembly(ies), {request.Workspace.LoadedPluginRunnerIds.Count} loaded runner(s).";

        return CommandResult.Ok();
    }
}
