namespace RemoteAgent.Service.Agents;

/// <summary>Abstraction for starting an agent session (TR-10.1, FR-8.1). Implementations: process spawn (default) or plugin-backed agents.</summary>
/// <remarks>The service uses this to spawn the Cursor agent (FR-1.2) or a plugin-provided agent. Implementations forward stdin and stream stdout/stderr (TR-3.3, TR-3.4).</remarks>
/// <example><code>
/// var runner = agentRunnerFactory.GetRunner();
/// var session = await runner.StartAsync(cmd, args, sessionId, logWriter, ct);
/// if (session != null)
///     await session.SendInputAsync("Hello", ct);
/// </code></example>
/// <see href="https://sharpninja.github.io/remote-agent/functional-requirements.html">Functional requirements</see>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-10)</see>
public interface IAgentRunner
{
    /// <summary>Starts an agent session. Returns null if the runner cannot start (e.g. command not configured).</summary>
    /// <param name="command">Agent command (e.g. executable path). May be ignored by plugin runners.</param>
    /// <param name="arguments">Optional arguments. May be ignored by plugin runners.</param>
    /// <param name="sessionId">Session identifier for logging (TR-3.6).</param>
    /// <param name="logWriter">Optional session log writer (FR-1.5).</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>An active session, or null if the runner could not start.</returns>
    Task<IAgentSession?> StartAsync(
        string? command,
        string? arguments,
        string sessionId,
        StreamWriter? logWriter,
        CancellationToken cancellationToken = default);
}
