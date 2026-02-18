namespace RemoteAgent.App.Logic;

/// <summary>
/// Abstracts navigation for mobile shell-based apps, enabling testability.
/// </summary>
public interface INavigationService
{
    Task NavigateToAsync(string route);
    void CloseFlyout();
}
