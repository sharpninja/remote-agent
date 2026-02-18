using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class RemoveServerRegistrationHandler(IServerRegistrationStore store)
    : IRequestHandler<RemoveServerRegistrationRequest, CommandResult>
{
    public Task<CommandResult> HandleAsync(
        RemoveServerRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ServerId))
            return Task.FromResult(CommandResult.Fail("Server ID is required."));

        var deleted = store.Delete(request.ServerId);
        return deleted
            ? Task.FromResult(CommandResult.Ok())
            : Task.FromResult(CommandResult.Fail($"Failed to remove server '{request.DisplayName}'."));
    }
}
