using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class SaveServerRegistrationHandler(IServerRegistrationStore store)
    : IRequestHandler<SaveServerRegistrationRequest, CommandResult<ServerRegistration>>
{
    public Task<CommandResult<ServerRegistration>> HandleAsync(
        SaveServerRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Host))
            return Task.FromResult(CommandResult<ServerRegistration>.Fail("Server host is required."));

        if (request.Port is <= 0 or > 65535)
            return Task.FromResult(CommandResult<ServerRegistration>.Fail("Server port must be 1-65535."));

        var registration = new ServerRegistration
        {
            ServerId = request.ExistingServerId ?? Guid.NewGuid().ToString("N"),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? $"{request.Host}:{request.Port}"
                : request.DisplayName.Trim(),
            Host = request.Host.Trim(),
            Port = request.Port,
            ApiKey = request.ApiKey ?? "",
            PerRequestContext = request.PerRequestContext ?? "",
            DefaultSessionContext = request.DefaultSessionContext ?? ""
        };

        var saved = store.Upsert(registration);
        return Task.FromResult(CommandResult<ServerRegistration>.Ok(saved));
    }
}
