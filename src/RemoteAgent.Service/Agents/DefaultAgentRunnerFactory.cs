using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace RemoteAgent.Service.Agents;

/// <summary>Selects IAgentRunner by Agent:RunnerId from a registry of named runners.</summary>
public sealed class DefaultAgentRunnerFactory : IAgentRunnerFactory
{
    private readonly IReadOnlyDictionary<string, IAgentRunner> _runners;
    private readonly string _runnerId;

    public DefaultAgentRunnerFactory(
        IOptions<AgentOptions> options,
        IReadOnlyDictionary<string, IAgentRunner> runners)
    {
        _runnerId = options.Value.RunnerId ?? "process";
        _runners = runners;
    }

    public IAgentRunner GetRunner()
    {
        if (_runners.TryGetValue(_runnerId, out var runner))
            return runner;
        return _runners["process"]; // fallback to default
    }
}
