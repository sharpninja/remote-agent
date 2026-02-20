using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.App.Requests;
using RemoteAgent.App.Services;

namespace RemoteAgent.App.Handlers;

public sealed class ConnectMobileSessionHandler(
    IAgentGatewayClient gateway,
    ISessionStore sessionStore,
    IServerApiClient apiClient,
    IAgentSelector agentSelector,
    IAppPreferences preferences)
    : IRequestHandler<ConnectMobileSessionRequest, CommandResult>
{
    private const string PrefServerHost = "ServerHost";
    private const string PrefServerPort = "ServerPort";
    private const string PrefApiKey     = "ApiKey";
    private const string DefaultPort = "5243";

    public async Task<CommandResult> HandleAsync(ConnectMobileSessionRequest request, CancellationToken ct = default)
    {
        var workspace = request.Workspace;

        var host = (workspace.Host ?? "").Trim();
        var portText = (workspace.Port ?? DefaultPort).Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            workspace.Status = "Enter a host.";
            return CommandResult.Fail("Enter a host.");
        }

        if (!int.TryParse(portText, out var port) || port <= 0 || port > 65535)
        {
            workspace.Status = "Enter a valid port (1-65535).";
            return CommandResult.Fail("Enter a valid port (1-65535).");
        }

        SessionItem? sessionToConnect = workspace.CurrentSession;
        if (sessionToConnect == null)
        {
            sessionToConnect = new SessionItem
            {
                SessionId = Guid.NewGuid().ToString("N")[..12],
                Title = "New chat",
                ConnectionMode = "server"
            };
            workspace.Sessions.Insert(0, sessionToConnect);
            sessionStore.Add(sessionToConnect);
            workspace.CurrentSession = sessionToConnect;
        }

        if (string.IsNullOrWhiteSpace(sessionToConnect.AgentId))
        {
            workspace.Status = "Getting server info...";
            var serverInfo = await apiClient.GetServerInfoAsync(host, port, apiKey: workspace.ApiKey, ct: ct);
            if (serverInfo == null)
            {
                workspace.Status = "Could not reach server.";
                return CommandResult.Fail("Could not reach server.");
            }

            var agentId = await agentSelector.SelectAsync(serverInfo);
            if (agentId == null)
            {
                workspace.Status = "Connect cancelled.";
                return CommandResult.Fail("Connect cancelled.");
            }

            sessionToConnect.AgentId = agentId;
            sessionStore.UpdateAgentId(sessionToConnect.SessionId, sessionToConnect.AgentId);
        }

        sessionToConnect.ConnectionMode = "server";
        sessionStore.UpdateConnectionMode(sessionToConnect.SessionId, "server");

        var capacity = await apiClient.GetSessionCapacityAsync(host, port, sessionToConnect.AgentId, apiKey: workspace.ApiKey, ct: ct);
        if (capacity == null)
        {
            workspace.Status = "Could not verify server session capacity.";
            return CommandResult.Fail("Could not verify server session capacity.");
        }

        if (!capacity.CanCreateSession)
        {
            workspace.Status = string.IsNullOrWhiteSpace(capacity.Reason)
                ? "Server session capacity reached."
                : capacity.Reason;
            return CommandResult.Fail(workspace.Status);
        }

        workspace.Status = "Connecting (server)...";
        try
        {
            await gateway.ConnectAsync(host, port, sessionToConnect.SessionId, sessionToConnect.AgentId,
                apiKey: workspace.ApiKey, ct: ct);
            preferences.Set(PrefServerHost, host ?? "");
            preferences.Set(PrefServerPort, port.ToString());
            preferences.Set(PrefApiKey,     workspace.ApiKey ?? "");
            workspace.Host = host ?? "";
            workspace.Port = port.ToString();
            workspace.Status = "Connected (server).";
            workspace.NotifyConnectionStateChanged();
        }
        catch (Exception ex)
        {
            workspace.Status = $"Failed: {ex.Message}";
            return CommandResult.Fail(workspace.Status);
        }

        return CommandResult.Ok();
    }
}
