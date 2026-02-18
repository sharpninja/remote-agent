using RemoteAgent.Proto;

namespace RemoteAgent.App.Logic;

/// <summary>
/// Production implementation of <see cref="IServerApiClient"/> that delegates to the static <see cref="ServerApiClient"/>.
/// </summary>
public sealed class ServerApiClientAdapter : IServerApiClient
{
    public Task<ServerInfoResponse?> GetServerInfoAsync(string host, int port, string? clientVersion = null, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.GetServerInfoAsync(host, port, clientVersion, apiKey, ct);

    public Task<ListMcpServersResponse?> ListMcpServersAsync(string host, int port, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.ListMcpServersAsync(host, port, apiKey, ct);

    public Task<UpsertMcpServerResponse?> UpsertMcpServerAsync(string host, int port, McpServerDefinition server, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.UpsertMcpServerAsync(host, port, server, apiKey, ct);

    public Task<DeleteMcpServerResponse?> DeleteMcpServerAsync(string host, int port, string serverId, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.DeleteMcpServerAsync(host, port, serverId, apiKey, ct);

    public Task<ListPromptTemplatesResponse?> ListPromptTemplatesAsync(string host, int port, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.ListPromptTemplatesAsync(host, port, apiKey, ct);

    public Task<GetPluginsResponse?> GetPluginsAsync(string host, int port, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.GetPluginsAsync(host, port, apiKey, ct);

    public Task<UpdatePluginsResponse?> UpdatePluginsAsync(string host, int port, IEnumerable<string> assemblies, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.UpdatePluginsAsync(host, port, assemblies, apiKey, ct);

    public Task<UpsertPromptTemplateResponse?> UpsertPromptTemplateAsync(string host, int port, PromptTemplateDefinition template, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.UpsertPromptTemplateAsync(host, port, template, apiKey, ct);

    public Task<DeletePromptTemplateResponse?> DeletePromptTemplateAsync(string host, int port, string templateId, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.DeletePromptTemplateAsync(host, port, templateId, apiKey, ct);

    public Task<GetAgentMcpServersResponse?> GetAgentMcpServersAsync(string host, int port, string agentId, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.GetAgentMcpServersAsync(host, port, agentId, apiKey, ct);

    public Task<SetAgentMcpServersResponse?> SetAgentMcpServersAsync(string host, int port, string agentId, IEnumerable<string> serverIds, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.SetAgentMcpServersAsync(host, port, agentId, serverIds, apiKey, ct);

    public Task<SeedSessionContextResponse?> SeedSessionContextAsync(string host, int port, string sessionId, string contextType, string content, string? source = null, string? correlationId = null, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.SeedSessionContextAsync(host, port, sessionId, contextType, content, source, correlationId, apiKey, ct);

    public Task<SessionCapacitySnapshot?> GetSessionCapacityAsync(string host, int port, string? agentId = null, string? apiKey = null, CancellationToken ct = default)
        => ServerApiClient.GetSessionCapacityAsync(host, port, agentId, apiKey, ct);
}
