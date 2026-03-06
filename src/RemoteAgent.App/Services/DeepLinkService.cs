using RemoteAgent.App.Logic;

namespace RemoteAgent.App.Services;

/// <summary>
/// Singleton that routes deep-link URIs (e.g. from <c>remoteagent://pair?…</c>) to subscribers.
/// If a URI arrives before any subscriber has registered it is held as a pending link and
/// delivered the moment the first subscriber calls <see cref="Subscribe"/>.
/// </summary>
public sealed class DeepLinkService : IDeepLinkService
{
    private readonly object _lock = new();
    private Action<string>? _handlers;
    private string? _pending;

    public void Subscribe(Action<string> handler)
    {
        string? pending;
        lock (_lock)
        {
            _handlers += handler;
            pending = _pending;
            _pending = null;
        }
        if (pending != null)
            handler(pending);
    }

    public void Dispatch(string rawUri)
    {
        Action<string>? snapshot;
        lock (_lock)
        {
            snapshot = _handlers;
            if (snapshot == null)
            {
                _pending = rawUri;
                return;
            }
        }
        snapshot(rawUri);
    }
}
