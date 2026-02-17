using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using RemoteAgent.App.Logic;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Logging;
using RemoteAgent.Desktop.ViewModels;
using RemoteAgent.Desktop.Views;

namespace RemoteAgent.Desktop;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = default!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dataDir = Path.Combine(appData, "RemoteAgent.Desktop");
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "desktop-structured-logs.db");
        var registryPath = Path.Combine(dataDir, "desktop-server-registry.db");

        services.AddSingleton<IServerCapacityClient, ServerCapacityClient>();
        services.AddSingleton<IServerRegistrationStore>(_ => new LiteDbServerRegistrationStore(registryPath));
        services.AddScoped<CurrentServerContext>();
        services.AddSingleton<IDesktopStructuredLogStore>(_ => new DesktopStructuredLogStore(dbPath));
        services.AddTransient<IAgentSessionClient, AgentSessionClient>();
        services.AddTransient<DesktopSessionViewModel>();
        services.AddScoped<IDesktopSessionViewModelFactory, DesktopSessionViewModelFactory>();
        services.AddScoped<ServerWorkspaceViewModel>();
        services.AddSingleton<IServerWorkspaceFactory, ServerWorkspaceFactory>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
    }
}
