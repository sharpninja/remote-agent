using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Requests;

public sealed record DeletePromptTemplateRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    string TemplateId,
    string? ApiKey,
    PromptTemplatesViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"DeletePromptTemplateRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, TemplateId = {TemplateId}, ApiKey = [REDACTED] }}";
}
