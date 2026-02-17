using System.Diagnostics;
using System.Net;

namespace RemoteAgent.Desktop.Infrastructure;

public sealed record LocalServerProbeResult(
    bool IsRunning,
    bool IsManagedByApp,
    bool CanApplyAction,
    string RecommendedActionLabel,
    string Message);

public sealed record LocalServerActionResult(
    bool Success,
    string Message);

public interface ILocalServerManager
{
    Task<LocalServerProbeResult> ProbeAsync(CancellationToken cancellationToken = default);
    Task<LocalServerActionResult> StartAsync(CancellationToken cancellationToken = default);
    Task<LocalServerActionResult> StopAsync(CancellationToken cancellationToken = default);
}

public sealed class LocalServerManager : ILocalServerManager
{
    private readonly object _gate = new();
    private Process? _managedProcess;
    private static readonly Uri LocalServiceUri = new("http://127.0.0.1:5243/");

    public async Task<LocalServerProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
    {
        var isRunning = await IsServiceReachableAsync(cancellationToken);
        var isManaged = IsManagedProcessRunning();
        if (isRunning)
        {
            var message = isManaged
                ? "Local server is running (managed by desktop app)."
                : "Local server is running.";
            return new LocalServerProbeResult(
                IsRunning: true,
                IsManagedByApp: isManaged,
                CanApplyAction: isManaged,
                RecommendedActionLabel: isManaged ? "Stop Local Server" : "Local Server Running",
                Message: message);
        }

        return new LocalServerProbeResult(
            IsRunning: false,
            IsManagedByApp: isManaged,
            CanApplyAction: true,
            RecommendedActionLabel: "Start Local Server",
            Message: "Local server is not running.");
    }

    public async Task<LocalServerActionResult> StartAsync(CancellationToken cancellationToken = default)
    {
        if (await IsServiceReachableAsync(cancellationToken))
            return new LocalServerActionResult(true, "Local server is already running.");

        var projectPath = TryResolveServiceProjectPath();
        if (projectPath == null)
            return new LocalServerActionResult(false, "Could not locate RemoteAgent.Service project path.");

        Process? process;
        lock (_gate)
        {
            if (_managedProcess is { HasExited: false })
                return new LocalServerActionResult(true, "Local server process is already managed by desktop app.");

            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    WorkingDirectory = Path.GetDirectoryName(projectPath)!,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.StartInfo.ArgumentList.Add("run");
            process.StartInfo.ArgumentList.Add("--project");
            process.StartInfo.ArgumentList.Add(projectPath);
            _managedProcess = process;
        }

        process.Start();
        var started = await WaitUntilAsync(IsServiceReachableAsync, timeoutMs: 15000, pollMs: 250, cancellationToken);
        if (!started)
            return new LocalServerActionResult(false, "Local server did not respond on http://127.0.0.1:5243/ within timeout.");

        return new LocalServerActionResult(true, "Local server started.");
    }

    public async Task<LocalServerActionResult> StopAsync(CancellationToken cancellationToken = default)
    {
        Process? processToStop;
        lock (_gate)
        {
            processToStop = _managedProcess;
            _managedProcess = null;
        }

        if (processToStop is { HasExited: false })
        {
            try
            {
                processToStop.Kill(entireProcessTree: true);
                processToStop.WaitForExit(5000);
            }
            catch
            {
                // best effort stop
            }
        }

        var stillRunning = await IsServiceReachableAsync(cancellationToken);
        if (stillRunning)
            return new LocalServerActionResult(false, "Local server appears to be running but is not managed by this desktop app.");

        return new LocalServerActionResult(true, "Local server stopped.");
    }

    private bool IsManagedProcessRunning()
    {
        lock (_gate)
        {
            return _managedProcess is { HasExited: false };
        }
    }

    private static async Task<bool> IsServiceReachableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            using var response = await client.GetAsync(LocalServiceUri, cancellationToken);
            return response.StatusCode == HttpStatusCode.OK;
        }
        catch
        {
            return false;
        }
    }

    private static string? TryResolveServiceProjectPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var current = new DirectoryInfo(baseDir);
        for (var i = 0; i < 10 && current != null; i++)
        {
            var candidate = Path.Combine(current.FullName, "src", "RemoteAgent.Service", "RemoteAgent.Service.csproj");
            if (File.Exists(candidate))
                return candidate;
            current = current.Parent;
        }

        var cwdCandidate = Path.Combine(Environment.CurrentDirectory, "src", "RemoteAgent.Service", "RemoteAgent.Service.csproj");
        return File.Exists(cwdCandidate) ? cwdCandidate : null;
    }

    private static async Task<bool> WaitUntilAsync(
        Func<CancellationToken, Task<bool>> condition,
        int timeoutMs,
        int pollMs,
        CancellationToken cancellationToken)
    {
        var startedAt = Environment.TickCount64;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (await condition(cancellationToken))
                return true;

            if (Environment.TickCount64 - startedAt > timeoutMs)
                return false;

            await Task.Delay(pollMs, cancellationToken);
        }

        return false;
    }
}
