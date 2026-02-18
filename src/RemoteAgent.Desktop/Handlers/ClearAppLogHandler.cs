using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class ClearAppLogHandler(IAppLogStore store)
    : IRequestHandler<ClearAppLogRequest, CommandResult>
{
    public Task<CommandResult> HandleAsync(ClearAppLogRequest request, CancellationToken cancellationToken = default)
    {
        store.Clear();
        request.Workspace.Entries.Clear();
        request.Workspace.StatusText = "App log cleared.";
        return Task.FromResult(CommandResult.Ok());
    }
}
