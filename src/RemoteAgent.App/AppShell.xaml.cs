using RemoteAgent.App.Logic.ViewModels;

namespace RemoteAgent.App;

public partial class AppShell : Shell
{
    private readonly AppShellViewModel _vm;

    public AppShell(
        AppShellViewModel viewModel,
        MainPage mainPage,
        McpRegistryPage mcpRegistryPage,
        SettingsPage settingsPage,
        AccountManagementPage accountManagementPage)
    {
        _vm = viewModel;

        InitializeComponent();
        BindingContext = _vm;

        Items.Add(new ShellContent
        {
            Title = "Home",
            Route = "MainPage",
            Content = mainPage
        });
        Items.Add(new ShellContent
        {
            Title = "MCP Registry",
            Route = "McpRegistryPage",
            Content = mcpRegistryPage
        });
        Items.Add(new ShellContent
        {
            Title = "Settings",
            Route = "SettingsPage",
            Content = settingsPage,
            FlyoutItemIsVisible = false
        });
        Items.Add(new ShellContent
        {
            Title = "Account Management",
            Route = "AccountManagementPage",
            Content = accountManagementPage,
            FlyoutItemIsVisible = false
        });

        PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(FlyoutIsPresented) && FlyoutIsPresented)
                _vm.RefreshSessions();
        };
        Navigated += (_, _) => _vm.RefreshSessions();
    }
}
