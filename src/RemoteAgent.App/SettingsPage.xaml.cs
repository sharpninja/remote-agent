using RemoteAgent.App.ViewModels;

namespace RemoteAgent.App;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsPageViewModel _vm;

    public SettingsPage(SettingsPageViewModel vm)
    {
        _vm = vm;
        BindingContext = _vm;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.RefreshProfiles();
    }
}
