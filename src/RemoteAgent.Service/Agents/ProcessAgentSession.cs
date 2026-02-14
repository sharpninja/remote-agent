using System.Diagnostics;
using System.Text;

namespace RemoteAgent.Service.Agents;

/// <summary>Agent session backed by a single process (TR-3.3, TR-3.4). Wraps <see cref="Process"/> stdin/stdout/stderr for line-oriented forwarding.</summary>
/// <remarks>Created by <see cref="ProcessAgentRunner"/>. Disposing or calling <see cref="Stop"/> kills the process tree.</remarks>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-3)</see>
public sealed class ProcessAgentSession : IAgentSession
{
    private readonly Process _process;
    private bool _disposed;

    /// <summary>Creates a session wrapping the given process.</summary>
    /// <param name="process">The started agent process with redirected stdin, stdout, and stderr.</param>
    public ProcessAgentSession(Process process)
    {
        _process = process;
    }

    /// <inheritdoc />
    public Task SendInputAsync(string text, CancellationToken cancellationToken = default)
    {
        if (_process.StandardInput == null)
            return Task.CompletedTask;
        _process.StandardInput.WriteLine(text);
        return _process.StandardInput.FlushAsync(cancellationToken);
    }

    /// <inheritdoc />
    public StreamReader StandardOutput => _process.StandardOutput ?? throw new InvalidOperationException("No stdout.");
    /// <inheritdoc />
    public StreamReader StandardError => _process.StandardError ?? throw new InvalidOperationException("No stderr.");
    /// <inheritdoc />
    public bool HasExited => _process.HasExited;

    /// <inheritdoc />
    public void Stop()
    {
        try
        {
            _process.Kill(entireProcessTree: true);
        }
        catch
        {
            // ignore
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        try { _process.Dispose(); } catch { }
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
