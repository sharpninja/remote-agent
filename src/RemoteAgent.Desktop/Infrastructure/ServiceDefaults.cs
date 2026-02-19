namespace RemoteAgent.Desktop.Infrastructure;

/// <summary>
/// Platform-calculated defaults for connecting to the Remote Agent service.
/// Windows uses port 5244 (the native Windows service port); all other platforms use 5243.
/// </summary>
public static class ServiceDefaults
{
    /// <summary>Default service port for the current platform.</summary>
    public static readonly int Port = OperatingSystem.IsWindows() ? 5244 : 5243;

    /// <summary>Default service port as a string (for ViewModel binding).</summary>
    public static readonly string PortString = Port.ToString();

    /// <summary>Default local service base URI for the current platform.</summary>
    public static readonly Uri LocalServiceUri = new($"http://127.0.0.1:{Port}/");
}
