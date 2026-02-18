using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

/// <summary>Format values: "txt", "json", "csv"</summary>
public sealed record SaveAppLogRequest(
    Guid CorrelationId,
    IReadOnlyList<AppLogEntry> Entries,
    string Format,
    string FilePath,
    AppLogViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"SaveAppLogRequest {{ CorrelationId = {CorrelationId}, Format = {Format}, EntryCount = {Entries.Count}, FilePath = {FilePath} }}";
}
