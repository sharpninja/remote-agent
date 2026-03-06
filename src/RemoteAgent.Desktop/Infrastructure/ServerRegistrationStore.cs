using LiteDB;

namespace RemoteAgent.Desktop.Infrastructure;

public interface IServerRegistrationStore
{
    IReadOnlyList<ServerRegistration> GetAll();
    ServerRegistration Upsert(ServerRegistration registration);
    bool Delete(string serverId);
}

public sealed class LiteDbServerRegistrationStore(string dbPath) : IServerRegistrationStore
{
    private const string CollectionName = "server_registrations";

    public IReadOnlyList<ServerRegistration> GetAll()
    {
        using var db = new LiteDatabase(dbPath);
        var col = db.GetCollection<ServerRegistration>(CollectionName);
        col.EnsureIndex(x => x.ServerId, unique: true);
        return col.FindAll()
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public ServerRegistration Upsert(ServerRegistration registration)
    {
        if (registration == null)
            throw new ArgumentNullException(nameof(registration));

        var copy = new ServerRegistration
        {
            ServerId = string.IsNullOrWhiteSpace(registration.ServerId) ? Guid.NewGuid().ToString("N") : registration.ServerId.Trim(),
            DisplayName = registration.DisplayName?.Trim() ?? "",
            Host = string.IsNullOrWhiteSpace(registration.Host) ? "127.0.0.1" : registration.Host.Trim(),
            Port = registration.Port <= 0 ? ServiceDefaults.Port : registration.Port,
            ApiKey = registration.ApiKey ?? "",
            PerRequestContext = registration.PerRequestContext ?? "",
            DefaultSessionContext = registration.DefaultSessionContext ?? ""
        };
        if (string.IsNullOrWhiteSpace(copy.DisplayName))
            copy.DisplayName = $"{copy.Host}:{copy.Port}";

        using var db = new LiteDatabase(dbPath);
        var col = db.GetCollection<ServerRegistration>(CollectionName);
        col.EnsureIndex(x => x.ServerId, unique: true);
        col.Upsert(copy);
        return copy;
    }

    public bool Delete(string serverId)
    {
        if (string.IsNullOrWhiteSpace(serverId))
            return false;

        using var db = new LiteDatabase(dbPath);
        var col = db.GetCollection<ServerRegistration>(CollectionName);
        col.EnsureIndex(x => x.ServerId, unique: true);
        return col.DeleteMany(x => x.ServerId == serverId.Trim()) > 0;
    }
}
