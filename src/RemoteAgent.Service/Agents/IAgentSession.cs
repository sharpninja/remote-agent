namespace RemoteAgent.Service.Agents;

/// <summary>Represents a running agent session (FR-7.1). Input is sent to the agent; stdout/stderr are read via the exposed readers (TR-3.3, TR-3.4).</summary>
/// <remarks>Call <see cref="Stop"/> or <see cref="IDisposable.Dispose"/> when the session ends (e.g. client disconnect).</remarks>
/// <example><code>
/// string? line;
/// while ((line = await session.StandardOutput.ReadLineAsync(ct)) != null)
///     await responseStream.WriteAsync(new ServerMessage { Output = line }, ct);
/// </code></example>
/// <see href="https://sharpninja.github.io/remote-agent/functional-requirements.html">Functional requirements (FR-7)</see>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-3)</see>
public interface IAgentSession : IDisposable
{
    /// <summary>Sends a line of text to the agent's stdin (FR-1.3, TR-3.3).</summary>
    /// <param name="text">Line to forward to the agent.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    Task SendInputAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>Agent stdout. Read line-by-line with <c>ReadLineAsync</c> and stream to the client (FR-1.4).</summary>
    StreamReader StandardOutput { get; }

    /// <summary>Agent stderr. Read line-by-line and send as <c>ServerMessage.Error</c> (TR-3.4).</summary>
    StreamReader StandardError { get; }

    /// <summary>Whether the agent process has exited. When true, do not call <see cref="SendInputAsync"/>.</summary>
    bool HasExited { get; }

    /// <summary>Stops the agent (e.g. kill process). Call on disconnect (FR-7.1).</summary>
    void Stop();
}
