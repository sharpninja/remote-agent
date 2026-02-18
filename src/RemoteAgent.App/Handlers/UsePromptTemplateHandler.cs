using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Requests;
using RemoteAgent.App.Services;

namespace RemoteAgent.App.Handlers;

public sealed class UsePromptTemplateHandler(
    IServerApiClient apiClient,
    IPromptTemplateSelector templateSelector,
    IPromptVariableProvider variableProvider,
    IAgentGatewayClient gateway)
    : IRequestHandler<UsePromptTemplateRequest, CommandResult>
{
    private const string DefaultPort = "5243";

    public async Task<CommandResult> HandleAsync(UsePromptTemplateRequest request, CancellationToken ct = default)
    {
        var workspace = request.Workspace;
        var host = (workspace.Host ?? "").Trim();
        var portText = (workspace.Port ?? DefaultPort).Trim();

        if (string.IsNullOrWhiteSpace(host))
        {
            workspace.Status = "Enter host to load templates.";
            return CommandResult.Fail("Enter host to load templates.");
        }

        if (!int.TryParse(portText, out var port) || port <= 0 || port > 65535)
        {
            workspace.Status = "Enter a valid port (1-65535).";
            return CommandResult.Fail("Enter a valid port (1-65535).");
        }

        var response = await apiClient.ListPromptTemplatesAsync(host, port, ct: ct);
        if (response == null || response.Templates.Count == 0)
        {
            workspace.Status = "No prompt templates available.";
            return CommandResult.Fail("No prompt templates available.");
        }

        var template = await templateSelector.SelectAsync(response.Templates.ToList());
        if (template == null)
        {
            workspace.Status = "Prompt template selection cancelled.";
            return CommandResult.Fail("Prompt template selection cancelled.");
        }

        var variables = PromptTemplateEngine.ExtractVariables(template.TemplateContent);
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var variable in variables)
        {
            var value = await variableProvider.GetValueAsync(variable);
            if (value == null)
            {
                workspace.Status = "Prompt template input cancelled.";
                return CommandResult.Fail("Prompt template input cancelled.");
            }

            data[variable] = value;
        }

        workspace.PendingMessage = PromptTemplateEngine.Render(template.TemplateContent, data);

        // Send the rendered message immediately (mirrors original UsePromptTemplateAsync calling SendMessageAsync)
        var text = workspace.PendingMessage.Trim();
        if (!string.IsNullOrWhiteSpace(text) && gateway.IsConnected)
        {
            if (workspace.CurrentSession != null &&
                (workspace.CurrentSession.Title == "New chat" || string.IsNullOrWhiteSpace(workspace.CurrentSession.Title)))
            {
                workspace.CurrentSession.Title = text.Length > 60 ? text[..60] + "…" : text;
                workspace.UpdateSessionTitle(workspace.CurrentSession.SessionId, workspace.CurrentSession.Title);
            }

            gateway.AddUserMessage(new ChatMessage { IsUser = true, Text = text });
            workspace.PendingMessage = "";

            try
            {
                await gateway.SendTextAsync(text, ct);
            }
            catch (Exception ex)
            {
                gateway.Messages.Add(new ChatMessage { IsError = true, Text = ex.Message });
                return CommandResult.Fail(ex.Message);
            }
        }

        return CommandResult.Ok();
    }
}
