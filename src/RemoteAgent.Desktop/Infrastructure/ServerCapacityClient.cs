using System.Net.Http.Json;
using System.Text.Json;
using RemoteAgent.App.Logic;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.Infrastructure;

public interface IServerCapacityClient
{
    Task<SessionCapacitySnapshot?> GetCapacityAsync(
        string host,
        int port,
        string? agentId,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpenServerSessionSnapshot>> GetOpenSessionsAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<bool> TerminateSessionAsync(
        string host,
        int port,
        string sessionId,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AbandonedServerSessionSnapshot>> GetAbandonedSessionsAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConnectedPeerSnapshot>> GetConnectedPeersAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConnectionHistorySnapshot>> GetConnectionHistoryAsync(
        string host,
        int port,
        int limit,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BannedPeerSnapshot>> GetBannedPeersAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<bool> BanPeerAsync(
        string host,
        int port,
        string peer,
        string? reason,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<bool> UnbanPeerAsync(
        string host,
        int port,
        string peer,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuthUserSnapshot>> GetAuthUsersAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetPermissionRolesAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<AuthUserSnapshot?> UpsertAuthUserAsync(
        string host,
        int port,
        AuthUserSnapshot user,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAuthUserAsync(
        string host,
        int port,
        string userId,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<PluginConfigurationSnapshot?> GetPluginsAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<PluginConfigurationSnapshot?> UpdatePluginsAsync(
        string host,
        int port,
        IEnumerable<string> assemblies,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<McpServerDefinition>> ListMcpServersAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<McpServerDefinition?> UpsertMcpServerAsync(
        string host,
        int port,
        McpServerDefinition server,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteMcpServerAsync(
        string host,
        int port,
        string serverId,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<GetAgentMcpServersResponse?> GetAgentMcpServersAsync(
        string host,
        int port,
        string agentId,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<bool> SetAgentMcpServersAsync(
        string host,
        int port,
        string agentId,
        IEnumerable<string> serverIds,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PromptTemplateDefinition>> ListPromptTemplatesAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<PromptTemplateDefinition?> UpsertPromptTemplateAsync(
        string host,
        int port,
        PromptTemplateDefinition template,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<bool> DeletePromptTemplateAsync(
        string host,
        int port,
        string templateId,
        string? apiKey,
        CancellationToken cancellationToken = default);

    Task<bool> SeedSessionContextAsync(
        string host,
        int port,
        string sessionId,
        string contextType,
        string content,
        string? source,
        string? correlationId,
        string? apiKey,
        CancellationToken cancellationToken = default);
}

public sealed class ServerCapacityClient : IServerCapacityClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<SessionCapacitySnapshot?> GetCapacityAsync(
        string host,
        int port,
        string? agentId,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ServerApiClient.BuildBaseUrl(host, port).TrimEnd('/');
        var query = string.IsNullOrWhiteSpace(agentId)
            ? ""
            : $"?agentId={Uri.EscapeDataString(agentId.Trim())}";
        var url = $"{baseUrl}/api/sessions/capacity{query}";

        using var client = new HttpClient();
        if (!string.IsNullOrWhiteSpace(apiKey))
            client.DefaultRequestHeaders.Add("x-api-key", apiKey.Trim());

        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateHttpFailureAsync(response, "Capacity check failed", cancellationToken);
        }

        return await response.Content.ReadFromJsonAsync<SessionCapacitySnapshot>(JsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<OpenServerSessionSnapshot>> GetOpenSessionsAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ServerApiClient.BuildBaseUrl(host, port).TrimEnd('/');
        var url = $"{baseUrl}/api/sessions/open";
        using var client = new HttpClient();
        if (!string.IsNullOrWhiteSpace(apiKey))
            client.DefaultRequestHeaders.Add("x-api-key", apiKey.Trim());

        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await CreateHttpFailureAsync(response, "Open sessions query failed", cancellationToken);

        var rows = await response.Content.ReadFromJsonAsync<List<OpenServerSessionSnapshot>>(JsonOptions, cancellationToken);
        return rows ?? [];
    }

    public async Task<bool> TerminateSessionAsync(
        string host,
        int port,
        string sessionId,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        var baseUrl = ServerApiClient.BuildBaseUrl(host, port).TrimEnd('/');
        var url = $"{baseUrl}/api/sessions/{Uri.EscapeDataString(sessionId.Trim())}/terminate";
        using var client = new HttpClient();
        if (!string.IsNullOrWhiteSpace(apiKey))
            client.DefaultRequestHeaders.Add("x-api-key", apiKey.Trim());

        using var response = await client.PostAsync(url, content: null, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await CreateHttpFailureAsync(response, "Session termination request failed", cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<TerminateSessionResponse>(JsonOptions, cancellationToken);
        if (payload is { Success: false })
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(payload.Message) ? "Session termination failed." : payload.Message);
        return payload?.Success ?? false;
    }

    public async Task<IReadOnlyList<AbandonedServerSessionSnapshot>> GetAbandonedSessionsAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ServerApiClient.BuildBaseUrl(host, port).TrimEnd('/');
        using var client = CreateClient(apiKey);
        using var response = await client.GetAsync($"{baseUrl}/api/sessions/abandoned", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await CreateHttpFailureAsync(response, "Abandoned sessions query failed", cancellationToken);
        var rows = await response.Content.ReadFromJsonAsync<List<AbandonedServerSessionSnapshot>>(JsonOptions, cancellationToken);
        return rows ?? [];
    }

    public async Task<IReadOnlyList<ConnectedPeerSnapshot>> GetConnectedPeersAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ServerApiClient.BuildBaseUrl(host, port).TrimEnd('/');
        using var client = CreateClient(apiKey);
        using var response = await client.GetAsync($"{baseUrl}/api/connections/peers", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await CreateHttpFailureAsync(response, "Connected peers query failed", cancellationToken);
        var rows = await response.Content.ReadFromJsonAsync<List<ConnectedPeerSnapshot>>(JsonOptions, cancellationToken);
        return rows ?? [];
    }

    public async Task<IReadOnlyList<ConnectionHistorySnapshot>> GetConnectionHistoryAsync(
        string host,
        int port,
        int limit,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ServerApiClient.BuildBaseUrl(host, port).TrimEnd('/');
        using var client = CreateClient(apiKey);
        using var response = await client.GetAsync($"{baseUrl}/api/connections/history?limit={Math.Clamp(limit, 1, 5000)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await CreateHttpFailureAsync(response, "Connection history query failed", cancellationToken);
        var rows = await response.Content.ReadFromJsonAsync<List<ConnectionHistorySnapshot>>(JsonOptions, cancellationToken);
        return rows ?? [];
    }

    public async Task<IReadOnlyList<BannedPeerSnapshot>> GetBannedPeersAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ServerApiClient.BuildBaseUrl(host, port).TrimEnd('/');
        using var client = CreateClient(apiKey);
        using var response = await client.GetAsync($"{baseUrl}/api/devices/banned", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await CreateHttpFailureAsync(response, "Banned devices query failed", cancellationToken);
        var rows = await response.Content.ReadFromJsonAsync<List<BannedPeerSnapshot>>(JsonOptions, cancellationToken);
        return rows ?? [];
    }

    public async Task<bool> BanPeerAsync(
        string host,
        int port,
        string peer,
        string? reason,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(peer))
            return false;

        var baseUrl = ServerApiClient.BuildBaseUrl(host, port).TrimEnd('/');
        var query = string.IsNullOrWhiteSpace(reason)
            ? ""
            : $"?reason={Uri.EscapeDataString(reason.Trim())}";
        using var client = CreateClient(apiKey);
        using var response = await client.PostAsync($"{baseUrl}/api/devices/{Uri.EscapeDataString(peer.Trim())}/ban{query}", null, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await CreateHttpFailureAsync(response, "Peer ban request failed", cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<TerminateSessionResponse>(JsonOptions, cancellationToken);
        if (payload is { Success: false })
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(payload.Message) ? "Peer ban failed." : payload.Message);
        return payload?.Success ?? false;
    }

    public async Task<bool> UnbanPeerAsync(
        string host,
        int port,
        string peer,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(peer))
            return false;

        var baseUrl = ServerApiClient.BuildBaseUrl(host, port).TrimEnd('/');
        using var client = CreateClient(apiKey);
        using var response = await client.DeleteAsync($"{baseUrl}/api/devices/{Uri.EscapeDataString(peer.Trim())}/ban", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await CreateHttpFailureAsync(response, "Peer unban request failed", cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<TerminateSessionResponse>(JsonOptions, cancellationToken);
        if (payload is { Success: false })
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(payload.Message) ? "Peer unban failed." : payload.Message);
        return payload?.Success ?? false;
    }

    public async Task<IReadOnlyList<AuthUserSnapshot>> GetAuthUsersAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ServerApiClient.BuildBaseUrl(host, port).TrimEnd('/');
        using var client = CreateClient(apiKey);
        using var response = await client.GetAsync($"{baseUrl}/api/auth/users", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await CreateHttpFailureAsync(response, "Auth users query failed", cancellationToken);
        var rows = await response.Content.ReadFromJsonAsync<List<AuthUserSnapshot>>(JsonOptions, cancellationToken);
        return rows ?? [];
    }

    public async Task<IReadOnlyList<string>> GetPermissionRolesAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ServerApiClient.BuildBaseUrl(host, port).TrimEnd('/');
        using var client = CreateClient(apiKey);
        using var response = await client.GetAsync($"{baseUrl}/api/auth/permissions", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await CreateHttpFailureAsync(response, "Permission roles query failed", cancellationToken);
        var rows = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions, cancellationToken);
        return rows ?? [];
    }

    public async Task<AuthUserSnapshot?> UpsertAuthUserAsync(
        string host,
        int port,
        AuthUserSnapshot user,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ServerApiClient.BuildBaseUrl(host, port).TrimEnd('/');
        using var client = CreateClient(apiKey);
        using var response = await client.PostAsJsonAsync($"{baseUrl}/api/auth/users", user, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await CreateHttpFailureAsync(response, "Auth user save failed", cancellationToken);
        return await response.Content.ReadFromJsonAsync<AuthUserSnapshot>(JsonOptions, cancellationToken);
    }

    public async Task<bool> DeleteAuthUserAsync(
        string host,
        int port,
        string userId,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        var baseUrl = ServerApiClient.BuildBaseUrl(host, port).TrimEnd('/');
        using var client = CreateClient(apiKey);
        using var response = await client.DeleteAsync($"{baseUrl}/api/auth/users/{Uri.EscapeDataString(userId.Trim())}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await CreateHttpFailureAsync(response, "Auth user delete failed", cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<TerminateSessionResponse>(JsonOptions, cancellationToken);
        if (payload is { Success: false })
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(payload.Message) ? "Auth user delete failed." : payload.Message);
        return payload?.Success ?? false;
    }

    public async Task<PluginConfigurationSnapshot?> GetPluginsAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var response = await ServerApiClient.GetPluginsAsync(host, port, apiKey, cancellationToken, throwOnError: true);
        if (response == null)
            throw new InvalidOperationException("Get plugins failed: response body was empty.");
        return response == null
            ? null
            : new PluginConfigurationSnapshot(
                response.ConfiguredAssemblies.ToList(),
                response.LoadedRunnerIds.ToList(),
                false,
                "");
    }

    public async Task<PluginConfigurationSnapshot?> UpdatePluginsAsync(
        string host,
        int port,
        IEnumerable<string> assemblies,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var response = await ServerApiClient.UpdatePluginsAsync(host, port, assemblies, apiKey, cancellationToken, throwOnError: true);
        if (response == null)
            throw new InvalidOperationException("Update plugins failed: response body was empty.");
        return response == null
            ? null
            : new PluginConfigurationSnapshot(
                response.ConfiguredAssemblies.ToList(),
                [],
                response.Success,
                response.Message ?? "");
    }

    public async Task<IReadOnlyList<McpServerDefinition>> ListMcpServersAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var response = await ServerApiClient.ListMcpServersAsync(host, port, apiKey, cancellationToken, throwOnError: true);
        if (response == null)
            throw new InvalidOperationException("List MCP servers failed: response body was empty.");
        return response?.Servers?.ToList() ?? [];
    }

    public async Task<McpServerDefinition?> UpsertMcpServerAsync(
        string host,
        int port,
        McpServerDefinition server,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var response = await ServerApiClient.UpsertMcpServerAsync(host, port, server, apiKey, cancellationToken, throwOnError: true);
        if (response == null)
            throw new InvalidOperationException("Save MCP server failed: response body was empty.");
        return response?.Server;
    }

    public async Task<bool> DeleteMcpServerAsync(
        string host,
        int port,
        string serverId,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var response = await ServerApiClient.DeleteMcpServerAsync(host, port, serverId, apiKey, cancellationToken, throwOnError: true);
        if (response == null)
            throw new InvalidOperationException("Delete MCP server failed: response body was empty.");
        if (!response.Success)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(response.Message) ? "Delete MCP server failed." : response.Message);
        return response?.Success ?? false;
    }

    public Task<GetAgentMcpServersResponse?> GetAgentMcpServersAsync(
        string host,
        int port,
        string agentId,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        return ServerApiClient.GetAgentMcpServersAsync(host, port, agentId, apiKey, cancellationToken, throwOnError: true);
    }

    public async Task<bool> SetAgentMcpServersAsync(
        string host,
        int port,
        string agentId,
        IEnumerable<string> serverIds,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var response = await ServerApiClient.SetAgentMcpServersAsync(host, port, agentId, serverIds, apiKey, cancellationToken, throwOnError: true);
        if (response == null)
            throw new InvalidOperationException("Set agent MCP mapping failed: response body was empty.");
        if (!response.Success)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(response.Message) ? "Set agent MCP mapping failed." : response.Message);
        return response?.Success ?? false;
    }

    public async Task<IReadOnlyList<PromptTemplateDefinition>> ListPromptTemplatesAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var response = await ServerApiClient.ListPromptTemplatesAsync(host, port, apiKey, cancellationToken, throwOnError: true);
        if (response == null)
            throw new InvalidOperationException("List prompt templates failed: response body was empty.");
        return response?.Templates?.ToList() ?? [];
    }

    public async Task<PromptTemplateDefinition?> UpsertPromptTemplateAsync(
        string host,
        int port,
        PromptTemplateDefinition template,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var response = await ServerApiClient.UpsertPromptTemplateAsync(host, port, template, apiKey, cancellationToken, throwOnError: true);
        if (response == null)
            throw new InvalidOperationException("Save prompt template failed: response body was empty.");
        return response?.Template;
    }

    public async Task<bool> DeletePromptTemplateAsync(
        string host,
        int port,
        string templateId,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var response = await ServerApiClient.DeletePromptTemplateAsync(host, port, templateId, apiKey, cancellationToken, throwOnError: true);
        if (response == null)
            throw new InvalidOperationException("Delete prompt template failed: response body was empty.");
        if (!response.Success)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(response.Message) ? "Delete prompt template failed." : response.Message);
        return response?.Success ?? false;
    }

    public async Task<bool> SeedSessionContextAsync(
        string host,
        int port,
        string sessionId,
        string contextType,
        string content,
        string? source,
        string? correlationId,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var response = await ServerApiClient.SeedSessionContextAsync(
            host,
            port,
            sessionId,
            contextType,
            content,
            source,
            correlationId,
            apiKey,
            cancellationToken,
            throwOnError: true);
        if (response == null)
            throw new InvalidOperationException("Seed session context failed: response body was empty.");
        if (!response.Success)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(response.Message) ? "Seed session context failed." : response.Message);
        return response?.Success ?? false;
    }

    private static HttpClient CreateClient(string? apiKey)
    {
        var client = new HttpClient();
        if (!string.IsNullOrWhiteSpace(apiKey))
            client.DefaultRequestHeaders.Add("x-api-key", apiKey.Trim());
        return client;
    }

    private static async Task<string?> TryReadErrorDetailAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(body))
                return null;

            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("message", out var messageNode) &&
                messageNode.ValueKind == JsonValueKind.String)
            {
                return messageNode.GetString();
            }

            return body.Length <= 200 ? body : body[..200];
        }
        catch
        {
            return null;
        }
    }

    private static async Task<InvalidOperationException> CreateHttpFailureAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        var detail = await TryReadErrorDetailAsync(response, cancellationToken);
        var statusCode = (int)response.StatusCode;
        var reason = string.IsNullOrWhiteSpace(response.ReasonPhrase) ? "HTTP error" : response.ReasonPhrase;
        var message = string.IsNullOrWhiteSpace(detail)
            ? $"{operation} ({statusCode} {reason})."
            : $"{operation} ({statusCode} {reason}): {detail}";
        return new InvalidOperationException(message);
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

public sealed record OpenServerSessionSnapshot(
    string SessionId,
    string AgentId,
    bool CanAcceptInput);

public sealed record TerminateSessionResponse(
    bool Success,
    string Message);

public sealed record AbandonedServerSessionSnapshot(
    string SessionId,
    string AgentId,
    string Reason,
    DateTimeOffset AbandonedUtc);

public sealed record ConnectedPeerSnapshot(
    string Peer,
    int ActiveConnections,
    bool IsBlocked,
    DateTime? BlockedUntilUtc,
    DateTime LastSeenUtc);

public sealed record ConnectionHistorySnapshot(
    DateTimeOffset TimestampUtc,
    string Peer,
    string Action,
    bool Allowed,
    string? Detail);

public sealed record BannedPeerSnapshot(
    string Peer,
    string Reason,
    DateTimeOffset BannedUtc);

public sealed record AuthUserSnapshot(
    string UserId,
    string DisplayName,
    string Role,
    bool Enabled,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);

public sealed record PluginConfigurationSnapshot(
    IReadOnlyList<string> ConfiguredAssemblies,
    IReadOnlyList<string> LoadedRunnerIds,
    bool Success,
    string Message);
