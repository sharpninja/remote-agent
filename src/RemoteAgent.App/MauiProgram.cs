using Microsoft.Extensions.Logging;
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
		builder.Services.AddSingleton<AgentGatewayClientService>();
		builder.Services.AddSingleton<MainPageViewModel>();
		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddSingleton<McpRegistryPage>();
		builder.Services.AddSingleton<AppShell>();

		return builder.Build();
	}
}
