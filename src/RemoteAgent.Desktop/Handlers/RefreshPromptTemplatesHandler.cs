using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.Handlers;

public sealed class RefreshPromptTemplatesHandler(IServerCapacityClient client)
    : IRequestHandler<RefreshPromptTemplatesRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(
        RefreshPromptTemplatesRequest request,
        CancellationToken cancellationToken = default)
    {
        var templates = await client.ListPromptTemplatesAsync(
            request.Host, request.Port, request.ApiKey, cancellationToken);

        request.Workspace.PromptTemplates.Clear();
        foreach (var row in templates)
            request.Workspace.PromptTemplates.Add(row);
        request.Workspace.SelectedPromptTemplate = request.Workspace.PromptTemplates.FirstOrDefault();
        request.Workspace.PromptTemplateStatus = $"Loaded {request.Workspace.PromptTemplates.Count} prompt template(s).";

        return CommandResult.Ok();
    }
}
