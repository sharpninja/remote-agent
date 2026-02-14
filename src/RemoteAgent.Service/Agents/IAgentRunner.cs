namespace RemoteAgent.Service.Agents;

/// <summary>Starts and provides an agent session. Implementations: process spawn (default), plugin-backed agents.</summary>
public interface IAgentRunner
{
    /// <summary>Starts an agent session. Returns null if the runner cannot start (e.g. command not configured).</summary>
    /// <param name="command">Agent command (e.g. executable path). May be ignored by plugin runners.</param>
    /// <param name="arguments">Optional arguments. May be ignored by plugin runners.</param>
    /// <param name="sessionId">Session identifier for logging.</param>
    /// <param name="logWriter">Optional session log writer.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    Task<IAgentSession?> StartAsync(
        string? command,
        string? arguments,
        string sessionId,
        StreamWriter? logWriter,
        CancellationToken cancellationToken = default);
}
