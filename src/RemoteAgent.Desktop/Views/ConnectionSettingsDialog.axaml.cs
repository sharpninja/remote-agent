using Avalonia.Controls;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Views;

public sealed record ConnectionSettingsDialogResult(
    string Host,
    string Port,
    string SelectedConnectionMode,
    string SelectedAgentId,
    string ApiKey,
    string PerRequestContext);

public partial class ConnectionSettingsDialog : Window
{
    public ConnectionSettingsDialog(ConnectionSettingsDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += accepted => Close(accepted);
    }
}
