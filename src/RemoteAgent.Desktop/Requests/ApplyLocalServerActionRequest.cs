using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;

namespace RemoteAgent.Desktop.Requests;

public sealed record ApplyLocalServerActionRequest(
    Guid CorrelationId,
    bool IsCurrentlyRunning) : IRequest<CommandResult<LocalServerProbeResult>>;
