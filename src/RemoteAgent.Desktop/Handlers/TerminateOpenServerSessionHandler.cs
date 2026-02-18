using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class TerminateOpenServerSessionHandler(IServerCapacityClient client)
    : IRequestHandler<TerminateOpenServerSessionRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(
        TerminateOpenServerSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var success = await client.TerminateSessionAsync(
            request.Host, request.Port, request.SessionId, request.ApiKey, cancellationToken);

        if (!success)
        {
            request.Workspace.StatusText = $"Failed to terminate server session {request.SessionId}.";
            return CommandResult.Fail($"Failed to terminate server session {request.SessionId}.");
        }

        request.Workspace.StatusText = $"Terminated server session {request.SessionId}.";

        // Refresh open sessions inline
        var sessions = await client.GetOpenSessionsAsync(
            request.Host, request.Port, request.ApiKey, cancellationToken);
        request.Workspace.OpenServerSessions.Clear();
        foreach (var s in sessions)
            request.Workspace.OpenServerSessions.Add(s);
        request.Workspace.SelectedOpenServerSession = request.Workspace.OpenServerSessions.FirstOrDefault();

        return CommandResult.Ok();
    }
}
