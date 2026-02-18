using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class DeletePromptTemplateHandler(IServerCapacityClient client)
    : IRequestHandler<DeletePromptTemplateRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(DeletePromptTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var ok = await client.DeletePromptTemplateAsync(
            request.Host, request.Port, request.TemplateId, request.ApiKey, cancellationToken);

        request.Workspace.PromptTemplateStatus = ok
            ? $"Deleted template '{request.TemplateId}'."
            : $"Failed to delete template '{request.TemplateId}'.";

        // Refresh templates inline
        var templates = await client.ListPromptTemplatesAsync(request.Host, request.Port, request.ApiKey, cancellationToken);
        request.Workspace.PromptTemplates.Clear();
        foreach (var row in templates)
            request.Workspace.PromptTemplates.Add(row);
        request.Workspace.SelectedPromptTemplate = request.Workspace.PromptTemplates.FirstOrDefault();

        request.Workspace.PromptTemplateStatus = $"Loaded {request.Workspace.PromptTemplates.Count} prompt template(s).";
        return ok ? CommandResult.Ok() : CommandResult.Fail($"Failed to delete prompt template {request.TemplateId}.");
    }
}
