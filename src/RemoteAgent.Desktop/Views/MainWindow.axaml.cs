using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.SetOwnerWindow(() => this);
        Opened += OnOpened;
    }

    // View-adapter: measures rendered NavigationViewItem controls to set OpenPaneLength.
    // Pure layout concern — no business logic, no service calls, no state mutation.
    private void OnOpened(object? sender, EventArgs e)
    {
        if (this.FindControl<NavigationView>("ManagementNavigationView") is not { } navView)
            return;

        void OnNavLayoutUpdated(object? _, EventArgs __)
        {
            if (!TrySetOpenPaneLength(navView))
                return;

            navView.LayoutUpdated -= OnNavLayoutUpdated;
        }

        navView.LayoutUpdated += OnNavLayoutUpdated;

        Dispatcher.UIThread.Post(() =>
        {
            if (TrySetOpenPaneLength(navView))
                navView.LayoutUpdated -= OnNavLayoutUpdated;
        }, DispatcherPriority.Loaded);
    }

    private static bool TrySetOpenPaneLength(NavigationView navView)
    {
        const double contentInset = 24d;
        var widestContentWidth = 0d;
        var foundItem = false;

        foreach (var menuItem in navView.MenuItems)
        {
            if (menuItem is not NavigationViewItem navItem)
                continue;

            foundItem = true;

            var contentWidth = 0d;
            if (navItem.Content is Control contentControl)
            {
                contentControl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                contentWidth = contentControl.DesiredSize.Width;
            }
            else
            {
                navItem.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                contentWidth = navItem.DesiredSize.Width;
            }

            var itemWidth = contentWidth + navItem.Padding.Left + navItem.Padding.Right + contentInset;
            widestContentWidth = Math.Max(widestContentWidth, itemWidth);
        }

        if (!foundItem || widestContentWidth <= 0)
            return false;

        navView.OpenPaneLength = Math.Ceiling(widestContentWidth + 12d);
        return true;
    }
}
