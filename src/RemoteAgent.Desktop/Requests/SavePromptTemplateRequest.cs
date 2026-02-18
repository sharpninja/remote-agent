using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.ViewModels;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.Requests;

public sealed record SavePromptTemplateRequest(
    Guid CorrelationId,
    string Host,
    int Port,
    PromptTemplateDefinition Template,
    string? ApiKey,
    PromptTemplatesViewModel Workspace) : IRequest<CommandResult>
{
    public override string ToString() =>
        $"SavePromptTemplateRequest {{ CorrelationId = {CorrelationId}, Host = {Host}, Port = {Port}, TemplateId = {Template.TemplateId}, ApiKey = [REDACTED] }}";
}
