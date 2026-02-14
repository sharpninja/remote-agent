using System.Diagnostics;
using System.Text;
using RemoteAgent.Proto;

namespace RemoteAgent.Service.Services;

/// <summary>Runs bash or PowerShell scripts and returns stdout and stderr on completion (FR-9.1, FR-9.2).</summary>
/// <remarks>Invoked by the service when the client sends a <see cref="Proto.ScriptRequest"/>. Output is streamed back to the app as chat messages.</remarks>
/// <example><code>
/// var (stdout, stderr) = await ScriptRunner.RunAsync("/path/to/script.sh", ScriptType.Bash, ct);
/// if (!string.IsNullOrEmpty(stdout))
///     await responseStream.WriteAsync(new ServerMessage { Output = stdout }, ct);
/// </code></example>
/// <see href="https://sharpninja.github.io/remote-agent/functional-requirements.html">Functional requirements (FR-9)</see>
public static class ScriptRunner
{
    /// <summary>Runs the script or command and returns combined stdout and stderr when the process exits.</summary>
    /// <param name="pathOrCommand">Path to script file or command string (passed to bash -c or pwsh -Command).</param>
    /// <param name="scriptType">Bash or Pwsh.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Stdout and stderr as strings (trimmed).</returns>
    public static async Task<(string Stdout, string Stderr)> RunAsync(
        string pathOrCommand,
        ScriptType scriptType,
        CancellationToken cancellationToken = default)
    {
        var (fileName, arguments) = GetProcessArgs(pathOrCommand, scriptType);
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                CreateNoWindow = true,
            }
        };
        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return (stdout.TrimEnd(), stderr.TrimEnd());
    }

    private static (string FileName, string Arguments) GetProcessArgs(string pathOrCommand, ScriptType scriptType)
    {
        return scriptType switch
        {
            ScriptType.Bash => ("bash", $"-c \"{EscapeBash(pathOrCommand)}\""),
            ScriptType.Pwsh => ("pwsh", $"-NoProfile -Command \"{EscapePwsh(pathOrCommand)}\""),
            _ => ("bash", $"-c \"{EscapeBash(pathOrCommand)}\""),
        };
    }

    private static string EscapeBash(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string EscapePwsh(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "`\"");
    }
}
