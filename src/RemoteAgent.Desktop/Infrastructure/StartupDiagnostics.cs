using System.Text;
using System.Diagnostics;

namespace RemoteAgent.Desktop.Infrastructure;

internal static class StartupDiagnostics
{
    private static readonly object Sync = new();
    private static bool initialized;
    private static bool fileLoggingEnabled = true;
    private static bool fileLoggingWarningEmitted;

    internal static string LogFilePath { get; private set; } = string.Empty;
    internal static string TraceLogFilePath { get; private set; } = string.Empty;

    internal static void Initialize(string[] args)
    {
        lock (Sync)
        {
            if (initialized)
                return;

            try
            {
                var dataDir = ResolveWritableDataDirectory();
                LogFilePath = Path.Combine(dataDir, "startup-debug.log");
                TraceLogFilePath = Path.Combine(dataDir, "startup-trace.log");
            }
            catch
            {
                TryConfigureFallbackLogPath();
            }
            initialized = true;
        }

        ConfigureTraceListeners();

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var exception = e.ExceptionObject as Exception;
            LogException("Unhandled exception", exception ?? new Exception("Unknown unhandled exception object."));
            Log($"Runtime terminating: {e.IsTerminating}");
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogException("Unobserved task exception", e.Exception);
            e.SetObserved();
        };

        Log("Startup diagnostics initialized.");
        Log($"Args: {string.Join(' ', args)}");
        LogEnvironment();
    }

    internal static void Log(string message)
    {
        var line = $"[{DateTimeOffset.UtcNow:O}] {message}";
        Console.WriteLine(line);

        lock (Sync)
        {
            if (!initialized || string.IsNullOrWhiteSpace(LogFilePath))
                return;

            if (!fileLoggingEnabled)
                return;

            try
            {
                File.AppendAllText(LogFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                TryConfigureFallbackLogPath();
                if (fileLoggingEnabled)
                    return;

                EmitFileLoggingDisabledWarning(ex.Message);
            }
        }
    }

    internal static void LogException(string context, Exception exception)
    {
        Log($"{context}: {exception}");
        if (exception.InnerException is not null)
            Log($"Inner exception: {exception.InnerException}");
    }

    private static void LogEnvironment()
    {
        Log($"User: {Environment.UserName}");
        Log($"Machine: {Environment.MachineName}");
        Log($"OS: {Environment.OSVersion}");
        Log($"ProcessId: {Environment.ProcessId}");
        Log($"CurrentDirectory: {Environment.CurrentDirectory}");
        Log($".NET: {Environment.Version}");
        Log($"DISPLAY={Environment.GetEnvironmentVariable("DISPLAY") ?? "<null>"}");
        Log($"WAYLAND_DISPLAY={Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") ?? "<null>"}");
        Log($"XDG_SESSION_TYPE={Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") ?? "<null>"}");
        Log($"XAUTHORITY={Environment.GetEnvironmentVariable("XAUTHORITY") ?? "<null>"}");
        Log($"GDK_BACKEND={Environment.GetEnvironmentVariable("GDK_BACKEND") ?? "<null>"}");
        Log($"HOME={Environment.GetEnvironmentVariable("HOME") ?? "<null>"}");
    }

    internal static string ResolveWritableDataDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var localShareDir = Path.Combine(appData, "RemoteAgent.Desktop");
        if (TryEnsureWritableDirectory(localShareDir))
            return localShareDir;

        var userTempDir = Path.Combine(Path.GetTempPath(), $"RemoteAgent.Desktop-{Environment.UserName}");
        if (TryEnsureWritableDirectory(userTempDir))
            return userTempDir;

        return Path.GetTempPath();
    }

    private static void TryConfigureFallbackLogPath()
    {
        try
        {
            var fallbackDir = ResolveWritableDataDirectory();
            var fallbackPath = Path.Combine(fallbackDir, "RemoteAgent.Desktop-startup-debug.log");
            File.AppendAllText(fallbackPath, string.Empty, Encoding.UTF8);
            LogFilePath = fallbackPath;
            TraceLogFilePath = Path.Combine(fallbackDir, "RemoteAgent.Desktop-startup-trace.log");
            fileLoggingEnabled = true;
        }
        catch (Exception ex)
        {
            fileLoggingEnabled = false;
            EmitFileLoggingDisabledWarning(ex.Message);
        }
    }

    private static bool TryEnsureWritableDirectory(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var probePath = Path.Combine(directory, $".write-probe-{Environment.ProcessId}-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probePath, "ok", Encoding.UTF8);
            File.Delete(probePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void EmitFileLoggingDisabledWarning(string reason)
    {
        if (fileLoggingWarningEmitted)
            return;

        fileLoggingWarningEmitted = true;
        Console.WriteLine($"[{DateTimeOffset.UtcNow:O}] StartupDiagnostics file logging disabled: {reason}");
    }

    private static void ConfigureTraceListeners()
    {
        try
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out, "console"));

            if (!string.IsNullOrWhiteSpace(TraceLogFilePath))
                Trace.Listeners.Add(new TextWriterTraceListener(TraceLogFilePath, "file"));

            Trace.AutoFlush = true;
            Log($"Trace listeners configured. Trace log path: {TraceLogFilePath}");
        }
        catch (Exception ex)
        {
            EmitFileLoggingDisabledWarning($"Trace listener setup failed: {ex.Message}");
        }
    }
}
