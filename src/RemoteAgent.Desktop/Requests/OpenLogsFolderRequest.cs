using RemoteAgent.App.Logic.Cqrs;

namespace RemoteAgent.Desktop.Requests;

public sealed record OpenLogsFolderRequest(
    Guid CorrelationId,
    string FolderPath) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"OpenLogsFolderRequest {{ CorrelationId = {CorrelationId}, FolderPath = {FolderPath} }}";
}
