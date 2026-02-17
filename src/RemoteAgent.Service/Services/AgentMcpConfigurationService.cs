using LiteDB;
using Microsoft.Extensions.Options;

namespace RemoteAgent.Service.Services;

/// <summary>Stores MCP server definitions, per-agent MCP associations, and queued session seed context entries.</summary>
public sealed class AgentMcpConfigurationService
{
    private readonly string _dbPath;
    private const string ServersCollection = "mcp_servers";
    private const string AgentMapCollection = "agent_mcp_map";
    private const string SeedCollection = "session_seed_context";

    public AgentMcpConfigurationService(IOptions<AgentOptions> options)
    {
        var dataDir = options.Value.DataDirectory?.Trim();
        if (string.IsNullOrWhiteSpace(dataDir))
            dataDir = Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(dataDir);
        _dbPath = Path.Combine(dataDir, "remote-agent.db");
    }

    public IReadOnlyList<McpServerRecord> ListServers()
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<McpServerRecord>(ServersCollection);
        col.EnsureIndex(x => x.ServerId, unique: true);
        return col.FindAll().OrderBy(x => x.DisplayName).ToList();
    }

    public McpServerRecord UpsertServer(McpServerRecord server)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<McpServerRecord>(ServersCollection);
        col.EnsureIndex(x => x.ServerId, unique: true);
        var now = DateTimeOffset.UtcNow;
        var id = string.IsNullOrWhiteSpace(server.ServerId)
            ? GenerateServerId(server.DisplayName, server.Command, server.Endpoint)
            : server.ServerId.Trim();

        var existing = col.FindOne(x => x.ServerId == id);
        var record = new McpServerRecord
        {
            ServerId = id,
            DisplayName = server.DisplayName?.Trim() ?? id,
            Transport = server.Transport?.Trim() ?? "",
            Endpoint = server.Endpoint?.Trim() ?? "",
            Command = server.Command?.Trim() ?? "",
            Arguments = server.Arguments ?? [],
            AuthType = server.AuthType?.Trim() ?? "",
            AuthConfigJson = server.AuthConfigJson?.Trim() ?? "",
            Enabled = server.Enabled,
            MetadataJson = server.MetadataJson?.Trim() ?? "",
            CreatedUtc = existing?.CreatedUtc ?? now,
            UpdatedUtc = now
        };

        col.Upsert(record);
        return record;
    }

    public bool DeleteServer(string serverId)
    {
        if (string.IsNullOrWhiteSpace(serverId)) return false;
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<McpServerRecord>(ServersCollection);
        var deleted = col.DeleteMany(x => x.ServerId == serverId.Trim()) > 0;

        var mapCol = db.GetCollection<AgentMcpMapRecord>(AgentMapCollection);
        foreach (var map in mapCol.FindAll())
        {
            var updated = map.ServerIds.Where(x => !string.Equals(x, serverId.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            map.ServerIds = updated;
            mapCol.Upsert(map);
        }

        return deleted;
    }

    public IReadOnlyList<string> SetAgentServers(string agentId, IEnumerable<string> serverIds)
    {
        var normalizedAgent = string.IsNullOrWhiteSpace(agentId) ? "process" : agentId.Trim();
        var normalizedServers = serverIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<AgentMcpMapRecord>(AgentMapCollection);
        col.EnsureIndex(x => x.AgentId, unique: true);
        col.Upsert(new AgentMcpMapRecord
        {
            AgentId = normalizedAgent,
            ServerIds = normalizedServers,
            UpdatedUtc = DateTimeOffset.UtcNow
        });

        return normalizedServers;
    }

    public IReadOnlyList<string> GetAgentServerIds(string agentId)
    {
        var normalizedAgent = string.IsNullOrWhiteSpace(agentId) ? "process" : agentId.Trim();
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<AgentMcpMapRecord>(AgentMapCollection);
        var row = col.FindOne(x => x.AgentId == normalizedAgent);
        return row?.ServerIds?.ToList() ?? [];
    }

    public IReadOnlyList<McpServerRecord> GetAgentServers(string agentId)
    {
        var ids = GetAgentServerIds(agentId);
        if (ids.Count == 0) return [];
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<McpServerRecord>(ServersCollection);
        return col.Find(x => ids.Contains(x.ServerId)).ToList();
    }

    public SeedContextRecord AddSeedContext(string sessionId, string contextType, string content, string source)
    {
        var sid = SanitizeSessionId(sessionId);
        var record = new SeedContextRecord
        {
            SeedId = Guid.NewGuid().ToString("N"),
            SessionId = sid,
            ContextType = string.IsNullOrWhiteSpace(contextType) ? "context" : contextType.Trim(),
            Content = content ?? "",
            Source = source ?? "",
            CreatedUtc = DateTimeOffset.UtcNow
        };

        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<SeedContextRecord>(SeedCollection);
        col.EnsureIndex(x => x.SessionId);
        col.EnsureIndex(x => x.SeedId, unique: true);
        col.Insert(record);
        return record;
    }

    public IReadOnlyList<SeedContextRecord> GetSeedContext(string sessionId)
    {
        var sid = SanitizeSessionId(sessionId);
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<SeedContextRecord>(SeedCollection);
        return col.Find(x => x.SessionId == sid).OrderBy(x => x.CreatedUtc).ToList();
    }

    public IReadOnlyList<SeedContextRecord> ConsumeSeedContext(string sessionId)
    {
        var sid = SanitizeSessionId(sessionId);
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<SeedContextRecord>(SeedCollection);
        var rows = col.Find(x => x.SessionId == sid).OrderBy(x => x.CreatedUtc).ToList();
        col.DeleteMany(x => x.SessionId == sid);
        return rows;
    }

    public int ClearSeedContext(string sessionId)
    {
        var sid = SanitizeSessionId(sessionId);
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<SeedContextRecord>(SeedCollection);
        return col.DeleteMany(x => x.SessionId == sid);
    }

    private static string GenerateServerId(string? displayName, string? command, string? endpoint)
    {
        var baseValue = displayName ?? command ?? endpoint ?? "mcp-server";
        var chars = baseValue.Trim().ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray();
        var cleaned = new string(chars);
        if (string.IsNullOrWhiteSpace(cleaned))
            cleaned = "mcp-server";
        return cleaned;
    }

    private static string SanitizeSessionId(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return Guid.NewGuid().ToString("N")[..8];
        var chars = sessionId.Trim().Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray();
        return chars.Length == 0 ? Guid.NewGuid().ToString("N")[..8] : new string(chars);
    }
}

public sealed class McpServerRecord
{
    [BsonId]
    public string ServerId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Transport { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public string Command { get; set; } = "";
    public List<string> Arguments { get; set; } = [];
    public string AuthType { get; set; } = "";
    public string AuthConfigJson { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public string MetadataJson { get; set; } = "";
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}

public sealed class AgentMcpMapRecord
{
    [BsonId]
    public string AgentId { get; set; } = "";
    public List<string> ServerIds { get; set; } = [];
    public DateTimeOffset UpdatedUtc { get; set; }
}

public sealed class SeedContextRecord
{
    [BsonId]
    public string SeedId { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string ContextType { get; set; } = "";
    public string Content { get; set; } = "";
    public string Source { get; set; } = "";
    public DateTimeOffset CreatedUtc { get; set; }
}
