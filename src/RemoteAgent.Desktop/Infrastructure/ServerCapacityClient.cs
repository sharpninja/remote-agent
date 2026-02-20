using Grpc.Net.Client;
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

    Task<string> SetPairingUsersAsync(
        string host,
        int port,
        IEnumerable<(string Username, string PasswordHash)> users,
        bool replace,
        string? apiKey,
        CancellationToken cancellationToken = default);
}

public sealed class ServerCapacityClient : IServerCapacityClient
{
    public async Task<SessionCapacitySnapshot?> GetCapacityAsync(
        string host,
        int port,
        string? agentId,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ServerApiClient.BuildBaseUrl(host, port);
        using var channel = GrpcChannel.ForAddress(baseUrl);
        var client = new AgentGateway.AgentGatewayClient(channel);
        var response = await client.CheckSessionCapacityAsync(
            new CheckSessionCapacityRequest { AgentId = agentId?.Trim() ?? "" },
            headers: ServerApiClient.CreateHeaders(apiKey),
            cancellationToken: cancellationToken);
        return new SessionCapacitySnapshot(
            response.CanCreateSession, response.Reason,
            response.MaxConcurrentSessions, response.ActiveSessionCount,
            response.RemainingServerCapacity, response.AgentId,
            response.HasAgentLimit ? response.AgentMaxConcurrentSessions : null,
            response.AgentActiveSessionCount,
            response.HasAgentLimit ? response.RemainingAgentCapacity : null);
    }

    public async Task<IReadOnlyList<OpenServerSessionSnapshot>> GetOpenSessionsAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ServerApiClient.BuildBaseUrl(host, port);
        using var channel = GrpcChannel.ForAddress(baseUrl);
        var client = new AgentGateway.AgentGatewayClient(channel);
        var response = await client.ListOpenSessionsAsync(
            new ListOpenSessionsRequest(),
            headers: ServerApiClient.CreateHeaders(apiKey),
            cancellationToken: cancellationToken);
        return response.Sessions.Select(s => new OpenServerSessionSnapshot(s.SessionId, s.AgentId, s.CanAcceptInput)).ToList();
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

