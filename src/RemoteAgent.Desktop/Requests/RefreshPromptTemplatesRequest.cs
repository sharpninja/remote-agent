using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.ViewModels;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.Requests;

public sealed record PromptTemplatesData(
    IReadOnlyList<PromptTemplateDefinition> Templates);

public sealed record RefreshPromptTemplatesRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    string? ApiKey,
    PromptTemplatesViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"RefreshPromptTemplatesRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, ApiKey = [REDACTED] }}";
}
