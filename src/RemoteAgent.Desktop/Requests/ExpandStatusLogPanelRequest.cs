using RemoteAgent.App.Logic.Cqrs;

namespace RemoteAgent.Desktop.Requests;

public sealed record ExpandStatusLogPanelRequest(Guid CorrelationId) : IRequest<Unit>;
