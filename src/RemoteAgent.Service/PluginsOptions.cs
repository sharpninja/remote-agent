namespace RemoteAgent.Service;

/// <summary>Configuration for plugin discovery (TR-10.2). Assemblies to load that contribute IAgentRunner implementations.</summary>
public class PluginsOptions
{
    public const string SectionName = "Plugins";

    /// <summary>Paths to plugin assemblies (e.g. relative to app or absolute). Loaded at startup; types implementing IAgentRunner are registered by full type name.</summary>
    public List<string> Assemblies { get; set; } = new();
}
