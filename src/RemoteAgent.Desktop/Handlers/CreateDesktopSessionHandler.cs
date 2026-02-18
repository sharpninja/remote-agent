using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Handlers;

public sealed class CreateDesktopSessionHandler(
    IServerCapacityClient capacityClient,
    IDesktopSessionViewModelFactory sessionFactory)
    : IRequestHandler<CreateDesktopSessionRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(
        CreateDesktopSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(request.ConnectionMode, "server", StringComparison.OrdinalIgnoreCase))
        {
            SessionCapacitySnapshot? capacity;
            try
            {
                capacity = await capacityClient.GetCapacityAsync(
                    request.Host,
                    request.Port,
                    request.AgentId,
                    request.ApiKey,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                return CommandResult.Fail($"Capacity check failed: {ex.Message}");
            }

            if (capacity is null)
                return CommandResult.Fail("Capacity endpoint returned no data.");

            if (!capacity.CanCreateSession)
                return CommandResult.Fail(capacity.Reason ?? "Session capacity exhausted.");
        }

        var session = sessionFactory.Create(request.Title, request.ConnectionMode, request.AgentId);
        session.Messages.Add($"[{DateTimeOffset.UtcNow:HH:mm:ss}] session initialized ({session.ConnectionMode}).");

        var connectHost = string.Equals(request.ConnectionMode, "direct", StringComparison.OrdinalIgnoreCase)
            ? "127.0.0.1"
            : request.Host;

        session.SessionClient.PerRequestContext = (request.PerRequestContext ?? "").Trim();
        try
        {
            await session.SessionClient.ConnectAsync(
                connectHost,
                request.Port,
                session.SessionId,
                session.AgentId,
                apiKey: request.ApiKey);
            session.IsConnected = true;
            session.Messages.Add($"[{DateTimeOffset.UtcNow:HH:mm:ss}] connected to {connectHost}:{request.Port}.");
        }
        catch (Exception ex)
        {
            session.IsConnected = false;
            session.Messages.Add($"[{DateTimeOffset.UtcNow:HH:mm:ss}] connection failed: {ex.Message}");
            return CommandResult.Fail($"Connection failed: {ex.Message}");
        }

        request.Workspace.Sessions.Add(session);
        request.Workspace.SelectedSession = session;
        request.Workspace.StatusText = $"Created {session.Title}. Connected.";
        return CommandResult.Ok();
    }
}
