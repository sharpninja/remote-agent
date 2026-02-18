using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

public sealed class SendDesktopMessageHandler
    : IRequestHandler<SendDesktopMessageRequest, CommandResult>
{
    public async Task<CommandResult> HandleAsync(
        SendDesktopMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = request.Session;
        if (session is null)
            return CommandResult.Fail("No session specified.");

        var text = session.PendingMessage?.TrimEnd() ?? "";
        if (string.IsNullOrWhiteSpace(text))
            return CommandResult.Fail("Message is empty.");

        if (!session.SessionClient.IsConnected)
        {
            if (string.IsNullOrWhiteSpace(request.Host))
                return CommandResult.Fail("Host is required to reconnect.");

            if (request.Port <= 0 || request.Port > 65535)
                return CommandResult.Fail("Port must be 1-65535.");

            session.SessionClient.PerRequestContext = (request.PerRequestContext ?? "").Trim();
            try
            {
                await session.SessionClient.ConnectAsync(
                    request.Host,
                    request.Port,
                    session.SessionId,
                    session.AgentId,
                    apiKey: request.ApiKey);
                session.IsConnected = true;
            }
            catch (Exception ex)
            {
                session.IsConnected = false;
                return CommandResult.Fail($"Reconnect failed: {ex.Message}");
            }
        }

        session.SessionClient.PerRequestContext = (request.PerRequestContext ?? "").Trim();
        try
        {
            await session.SessionClient.SendTextAsync(text);
        }
        catch (Exception ex)
        {
            return CommandResult.Fail($"Send failed: {ex.Message}");
        }

        session.Messages.Add($"[{DateTimeOffset.UtcNow:HH:mm:ss}] user: {text}");
        session.PendingMessage = "";
        return CommandResult.Ok();
    }
}
