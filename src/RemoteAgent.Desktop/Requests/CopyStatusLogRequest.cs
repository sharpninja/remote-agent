using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record CopyStatusLogRequest(
    Guid CorrelationId,
    IReadOnlyList<StatusLogEntry> Entries) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"CopyStatusLogRequest {{ CorrelationId = {CorrelationId}, EntryCount = {Entries.Count} }}";
}
