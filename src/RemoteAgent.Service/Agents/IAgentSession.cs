namespace RemoteAgent.Service.Agents;

/// <summary>Represents a running agent session. Input can be sent; stdout/stderr are read via the exposed readers.</summary>
public interface IAgentSession : IDisposable
{
    /// <summary>Sends a line of text to the agent's stdin.</summary>
    Task SendInputAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>Agent stdout (one line at a time via ReadLineAsync).</summary>
    StreamReader StandardOutput { get; }

    /// <summary>Agent stderr (one line at a time via ReadLineAsync).</summary>
    StreamReader StandardError { get; }

    /// <summary>Whether the agent process has exited.</summary>
    bool HasExited { get; }

    /// <summary>Stops the agent (e.g. kill process).</summary>
    void Stop();
}