        var baseUrl = ServerApiClient.BuildBaseUrl(host, port);
        using var channel = GrpcChannel.ForAddress(baseUrl);
        var client = new AgentGateway.AgentGatewayClient(channel);
        var response = await client.TerminateSessionAsync(
            new TerminateSessionRequest { SessionId = sessionId.Trim() },
            headers: ServerApiClient.CreateHeaders(apiKey),
            cancellationToken: cancellationToken);
        if (!response.Success)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(response.Message) ? "Session termination failed." : response.Message);
        return response.Success;
    }

    public async Task<IReadOnlyList<AbandonedServerSessionSnapshot>> GetAbandonedSessionsAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ServerApiClient.BuildBaseUrl(host, port);
        using var channel = GrpcChannel.ForAddress(baseUrl);
        var client = new AgentGateway.AgentGatewayClient(channel);
        var response = await client.ListAbandonedSessionsAsync(
            new ListAbandonedSessionsRequest(),
            headers: ServerApiClient.CreateHeaders(apiKey),
            cancellationToken: cancellationToken);
        return response.Sessions.Select(s => new AbandonedServerSessionSnapshot(
            s.SessionId, s.AgentId, s.Reason,
            DateTimeOffset.TryParse(s.AbandonedUtc, out var dt) ? dt : DateTimeOffset.UtcNow)).ToList();
    }

    public async Task<IReadOnlyList<ConnectedPeerSnapshot>> GetConnectedPeersAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ServerApiClient.BuildBaseUrl(host, port);
        using var channel = GrpcChannel.ForAddress(baseUrl);
        var client = new AgentGateway.AgentGatewayClient(channel);
        var response = await client.ListConnectedPeersAsync(
            new ListConnectedPeersRequest(),
            headers: ServerApiClient.CreateHeaders(apiKey),
            cancellationToken: cancellationToken);
        return response.Peers.Select(p => new ConnectedPeerSnapshot(
            p.Peer, p.ActiveConnections, p.IsBlocked,
            string.IsNullOrEmpty(p.BlockedUntilUtc) ? null : (DateTime.TryParse(p.BlockedUntilUtc, out var bu) ? bu : (DateTime?)null),
            DateTime.TryParse(p.LastSeenUtc, out var ls) ? ls : DateTime.UtcNow)).ToList();
    }

    public async Task<IReadOnlyList<ConnectionHistorySnapshot>> GetConnectionHistoryAsync(
        string host,
        int port,
        int limit,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ServerApiClient.BuildBaseUrl(host, port);
        using var channel = GrpcChannel.ForAddress(baseUrl);
        var client = new AgentGateway.AgentGatewayClient(channel);
        var response = await client.ListConnectionHistoryAsync(
            new ListConnectionHistoryRequest { Limit = Math.Clamp(limit, 1, 5000) },
            headers: ServerApiClient.CreateHeaders(apiKey),
            cancellationToken: cancellationToken);
        return response.Entries.Select(e => new ConnectionHistorySnapshot(
            DateTimeOffset.TryParse(e.TimestampUtc, out var ts) ? ts : DateTimeOffset.UtcNow,
            e.Peer, e.Action, e.Allowed, string.IsNullOrEmpty(e.Detail) ? null : e.Detail)).ToList();
    }

    public async Task<IReadOnlyList<BannedPeerSnapshot>> GetBannedPeersAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ServerApiClient.BuildBaseUrl(host, port);
        using var channel = GrpcChannel.ForAddress(baseUrl);
        var client = new AgentGateway.AgentGatewayClient(channel);
        var response = await client.ListBannedPeersAsync(
            new ListBannedPeersRequest(),
            headers: ServerApiClient.CreateHeaders(apiKey),
            cancellationToken: cancellationToken);
        return response.Peers.Select(b => new BannedPeerSnapshot(
            b.Peer, b.Reason,
            DateTimeOffset.TryParse(b.BannedUtc, out var bt) ? bt : DateTimeOffset.UtcNow)).ToList();
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

        var baseUrl = ServerApiClient.BuildBaseUrl(host, port);
        using var channel = GrpcChannel.ForAddress(baseUrl);
        var client = new AgentGateway.AgentGatewayClient(channel);
        var response = await client.BanPeerAsync(
            new BanPeerRequest { Peer = peer.Trim(), Reason = reason?.Trim() ?? "" },
            headers: ServerApiClient.CreateHeaders(apiKey),
            cancellationToken: cancellationToken);
        if (!response.Success)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(response.Message) ? "Peer ban failed." : response.Message);
        return response.Success;
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

        var baseUrl = ServerApiClient.BuildBaseUrl(host, port);
        using var channel = GrpcChannel.ForAddress(baseUrl);
        var client = new AgentGateway.AgentGatewayClient(channel);
        var response = await client.UnbanPeerAsync(
            new UnbanPeerRequest { Peer = peer.Trim() },
            headers: ServerApiClient.CreateHeaders(apiKey),
            cancellationToken: cancellationToken);
        if (!response.Success)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(response.Message) ? "Peer unban failed." : response.Message);
        return response.Success;
    }

    public async Task<IReadOnlyList<AuthUserSnapshot>> GetAuthUsersAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ServerApiClient.BuildBaseUrl(host, port);
        using var channel = GrpcChannel.ForAddress(baseUrl);
        var client = new AgentGateway.AgentGatewayClient(channel);
        var response = await client.ListAuthUsersAsync(
            new ListAuthUsersRequest(),
            headers: ServerApiClient.CreateHeaders(apiKey),
            cancellationToken: cancellationToken);
        return response.Users.Select(u => new AuthUserSnapshot(
            u.UserId, u.DisplayName, u.Role, u.Enabled,
            DateTimeOffset.TryParse(u.CreatedUtc, out var c) ? c : DateTimeOffset.UtcNow,
            DateTimeOffset.TryParse(u.UpdatedUtc, out var upd) ? upd : DateTimeOffset.UtcNow)).ToList();
    }

    public async Task<IReadOnlyList<string>> GetPermissionRolesAsync(
        string host,
        int port,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ServerApiClient.BuildBaseUrl(host, port);
        using var channel = GrpcChannel.ForAddress(baseUrl);
        var client = new AgentGateway.AgentGatewayClient(channel);
        var response = await client.ListPermissionRolesAsync(
            new ListPermissionRolesRequest(),
            headers: ServerApiClient.CreateHeaders(apiKey),
            cancellationToken: cancellationToken);
        return response.Roles.ToList();
    }

    public async Task<AuthUserSnapshot?> UpsertAuthUserAsync(
        string host,
        int port,
        AuthUserSnapshot user,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ServerApiClient.BuildBaseUrl(host, port);
        using var channel = GrpcChannel.ForAddress(baseUrl);
        var client = new AgentGateway.AgentGatewayClient(channel);
        var entry = new AuthUserEntry { UserId = user.UserId, DisplayName = user.DisplayName, Role = user.Role, Enabled = user.Enabled };
        var response = await client.UpsertAuthUserAsync(
            new UpsertAuthUserRequest { User = entry },
            headers: ServerApiClient.CreateHeaders(apiKey),
            cancellationToken: cancellationToken);
        if (response.User == null)
            return null;
        var u = response.User;
        return new AuthUserSnapshot(
            u.UserId, u.DisplayName, u.Role, u.Enabled,
            DateTimeOffset.TryParse(u.CreatedUtc, out var c) ? c : DateTimeOffset.UtcNow,
            DateTimeOffset.TryParse(u.UpdatedUtc, out var upd) ? upd : DateTimeOffset.UtcNow);
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

        var baseUrl = ServerApiClient.BuildBaseUrl(host, port);
        using var channel = GrpcChannel.ForAddress(baseUrl);
        var client = new AgentGateway.AgentGatewayClient(channel);
        var response = await client.DeleteAuthUserAsync(
            new DeleteAuthUserRequest { UserId = userId.Trim() },
            headers: ServerApiClient.CreateHeaders(apiKey),
            cancellationToken: cancellationToken);
        if (!response.Success)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(response.Message) ? "Auth user delete failed." : response.Message);
        return response.Success;
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

    public async Task<string> SetPairingUsersAsync(
        string host,
        int port,
        IEnumerable<(string Username, string PasswordHash)> users,
        bool replace,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ServerApiClient.BuildBaseUrl(host, port);
        using var channel = GrpcChannel.ForAddress(baseUrl);
        var client = new AgentGateway.AgentGatewayClient(channel);
        var grpcRequest = new SetPairingUsersRequest { Replace = replace };
        foreach (var (username, passwordHash) in users)
            grpcRequest.Users.Add(new PairingUserEntry { Username = username, PasswordHash = passwordHash });
        var response = await client.SetPairingUsersAsync(
            grpcRequest,
            headers: ServerApiClient.CreateHeaders(apiKey),
            cancellationToken: cancellationToken);
        if (!response.Success)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(response.Error) ? "Set pairing users failed." : response.Error);
        return response.GeneratedApiKey;
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
