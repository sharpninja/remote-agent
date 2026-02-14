using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RemoteAgent.Service.Agents;

namespace RemoteAgent.Service;

/// <summary>Loads plugin assemblies and discovers <see cref="IAgentRunner"/> implementations (TR-10.2, FR-8.1).</summary>
/// <remarks>Reads <see cref="PluginsOptions.Assemblies"/> and loads each assembly; exported types implementing <see cref="IAgentRunner"/> are instantiated via the service provider and registered by full type name. The "process" runner is always included.</remarks>
/// <example><code>
/// // In Program.cs after building the service provider:
/// var registry = PluginLoader.BuildRunnerRegistry(serviceProvider);
/// services.AddSingleton(registry);
/// </code></example>
/// <see href="https://sharpninja.github.io/remote-agent/functional-requirements.html">Functional requirements (FR-8)</see>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-10)</see>
public static class PluginLoader
{
    /// <summary>Builds a registry of named runners: "process" (default) plus any from <see cref="PluginsOptions.Assemblies"/>.</summary>
    /// <param name="serviceProvider">Used to resolve <see cref="PluginsOptions"/> and to create plugin runner instances.</param>
    /// <returns>Dictionary of runner id to <see cref="IAgentRunner"/> (key is "process" or full type name of plugin type).</returns>
    public static IReadOnlyDictionary<string, IAgentRunner> BuildRunnerRegistry(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<PluginsOptions>>().Value;
        var dict = new Dictionary<string, IAgentRunner>(StringComparer.OrdinalIgnoreCase)
        {
            ["process"] = serviceProvider.GetRequiredService<ProcessAgentRunner>()
        };

        var baseDir = AppContext.BaseDirectory;
        foreach (var assemblyPath in options.Assemblies ?? [])
        {
            if (string.IsNullOrWhiteSpace(assemblyPath)) continue;
            var fullPath = Path.IsPathRooted(assemblyPath) ? assemblyPath : Path.GetFullPath(assemblyPath, baseDir);
            if (!File.Exists(fullPath)) continue;

            try
            {
                var assembly = Assembly.LoadFrom(fullPath);
                foreach (var type in assembly.GetExportedTypes())
                {
                    if (type.IsAbstract || !typeof(IAgentRunner).IsAssignableFrom(type)) continue;
                    try
                    {
                        var instance = (IAgentRunner)ActivatorUtilities.CreateInstance(serviceProvider, type);
                        var key = type.FullName ?? type.Name;
                        dict[key] = instance;
                    }
                    catch
                    {
                        // skip type if creation fails (e.g. missing ctor deps)
                    }
                }
            }
            catch
            {
                // skip assembly if load fails
            }
        }

        return dict;
    }
}
