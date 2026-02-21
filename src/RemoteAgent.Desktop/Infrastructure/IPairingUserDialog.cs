using Avalonia.Controls;

namespace RemoteAgent.Desktop.Infrastructure;

/// <summary>Result returned by the pairing-user dialog.</summary>
public sealed record PairingUserDialogResult(string Username, string PasswordHash);

/// <summary>Dialog service for collecting a pairing-user username and password from the operator.</summary>
public interface IPairingUserDialog
{
    /// <summary>Shows the dialog and returns the result, or <c>null</c> if the operator cancelled.</summary>
    Task<PairingUserDialogResult?> ShowAsync(Window ownerWindow, CancellationToken cancellationToken = default);
}
