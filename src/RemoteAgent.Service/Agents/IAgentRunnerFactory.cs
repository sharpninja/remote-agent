namespace RemoteAgent.Service.Agents;

/// <summary>Returns the agent runner selected by configuration (default or plugin).</summary>
public interface IAgentRunnerFactory
{
    /// <summary>Gets the configured runner (e.g. "process" or a plugin runner id).</summary>
    IAgentRunner GetRunner();
}
