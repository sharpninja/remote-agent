using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RemoteAgent.Service;
using RemoteAgent.Service.Agents;
using RemoteAgent.Service.Logging;
using RemoteAgent.Service.Services;
using RemoteAgent.Service.Storage;

namespace RemoteAgent.Service;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseWindowsService(options =>
        {
            options.ServiceName = "Remote Agent Service";
        });
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
        services.AddSingleton<StructuredLogService>();
        services.AddSingleton<MediaStorageService>();
        services.AddSingleton<PluginConfigurationService>();
        services.AddSingleton<AgentMcpConfigurationService>();
        services.AddSingleton<PromptTemplateService>();
        services.AddSingleton<ConnectionProtectionService>();
        services.AddSingleton<SessionCapacityService>();
        services.AddSingleton<AuthUserService>();
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
    }

    private static bool IsAuthorizedHttp(HttpContext context, AgentOptions options)
    {
        var remote = context.Connection.RemoteIpAddress;
        if (options.AllowUnauthenticatedLoopback && remote != null && IPAddress.IsLoopback(remote))
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

