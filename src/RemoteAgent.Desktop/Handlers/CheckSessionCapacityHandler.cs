using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class CheckSessionCapacityHandler(IServerCapacityClient client)
    : IRequestHandler<CheckSessionCapacityRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(
        CheckSessionCapacityRequest request,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await client.GetCapacityAsync(
            request.Host, request.Port, request.AgentId, request.ApiKey, cancellationToken);

        if (snapshot is null)
        {
            request.Workspace.CapacitySummary = "Capacity check failed.";
            request.Workspace.StatusText = "Capacity endpoint returned no data.";
            return CommandResult.Fail("Capacity endpoint returned no data.");
        }

        request.Workspace.CapacitySummary =
            $"Server {snapshot.ActiveSessionCount}/{snapshot.MaxConcurrentSessions} active, remaining {snapshot.RemainingServerCapacity}; " +
            $"Agent {snapshot.AgentActiveSessionCount}/{snapshot.AgentMaxConcurrentSessions?.ToString() ?? "-"}.";
        request.Workspace.StatusText = snapshot.CanCreateSession ? "Capacity available." : snapshot.Reason;
        return CommandResult.Ok();
    }
}
