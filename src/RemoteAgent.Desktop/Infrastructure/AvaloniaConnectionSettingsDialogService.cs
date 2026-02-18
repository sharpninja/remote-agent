using Avalonia.Controls;
using RemoteAgent.Desktop.ViewModels;
using RemoteAgent.Desktop.Views;

namespace RemoteAgent.Desktop.Infrastructure;

public sealed class AvaloniaConnectionSettingsDialogService : IConnectionSettingsDialogService
{
    public async Task<ConnectionSettingsDialogResult?> ShowAsync(
        Window ownerWindow,
        ConnectionSettingsDefaults defaults,
        CancellationToken cancellationToken = default)
    {
        var viewModel = new ConnectionSettingsDialogViewModel(defaults);
        var dialog = new ConnectionSettingsDialog(viewModel);

        var accepted = await dialog.ShowDialog<bool>(ownerWindow);
        return accepted && viewModel.IsAccepted ? viewModel.ToResult() : null;
    }
}
