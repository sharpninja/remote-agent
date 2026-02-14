namespace RemoteAgent.Service.Agents;

/// <summary>Returns the agent runner selected by configuration (TR-10.1). Resolves <see cref="AgentOptions.RunnerId"/> to an <see cref="IAgentRunner"/> from the registry.</summary>
/// <remarks>Default is the process runner; plugins add additional runners via <see cref="PluginsOptions"/> (FR-8.1).</remarks>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-10)</see>
public interface IAgentRunnerFactory
{
    /// <summary>Gets the configured runner (e.g. "process" or a plugin runner id from <see cref="AgentOptions.RunnerId"/>).</summary>
    /// <returns>The runner to use for starting agent sessions.</returns>
    IAgentRunner GetRunner();
}
