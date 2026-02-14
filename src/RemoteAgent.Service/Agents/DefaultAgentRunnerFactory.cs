using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace RemoteAgent.Service.Agents;

/// <summary>Selects <see cref="IAgentRunner"/> by <see cref="AgentOptions.RunnerId"/> from a registry of named runners (TR-10.1).</summary>
/// <remarks>Falls back to the "process" runner if the configured <see cref="AgentOptions.RunnerId"/> is not in the registry. Registry is built from default process runner plus plugins (FR-8.1).</remarks>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-10)</see>
public sealed class DefaultAgentRunnerFactory : IAgentRunnerFactory
{
    private readonly IReadOnlyDictionary<string, IAgentRunner> _runners;
    private readonly string _runnerId;

    /// <summary>Creates the factory with the given options and runner registry.</summary>
    public DefaultAgentRunnerFactory(
        IOptions<AgentOptions> options,
        IReadOnlyDictionary<string, IAgentRunner> runners)
    {
        _runnerId = options.Value.RunnerId ?? "process";
        _runners = runners;
    }

    /// <inheritdoc />
    public IAgentRunner GetRunner()
    {
        if (_runners.TryGetValue(_runnerId, out var runner))
            return runner;
        return _runners["process"]; // fallback to default
    }
}
