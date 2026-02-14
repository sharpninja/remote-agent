using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;

namespace RemoteAgent.Service.Agents;

/// <summary>Starts an agent by spawning a process (Agent:Command). Default implementation for TR-10.1.</summary>
public sealed class ProcessAgentRunner(IOptions<AgentOptions> options) : IAgentRunner
{
    public Task<IAgentSession?> StartAsync(
        string? command,
        string? arguments,
        string sessionId,
        StreamWriter? logWriter,
        CancellationToken cancellationToken = default)
    {
        var cmd = command ?? options.Value.Command;
        if (string.IsNullOrWhiteSpace(cmd))
            return Task.FromResult<IAgentSession?>(null);

        var psi = new ProcessStartInfo
        {
            FileName = cmd,
            Arguments = arguments ?? options.Value.Arguments ?? "",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        var process = Process.Start(psi);
        if (process == null)
            return Task.FromResult<IAgentSession?>(null);

        return Task.FromResult<IAgentSession?>(new ProcessAgentSession(process));
    }
}
