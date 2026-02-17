using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Grpc.Core;
using RemoteAgent.Service;
using RemoteAgent.Service.Agents;
using RemoteAgent.Service.Logging;
using RemoteAgent.Service.Services;
using RemoteAgent.Service.Storage;
using System.Runtime.InteropServices;
using System.Net;
using Microsoft.Extensions.Options;

namespace RemoteAgent.Service.IntegrationTests;

/// <summary>In-process test host for gRPC integration tests with configurable Agent settings.</summary>
public class RemoteAgentWebApplicationFactory : IDisposable
{
    private readonly IHost _host;
    private readonly TestServer _server;
    private readonly string _apiKey;

    public RemoteAgentWebApplicationFactory(
        string? command = null,
        string? arguments = null,
        string? runnerId = null,
        string? apiKey = null,
        bool allowUnauthenticatedLoopback = true,
        int? maxConcurrentConnectionsPerPeer = null,
        int? maxConnectionAttemptsPerWindow = null,
        int? maxClientMessagesPerWindow = null,
        int? clientMessageWindowSeconds = null,
        int? maxConcurrentSessions = null,
        int? processAgentMaxConcurrentSessions = null)
    {
        _apiKey = apiKey ?? "";
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
                        ["Agent:AllowUnauthenticatedLoopback"] = allowUnauthenticatedLoopback ? "true" : "false",
                        ["Agent:ApiKey"] = _apiKey,
                        ["Agent:MaxConcurrentConnectionsPerPeer"] = (maxConcurrentConnectionsPerPeer ?? 8).ToString(),
                        ["Agent:MaxConnectionAttemptsPerWindow"] = (maxConnectionAttemptsPerWindow ?? 20).ToString(),
                        ["Agent:MaxClientMessagesPerWindow"] = (maxClientMessagesPerWindow ?? 120).ToString(),
                        ["Agent:ClientMessageWindowSeconds"] = (clientMessageWindowSeconds ?? 5).ToString(),
                        ["Agent:MaxConcurrentSessions"] = (maxConcurrentSessions ?? 50).ToString(),
                        ["Agent:AgentConcurrentSessionLimits:process"] = (processAgentMaxConcurrentSessions ?? 50).ToString()
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
                        endpoints.MapGet("/api/sessions/capacity", (
                            HttpContext context,
                            IOptions<AgentOptions> options,
                            SessionCapacityService sessionCapacity) =>
                        {
                            if (!IsAuthorizedHttp(context, options.Value))
                                return Results.Unauthorized();

                            var agentId = context.Request.Query["agentId"].ToString();
                            var status = sessionCapacity.GetStatus(agentId);
                            return Results.Ok(status);
                        });
                        endpoints.MapGet("/api/sessions/open", (
                            HttpContext context,
                            IOptions<AgentOptions> options,
                            SessionCapacityService sessionCapacity) =>
                        {
                            if (!IsAuthorizedHttp(context, options.Value))
                                return Results.Unauthorized();

                            return Results.Ok(sessionCapacity.ListOpenSessions());
                        });
                        endpoints.MapPost("/api/sessions/{sessionId}/terminate", (
                            HttpContext context,
                            IOptions<AgentOptions> options,
                            SessionCapacityService sessionCapacity,
                            string sessionId) =>
                        {
                            if (!IsAuthorizedHttp(context, options.Value))
                                return Results.Unauthorized();

                            var success = sessionCapacity.TryTerminateSession(sessionId, out var reason);
                            return Results.Ok(new { success, message = success ? "Session terminated." : reason });
                        });
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
        services.AddSingleton<StructuredLogService>();
        services.AddSingleton<MediaStorageService>();
        services.AddSingleton<PluginConfigurationService>();
        services.AddSingleton<AgentMcpConfigurationService>();
        services.AddSingleton<PromptTemplateService>();
        services.AddSingleton<ConnectionProtectionService>();
        services.AddSingleton<SessionCapacityService>();
        services.AddGrpc();
    }

    public HttpMessageHandler CreateHandler() => _server.CreateHandler();

    public Uri BaseAddress => _server.BaseAddress;

    public Metadata CreateAuthHeadersOrNull()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return new Metadata();
        return new Metadata { { "x-api-key", _apiKey } };
    }

    public void Dispose()
    {
        _host.Dispose();
    }

    private static bool IsAuthorizedHttp(HttpContext context, AgentOptions options)
    {
        var configuredApiKey = options.ApiKey?.Trim();
        if (!string.IsNullOrEmpty(configuredApiKey))
        {
            var provided = context.Request.Headers["x-api-key"].FirstOrDefault();
            return string.Equals(configuredApiKey, provided, StringComparison.Ordinal);
        }

        if (!options.AllowUnauthenticatedLoopback)
            return false;

        var remote = context.Connection.RemoteIpAddress;
        return remote != null && IPAddress.IsLoopback(remote);
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

public sealed class ApiKeyWebApplicationFactory : RemoteAgentWebApplicationFactory
{
    public ApiKeyWebApplicationFactory() : base(GetCommand(), GetArguments(), "process", apiKey: "test-key", allowUnauthenticatedLoopback: false) { }

    private static string GetCommand()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd" : "/bin/cat";
    }

    private static string GetArguments()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "/c more" : "";
    }
}

public sealed class ConnectionRateLimitedWebApplicationFactory : RemoteAgentWebApplicationFactory
{
    public ConnectionRateLimitedWebApplicationFactory()
        : base(GetCommand(), GetArguments(), "process", maxConcurrentConnectionsPerPeer: 1) { }

    private static string GetCommand()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd" : "/bin/cat";
    }

    private static string GetArguments()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "/c more" : "";
    }
}

public sealed class SessionLimitedWebApplicationFactory : RemoteAgentWebApplicationFactory
{
    public SessionLimitedWebApplicationFactory()
        : base(GetCommand(), GetArguments(), "process", maxConcurrentSessions: 1, processAgentMaxConcurrentSessions: 1) { }

    private static string GetCommand()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd" : "/bin/cat";
    }

    private static string GetArguments()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "/c more" : "";
    }
}

