using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Requests;
using RemoteAgent.App.Services;
using RemoteAgent.App.ViewModels;
using RemoteAgent.Proto;

namespace RemoteAgent.App.Handlers;

public sealed class SendMobileMessageHandler(IAgentGatewayClient gateway)
    : IRequestHandler<SendMobileMessageRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(SendMobileMessageRequest request, CancellationToken ct = default)
    {
        var workspace = request.Workspace;
        var text = (workspace.PendingMessage ?? "").Trim();

        if (string.IsNullOrWhiteSpace(text) || !gateway.IsConnected)
            return CommandResult.Fail("No message or not connected.");

        if (workspace.CurrentSession != null &&
            (workspace.CurrentSession.Title == "New chat" || string.IsNullOrWhiteSpace(workspace.CurrentSession.Title)))
        {
            workspace.CurrentSession.Title = text.Length > 60 ? text[..60] + "…" : text;
            workspace.UpdateSessionTitle(workspace.CurrentSession.SessionId, workspace.CurrentSession.Title);
        }

        gateway.AddUserMessage(new ChatMessage { IsUser = true, Text = text });
        workspace.PendingMessage = "";

        try
        {
            if (MainPageViewModel.TryParseScriptRun(text, out var pathOrCommand, out var scriptType))
                await gateway.SendScriptRequestAsync(pathOrCommand, scriptType, ct);
            else
                await gateway.SendTextAsync(text, ct);
        }
        catch (Exception ex)
        {
            gateway.Messages.Add(new ChatMessage { IsError = true, Text = ex.Message });
            return CommandResult.Fail(ex.Message);
        }

        return CommandResult.Ok();
    }
}
