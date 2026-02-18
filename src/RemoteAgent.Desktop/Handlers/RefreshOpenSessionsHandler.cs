using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class RefreshOpenSessionsHandler(IServerCapacityClient client)
    : IRequestHandler<RefreshOpenSessionsRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(
        RefreshOpenSessionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var sessions = await client.GetOpenSessionsAsync(
            request.Host, request.Port, request.ApiKey, cancellationToken);

        request.Workspace.OpenServerSessions.Clear();
        foreach (var s in sessions)
            request.Workspace.OpenServerSessions.Add(s);
        request.Workspace.SelectedOpenServerSession = request.Workspace.OpenServerSessions.FirstOrDefault();
        request.Workspace.StatusText = $"Loaded {request.Workspace.OpenServerSessions.Count} open server session(s).";

        return CommandResult.Ok();
    }
}
