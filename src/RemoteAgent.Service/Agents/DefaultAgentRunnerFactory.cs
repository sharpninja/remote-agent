using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;

namespace RemoteAgent.Service.Agents;

/// <summary>Selects <see cref="IAgentRunner"/> by <see cref="AgentOptions.RunnerId"/> from a registry of named runners (TR-10.1).</summary>
/// <remarks>When <see cref="AgentOptions.RunnerId"/> is not set: Linux (and other non-Windows) defaults to "process" (Cursor/agent CLI); Windows defaults to "copilot-windows". Falls back to "process" if the configured runner is not in the registry.</remarks>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-10)</see>
public sealed class DefaultAgentRunnerFactory : IAgentRunnerFactory
{
    private readonly IReadOnlyDictionary<string, IAgentRunner> _runners;
    private readonly string _runnerId;

    /// <summary>Default runner when not configured: process (agent/Cursor CLI) on Linux, copilot-windows on Windows.</summary>
    private static string DefaultRunnerId =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? CopilotWindowsAgentRunner.RunnerId : "process";

    /// <summary>Creates the factory with the given options and runner registry.</summary>
    public DefaultAgentRunnerFactory(
        IOptions<AgentOptions> options,
        IReadOnlyDictionary<string, IAgentRunner> runners)
    {
        _runnerId = string.IsNullOrWhiteSpace(options.Value.RunnerId) ? DefaultRunnerId : options.Value.RunnerId;
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
