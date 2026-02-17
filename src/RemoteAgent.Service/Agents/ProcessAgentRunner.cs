using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Options;

namespace RemoteAgent.Service.Agents;

/// <summary>Starts an agent by spawning a process (FR-1.2, TR-3.2, TR-10.1). Uses <see cref="AgentOptions.Command"/> and <see cref="AgentOptions.Arguments"/>.</summary>
/// <remarks>Default runner when <see cref="AgentOptions.RunnerId"/> is "process" or not set. When Command is not configured, uses "agent" on non-Windows and "copilot" on Windows. For a quick test, set Command to <c>/bin/cat</c> to echo lines back.</remarks>
/// <example><code>
/// // appsettings: "Agent": { "Command": "/path/to/agent", "RunnerId": "process" }
/// var session = await processAgentRunner.StartAsync(null, null, "sess-1", logWriter, ct);
/// </code></example>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-3, TR-10)</see>
public sealed class ProcessAgentRunner(IOptions<AgentOptions> options) : IAgentRunner
{
    /// <summary>Default agent command when not configured: "copilot" on Windows, "agent" otherwise.</summary>
    private static string DefaultCommand => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "copilot" : "agent";

    /// <inheritdoc />
    public Task<IAgentSession?> StartAsync(
        string? command,
        string? arguments,
        string sessionId,
        StreamWriter? logWriter,
        CancellationToken cancellationToken = default)
    {
        var cmd = command ?? options.Value.Command;
        if (string.IsNullOrWhiteSpace(cmd))
            cmd = DefaultCommand;
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
        try
        {
            var process = Process.Start(psi);
            if (process == null)
                return Task.FromResult<IAgentSession?>(null);
            return Task.FromResult<IAgentSession?>(new ProcessAgentSession(process));
        }
        catch (Exception)
        {
            return Task.FromResult<IAgentSession?>(null);
        }
    }
}
