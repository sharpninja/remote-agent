using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;

namespace RemoteAgent.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var services = IPlatformApplication.Current?.Services
            ?? throw new InvalidOperationException("Application service provider is unavailable.");
        var appShell = services.GetRequiredService<AppShell>();
        return new Window(appShell);
    }
}
