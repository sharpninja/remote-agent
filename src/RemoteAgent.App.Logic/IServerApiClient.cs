using RemoteAgent.Proto;

namespace RemoteAgent.App.Logic;

/// <summary>
/// Abstraction over static <see cref="ServerApiClient"/> methods for testability and DI.
/// </summary>
public interface IServerApiClient
{
    Task<ServerInfoResponse?> GetServerInfoAsync(string host, int port, string? clientVersion = null, string? apiKey = null, CancellationToken ct = default);
    Task<ListMcpServersResponse?> ListMcpServersAsync(string host, int port, string? apiKey = null, CancellationToken ct = default);
    Task<UpsertMcpServerResponse?> UpsertMcpServerAsync(string host, int port, McpServerDefinition server, string? apiKey = null, CancellationToken ct = default);
    Task<DeleteMcpServerResponse?> DeleteMcpServerAsync(string host, int port, string serverId, string? apiKey = null, CancellationToken ct = default);
    Task<ListPromptTemplatesResponse?> ListPromptTemplatesAsync(string host, int port, string? apiKey = null, CancellationToken ct = default);
    Task<GetPluginsResponse?> GetPluginsAsync(string host, int port, string? apiKey = null, CancellationToken ct = default);
    Task<UpdatePluginsResponse?> UpdatePluginsAsync(string host, int port, IEnumerable<string> assemblies, string? apiKey = null, CancellationToken ct = default);
    Task<UpsertPromptTemplateResponse?> UpsertPromptTemplateAsync(string host, int port, PromptTemplateDefinition template, string? apiKey = null, CancellationToken ct = default);
    Task<DeletePromptTemplateResponse?> DeletePromptTemplateAsync(string host, int port, string templateId, string? apiKey = null, CancellationToken ct = default);
    Task<GetAgentMcpServersResponse?> GetAgentMcpServersAsync(string host, int port, string agentId, string? apiKey = null, CancellationToken ct = default);
    Task<SetAgentMcpServersResponse?> SetAgentMcpServersAsync(string host, int port, string agentId, IEnumerable<string> serverIds, string? apiKey = null, CancellationToken ct = default);
    Task<SeedSessionContextResponse?> SeedSessionContextAsync(string host, int port, string sessionId, string contextType, string content, string? source = null, string? correlationId = null, string? apiKey = null, CancellationToken ct = default);
    Task<SessionCapacitySnapshot?> GetSessionCapacityAsync(string host, int port, string? agentId = null, string? apiKey = null, CancellationToken ct = default);
}
