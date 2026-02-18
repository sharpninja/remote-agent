using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;

namespace RemoteAgent.Desktop.Requests;

public sealed record CheckLocalServerRequest(Guid CorrelationId) : IRequest<CommandResult<LocalServerProbeResult>>;
