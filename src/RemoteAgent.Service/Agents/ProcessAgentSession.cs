using System.Diagnostics;
using System.Text;

namespace RemoteAgent.Service.Agents;

/// <summary>Agent session backed by a single process. Wraps Process stdin/stdout/stderr.</summary>
public sealed class ProcessAgentSession : IAgentSession
{
    private readonly Process _process;
    private bool _disposed;

    public ProcessAgentSession(Process process)
    {
        _process = process;
    }

    public Task SendInputAsync(string text, CancellationToken cancellationToken = default)
    {
        if (_process.StandardInput == null)
            return Task.CompletedTask;
        _process.StandardInput.WriteLine(text);
        return _process.StandardInput.FlushAsync(cancellationToken);
    }

    public StreamReader StandardOutput => _process.StandardOutput ?? throw new InvalidOperationException("No stdout.");
    public StreamReader StandardError => _process.StandardError ?? throw new InvalidOperationException("No stderr.");
    public bool HasExited => _process.HasExited;

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

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        try { _process.Dispose(); } catch { }
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
