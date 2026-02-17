using LiteDB;
using Microsoft.Extensions.Options;

namespace RemoteAgent.Service.Services;

/// <summary>Persistent local auth-user and permission-role management for operator workflows.</summary>
public sealed class AuthUserService
{
    private readonly string _dbPath;
    private const string CollectionName = "auth_users";
    private static readonly string[] DefaultRoles = ["viewer", "operator", "admin"];

    public AuthUserService(IOptions<AgentOptions> options)
    {
        var dataDir = options.Value.DataDirectory?.Trim();
        if (string.IsNullOrWhiteSpace(dataDir))
            dataDir = Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(dataDir);
        _dbPath = Path.Combine(dataDir, "remote-agent.db");
    }

    public IReadOnlyList<AuthUserRecord> List()
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<AuthUserRecord>(CollectionName);
            col.EnsureIndex(x => x.UserId, unique: true);
            return col.FindAll()
                .OrderBy(x => x.UserId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public IReadOnlyList<string> ListRoles() => DefaultRoles;

    public AuthUserRecord Upsert(AuthUserRecord user)
    {
        var now = DateTimeOffset.UtcNow;
        var normalizedId = string.IsNullOrWhiteSpace(user.UserId) ? $"user-{Guid.NewGuid():N}" : user.UserId.Trim();
        var normalizedRole = NormalizeRole(user.Role);
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<AuthUserRecord>(CollectionName);
            col.EnsureIndex(x => x.UserId, unique: true);
            var existing = col.FindById(normalizedId);
            var row = new AuthUserRecord
            {
                UserId = normalizedId,
                DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? normalizedId : user.DisplayName.Trim(),
                Role = normalizedRole,
                Enabled = user.Enabled,
                CreatedUtc = existing?.CreatedUtc ?? now,
                UpdatedUtc = now
            };
            col.Upsert(row);
            return row;
        }
        catch
        {
            return new AuthUserRecord
            {
                UserId = normalizedId,
                DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? normalizedId : user.DisplayName.Trim(),
                Role = normalizedRole,
                Enabled = user.Enabled,
                CreatedUtc = now,
                UpdatedUtc = now
            };
        }
    }

    public bool Delete(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        try
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<AuthUserRecord>(CollectionName);
            return col.Delete(userId.Trim());
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return "viewer";
        var value = role.Trim().ToLowerInvariant();
        return DefaultRoles.Contains(value, StringComparer.OrdinalIgnoreCase) ? value : "viewer";
    }
}

public sealed class AuthUserRecord
{
    [BsonId]
    public string UserId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "viewer";
    public bool Enabled { get; set; } = true;
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}
