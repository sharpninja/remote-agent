using Avalonia.Controls;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Views;

/// <summary>Modal dialog for setting a pairing user username and password.</summary>
public partial class PairingUserDialog : Window
{
    public PairingUserDialog(PairingUserDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += accepted => Close(accepted);
    }
}
