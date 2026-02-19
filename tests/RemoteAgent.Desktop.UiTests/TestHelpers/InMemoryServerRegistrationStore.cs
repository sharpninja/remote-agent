using RemoteAgent.Desktop.Infrastructure;

namespace RemoteAgent.Desktop.UiTests.TestHelpers;

/// <summary>In-memory <see cref="IServerRegistrationStore"/> stub used by Desktop handler tests. TR-18.3.</summary>
public sealed class InMemoryServerRegistrationStore : IServerRegistrationStore
{
    private readonly List<ServerRegistration> _servers =
    [
        new ServerRegistration
        {
            ServerId = "srv-local",
            DisplayName = "Local",
            Host = "127.0.0.1",
            Port = 5243,
            ApiKey = ""
        }
    ];

    public IReadOnlyList<ServerRegistration> GetAll() => _servers.ToList();

    public ServerRegistration Upsert(ServerRegistration registration)
    {
        var copy = new ServerRegistration
        {
            ServerId = string.IsNullOrWhiteSpace(registration.ServerId) ? Guid.NewGuid().ToString("N") : registration.ServerId,
            DisplayName = registration.DisplayName,
            Host = registration.Host,
            Port = registration.Port,
            ApiKey = registration.ApiKey
        };
        var existing = _servers.FindIndex(x => x.ServerId == copy.ServerId);
        if (existing >= 0)
            _servers[existing] = copy;
        else
            _servers.Add(copy);
        return copy;
    }

    public bool Delete(string serverId)
    {
        return _servers.RemoveAll(x => x.ServerId == serverId) > 0;
    }
}
