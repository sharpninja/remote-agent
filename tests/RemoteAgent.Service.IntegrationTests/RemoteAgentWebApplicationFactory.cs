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
    private readonly string _dataDirectory;
    private readonly string _logDirectory;

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
        _dataDirectory = Path.Combine(Path.GetTempPath(), "remote-agent-it", Guid.NewGuid().ToString("N"));
        _logDirectory = Path.Combine(Path.GetTempPath(), "remote-agent-it-logs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dataDirectory);
        Directory.CreateDirectory(_logDirectory);
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
                        ["Agent:DataDirectory"] = _dataDirectory,
                        ["Agent:LogDirectory"] = _logDirectory,
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
                    app.Use(async (context, next) =>
                    {
                        // TestServer may leave RemoteIpAddress null/"unknown", which breaks loopback auth checks.
                        context.Connection.RemoteIpAddress ??= IPAddress.Loopback;
                        context.Connection.LocalIpAddress ??= IPAddress.Loopback;
                        await next();
                    });
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        Program.ConfigureEndpoints(endpoints);
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
        services.AddSingleton<AuthUserService>();
        services.AddGrpc(options => options.EnableDetailedErrors = true);
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
        try
        {
            if (Directory.Exists(_dataDirectory))
                Directory.Delete(_dataDirectory, recursive: true);
            if (Directory.Exists(_logDirectory))
                Directory.Delete(_logDirectory, recursive: true);
        }
        catch
        {
            // Best-effort cleanup for temporary integration-test storage.
        }
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

public sealed class ApiKeyLoopbackAllowedWebApplicationFactory : RemoteAgentWebApplicationFactory
{
    public ApiKeyLoopbackAllowedWebApplicationFactory() : base(GetCommand(), GetArguments(), "process", apiKey: "test-key", allowUnauthenticatedLoopback: true) { }

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

