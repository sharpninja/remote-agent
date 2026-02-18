using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class SeedSessionContextHandler(IServerCapacityClient client)
    : IRequestHandler<SeedSessionContextRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(SeedSessionContextRequest request, CancellationToken cancellationToken = default)
    {
        var ok = await client.SeedSessionContextAsync(
            request.Host, request.Port, request.SessionId, request.ContextType,
            request.Content, request.Source, request.CorrelationId.ToString(), request.ApiKey, cancellationToken);

        request.Workspace.SeedStatus = ok
            ? $"Seed context queued for session '{request.SessionId}'."
            : $"Failed to seed context for session '{request.SessionId}'.";

        return ok
            ? CommandResult.Ok()
            : CommandResult.Fail("Failed to seed session context.");
    }
}
