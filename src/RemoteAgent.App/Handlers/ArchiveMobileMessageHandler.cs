using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Requests;
using RemoteAgent.App.Services;

namespace RemoteAgent.App.Handlers;

public sealed class ArchiveMobileMessageHandler(IAgentGatewayClient gateway)
    : IRequestHandler<ArchiveMobileMessageRequest, CommandResult>
{
    public Task<CommandResult> HandleAsync(ArchiveMobileMessageRequest request, CancellationToken ct = default)
    {
        var message = request.Message;
        if (message == null)
            return Task.FromResult(CommandResult.Fail("No message."));

        message.IsArchived = true;
        gateway.SetArchived(message, true);
        return Task.FromResult(CommandResult.Ok());
    }
}
