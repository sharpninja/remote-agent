using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RemoteAgent.Service.Agents;

namespace RemoteAgent.Service;

/// <summary>Loads plugin assemblies and discovers IAgentRunner implementations (TR-10.2).</summary>
public static class PluginLoader
{
    /// <summary>Builds a registry of named runners: "process" plus any from plugin assemblies.</summary>
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
