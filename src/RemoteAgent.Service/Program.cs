using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RemoteAgent.Service;
using RemoteAgent.Service.Agents;
using RemoteAgent.Service.Services;
using RemoteAgent.Service.Storage;

namespace RemoteAgent.Service;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureServices(builder.Services, builder.Configuration);

        var app = builder.Build();
        ConfigureEndpoints(app);
        app.Run();
    }

    // Required for Microsoft.AspNetCore.Mvc.Testing host bootstrap in integration tests.
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureServices((context, services) =>
                {
                    ConfigureServices(services, context.Configuration);
                });

                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => ConfigureEndpoints(endpoints));
                });
            });
    }

    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AgentOptions>(configuration.GetSection(AgentOptions.SectionName));
        services.Configure<PluginsOptions>(configuration.GetSection(PluginsOptions.SectionName));
        services.AddSingleton<ProcessAgentRunner>();
        services.AddSingleton<CopilotWindowsAgentRunner>();
        services.AddSingleton<IReadOnlyDictionary<string, IAgentRunner>>(sp => PluginLoader.BuildRunnerRegistry(sp));
        services.AddSingleton<IAgentRunnerFactory, DefaultAgentRunnerFactory>();
        services.AddSingleton<ILocalStorage, LiteDbLocalStorage>();
        services.AddSingleton<MediaStorageService>();
        services.AddGrpc();
    }

    public static void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<AgentGatewayService>();
        endpoints.MapGet("/", () => "RemoteAgent gRPC service. Use the Android app to connect.");
    }
}

