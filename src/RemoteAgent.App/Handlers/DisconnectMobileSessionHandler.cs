using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Requests;

namespace RemoteAgent.App.Handlers;

public sealed class DisconnectMobileSessionHandler(IAgentGatewayClient gateway)
    : IRequestHandler<DisconnectMobileSessionRequest, CommandResult>
{
    public Task<CommandResult> HandleAsync(DisconnectMobileSessionRequest request, CancellationToken ct = default)
    {
        gateway.Disconnect();
        request.Workspace.NotifyConnectionStateChanged();
        return Task.FromResult(CommandResult.Ok());
    }
}
