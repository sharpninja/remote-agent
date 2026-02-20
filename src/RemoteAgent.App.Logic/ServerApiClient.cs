using Grpc.Core;
using Grpc.Net.Client;
using RemoteAgent.Proto;

namespace RemoteAgent.App.Logic;

/// <summary>Shared server interaction APIs used by mobile and desktop clients.</summary>
public static class ServerApiClient
{
    public static Task<ServerInfoResponse?> GetServerInfoAsync(
        string host,
        int port,
        string? clientVersion = null,
        string? apiKey = null,
        CancellationToken ct = default,
        bool throwOnError = false)
        => ExecuteGrpcAsync(
            host,
            port,
            apiKey,
            "Get server info",
            throwOnError,
            ct,
            (client, headers, token) => client.GetServerInfoAsync(new ServerInfoRequest { ClientVersion = clientVersion ?? "" }, headers, deadline: null, cancellationToken: token).ResponseAsync);

    public static Task<GetPluginsResponse?> GetPluginsAsync(
        string host,
        int port,
        string? apiKey = null,
        CancellationToken ct = default,
        bool throwOnError = false)
        => ExecuteGrpcAsync(
            host,
            port,
            apiKey,
            "Get plugins",
            throwOnError,
            ct,
            (client, headers, token) => client.GetPluginsAsync(new GetPluginsRequest(), headers, deadline: null, cancellationToken: token).ResponseAsync);

