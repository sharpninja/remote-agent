using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RemoteAgent.Service;
using RemoteAgent.Service.Agents;
using RemoteAgent.Service.Services;
using RemoteAgent.Service.Storage;
using System.Runtime.InteropServices;

namespace RemoteAgent.Service.IntegrationTests;

/// <summary>In-process test host for gRPC integration tests with configurable Agent settings.</summary>
public class RemoteAgentWebApplicationFactory : IDisposable
{
    private readonly IHost _host;
    private readonly TestServer _server;

    public RemoteAgentWebApplicationFactory(string? command = null, string? arguments = null, string? runnerId = null)
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseEnvironment("Testing");
                webBuilder.UseTestServer();
                webBuilder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Agent:Command"] = command ?? "",
                        ["Agent:Arguments"] = arguments ?? "",
                        ["Agent:RunnerId"] = runnerId ?? "",
                        ["Agent:LogDirectory"] = Path.GetTempPath(),
                        ["Agent:AllowUnauthenticatedLoopback"] = "true",
                        ["Agent:ApiKey"] = ""
                    });
                });

                webBuilder.ConfigureServices((context, services) =>
                {
                    ConfigureTestServices(services, context.Configuration);
                });

                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGrpcService<AgentGatewayService>();
                        endpoints.MapGet("/", async ctx => await ctx.Response.WriteAsync("RemoteAgent gRPC service. Use the Android app to connect."));
                    });
                });
            })
            .Start();

        _server = _host.GetTestServer();
    }

    private static void ConfigureTestServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AgentOptions>(configuration.GetSection(AgentOptions.SectionName));
        services.Configure<PluginsOptions>(configuration.GetSection(PluginsOptions.SectionName));

        services.AddSingleton<ProcessAgentRunner>();
        services.AddSingleton<CopilotWindowsAgentRunner>();
        services.AddSingleton<IReadOnlyDictionary<string, IAgentRunner>>(sp => new Dictionary<string, IAgentRunner>(StringComparer.OrdinalIgnoreCase)
        {
            ["process"] = sp.GetRequiredService<ProcessAgentRunner>(),
            [CopilotWindowsAgentRunner.RunnerId] = sp.GetRequiredService<CopilotWindowsAgentRunner>()
        });

        services.AddSingleton<IAgentRunnerFactory, DefaultAgentRunnerFactory>();
        services.AddSingleton<ILocalStorage, LiteDbLocalStorage>();
        services.AddSingleton<MediaStorageService>();
        services.AddGrpc();
    }

    public HttpMessageHandler CreateHandler() => _server.CreateHandler();

    public Uri BaseAddress => _server.BaseAddress;

    public void Dispose()
    {
        _host.Dispose();
    }
}

public sealed class NoCommandWebApplicationFactory : RemoteAgentWebApplicationFactory
{
    public NoCommandWebApplicationFactory() : base("none", "", "process") { }
}

public sealed class CatWebApplicationFactory : RemoteAgentWebApplicationFactory
{
    public CatWebApplicationFactory() : base(GetCommand(), GetArguments(), "process") { }

    private static string GetCommand()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd" : "/bin/cat";
    }

    private static string GetArguments()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "/c more" : "";
    }
}

public sealed class SleepWebApplicationFactory : RemoteAgentWebApplicationFactory
{
    public SleepWebApplicationFactory() : base(GetCommand(), GetArguments(), "process") { }

    private static string GetCommand()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd" : "/bin/sleep";
    }

    private static string GetArguments()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "/c timeout /t 600 /nobreak" : "600";
    }
}

