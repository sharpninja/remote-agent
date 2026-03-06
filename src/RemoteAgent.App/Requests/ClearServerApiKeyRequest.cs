using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.ViewModels;

namespace RemoteAgent.App.Requests;

public sealed record ClearServerApiKeyRequest(
    Guid CorrelationId,
    SettingsPageViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"ClearServerApiKeyRequest {{ CorrelationId = {CorrelationId} }}";
}
