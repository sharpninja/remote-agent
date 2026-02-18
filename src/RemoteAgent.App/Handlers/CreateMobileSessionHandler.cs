using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Requests;
using RemoteAgent.App.Services;

namespace RemoteAgent.App.Handlers;

public sealed class CreateMobileSessionHandler(ISessionStore sessionStore)
    : IRequestHandler<CreateMobileSessionRequest, CommandResult>
{
    public Task<CommandResult> HandleAsync(CreateMobileSessionRequest request, CancellationToken ct = default)
    {
        var session = new SessionItem
        {
            SessionId = Guid.NewGuid().ToString("N")[..12],
            Title = "New chat",
            AgentId = "",
            ConnectionMode = "server"
        };
        sessionStore.Add(session);
        request.Workspace.Sessions.Insert(0, session);
        request.Workspace.CurrentSession = session;
        return Task.FromResult(CommandResult.Ok());
    }
}
