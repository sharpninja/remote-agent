using System.Diagnostics;
using System.Text;
using RemoteAgent.Proto;

namespace RemoteAgent.Service.Services;

/// <summary>Runs bash or pwsh scripts and returns stdout/stderr on completion (FR-9.1, FR-9.2).</summary>
public static class ScriptRunner
{
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
