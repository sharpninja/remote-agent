using Avalonia.Controls;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
