using RemoteAgent.App.Services;

namespace RemoteAgent.App;

public partial class AppShell : Shell
{
    private readonly MainPage _mainPage;
    private readonly SettingsPage _settingsPage;
    private readonly AccountManagementPage _accountManagementPage;
    private readonly ISessionStore _sessionStore;

    public AppShell(
        MainPage mainPage,
        McpRegistryPage mcpRegistryPage,
        SettingsPage settingsPage,
        AccountManagementPage accountManagementPage,
        ISessionStore sessionStore)
    {
        _mainPage = mainPage;
        _settingsPage = settingsPage;
        _accountManagementPage = accountManagementPage;
        _sessionStore = sessionStore;

        InitializeComponent();
        Items.Add(new ShellContent
        {
            Title = "Home",
            Route = "MainPage",
            Content = _mainPage
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
            Content = _settingsPage,
            FlyoutItemIsVisible = false
        });
        Items.Add(new ShellContent
        {
            Title = "Account Management",
            Route = "AccountManagementPage",
            Content = _accountManagementPage,
            FlyoutItemIsVisible = false
        });

        PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(FlyoutIsPresented) && FlyoutIsPresented)
                BuildSessionButtons();
        };
        Navigated += (_, _) => BuildSessionButtons();
        BuildSessionButtons();
    }

    private void BuildSessionButtons()
    {
        if (SessionButtonsHost == null) return;

        SessionButtonsHost.Children.Clear();
        var sessions = _sessionStore.GetAll();
        if (sessions.Count == 0)
        {
            SessionButtonsHost.Children.Add(new Label
            {
                Text = "No sessions yet.",
                FontSize = 12
            });
            return;
        }

        foreach (var session in sessions)
        {
            var sid = session.SessionId;
            var row = new HorizontalStackLayout
            {
                Spacing = 6
            };

            var button = new Button
            {
                Text = string.IsNullOrWhiteSpace(session.Title) ? sid : session.Title,
                FontSize = 13,
                HorizontalOptions = LayoutOptions.Fill
            };

            button.Clicked += async (_, _) =>
            {
                _mainPage.SelectSessionFromShell(sid);
                FlyoutIsPresented = false;
                await GoToAsync("//MainPage");
            };

            var terminateButton = new Button
            {
                Text = "X",
                WidthRequest = 34,
                HeightRequest = 34,
                Padding = 0
            };
            terminateButton.Clicked += async (_, _) =>
            {
                await _mainPage.TerminateSessionFromShellAsync(sid);
                BuildSessionButtons();
            };

            row.Children.Add(button);
            row.Children.Add(terminateButton);
            SessionButtonsHost.Children.Add(row);
        }
    }

    private async void OnOpenSessionsClicked(object? sender, EventArgs e)
    {
        BuildSessionButtons();
        _mainPage.SelectSessionFromShell(_mainPage.GetCurrentSessionId());
        FlyoutIsPresented = false;
        await GoToAsync("//MainPage");
    }

    private async void OnStartSessionClicked(object? sender, EventArgs e)
    {
        _mainPage.StartNewSessionFromShell();
        BuildSessionButtons();
        FlyoutIsPresented = false;
        await GoToAsync("//MainPage");
    }

    private async void OnSettingsClicked(object? sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        await GoToAsync("//SettingsPage");
    }

    private async void OnAccountManagementClicked(object? sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        await GoToAsync("//AccountManagementPage");
    }
}
