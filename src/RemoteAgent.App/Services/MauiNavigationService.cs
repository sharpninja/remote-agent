using RemoteAgent.App.Logic;

namespace RemoteAgent.App.Services;

public sealed class MauiNavigationService : INavigationService
{
    public async Task NavigateToAsync(string route)
    {
        if (Shell.Current != null)
            await Shell.Current.GoToAsync(route);
    }

    public void CloseFlyout()
    {
        if (Shell.Current != null)
            Shell.Current.FlyoutIsPresented = false;
    }
}
