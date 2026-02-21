using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class TerminateDesktopSessionHandler(IServerCapacityClient capacityClient)
    : IRequestHandler<TerminateDesktopSessionRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(
        TerminateDesktopSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = request.Session;
        if (session is null)
            return CommandResult.Fail("No session specified.");

        var workspace = request.Workspace;

        // Notify the server to end the session before disconnecting locally.
        if (session.IsConnected &&
            int.TryParse(workspace.Port, out var port))
        {
            try
            {
                await capacityClient.TerminateSessionAsync(
                    workspace.Host, port, session.SessionId, workspace.ApiKey, cancellationToken);
            }
            catch
            {
                // best-effort; still perform local cleanup below
            }
        }

        try
        {
            if (session.SessionClient.IsConnected)
                await session.SessionClient.StopSessionAsync();
        }
        catch
        {
            // best-effort stop; proceed to disconnect
        }

        session.SessionClient.Disconnect();

        var title = session.Title;
        workspace.UnregisterSessionEvents(session);
        workspace.Sessions.Remove(session);
        if (workspace.SelectedSession == session)
            workspace.SelectedSession = workspace.Sessions.FirstOrDefault();

        workspace.StatusText = $"Terminated {title}.";
        return CommandResult.Ok();
    }
}
