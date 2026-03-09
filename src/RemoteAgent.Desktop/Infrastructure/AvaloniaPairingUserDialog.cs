using Avalonia.Controls;
using RemoteAgent.Desktop.ViewModels;
using RemoteAgent.Desktop.Views;

namespace RemoteAgent.Desktop.Infrastructure;

/// <summary>Avalonia implementation of <see cref="IPairingUserDialog"/>.</summary>
public sealed class AvaloniaPairingUserDialog : IPairingUserDialog
{
    /// <inheritdoc />
    public async Task<PairingUserDialogResult?> ShowAsync(
        Window ownerWindow,
        CancellationToken cancellationToken = default)
    {
        var viewModel = new PairingUserDialogViewModel();
        var dialog = new PairingUserDialog(viewModel);

        var accepted = await dialog.ShowDialog<bool>(ownerWindow);
        if (!accepted || !viewModel.IsAccepted)
            return null;

        var passwordHash = ComputePasswordHash(viewModel.Password);
        return new PairingUserDialogResult(viewModel.Username.Trim(), passwordHash);
    }

    private static string ComputePasswordHash(string password)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(password);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
