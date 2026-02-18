using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Handlers;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Logging;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Desktop.ViewModels;
using RemoteAgent.Desktop.Views;
using SessionCapacitySnapshot = RemoteAgent.Desktop.Infrastructure.SessionCapacitySnapshot;

namespace RemoteAgent.Desktop;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = default!;

    public override void Initialize()
    {
        StartupDiagnostics.Log("App.Initialize start.");
        try
        {
            AvaloniaXamlLoader.Load(this);
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogException("App.Initialize failed during AvaloniaXamlLoader.Load", ex);
            throw;
        }
        StartupDiagnostics.Log("App.Initialize complete.");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        StartupDiagnostics.Log("OnFrameworkInitializationCompleted start.");
        try
        {
            var serviceCollection = new ServiceCollection();
            StartupDiagnostics.Log("Configuring DI services.");
            ConfigureServices(serviceCollection);
            Services = serviceCollection.BuildServiceProvider();
            StartupDiagnostics.Log("DI container built.");

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                StartupDiagnostics.Log("Resolving MainWindow from services.");
                desktop.MainWindow = Services.GetRequiredService<MainWindow>();
                StartupDiagnostics.Log("MainWindow assigned to desktop lifetime.");
            }
            else
            {
                StartupDiagnostics.Log("ApplicationLifetime is not classic desktop.");
            }
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogException("Framework initialization failed", ex);
            throw;
        }

        base.OnFrameworkInitializationCompleted();
        StartupDiagnostics.Log("OnFrameworkInitializationCompleted complete.");
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var dataDir = StartupDiagnostics.ResolveWritableDataDirectory();
        var dbPath = Path.Combine(dataDir, "desktop-structured-logs.db");
        var registryPath = Path.Combine(dataDir, "desktop-server-registry.db");
        StartupDiagnostics.Log($"Data directory: {dataDir}");
        StartupDiagnostics.Log($"Log DB path: {dbPath}");
        StartupDiagnostics.Log($"Registry DB path: {registryPath}");

        services.AddSingleton<IServerCapacityClient, ServerCapacityClient>();
        services.AddSingleton<ILocalServerManager, LocalServerManager>();
        services.AddSingleton<IServerRegistrationStore>(_ => new LiteDbServerRegistrationStore(registryPath));
        services.AddScoped<CurrentServerContext>();
        services.AddSingleton<IDesktopStructuredLogStore>(_ => new DesktopStructuredLogStore(dbPath));
        services.AddSingleton<IStructuredLogClient, StructuredLogClient>();
        services.AddTransient<IAgentSessionClient, AgentSessionClient>();
        services.AddTransient<DesktopSessionViewModel>();
        services.AddScoped<IDesktopSessionViewModelFactory, DesktopSessionViewModelFactory>();
        services.AddScoped<ServerWorkspaceViewModel>();
        services.AddSingleton<IServerWorkspaceFactory, ServerWorkspaceFactory>();
        services.AddSingleton<IAppLogStore, InMemoryAppLogStore>();
        services.AddSingleton<ILoggerProvider, AppLoggerProvider>();
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<IFileSaveDialogService, AvaloniaFileSaveDialogService>();
        services.AddSingleton<AppLogViewModel>();
        services.AddSingleton<IRequestDispatcher, ServiceProviderRequestDispatcher>();
        services.AddTransient<IConnectionSettingsDialogService, AvaloniaConnectionSettingsDialogService>();

        services.AddTransient<IRequestHandler<SetManagementSectionRequest, Unit>, SetManagementSectionHandler>();
        services.AddTransient<IRequestHandler<ExpandStatusLogPanelRequest, Unit>, ExpandStatusLogPanelHandler>();
        services.AddTransient<IRequestHandler<SaveServerRegistrationRequest, CommandResult<ServerRegistration>>, SaveServerRegistrationHandler>();
        services.AddTransient<IRequestHandler<RemoveServerRegistrationRequest, CommandResult>, RemoveServerRegistrationHandler>();
        services.AddTransient<IRequestHandler<CheckLocalServerRequest, CommandResult<LocalServerProbeResult>>, CheckLocalServerHandler>();
        services.AddTransient<IRequestHandler<ApplyLocalServerActionRequest, CommandResult<LocalServerProbeResult>>, ApplyLocalServerActionHandler>();
        services.AddTransient<IRequestHandler<OpenNewSessionRequest, CommandResult>, OpenNewSessionHandler>();
        services.AddTransient<IRequestHandler<CheckSessionCapacityRequest, CommandResult>, CheckSessionCapacityHandler>();
        services.AddTransient<IRequestHandler<RefreshOpenSessionsRequest, CommandResult>, RefreshOpenSessionsHandler>();
        services.AddTransient<IRequestHandler<TerminateOpenServerSessionRequest, CommandResult>, TerminateOpenServerSessionHandler>();
        services.AddTransient<IRequestHandler<RefreshSecurityDataRequest, CommandResult>, RefreshSecurityDataHandler>();
        services.AddTransient<IRequestHandler<BanPeerRequest, CommandResult>, BanPeerHandler>();
        services.AddTransient<IRequestHandler<UnbanPeerRequest, CommandResult>, UnbanPeerHandler>();
        services.AddTransient<IRequestHandler<RefreshAuthUsersRequest, CommandResult>, RefreshAuthUsersHandler>();
        services.AddTransient<IRequestHandler<SaveAuthUserRequest, CommandResult>, SaveAuthUserHandler>();
        services.AddTransient<IRequestHandler<DeleteAuthUserRequest, CommandResult>, DeleteAuthUserHandler>();
        services.AddTransient<IRequestHandler<RefreshPluginsRequest, CommandResult>, RefreshPluginsHandler>();
        services.AddTransient<IRequestHandler<SavePluginsRequest, CommandResult>, SavePluginsHandler>();
        services.AddTransient<IRequestHandler<RefreshMcpRegistryRequest, CommandResult>, RefreshMcpRegistryHandler>();
        services.AddTransient<IRequestHandler<SaveMcpServerRequest, CommandResult>, SaveMcpServerHandler>();
        services.AddTransient<IRequestHandler<Requests.DeleteMcpServerRequest, CommandResult>, DeleteMcpServerHandler>();
        services.AddTransient<IRequestHandler<SaveAgentMcpMappingRequest, CommandResult>, SaveAgentMcpMappingHandler>();
        services.AddTransient<IRequestHandler<RefreshPromptTemplatesRequest, CommandResult>, RefreshPromptTemplatesHandler>();
        services.AddTransient<IRequestHandler<SavePromptTemplateRequest, CommandResult>, SavePromptTemplateHandler>();
        services.AddTransient<IRequestHandler<Requests.DeletePromptTemplateRequest, CommandResult>, DeletePromptTemplateHandler>();
        services.AddTransient<IRequestHandler<Requests.SeedSessionContextRequest, CommandResult>, SeedSessionContextHandler>();
        services.AddTransient<IRequestHandler<Requests.CreateDesktopSessionRequest, CommandResult>, CreateDesktopSessionHandler>();
        services.AddTransient<IRequestHandler<Requests.TerminateDesktopSessionRequest, CommandResult>, TerminateDesktopSessionHandler>();
        services.AddTransient<IRequestHandler<Requests.SendDesktopMessageRequest, CommandResult>, SendDesktopMessageHandler>();
        services.AddTransient<IRequestHandler<Requests.StartLogMonitoringRequest, CommandResult<Requests.StartLogMonitoringResult>>, StartLogMonitoringHandler>();
        services.AddTransient<IRequestHandler<Requests.ClearAppLogRequest, CommandResult>, ClearAppLogHandler>();
        services.AddTransient<IRequestHandler<Requests.SaveAppLogRequest, CommandResult>, SaveAppLogHandler>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
        StartupDiagnostics.Log("Service registrations completed.");
    }
}
