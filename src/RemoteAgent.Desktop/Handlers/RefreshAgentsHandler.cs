using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class RefreshAgentsHandler(IServerCapacityClient client)
    : IRequestHandler<RefreshAgentsRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(RefreshAgentsRequest request, CancellationToken cancellationToken = default)
    {
        // Try the new ListAgentRunners API first (provides full runner details).
        try
        {
            var runnersResponse = await ServerApiClient.ListAgentRunnersAsync(
                request.Host, request.Port, request.ApiKey, cancellationToken, throwOnError: true);

            if (runnersResponse != null)
            {
                // Also get server version via GetServerInfo.
                var serverInfo = await client.GetServerInfoAsync(request.Host, request.Port, request.ApiKey, cancellationToken);
                request.Workspace.ServerVersion = serverInfo?.ServerVersion ?? "";

                var agents = new List<AgentSnapshot>();
                foreach (var runner in runnersResponse.Runners)
                {
                    var isDefault = string.Equals(runner.RunnerId, request.CurrentDefaultAgentId, StringComparison.OrdinalIgnoreCase)
                                    || runner.IsDefault;
                    var remaining = runner.MaxConcurrentSessions > 0
                        ? runner.MaxConcurrentSessions - runner.ActiveSessionCount
                        : (int?)null;
                    agents.Add(new AgentSnapshot(
                        runner.RunnerId,
                        runner.ActiveSessionCount,
                        runner.MaxConcurrentSessions > 0 ? runner.MaxConcurrentSessions : null,
                        remaining,
                        isDefault,
                        runner.RunnerType,
                        runner.Command,
                        runner.Arguments,
                        runner.Description));
                }

                request.Workspace.Agents.Clear();
                foreach (var agent in agents)
                    request.Workspace.Agents.Add(agent);

                request.Workspace.SelectedAgent = request.Workspace.Agents.FirstOrDefault(a => a.IsDefault)
                    ?? request.Workspace.Agents.FirstOrDefault();

                request.Workspace.AgentsStatus = $"Loaded {agents.Count} agent runner(s) from server v{request.Workspace.ServerVersion}.";
                return CommandResult.Ok();
            }
        }
        catch
        {
            // Fall through to legacy approach if ListAgentRunners is not available.
        }

        // Legacy fallback: use GetServerInfo + per-agent CheckSessionCapacity.
        var info = await client.GetServerInfoAsync(request.Host, request.Port, request.ApiKey, cancellationToken);
        if (info == null)
        {
            request.Workspace.AgentsStatus = "Failed to retrieve server info.";
            return CommandResult.Fail("Failed to retrieve server info.");
        }

        request.Workspace.ServerVersion = info.ServerVersion;

        var legacyAgents = new List<AgentSnapshot>();
        foreach (var agentId in info.AvailableAgents)
        {
            var isDefault = string.Equals(agentId, request.CurrentDefaultAgentId, StringComparison.OrdinalIgnoreCase);
            try
            {
                var capacity = await client.GetCapacityAsync(request.Host, request.Port, agentId, request.ApiKey, cancellationToken);
                if (capacity != null)
                {
                    legacyAgents.Add(new AgentSnapshot(
                        agentId,
                        capacity.AgentActiveSessionCount,
                        capacity.AgentMaxConcurrentSessions,
                        capacity.RemainingAgentCapacity,
                        isDefault));
                }
                else
                {
                    legacyAgents.Add(new AgentSnapshot(agentId, 0, null, null, isDefault));
                }
            }
            catch
            {
                legacyAgents.Add(new AgentSnapshot(agentId, 0, null, null, isDefault));
            }
        }

        request.Workspace.Agents.Clear();
        foreach (var agent in legacyAgents)
            request.Workspace.Agents.Add(agent);

        request.Workspace.SelectedAgent = request.Workspace.Agents.FirstOrDefault(a => a.IsDefault)
            ?? request.Workspace.Agents.FirstOrDefault();

        request.Workspace.AgentsStatus = $"Loaded {legacyAgents.Count} agent(s) from server v{info.ServerVersion}.";
        return CommandResult.Ok();
    }
}
