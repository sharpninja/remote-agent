namespace RemoteAgent.App.Logic;

/// <summary>
/// Abstraction over platform-specific key/value preferences storage.
/// </summary>
public interface IAppPreferences
{
    string Get(string key, string defaultValue);
    void Set(string key, string value);
}
