using LiteDB;
using RemoteAgent.App.Logic;

namespace RemoteAgent.App.Services;

/// <summary>LiteDB implementation of <see cref="IServerProfileStore"/>.</summary>
public sealed class LocalServerProfileStore : IServerProfileStore
{
    private readonly string _dbPath;
    private const string CollectionName = "server_profiles";

    public LocalServerProfileStore(string dbPath)
    {
        _dbPath = dbPath;
    }

    public IReadOnlyList<ServerProfile> GetAll()
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<StoredProfile>(CollectionName);
            return col.FindAll().Select(ToProfile).ToList();
        }
        catch
        {
            return Array.Empty<ServerProfile>();
        }
    }

    public ServerProfile? GetByHostPort(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host)) return null;
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<StoredProfile>(CollectionName);
            var key = MakeKey(host, port);
            var record = col.FindOne(r => r.HostPortKey == key);
            return record == null ? null : ToProfile(record);
        }
        catch
        {
            return null;
        }
    }

    public void Upsert(ServerProfile profile)
    {
        using var db = new LiteDatabase(_dbPath);
        var col = db.GetCollection<StoredProfile>(CollectionName);
        col.EnsureIndex(r => r.HostPortKey, true);

        var key = MakeKey(profile.Host, profile.Port);
        var existing = col.FindOne(r => r.HostPortKey == key);

        if (existing != null)
        {
            existing.Host = (profile.Host ?? "").Trim();
            existing.Port = profile.Port;
            existing.ApiKey = profile.ApiKey ?? "";
            existing.DisplayName = profile.DisplayName ?? "";
            existing.PerRequestContext = profile.PerRequestContext ?? "";
            existing.DefaultSessionContext = profile.DefaultSessionContext ?? "";
            existing.HostPortKey = key;
            col.Update(existing);
        }
        else
        {
            col.Insert(new StoredProfile
            {
                Host = (profile.Host ?? "").Trim(),
                Port = profile.Port,
                ApiKey = profile.ApiKey ?? "",
                DisplayName = profile.DisplayName ?? "",
                PerRequestContext = profile.PerRequestContext ?? "",
                DefaultSessionContext = profile.DefaultSessionContext ?? "",
                HostPortKey = key
            });
        }
    }

    public bool Delete(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host)) return false;
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<StoredProfile>(CollectionName);
            var key = MakeKey(host, port);
            return col.DeleteMany(r => r.HostPortKey == key) > 0;
        }
        catch
        {
            return false;
        }
    }

    private static string MakeKey(string host, int port) =>
        $"{(host ?? "").Trim().ToLowerInvariant()}:{port}";

    private static ServerProfile ToProfile(StoredProfile r) => new()
    {
        Host = r.Host,
        Port = r.Port,
        ApiKey = r.ApiKey,
        DisplayName = r.DisplayName,
        PerRequestContext = r.PerRequestContext,
        DefaultSessionContext = r.DefaultSessionContext
    };

    private sealed class StoredProfile
    {
        public int Id { get; set; }
        public string HostPortKey { get; set; } = "";
        public string Host { get; set; } = "";
        public int Port { get; set; }
        public string ApiKey { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string PerRequestContext { get; set; } = "";
        public string DefaultSessionContext { get; set; } = "";
    }
}
