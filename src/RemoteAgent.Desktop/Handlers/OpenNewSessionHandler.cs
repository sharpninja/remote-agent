using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class OpenNewSessionHandler(
    IConnectionSettingsDialogService dialogService,
    IRequestDispatcher dispatcher)
    : IRequestHandler<OpenNewSessionRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(
        OpenNewSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerWindow = request.OwnerWindowFactory();
        if (ownerWindow is null)
            return CommandResult.Fail("No owner window available.");

        var workspace = request.Workspace;
        var availableAgents = workspace.Agents.Agents
            .Select(a => a.AgentId)
            .ToList();

        var defaults = new ConnectionSettingsDefaults(
            workspace.Host,
            workspace.Port,
            workspace.SelectedConnectionMode,
            workspace.SelectedAgentId,
            workspace.ApiKey,
            workspace.PerRequestContext,
            workspace.ConnectionModes,
            availableAgents);

        var result = await dialogService.ShowAsync(ownerWindow, defaults, cancellationToken);
        if (result is null)
            return CommandResult.Fail("Cancelled.");

        workspace.Host = result.Host;
        workspace.Port = result.Port;
        workspace.SelectedConnectionMode = result.SelectedConnectionMode;
        workspace.SelectedAgentId = result.SelectedAgentId;
        workspace.ApiKey = result.ApiKey;
        workspace.PerRequestContext = result.PerRequestContext;

        if (!int.TryParse((result.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535)
            return CommandResult.Fail("Port must be 1-65535.");

        var title = $"Session {workspace.Sessions.Count + 1}";
        var createResult = await dispatcher.SendAsync(
            new CreateDesktopSessionRequest(
                request.CorrelationId,
                title,
                result.Host,
                port,
                result.SelectedConnectionMode,
                result.SelectedAgentId,
                result.ApiKey,
                result.PerRequestContext,
                Workspace: workspace),
            cancellationToken);

        if (!createResult.Success)
            return CommandResult.Fail(createResult.ErrorMessage ?? "Failed to create session.");

        return CommandResult.Ok();
    }
}
