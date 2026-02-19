using Avalonia;
using Avalonia.Logging;
using RemoteAgent.Desktop.Infrastructure;

namespace RemoteAgent.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        StartupDiagnostics.Initialize(args);
        StartupDiagnostics.Log("Program.Main entered.");

        try
        {
            StartupDiagnostics.Log("Starting Avalonia classic desktop lifetime.");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            StartupDiagnostics.Log("Avalonia lifetime exited normally.");
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogException("Fatal startup exception", ex);
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        StartupDiagnostics.Log("Configuring Avalonia AppBuilder.");
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace(LogEventLevel.Debug);
    }
}
