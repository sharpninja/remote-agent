using RemoteAgent.App.Logic;

namespace RemoteAgent.App.Services;

/// <summary>
/// MAUI implementation of <see cref="IAppPreferences"/> that delegates to <see cref="Preferences.Default"/>.
/// </summary>
public sealed class MauiAppPreferences : IAppPreferences
{
    public string Get(string key, string defaultValue) => Preferences.Default.Get(key, defaultValue);
    public void Set(string key, string value) => Preferences.Default.Set(key, value);
}
