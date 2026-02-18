using RemoteAgent.App.Logic.Cqrs;

namespace RemoteAgent.Desktop.Requests;

public sealed record RemoveServerRegistrationRequest(
    Guid CorrelationId,
    string ServerId,
    string DisplayName) : IRequest<CommandResult>;
