using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Requests;
using RemoteAgent.App.Services;

namespace RemoteAgent.App.Handlers;

public sealed class SendMobileAttachmentHandler(IAgentGatewayClient gateway, IAttachmentPicker attachmentPicker)
    : IRequestHandler<SendMobileAttachmentRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(SendMobileAttachmentRequest request, CancellationToken ct = default)
    {
        if (!gateway.IsConnected)
            return CommandResult.Fail("Not connected.");

        var picked = await attachmentPicker.PickAsync();
        if (picked == null)
            return CommandResult.Fail("No attachment selected.");

        try
        {
            gateway.AddUserMessage(new ChatMessage { IsUser = true, Text = $"[Attachment: {picked.FileName}]" });
            await gateway.SendMediaAsync(picked.Content, picked.ContentType, picked.FileName, ct);
        }
        catch (Exception ex)
        {
            gateway.Messages.Add(new ChatMessage { IsError = true, Text = ex.Message });
            return CommandResult.Fail(ex.Message);
        }

        return CommandResult.Ok();
    }
}
