using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class TerminateDesktopSessionHandler
    : IRequestHandler<TerminateDesktopSessionRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(
        TerminateDesktopSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = request.Session;
        if (session is null)
            return CommandResult.Fail("No session specified.");

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

        var workspace = request.Workspace;
        var title = session.Title;
        workspace.Sessions.Remove(session);
        if (workspace.SelectedSession == session)
            workspace.SelectedSession = workspace.Sessions.FirstOrDefault();

        workspace.StatusText = $"Terminated {title}.";
        return CommandResult.Ok();
    }
}
