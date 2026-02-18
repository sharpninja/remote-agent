using RemoteAgent.App.Logic.Cqrs;

namespace RemoteAgent.Desktop.Requests;

public sealed record SetManagementSectionRequest(Guid CorrelationId, string SectionKey) : IRequest<Unit>;
