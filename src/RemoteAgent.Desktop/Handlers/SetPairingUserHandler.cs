using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.Handlers;

/// <summary>Handles <see cref="SetPairingUserRequest"/>: shows the dialog, then calls the gRPC SetPairingUsers RPC.</summary>
public sealed class SetPairingUserHandler(IServerCapacityClient client, IPairingUserDialog dialog)
    : IRequestHandler<SetPairingUserRequest, CommandResult>
{
    /// <inheritdoc />
    public async Task<CommandResult> HandleAsync(
        SetPairingUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerWindow = request.OwnerWindowFactory();
        if (ownerWindow is null)
            return CommandResult.Fail("No owner window available.");

        var result = await dialog.ShowAsync(ownerWindow, cancellationToken);
        if (result is null)
            return CommandResult.Ok(); // user cancelled

        bool success;
        try
        {
            success = await client.SetPairingUsersAsync(
                request.Host,
                request.Port,
                [(result.Username, result.PasswordHash)],
                replace: false,
                request.ApiKey,
                cancellationToken);
        }
        catch (Exception ex)
        {
            request.Workspace.StatusText = $"Failed to set pairing user: {ex.Message}";
            return CommandResult.Fail(ex.Message);
        }

        if (!success)
        {
            request.Workspace.StatusText = "Failed to set pairing user.";
            return CommandResult.Fail("Failed to set pairing user.");
        }

        request.Workspace.StatusText = $"Pairing user '{result.Username}' set successfully.";
        return CommandResult.Ok();
    }
}
