namespace RemoteAgent.Service;

/// <summary>Configuration for plugin discovery (TR-10.2, FR-8.1). Assemblies to load that contribute <see cref="Agents.IAgentRunner"/> implementations.</summary>
/// <remarks>Plugin assemblies are loaded at startup. Exported types implementing <see cref="Agents.IAgentRunner"/> are registered in the runner registry by full type name and can be selected via <see cref="AgentOptions.RunnerId"/>.</remarks>
/// <example><code>
/// "Plugins": { "Assemblies": [ "./plugins/MyAgentPlugin.dll" ] }
/// </code></example>
/// <see href="https://sharpninja.github.io/remote-agent/functional-requirements.html">Functional requirements (FR-8.1)</see>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-10)</see>
public class PluginsOptions
{
    /// <summary>Configuration section name: "Plugins".</summary>
    public const string SectionName = "Plugins";

    /// <summary>Paths to plugin assemblies (relative to app base or absolute). Loaded at startup; types implementing <see cref="Agents.IAgentRunner"/> are registered by full type name.</summary>
    public List<string> Assemblies { get; set; } = new();
}
