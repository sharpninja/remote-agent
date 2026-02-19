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

        // Select the listen URL for the current platform from appsettings.json PlatformUrls.
        // Use ConfigureKestrel (explicit Listen* call) rather than UseUrls so that the
        // ASPNETCORE_URLS environment variable — set by 'dotnet run' from launchSettings.json
        // applicationUrl — cannot override the platform-specific port.
        var platformKey = OperatingSystem.IsWindows() ? "PlatformUrls:Windows" : "PlatformUrls:Linux";
        var platformUrl = builder.Configuration[platformKey];
        if (!string.IsNullOrWhiteSpace(platformUrl) && Uri.TryCreate(platformUrl, UriKind.Absolute, out var listenUri))
        {
            var listenPort = listenUri.Port;
            builder.WebHost.ConfigureKestrel(options =>
                options.ListenAnyIP(listenPort, o => o.Protocols = HttpProtocols.Http1AndHttp2));
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

        ConfigureEndpoints(app);

        // Write a success event once the host is fully started and listening.
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            var urls   = string.Join(", ", app.Urls.DefaultIfEmpty(platformUrl ?? "unknown"));
            logger.LogInformation("Remote Agent Service listening on {Urls}", urls);
            WriteEventLog(isError: false, 1000,
                $"Remote Agent Service started successfully. Listening on: {urls}");
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
        services.AddSingleton<PluginConfigurationService>();
        services.AddSingleton<AgentMcpConfigurationService>();
        services.AddSingleton<PromptTemplateService>();
        services.AddSingleton<ConnectionProtectionService>();
        services.AddSingleton<SessionCapacityService>();
        services.AddSingleton<AuthUserService>();
        services.AddSingleton<PairingSessionService>();
        services.AddGrpc();
    }

    public static void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<AgentGatewayService>();
        endpoints.MapGet("/api/sessions/capacity", (
            HttpContext httpContext,
            IOptions<AgentOptions> options,
            SessionCapacityService sessionCapacity) =>
        {
            if (!IsAuthorizedHttp(httpContext, options.Value))
                return Results.Unauthorized();

            var agentId = httpContext.Request.Query["agentId"].ToString();
            var status = sessionCapacity.GetStatus(agentId);
            return Results.Ok(status);
        });
        endpoints.MapGet("/api/sessions/open", (
            HttpContext httpContext,
            IOptions<AgentOptions> options,
            SessionCapacityService sessionCapacity) =>
        {
            if (!IsAuthorizedHttp(httpContext, options.Value))
                return Results.Unauthorized();

            return Results.Ok(sessionCapacity.ListOpenSessions());
        });
        endpoints.MapGet("/api/sessions/abandoned", (
            HttpContext httpContext,
            IOptions<AgentOptions> options,
            SessionCapacityService sessionCapacity) =>
        {
            if (!IsAuthorizedHttp(httpContext, options.Value))
                return Results.Unauthorized();

            return Results.Ok(sessionCapacity.ListAbandonedSessions());
        });
        endpoints.MapPost("/api/sessions/{sessionId}/terminate", (
            HttpContext httpContext,
            IOptions<AgentOptions> options,
            SessionCapacityService sessionCapacity,
            string sessionId) =>
        {
            if (!IsAuthorizedHttp(httpContext, options.Value))
                return Results.Unauthorized();

            var success = sessionCapacity.TryTerminateSession(sessionId, out var reason);
            return Results.Ok(new { success, message = success ? "Session terminated." : reason });
        });
        endpoints.MapGet("/api/connections/peers", (
            HttpContext httpContext,
            IOptions<AgentOptions> options,
            ConnectionProtectionService protection) =>
        {
            if (!IsAuthorizedHttp(httpContext, options.Value))
                return Results.Unauthorized();

            return Results.Ok(protection.GetConnectedPeers());
        });
        endpoints.MapGet("/api/connections/history", (
            HttpContext httpContext,
            IOptions<AgentOptions> options,
            ConnectionProtectionService protection) =>
        {
            if (!IsAuthorizedHttp(httpContext, options.Value))
                return Results.Unauthorized();

            var limitRaw = httpContext.Request.Query["limit"].ToString();
            var limit = 500;
            if (!string.IsNullOrWhiteSpace(limitRaw) && int.TryParse(limitRaw, out var parsed))
                limit = parsed;
            return Results.Ok(protection.GetConnectionHistory(limit));
        });
        endpoints.MapGet("/api/devices/banned", (
            HttpContext httpContext,
            IOptions<AgentOptions> options,
            ConnectionProtectionService protection) =>
        {
            if (!IsAuthorizedHttp(httpContext, options.Value))
                return Results.Unauthorized();

            return Results.Ok(protection.GetBannedPeers());
        });
        endpoints.MapPost("/api/devices/{peer}/ban", (
            HttpContext httpContext,
            IOptions<AgentOptions> options,
            ConnectionProtectionService protection,
            string peer) =>
        {
            if (!IsAuthorizedHttp(httpContext, options.Value))
                return Results.Unauthorized();

            var reason = httpContext.Request.Query["reason"].ToString();
            var ok = protection.BanPeer(peer, reason, nameof(Program));
            return Results.Ok(new { success = ok, message = ok ? "Peer banned." : "Invalid peer." });
        });
        endpoints.MapDelete("/api/devices/{peer}/ban", (
            HttpContext httpContext,
            IOptions<AgentOptions> options,
            ConnectionProtectionService protection,
            string peer) =>
        {
            if (!IsAuthorizedHttp(httpContext, options.Value))
                return Results.Unauthorized();

            var ok = protection.UnbanPeer(peer, nameof(Program));
            return Results.Ok(new { success = ok, message = ok ? "Peer unbanned." : "Peer not found." });
        });
        endpoints.MapGet("/api/auth/users", (
            HttpContext httpContext,
            IOptions<AgentOptions> options,
            AuthUserService authUsers) =>
        {
            if (!IsAuthorizedHttp(httpContext, options.Value))
                return Results.Unauthorized();

            return Results.Ok(authUsers.List());
        });
        endpoints.MapGet("/api/auth/permissions", (
            HttpContext httpContext,
            IOptions<AgentOptions> options,
            AuthUserService authUsers) =>
        {
            if (!IsAuthorizedHttp(httpContext, options.Value))
                return Results.Unauthorized();

            return Results.Ok(authUsers.ListRoles());
        });
        endpoints.MapPost("/api/auth/users", (
            HttpContext httpContext,
            IOptions<AgentOptions> options,
            AuthUserService authUsers,
            StructuredLogService structuredLogs,
            AuthUserRecord user) =>
        {
            if (!IsAuthorizedHttp(httpContext, options.Value))
                return Results.Unauthorized();

            var row = authUsers.Upsert(user);
            structuredLogs.Write("INFO", "auth_user_upserted", "Auth user upserted via management API", nameof(Program), null, null, $"{{\"user_id\":\"{row.UserId}\",\"role\":\"{row.Role}\"}}");
            return Results.Ok(row);
        });
        endpoints.MapDelete("/api/auth/users/{userId}", (
            HttpContext httpContext,
            IOptions<AgentOptions> options,
            AuthUserService authUsers,
            StructuredLogService structuredLogs,
            string userId) =>
        {
            if (!IsAuthorizedHttp(httpContext, options.Value))
                return Results.Unauthorized();

            var ok = authUsers.Delete(userId);
            structuredLogs.Write("INFO", "auth_user_deleted", "Auth user delete requested via management API", nameof(Program), null, null, $"{{\"user_id\":\"{userId}\",\"deleted\":{ok.ToString().ToLowerInvariant()}}}");
            return Results.Ok(new { success = ok, message = ok ? "Auth user deleted." : "Auth user not found." });
        });
        endpoints.MapGet("/", () => "RemoteAgent gRPC service. Use the Android app to connect.");

        // ── Device-pairing web flow ────────────────────────────────────────────
        endpoints.MapGet("/pair", (IOptions<AgentOptions> options) =>
        {
            var noPairingUsers = options.Value.PairingUsers.Count == 0;
            return Results.Content(PairingHtml.LoginPage(noPairingUsers: noPairingUsers), "text/html");
        });

        endpoints.MapPost("/pair", async (HttpContext context, IOptions<AgentOptions> options, PairingSessionService sessions) =>
        {
            var form = await context.Request.ReadFormAsync();
            var username = form["username"].ToString();
            var password = form["password"].ToString();

            var user = options.Value.PairingUsers
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

        endpoints.MapGet("/pair/key", (HttpContext context, IOptions<AgentOptions> options, PairingSessionService sessions) =>
        {
            var token = context.Request.Cookies["ra_pair"];
            if (!sessions.Validate(token))
                return Results.Redirect("/pair");

            var apiKey = options.Value.ApiKey?.Trim() ?? "";
            var host   = context.Request.Host.Host;
            var port   = context.Request.Host.Port
                             ?? (context.Request.IsHttps ? 443 : (OperatingSystem.IsWindows() ? 5244 : 5243));
            var deepLink = $"remoteagent://pair?key={Uri.EscapeDataString(apiKey)}" +
                           $"&host={Uri.EscapeDataString(host)}&port={port}";

            return Results.Content(PairingHtml.KeyPage(apiKey, deepLink), "text/html");
        });
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

