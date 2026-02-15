using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Options;

namespace RemoteAgent.Service.Agents;

/// <summary>Agent strategy for GitHub Copilot CLI on Windows (TR-10.1, FR-8.1). Resolves the Copilot executable (e.g. from PATH after winget/npm install) and spawns it as a process.</summary>
/// <remarks>Use <c>RunnerId: "copilot-windows"</c> to select this runner. On non-Windows platforms, <see cref="StartAsync"/> returns null. When Command is not set, uses "copilot" (expect it on PATH, e.g. from <c>winget install GitHub.Copilot</c> or <c>npm install -g @github/copilot</c>).</remarks>
/// <see href="https://docs.github.com/en/copilot/how-tos/copilot-cli">GitHub Copilot CLI</see>
public sealed class CopilotWindowsAgentRunner(IOptions<AgentOptions> options) : IAgentRunner
{
    /// <summary>Runner id for configuration: "copilot-windows".</summary>
    public const string RunnerId = "copilot-windows";

    private static string DefaultCommand => "copilot";

    /// <inheritdoc />
    public Task<IAgentSession?> StartAsync(
        string? command,
        string? arguments,
        string sessionId,
        StreamWriter? logWriter,
        CancellationToken cancellationToken = default)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Task.FromResult<IAgentSession?>(null);

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