    public static Task<UpdatePluginsResponse?> UpdatePluginsAsync(
        string host,
        int port,
        IEnumerable<string> assemblies,
        string? apiKey = null,
        CancellationToken ct = default,
        bool throwOnError = false)
    {
        var request = new UpdatePluginsRequest();
        request.Assemblies.AddRange(assemblies.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
        return ExecuteGrpcAsync(
            host,
            port,
            apiKey,
            "Update plugins",
            throwOnError,
            ct,
            (client, headers, token) => client.UpdatePluginsAsync(request, headers, deadline: null, cancellationToken: token).ResponseAsync);
    }

    public static Task<ListMcpServersResponse?> ListMcpServersAsync(
        string host,
        int port,
        string? apiKey = null,
        CancellationToken ct = default,
        bool throwOnError = false)
        => ExecuteGrpcAsync(
            host,
            port,
            apiKey,
            "List MCP servers",
            throwOnError,
            ct,
            (client, headers, token) => client.ListMcpServersAsync(new ListMcpServersRequest(), headers, deadline: null, cancellationToken: token).ResponseAsync);

    public static Task<UpsertMcpServerResponse?> UpsertMcpServerAsync(
        string host,
        int port,
        McpServerDefinition server,
        string? apiKey = null,
        CancellationToken ct = default,
        bool throwOnError = false)
        => ExecuteGrpcAsync(
            host,
            port,
            apiKey,
            "Save MCP server",
            throwOnError,
            ct,
            (client, headers, token) => client.UpsertMcpServerAsync(new UpsertMcpServerRequest { Server = server }, headers, deadline: null, cancellationToken: token).ResponseAsync);

    public static Task<DeleteMcpServerResponse?> DeleteMcpServerAsync(
        string host,
        int port,
        string serverId,
        string? apiKey = null,
        CancellationToken ct = default,
        bool throwOnError = false)
        => ExecuteGrpcAsync(
            host,
            port,
            apiKey,
            "Delete MCP server",
            throwOnError,
            ct,
            (client, headers, token) => client.DeleteMcpServerAsync(new DeleteMcpServerRequest { ServerId = serverId ?? "" }, headers, deadline: null, cancellationToken: token).ResponseAsync);

    public static Task<ListPromptTemplatesResponse?> ListPromptTemplatesAsync(
        string host,
        int port,
        string? apiKey = null,
        CancellationToken ct = default,
        bool throwOnError = false)
        => ExecuteGrpcAsync(
            host,
            port,
            apiKey,
            "List prompt templates",
            throwOnError,
            ct,
            (client, headers, token) => client.ListPromptTemplatesAsync(new ListPromptTemplatesRequest(), headers, deadline: null, cancellationToken: token).ResponseAsync);

    public static Task<UpsertPromptTemplateResponse?> UpsertPromptTemplateAsync(
        string host,
        int port,
        PromptTemplateDefinition template,
        string? apiKey = null,
        CancellationToken ct = default,
        bool throwOnError = false)
        => ExecuteGrpcAsync(
            host,
            port,
            apiKey,
            "Save prompt template",
            throwOnError,
            ct,
            (client, headers, token) => client.UpsertPromptTemplateAsync(new UpsertPromptTemplateRequest { Template = template }, headers, deadline: null, cancellationToken: token).ResponseAsync);

    public static Task<DeletePromptTemplateResponse?> DeletePromptTemplateAsync(
        string host,
        int port,
        string templateId,
        string? apiKey = null,
        CancellationToken ct = default,
        bool throwOnError = false)
        => ExecuteGrpcAsync(
            host,
            port,
            apiKey,
            "Delete prompt template",
            throwOnError,
            ct,
            (client, headers, token) => client.DeletePromptTemplateAsync(new DeletePromptTemplateRequest { TemplateId = templateId ?? "" }, headers, deadline: null, cancellationToken: token).ResponseAsync);

    public static Task<GetAgentMcpServersResponse?> GetAgentMcpServersAsync(
        string host,
        int port,
        string agentId,
        string? apiKey = null,
        CancellationToken ct = default,
        bool throwOnError = false)
        => ExecuteGrpcAsync(
            host,
            port,
            apiKey,
            "Get agent MCP server mapping",
            throwOnError,
            ct,
            (client, headers, token) => client.GetAgentMcpServersAsync(new GetAgentMcpServersRequest { AgentId = agentId ?? "" }, headers, deadline: null, cancellationToken: token).ResponseAsync);

    public static Task<SetAgentMcpServersResponse?> SetAgentMcpServersAsync(
        string host,
        int port,
        string agentId,
        IEnumerable<string> serverIds,
        string? apiKey = null,
        CancellationToken ct = default,
        bool throwOnError = false)
    {
        var request = new SetAgentMcpServersRequest { AgentId = agentId ?? "" };
        request.ServerIds.AddRange(serverIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
        return ExecuteGrpcAsync(
            host,
            port,
            apiKey,
            "Set agent MCP server mapping",
            throwOnError,
            ct,
            (client, headers, token) => client.SetAgentMcpServersAsync(request, headers, deadline: null, cancellationToken: token).ResponseAsync);
    }

    public static Task<SeedSessionContextResponse?> SeedSessionContextAsync(
        string host,
        int port,
        string sessionId,
        string contextType,
        string content,
        string? source = null,
        string? correlationId = null,
        string? apiKey = null,
        CancellationToken ct = default,
        bool throwOnError = false)
        => ExecuteGrpcAsync(
            host,
            port,
            apiKey,
            "Seed session context",
            throwOnError,
            ct,
            (client, headers, token) => client.SeedSessionContextAsync(
                new SeedSessionContextRequest
                {
                    SessionId = sessionId ?? "",
                    ContextType = contextType ?? "",
                    Content = content ?? "",
                    Source = source ?? "",
                    CorrelationId = correlationId ?? ""
                },
                headers,
                deadline: null,
                cancellationToken: token).ResponseAsync);

    public static async Task<StructuredLogsSnapshotResponse?> GetStructuredLogsSnapshotAsync(
        string host,
        int port,
        long fromOffset = 0,
        int limit = 5000,
        string? apiKey = null,
        CancellationToken ct = default,
        bool throwOnError = false)
        => await ExecuteGrpcAsync(
            host,
            port,
            apiKey,
            "Get structured logs snapshot",
            throwOnError,
            ct,
            (client, headers, token) => client.GetStructuredLogsSnapshotAsync(
                new StructuredLogsSnapshotRequest { FromOffset = fromOffset, Limit = limit },
                headers,
                deadline: null,
                cancellationToken: token).ResponseAsync);

    public static async Task MonitorStructuredLogsAsync(
        string host,
        int port,
        long fromOffset,
        Func<StructuredLogEntry, Task> onEntry,
        string? apiKey = null,
        CancellationToken ct = default)
    {
        var baseUrl = BuildBaseUrl(host, port);
        using var channel = GrpcChannel.ForAddress(baseUrl);
        var client = new AgentGateway.AgentGatewayClient(channel);
        using var call = client.StreamStructuredLogs(
            new StructuredLogsStreamRequest { FromOffset = fromOffset },
            headers: CreateHeaders(apiKey),
            cancellationToken: ct);

        await foreach (var entry in call.ResponseStream.ReadAllAsync(ct))
            await onEntry(entry);
    }

    public static async Task<SessionCapacitySnapshot?> GetSessionCapacityAsync(
        string host,
        int port,
        string? agentId = null,
        string? apiKey = null,
        CancellationToken ct = default,
        bool throwOnError = false)
    {
        var response = await ExecuteGrpcAsync(
            host,
            port,
            apiKey,
            "Check session capacity",
            throwOnError,
            ct,
            (client, headers, token) => client.CheckSessionCapacityAsync(
                new CheckSessionCapacityRequest { AgentId = agentId ?? "" },
                headers,
                cancellationToken: token).ResponseAsync);

        if (response == null) return null;

        return new SessionCapacitySnapshot(
            response.CanCreateSession,
            response.Reason,
            response.MaxConcurrentSessions,
            response.ActiveSessionCount,
            response.RemainingServerCapacity,
            response.AgentId,
            response.HasAgentLimit ? response.AgentMaxConcurrentSessions : null,
            response.AgentActiveSessionCount,
            response.HasAgentLimit ? response.RemainingAgentCapacity : null);
    }

    private static async Task<TResponse?> ExecuteGrpcAsync<TResponse>(
        string host,
        int port,
        string? apiKey,
        string operation,
        bool throwOnError,
        CancellationToken ct,
        Func<AgentGateway.AgentGatewayClient, Metadata?, CancellationToken, Task<TResponse>> call)
        where TResponse : class
    {
        var baseUrl = BuildBaseUrl(host, port);
        GrpcChannel? channel = null;
        try
        {
            channel = GrpcChannel.ForAddress(baseUrl);
            var client = new AgentGateway.AgentGatewayClient(channel);
            return await call(client, CreateHeaders(apiKey), ct);
        }
        catch (RpcException) when (!throwOnError)
        {
            return null;
        }
        catch (RpcException ex)
        {
            throw CreateGrpcFailure(operation, ex);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception) when (!throwOnError)
        {
            return null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"{operation} failed: {ex.Message}", ex);
        }
        finally
        {
            channel?.Dispose();
        }
    }

    private static InvalidOperationException CreateGrpcFailure(string operation, RpcException ex)
    {
        var detail = string.IsNullOrWhiteSpace(ex.Status.Detail) ? ex.Message : ex.Status.Detail;
        return new InvalidOperationException($"{operation} failed ({ex.StatusCode}): {detail}", ex);
    }

    public static Metadata? CreateHeaders(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return null;
        return new Metadata { { "x-api-key", apiKey.Trim() } };
    }

    public static string BuildBaseUrl(string host, int port)
    {
        return port == 443 ? $"https://{host}" : $"http://{host}:{port}";
    }
}

public sealed record SessionCapacitySnapshot(
    bool CanCreateSession,
    string Reason,
    int MaxConcurrentSessions,
    int ActiveSessionCount,
    int RemainingServerCapacity,
    string AgentId,
    int? AgentMaxConcurrentSessions,
    int AgentActiveSessionCount,
    int? RemainingAgentCapacity);
