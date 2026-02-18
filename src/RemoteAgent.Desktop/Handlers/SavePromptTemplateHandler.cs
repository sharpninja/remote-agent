using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.Handlers;

public sealed class SavePromptTemplateHandler(IServerCapacityClient client)
    : IRequestHandler<SavePromptTemplateRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(
        SavePromptTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var saved = await client.UpsertPromptTemplateAsync(
            request.Host, request.Port, request.Template, request.ApiKey, cancellationToken);

        if (saved is null)
        {
            request.Workspace.PromptTemplateStatus = "Failed to save prompt template.";
            return CommandResult.Fail("Failed to save prompt template.");
        }

        request.Workspace.PromptTemplateStatus = $"Saved template '{saved.TemplateId}'.";

        // Refresh templates inline
        var templates = await client.ListPromptTemplatesAsync(request.Host, request.Port, request.ApiKey, cancellationToken);
        request.Workspace.PromptTemplates.Clear();
        foreach (var row in templates)
            request.Workspace.PromptTemplates.Add(row);

        var savedId = saved.TemplateId;
        request.Workspace.SelectedPromptTemplate =
            request.Workspace.PromptTemplates.FirstOrDefault(x => string.Equals(x.TemplateId, savedId, StringComparison.OrdinalIgnoreCase))
            ?? request.Workspace.PromptTemplates.FirstOrDefault();

        request.Workspace.PromptTemplateStatus = $"Loaded {request.Workspace.PromptTemplates.Count} prompt template(s).";
        return CommandResult.Ok();
    }
}
