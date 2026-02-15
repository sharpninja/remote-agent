namespace RemoteAgent.Service;

/// <summary>Configuration for the agent process and storage (TR-3.2, TR-3.6, TR-10.1, TR-11.1, TR-11.2). Bound from the "Agent" section in appsettings.</summary>
/// <remarks>Used by the service to spawn the Cursor agent (FR-1.2), write session logs (FR-1.5), and configure the data directory for LiteDB and media.</remarks>
/// <example><code>
/// // appsettings.json
/// "Agent": {
///   "Command": "/path/to/cursor-agent",
///   "Arguments": "",
///   "LogDirectory": "/var/log/remote-agent",
///   "RunnerId": "" (empty = OS default: process on Linux, copilot-windows on Windows),
///   "DataDirectory": "./data"
/// }
/// </code></example>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements</see>
public class AgentOptions
{
    /// <summary>Configuration section name: "Agent".</summary>
    public const string SectionName = "Agent";

    /// <summary>Command to spawn the agent (FR-1.2, TR-3.2). For a quick test use <c>/bin/cat</c> to echo lines back.</summary>
    public string? Command { get; set; }

    /// <summary>Optional arguments passed to the agent command.</summary>
    public string? Arguments { get; set; }

    /// <summary>Directory for session log files (FR-1.5, TR-3.6). Defaults to temp. Files are named <c>remote-agent-{sessionId}.log</c>.</summary>
    public string? LogDirectory { get; set; }

    /// <summary>Runner to use: "process" (default), "copilot-windows" (GitHub Copilot CLI on Windows), or a plugin runner id from <see cref="PluginsOptions"/> (TR-10.1, FR-8.1).</summary>
    public string? RunnerId { get; set; }

    /// <summary>Data directory for LiteDB and uploaded media (TR-11.1, TR-11.2). Defaults to <c>./data</c>. Media is stored under <c>data/media/</c>.</summary>
    public string? DataDirectory { get; set; }
}
