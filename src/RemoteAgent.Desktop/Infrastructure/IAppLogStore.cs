namespace RemoteAgent.Desktop.Infrastructure;

/// <summary>Thread-safe store for app-process log entries captured via the custom ILogger provider.</summary>
public interface IAppLogStore
{
    void Add(AppLogEntry entry);
    IReadOnlyList<AppLogEntry> GetAll();
    void Clear();
}
