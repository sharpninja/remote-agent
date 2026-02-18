using RemoteAgent.App.Logic.ViewModels;
using RemoteAgent.Proto;

namespace RemoteAgent.App;

public partial class McpRegistryPage : ContentPage
{
    private readonly McpRegistryPageViewModel _vm;

    public McpRegistryPage(McpRegistryPageViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.RefreshCommand.Execute(null);
    }

    private void OnServerTapped(object? sender, EventArgs e)
    {
        if (sender is not BindableObject bindable || bindable.BindingContext is not McpServerDefinition server)
            return;

        _vm.SelectServer(server);
    }
}
