namespace RemoteAgent.Service;

public class AgentOptions
{
    public const string SectionName = "Agent";

    /// <summary>Command to spawn the Cursor agent (e.g. path to script or executable).</summary>
    public string? Command { get; set; }

    /// <summary>Optional arguments for the agent command.</summary>
    public string? Arguments { get; set; }

    /// <summary>Directory to write session log files. Defaults to temp.</summary>
    public string? LogDirectory { get; set; }
}
