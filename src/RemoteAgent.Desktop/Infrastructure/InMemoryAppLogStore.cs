using System.Collections.Concurrent;

namespace RemoteAgent.Desktop.Infrastructure;

/// <summary>In-memory, thread-safe store for captured app log entries (bounded to 10 000 entries).</summary>
public sealed class InMemoryAppLogStore : IAppLogStore
{
    private const int MaxEntries = 10_000;
    private readonly ConcurrentQueue<AppLogEntry> _entries = new();

    public void Add(AppLogEntry entry)
    {
        _entries.Enqueue(entry);
        while (_entries.Count > MaxEntries)
            _entries.TryDequeue(out _);
    }

    public IReadOnlyList<AppLogEntry> GetAll() => _entries.ToArray();

    public void Clear()
    {
        while (_entries.TryDequeue(out _)) { }
    }
}
