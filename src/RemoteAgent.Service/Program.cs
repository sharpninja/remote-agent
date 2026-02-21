using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RemoteAgent.Service;
using RemoteAgent.Service.Agents;
using RemoteAgent.Service.Logging;
using RemoteAgent.Service.Services;
using RemoteAgent.Service.Storage;
using RemoteAgent.Service.Web;

namespace RemoteAgent.Service;

public partial class Program
{
    public static void Main(string[] args)
    {
        // Catch any unhandled exception on any thread and write it to the Windows Event Log.
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseWindowsService(options =>
        {
            options.ServiceName = "Remote Agent Service";
        });

        // Route ILogger output to the Windows Application Event Log on Windows so
        // all Information+ messages appear alongside the service lifecycle events.
        if (OperatingSystem.IsWindows())
            ConfigureWindowsEventLog(builder);

        // Select the listen URL for the current platform from appsettings.json PlatformUrls.
        // Use ConfigureKestrel (explicit Listen* call) rather than UseUrls so that the
        // ASPNETCORE_URLS environment variable — set by 'dotnet run' from launchSettings.json
        // applicationUrl — cannot override the platform-specific port.
        var platformKey = OperatingSystem.IsWindows() ? "PlatformUrls:Windows" : "PlatformUrls:Linux";
        var platformUrl = builder.Configuration[platformKey];
        int webPort = 0;
        if (!string.IsNullOrWhiteSpace(platformUrl) && Uri.TryCreate(platformUrl, UriKind.Absolute, out var listenUri))
        {
            var listenPort = listenUri.Port;
            webPort = int.Parse("1" + listenPort);   // e.g. 5244 → 15244
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(listenPort, o => o.Protocols = HttpProtocols.Http2);
                options.ListenAnyIP(webPort,    o => o.Protocols = HttpProtocols.Http1AndHttp2);
            });
        }

        ConfigureServices(builder.Services, builder.Configuration);

        WebApplication app;
        try
        {
            app = builder.Build();
        }
        catch (Exception ex)
        {
            WriteEventLog(isError: true, 1001,
                $"Remote Agent Service failed during startup (build phase):\n{ex}");
            throw;
        }

        ConfigureEndpoints(app, webPort);

        // Write a success event once the host is fully started and listening.
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            var urls   = string.Join(", ", app.Urls.DefaultIfEmpty(platformUrl ?? "unknown"));
            logger.LogInformation("Remote Agent Service listening on {Urls}", urls);
            WriteEventLog(isError: false, 1000,
                $"Remote Agent Service started successfully. Listening on: {urls}");

            // Log agent configuration so misconfigurations are obvious on startup.
            var agentOpts = app.Services.GetRequiredService<IOptions<AgentOptions>>().Value;
            var resolvedRunner = string.IsNullOrWhiteSpace(agentOpts.RunnerId)
                ? (OperatingSystem.IsWindows() ? "copilot-windows" : "process")
                : agentOpts.RunnerId;
            var resolvedCommand = string.IsNullOrWhiteSpace(agentOpts.Command)
                ? (OperatingSystem.IsWindows() ? "copilot" : "agent")
                : agentOpts.Command;
            var commandIsConfigured = !string.IsNullOrWhiteSpace(agentOpts.Command);
            logger.LogInformation(
                "Agent configuration — RunnerId={RunnerId} (resolved: {ResolvedRunner}), " +
                "Command={Command} (resolved: {ResolvedCommand}, explicit: {CommandIsConfigured}), " +
                "Arguments={Arguments}",
                agentOpts.RunnerId ?? "(empty)",
                resolvedRunner,
                agentOpts.Command ?? "(empty)",
                resolvedCommand,
                commandIsConfigured,
                agentOpts.Arguments ?? "(empty)");
            if (!commandIsConfigured)
                logger.LogWarning(
                    "Agent:Command is not configured in appsettings.json. " +
                    "The service will attempt to use the default command '{DefaultCommand}' which may not exist. " +
                    "Set Agent:Command to the path of your agent executable (e.g. claude, cursor, or /bin/cat for testing).",
                    resolvedCommand);
        });

        try
        {
            app.Run();
        }
        catch (Exception ex)
        {
            WriteEventLog(isError: true, 1001,
                $"Remote Agent Service terminated with an unhandled exception:\n{ex}");
            throw;
        }
    }

    /// <summary>
    /// Writes an entry to the Windows Application Event Log.
    /// No-ops on non-Windows platforms.
    /// Event source is created on first use (requires administrator privileges).
    /// </summary>
    /// <remarks>
    /// Event IDs:
    ///   1000 — service started successfully
    ///   1001 — startup or runtime fatal error
    ///   1002 — unhandled exception on background thread
    /// </remarks>
    private static void WriteEventLog(bool isError, int eventId, string message)
    {
        if (!OperatingSystem.IsWindows()) return;
        WriteEventLogCore(
            isError ? EventLogEntryType.Error : EventLogEntryType.Information,
            eventId, message);
    }

    /// <summary>Registers the Windows Application Event Log as an ILogger provider.
    /// Always called from within an <see cref="OperatingSystem.IsWindows()"/> guard.</summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void ConfigureWindowsEventLog(WebApplicationBuilder builder)
    {
        builder.Logging.AddEventLog(settings =>
        {
            settings.SourceName = "Remote Agent Service";
        });
    }

    /// <summary>Windows-only inner implementation — always called from within an
    /// <see cref="OperatingSystem.IsWindows()"/> guard.</summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void WriteEventLogCore(EventLogEntryType type, int eventId, string message)
    {
        const string Source = "Remote Agent Service";
        const string Log    = "Application";
        try
        {
            if (!EventLog.SourceExists(Source))
                EventLog.CreateEventSource(Source, Log);
            EventLog.WriteEntry(Source, message, type, eventId);
        }
        catch
        {
            // Best-effort only: if the Event Log is inaccessible (e.g. insufficient
            // privileges to create the source), silently continue rather than masking
            // the original exception.
        }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var message = e.ExceptionObject is Exception ex
            ? $"Unhandled exception in Remote Agent Service (IsTerminating={e.IsTerminating}):\n{ex}"
            : $"Unhandled non-exception object in Remote Agent Service: {e.ExceptionObject}";
        WriteEventLog(isError: true, 1002, message);
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
        services.AddSingleton<StructuredLogService>();
        services.AddSingleton<MediaStorageService>();
        services.AddSingleton<FilePathDetectorService>();
        services.AddSingleton<PluginConfigurationService>();
        services.AddSingleton<AgentMcpConfigurationService>();
        services.AddSingleton<PromptTemplateService>();
        services.AddSingleton<ConnectionProtectionService>();
        services.AddSingleton<SessionCapacityService>();
        services.AddSingleton<AuthUserService>();
        services.AddSingleton<PairingSessionService>();
        services.AddGrpc();
    }

    public static void ConfigureEndpoints(IEndpointRouteBuilder endpoints, int webPort = 0)
    {
        endpoints.MapGrpcService<AgentGatewayService>();
        endpoints.MapGet("/", () => "RemoteAgent gRPC service. Use the Android app to connect.");

        // ── Device-pairing web flow ────────────────────────────────────────────
        // Pairing endpoints are restricted to the HTTP/1+2 web port (e.g. 15244/15243)
        // so gRPC traffic on the primary port is not affected.
        var pairingHost = webPort > 0 ? $"*:{webPort}" : null;

        var getLogin = endpoints.MapGet("/pair", (IOptionsMonitor<AgentOptions> options) =>
        {
            var noPairingUsers = options.CurrentValue.PairingUsers.Count == 0;
            return Results.Content(PairingHtml.LoginPage(noPairingUsers: noPairingUsers), "text/html");
        });
        if (pairingHost is not null) getLogin.RequireHost(pairingHost);

        var postLogin = endpoints.MapPost("/pair", async (HttpContext context, IOptionsMonitor<AgentOptions> options, PairingSessionService sessions) =>
        {
            var form = await context.Request.ReadFormAsync();
            var username = form["username"].ToString();
            var password = form["password"].ToString();

            var user = options.CurrentValue.PairingUsers
                .FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));

            if (user is null || !VerifyPairingPassword(password, user.PasswordHash))
                return Results.Content(PairingHtml.LoginPage(error: true), "text/html");

            var token = sessions.CreateToken();
            context.Response.Cookies.Append("ra_pair", token, new CookieOptions
            {
                HttpOnly  = true,
                SameSite  = SameSiteMode.Strict,
                Expires   = DateTimeOffset.UtcNow.AddHours(1)
            });
            return Results.Redirect("/pair/key");
        }).DisableAntiforgery();
        if (pairingHost is not null) postLogin.RequireHost(pairingHost);

        var getKey = endpoints.MapGet("/pair/key", (HttpContext context, IOptionsMonitor<AgentOptions> options, PairingSessionService sessions) =>
        {
            var token = context.Request.Cookies["ra_pair"];
            if (!sessions.Validate(token))
                return Results.Redirect("/pair");

            var apiKey = options.CurrentValue.ApiKey?.Trim() ?? "";
            var host   = context.Request.Host.Host;
            var port   = context.Request.Host.Port
                             ?? (context.Request.IsHttps ? 443 : (OperatingSystem.IsWindows() ? 5244 : 5243));
            var deepLink = $"remoteagent://pair?key={Uri.EscapeDataString(apiKey)}" +
                           $"&host={Uri.EscapeDataString(host)}&port={port}";

            return Results.Content(PairingHtml.KeyPage(apiKey, deepLink), "text/html");
        });
        if (pairingHost is not null) getKey.RequireHost(pairingHost);
    }

    private static bool VerifyPairingPassword(string plaintext, string sha256Hex)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plaintext)));
        return string.Equals(hash, sha256Hex, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAuthorizedHttp(HttpContext context, AgentOptions options)
    {
        var remote = context.Connection.RemoteIpAddress;
        if (options.AllowUnauthenticatedLoopback && remote != null && IPAddress.IsLoopback(remote))
            return true;

        if (options.AllowUnauthenticatedRemote)
            return true;

        var configuredApiKey = options.ApiKey?.Trim();
        if (!string.IsNullOrEmpty(configuredApiKey))
        {
            var provided = context.Request.Headers["x-api-key"].FirstOrDefault();
            return string.Equals(configuredApiKey, provided, StringComparison.Ordinal);
        }

        return false;
    }
}

