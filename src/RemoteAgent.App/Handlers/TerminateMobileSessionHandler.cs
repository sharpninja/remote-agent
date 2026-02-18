using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Requests;
using RemoteAgent.App.Services;

namespace RemoteAgent.App.Handlers;

public sealed class TerminateMobileSessionHandler(
    IAgentGatewayClient gateway,
    ISessionStore sessionStore,
    ISessionTerminationConfirmation confirmation)
    : IRequestHandler<TerminateMobileSessionRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(TerminateMobileSessionRequest request, CancellationToken ct = default)
    {
        var session = request.Session;
        var workspace = request.Workspace;

        if (session == null)
        {
            workspace.Status = "No session selected.";
            return CommandResult.Fail("No session selected.");
        }

        var sessionLabel = string.IsNullOrWhiteSpace(session.Title) ? session.SessionId : session.Title;
        var confirmed = await confirmation.ConfirmAsync(sessionLabel);
        if (!confirmed)
        {
            workspace.Status = "Terminate cancelled.";
            return CommandResult.Fail("Terminate cancelled.");
        }

        var isCurrent = string.Equals(workspace.CurrentSession?.SessionId, session.SessionId, StringComparison.Ordinal);
        if (isCurrent && gateway.IsConnected)
        {
            try
            {
                await gateway.StopSessionAsync(ct);
            }
            catch
            {
                // best effort; always close local transport after stop request attempt
            }

            gateway.Disconnect();
        }

        sessionStore.Delete(session.SessionId);
        workspace.Sessions.Remove(session);
        if (isCurrent)
            workspace.CurrentSession = workspace.Sessions.FirstOrDefault();

        workspace.Status = $"Session terminated: {sessionLabel}";
        return CommandResult.Ok();
    }
}
