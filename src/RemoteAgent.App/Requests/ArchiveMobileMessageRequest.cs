using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Services;
using RemoteAgent.App.ViewModels;

namespace RemoteAgent.App.Requests;

public sealed record ArchiveMobileMessageRequest(
    Guid CorrelationId,
    ChatMessage? Message,
    MainPageViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"ArchiveMobileMessageRequest {{ CorrelationId = {CorrelationId} }}";
}
