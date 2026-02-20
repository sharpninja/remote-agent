using Microsoft.Extensions.Logging;
using RemoteAgent.App.Handlers;
using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Logic.Handlers;
using RemoteAgent.App.Logic.Requests;
using RemoteAgent.App.Logic.ViewModels;
using RemoteAgent.App.Requests;
using RemoteAgent.App.Services;
using RemoteAgent.App.ViewModels;

namespace RemoteAgent.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "remote-agent.db");
		builder.Services.AddSingleton<ILocalMessageStore>(_ => new LocalMessageStore(dbPath));
		builder.Services.AddSingleton<ISessionStore>(_ => new LocalSessionStore(dbPath));
		builder.Services.AddSingleton<IAgentGatewayClient, AgentGatewayClientService>();

		builder.Services.AddSingleton<IServerApiClient, ServerApiClientAdapter>();
		builder.Services.AddSingleton<IAppPreferences, MauiAppPreferences>();
		builder.Services.AddSingleton<IAttachmentPicker, MauiAttachmentPicker>();
		builder.Services.AddSingleton<INotificationService, PlatformNotificationServiceAdapter>();
		builder.Services.AddSingleton<IDeepLinkService, DeepLinkService>();

		builder.Services.AddSingleton<IRequestDispatcher, ServiceProviderRequestDispatcher>();
		builder.Services.AddTransient<IRequestHandler<ConnectMobileSessionRequest, CommandResult>, ConnectMobileSessionHandler>();
		builder.Services.AddTransient<IRequestHandler<DisconnectMobileSessionRequest, CommandResult>, DisconnectMobileSessionHandler>();
		builder.Services.AddTransient<IRequestHandler<CreateMobileSessionRequest, CommandResult>, CreateMobileSessionHandler>();
		builder.Services.AddTransient<IRequestHandler<TerminateMobileSessionRequest, CommandResult>, TerminateMobileSessionHandler>();
		builder.Services.AddTransient<IRequestHandler<SendMobileMessageRequest, CommandResult>, SendMobileMessageHandler>();
		builder.Services.AddTransient<IRequestHandler<SendMobileAttachmentRequest, CommandResult>, SendMobileAttachmentHandler>();
		builder.Services.AddTransient<IRequestHandler<ArchiveMobileMessageRequest, CommandResult>, ArchiveMobileMessageHandler>();
		builder.Services.AddTransient<IRequestHandler<UsePromptTemplateRequest, CommandResult>, UsePromptTemplateHandler>();
		builder.Services.AddTransient<IRequestHandler<LoadMcpServersRequest, CommandResult>, LoadMcpServersHandler>();
		builder.Services.AddTransient<IRequestHandler<SaveMcpServerRequest, CommandResult>, SaveMcpServerHandler>();
		builder.Services.AddTransient<IRequestHandler<DeleteMcpServerRequest, CommandResult>, DeleteMcpServerHandler>();
		builder.Services.AddTransient<IRequestHandler<ScanQrCodeRequest, CommandResult>, ScanQrCodeHandler>();

		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddSingleton<MainPageViewModel>();
		builder.Services.AddSingleton<ISessionCommandBus>(sp => sp.GetRequiredService<MainPageViewModel>());
		builder.Services.AddSingleton<ISessionListProvider, SessionListProviderAdapter>();
		builder.Services.AddSingleton<INavigationService, MauiNavigationService>();
		builder.Services.AddSingleton<IConnectionModeSelector>(sp =>
			new MauiConnectionModeSelector(() => sp.GetService<MainPage>()));
		builder.Services.AddSingleton<IAgentSelector>(sp =>
			new MauiAgentSelector(() => sp.GetService<MainPage>()));
		builder.Services.AddSingleton<IPromptTemplateSelector>(sp =>
			new MauiPromptTemplateSelector(() => sp.GetService<MainPage>()));
		builder.Services.AddSingleton<IPromptVariableProvider>(sp =>
			new MauiPromptVariableProvider(() => sp.GetService<MainPage>()));
		builder.Services.AddSingleton<ISessionTerminationConfirmation>(sp =>
			new MauiSessionTerminationConfirmation(() => sp.GetService<MainPage>()));
		builder.Services.AddSingleton<IQrCodeScanner, MauiQrCodeScanner>();

		builder.Services.AddSingleton<McpRegistryPageViewModel>(sp =>
			new McpRegistryPageViewModel(
				sp.GetRequiredService<IServerApiClient>(),
				sp.GetRequiredService<IAppPreferences>(),
				new MauiDeleteMcpServerConfirmation(() => sp.GetService<McpRegistryPage>()),
				sp.GetRequiredService<IRequestDispatcher>()));
		builder.Services.AddSingleton<McpRegistryPage>();
		builder.Services.AddSingleton<SettingsPage>();
		builder.Services.AddSingleton<AccountManagementPage>();
		builder.Services.AddSingleton<AppShellViewModel>();
		builder.Services.AddSingleton<AppShell>();

		return builder.Build();
	}
}
