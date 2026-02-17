using Grpc.Core;
using Grpc.Net.Client;
using RemoteAgent.Proto;
using System.Net.Http.Json;
using System.Text.Json;

namespace RemoteAgent.App.Logic;

/// <summary>Shared server interaction APIs used by mobile and desktop clients.</summary>
public static class ServerApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<ServerInfoResponse?> GetServerInfoAsync(string host, int port, string? clientVersion = null, string? apiKey = null, CancellationToken ct = default)
    {
        var baseUrl = BuildBaseUrl(host, port);
        GrpcChannel? channel = null;
        try
        {
            channel = GrpcChannel.ForAddress(baseUrl);
            var client = new AgentGateway.AgentGatewayClient(channel);
            return await client.GetServerInfoAsync(new ServerInfoRequest { ClientVersion = clientVersion ?? "" }, CreateHeaders(apiKey), deadline: null, cancellationToken: ct);
        }
        catch
        {
            return null;
        }
        finally
        {
            channel?.Dispose();
        }
    }

    public static async Task<GetPluginsResponse?> GetPluginsAsync(string host, int port, string? apiKey = null, CancellationToken ct = default)
    {
        var baseUrl = BuildBaseUrl(host, port);
        GrpcChannel? channel = null;
        try
        {
            channel = GrpcChannel.ForAddress(baseUrl);
            var client = new AgentGateway.AgentGatewayClient(channel);
            return await client.GetPluginsAsync(new GetPluginsRequest(), CreateHeaders(apiKey), deadline: null, cancellationToken: ct);
        }
        catch
        {
            return null;
        }
        finally
        {
            channel?.Dispose();
        }
    }

    public static async Task<UpdatePluginsResponse?> UpdatePluginsAsync(string host, int port, IEnumerable<string> assemblies, string? apiKey = null, CancellationToken ct = default)
    {
        var baseUrl = BuildBaseUrl(host, port);
        GrpcChannel? channel = null;
        try
        {
            channel = GrpcChannel.ForAddress(baseUrl);
            var client = new AgentGateway.AgentGatewayClient(channel);
            var request = new UpdatePluginsRequest();
            request.Assemblies.AddRange(assemblies.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
            return await client.UpdatePluginsAsync(request, CreateHeaders(apiKey), deadline: null, cancellationToken: ct);
        }
        catch
        {
            return null;
        }
        finally
        {
            channel?.Dispose();
        }
    }

    public static async Task<ListMcpServersResponse?> ListMcpServersAsync(string host, int port, string? apiKey = null, CancellationToken ct = default)
    {
        var baseUrl = BuildBaseUrl(host, port);
        GrpcChannel? channel = null;
        try
        {
            channel = GrpcChannel.ForAddress(baseUrl);
            var client = new AgentGateway.AgentGatewayClient(channel);
            return await client.ListMcpServersAsync(new ListMcpServersRequest(), CreateHeaders(apiKey), deadline: null, cancellationToken: ct);
        }
        catch
        {
            return null;
        }
        finally
        {
            channel?.Dispose();
        }
    }

    public static async Task<UpsertMcpServerResponse?> UpsertMcpServerAsync(string host, int port, McpServerDefinition server, string? apiKey = null, CancellationToken ct = default)
    {
        var baseUrl = BuildBaseUrl(host, port);
        GrpcChannel? channel = null;
        try
        {
            channel = GrpcChannel.ForAddress(baseUrl);
            var client = new AgentGateway.AgentGatewayClient(channel);
            return await client.UpsertMcpServerAsync(new UpsertMcpServerRequest { Server = server }, CreateHeaders(apiKey), deadline: null, cancellationToken: ct);
        }
        catch
        {
            return null;
        }
        finally
        {
            channel?.Dispose();
        }
    }

    public static async Task<DeleteMcpServerResponse?> DeleteMcpServerAsync(string host, int port, string serverId, string? apiKey = null, CancellationToken ct = default)
    {
        var baseUrl = BuildBaseUrl(host, port);
        GrpcChannel? channel = null;
        try
        {
            channel = GrpcChannel.ForAddress(baseUrl);
            var client = new AgentGateway.AgentGatewayClient(channel);
            return await client.DeleteMcpServerAsync(new DeleteMcpServerRequest { ServerId = serverId ?? "" }, CreateHeaders(apiKey), deadline: null, cancellationToken: ct);
        }
        catch
        {
            return null;
        }
        finally
        {
            channel?.Dispose();
        }
    }

    public static async Task<ListPromptTemplatesResponse?> ListPromptTemplatesAsync(string host, int port, string? apiKey = null, CancellationToken ct = default)
    {
        var baseUrl = BuildBaseUrl(host, port);
        GrpcChannel? channel = null;
        try
        {
            channel = GrpcChannel.ForAddress(baseUrl);
            var client = new AgentGateway.AgentGatewayClient(channel);
            return await client.ListPromptTemplatesAsync(new ListPromptTemplatesRequest(), CreateHeaders(apiKey), deadline: null, cancellationToken: ct);
        }
        catch
        {
            return null;
        }
        finally
        {
            channel?.Dispose();
        }
    }

    public static async Task<UpsertPromptTemplateResponse?> UpsertPromptTemplateAsync(string host, int port, PromptTemplateDefinition template, string? apiKey = null, CancellationToken ct = default)
    {
        var baseUrl = BuildBaseUrl(host, port);
        GrpcChannel? channel = null;
        try
        {
            channel = GrpcChannel.ForAddress(baseUrl);
            var client = new AgentGateway.AgentGatewayClient(channel);
            return await client.UpsertPromptTemplateAsync(new UpsertPromptTemplateRequest { Template = template }, CreateHeaders(apiKey), deadline: null, cancellationToken: ct);
        }
        catch
        {
            return null;
        }
        finally
        {
            channel?.Dispose();
        }
    }

    public static async Task<DeletePromptTemplateResponse?> DeletePromptTemplateAsync(string host, int port, string templateId, string? apiKey = null, CancellationToken ct = default)
    {
        var baseUrl = BuildBaseUrl(host, port);
        GrpcChannel? channel = null;
        try
        {
            channel = GrpcChannel.ForAddress(baseUrl);
            var client = new AgentGateway.AgentGatewayClient(channel);
            return await client.DeletePromptTemplateAsync(new DeletePromptTemplateRequest { TemplateId = templateId ?? "" }, CreateHeaders(apiKey), deadline: null, cancellationToken: ct);
        }
        catch
        {
            return null;
        }
        finally
        {
            channel?.Dispose();
        }
    }

    public static async Task<GetAgentMcpServersResponse?> GetAgentMcpServersAsync(string host, int port, string agentId, string? apiKey = null, CancellationToken ct = default)
    {
        var baseUrl = BuildBaseUrl(host, port);
        GrpcChannel? channel = null;
        try
        {
            channel = GrpcChannel.ForAddress(baseUrl);
            var client = new AgentGateway.AgentGatewayClient(channel);
            return await client.GetAgentMcpServersAsync(new GetAgentMcpServersRequest { AgentId = agentId ?? "" }, CreateHeaders(apiKey), deadline: null, cancellationToken: ct);
        }
        catch
        {
            return null;
        }
        finally
        {
            channel?.Dispose();
        }
    }

    public static async Task<SetAgentMcpServersResponse?> SetAgentMcpServersAsync(string host, int port, string agentId, IEnumerable<string> serverIds, string? apiKey = null, CancellationToken ct = default)
    {
        var baseUrl = BuildBaseUrl(host, port);
        GrpcChannel? channel = null;
        try
        {
            channel = GrpcChannel.ForAddress(baseUrl);
            var client = new AgentGateway.AgentGatewayClient(channel);
            var request = new SetAgentMcpServersRequest { AgentId = agentId ?? "" };
            request.ServerIds.AddRange(serverIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
            return await client.SetAgentMcpServersAsync(request, CreateHeaders(apiKey), deadline: null, cancellationToken: ct);
        }
        catch
        {
            return null;
        }
        finally
        {
            channel?.Dispose();
        }
    }

    public static async Task<SeedSessionContextResponse?> SeedSessionContextAsync(
        string host,
        int port,
        string sessionId,
        string contextType,
        string content,
        string? source = null,
        string? correlationId = null,
        string? apiKey = null,
        CancellationToken ct = default)
    {
        var baseUrl = BuildBaseUrl(host, port);
        GrpcChannel? channel = null;
        try
        {
            channel = GrpcChannel.ForAddress(baseUrl);
            var client = new AgentGateway.AgentGatewayClient(channel);
            return await client.SeedSessionContextAsync(
                new SeedSessionContextRequest
                {
                    SessionId = sessionId ?? "",
                    ContextType = contextType ?? "",
                    Content = content ?? "",
                    Source = source ?? "",
                    CorrelationId = correlationId ?? ""
                },
                CreateHeaders(apiKey),
                deadline: null,
                cancellationToken: ct);
        }
        catch
        {
            return null;
        }
        finally
        {
            channel?.Dispose();
        }
    }

    public static async Task<StructuredLogsSnapshotResponse?> GetStructuredLogsSnapshotAsync(
        string host,
        int port,
        long fromOffset = 0,
        int limit = 5000,
        string? apiKey = null,
        CancellationToken ct = default)
    {
        var baseUrl = BuildBaseUrl(host, port);
        GrpcChannel? channel = null;
        try
        {
            channel = GrpcChannel.ForAddress(baseUrl);
            var client = new AgentGateway.AgentGatewayClient(channel);
            return await client.GetStructuredLogsSnapshotAsync(
                new StructuredLogsSnapshotRequest { FromOffset = fromOffset, Limit = limit },
                CreateHeaders(apiKey),
                deadline: null,
                cancellationToken: ct);
        }
        catch
        {
            return null;
        }
        finally
        {
            channel?.Dispose();
        }
    }

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
        CancellationToken ct = default)
    {
        var baseUrl = BuildBaseUrl(host, port).TrimEnd('/');
        var query = string.IsNullOrWhiteSpace(agentId)
            ? ""
            : $"?agentId={Uri.EscapeDataString(agentId.Trim())}";
        var url = $"{baseUrl}/api/sessions/capacity{query}";

        using var client = new HttpClient();
        if (!string.IsNullOrWhiteSpace(apiKey))
            client.DefaultRequestHeaders.Add("x-api-key", apiKey.Trim());

        using var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<SessionCapacitySnapshot>(JsonOptions, ct);
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
