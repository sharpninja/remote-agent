using System.Collections.ObjectModel;
using RemoteAgent.App.Services;
using RemoteAgent.Proto;
using Microsoft.Maui.Storage;

namespace RemoteAgent.App;

public partial class McpRegistryPage : ContentPage
{
    private const string PrefServerHost = "ServerHost";
    private const string PrefServerPort = "ServerPort";
    private const string DefaultPort = "5243";

    private readonly ObservableCollection<McpServerDefinition> _servers = new();
    private McpServerDefinition? _selected;

    public McpRegistryPage()
    {
        InitializeComponent();
        ServersList.ItemsSource = _servers;
        LoadSavedServerDetails();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
    }

    private void LoadSavedServerDetails()
    {
        HostEntry.Text = Preferences.Default.Get(PrefServerHost, "");
        PortEntry.Text = Preferences.Default.Get(PrefServerPort, DefaultPort);
    }

    private bool TryGetEndpoint(out string host, out int port)
    {
        host = (HostEntry.Text ?? "").Trim();
        var portText = (PortEntry.Text ?? DefaultPort).Trim();

        if (string.IsNullOrWhiteSpace(host))
        {
            StatusLabel.Text = "Host is required.";
            port = 0;
            return false;
        }

        if (!int.TryParse(portText, out port) || port <= 0 || port > 65535)
        {
            StatusLabel.Text = "Valid port required (1-65535).";
            return false;
        }

        return true;
    }

    private async Task RefreshAsync()
    {
        if (!TryGetEndpoint(out var host, out var port))
            return;

        StatusLabel.Text = "Loading MCP servers...";
        var response = await AgentGatewayClientService.ListMcpServersAsync(host, port);
        if (response == null)
        {
            StatusLabel.Text = "Failed to load MCP servers.";
            return;
        }

        _servers.Clear();
        foreach (var server in response.Servers.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
            _servers.Add(server);

        StatusLabel.Text = $"Loaded {_servers.Count} MCP server(s).";
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await RefreshAsync();
    }

    private void OnServerTapped(object? sender, EventArgs e)
    {
        if (sender is not BindableObject bindable || bindable.BindingContext is not McpServerDefinition server)
            return;

        _selected = server;
        ServerIdEntry.Text = server.ServerId;
        DisplayNameEntry.Text = server.DisplayName;
        TransportEntry.Text = server.Transport;
        EndpointEntry.Text = server.Endpoint;
        CommandEntry.Text = server.Command;
        ArgumentsEntry.Text = string.Join(' ', server.Arguments);
        AuthTypeEntry.Text = server.AuthType;
        AuthConfigEditor.Text = server.AuthConfigJson;
        MetadataEditor.Text = server.MetadataJson;
        EnabledSwitch.IsToggled = server.Enabled;
        StatusLabel.Text = $"Editing '{server.ServerId}'.";
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (!TryGetEndpoint(out var host, out var port))
            return;

        var server = new McpServerDefinition
        {
            ServerId = (ServerIdEntry.Text ?? "").Trim(),
            DisplayName = (DisplayNameEntry.Text ?? "").Trim(),
            Transport = (TransportEntry.Text ?? "").Trim(),
            Endpoint = (EndpointEntry.Text ?? "").Trim(),
            Command = (CommandEntry.Text ?? "").Trim(),
            AuthType = (AuthTypeEntry.Text ?? "").Trim(),
            AuthConfigJson = (AuthConfigEditor.Text ?? "").Trim(),
            MetadataJson = (MetadataEditor.Text ?? "").Trim(),
            Enabled = EnabledSwitch.IsToggled,
        };

        foreach (var arg in ParseArguments(ArgumentsEntry.Text))
            server.Arguments.Add(arg);

        StatusLabel.Text = "Saving MCP server...";
        var response = await AgentGatewayClientService.UpsertMcpServerAsync(host, port, server);
        if (response == null)
        {
            StatusLabel.Text = "Failed to save MCP server.";
            return;
        }

        if (!response.Success)
        {
            StatusLabel.Text = response.Message;
            return;
        }

        _selected = response.Server;
        await RefreshAsync();
        PopulateFromServer(_selected);
        StatusLabel.Text = $"Saved '{_selected.ServerId}'.";
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (!TryGetEndpoint(out var host, out var port))
            return;

        var serverId = (ServerIdEntry.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(serverId))
        {
            StatusLabel.Text = "Select a server or enter a server id to delete.";
            return;
        }

        var confirmed = await DisplayAlertAsync("Delete MCP Server", $"Delete '{serverId}'?", "Delete", "Cancel");
        if (!confirmed)
            return;

        StatusLabel.Text = "Deleting MCP server...";
        var response = await AgentGatewayClientService.DeleteMcpServerAsync(host, port, serverId);
        if (response == null)
        {
            StatusLabel.Text = "Failed to delete MCP server.";
            return;
        }

        StatusLabel.Text = response.Message;
        if (response.Success)
        {
            _selected = null;
            ClearForm();
            await RefreshAsync();
        }
    }

    private void OnClearClicked(object? sender, EventArgs e)
    {
        _selected = null;
        ClearForm();
        StatusLabel.Text = "Editor cleared.";
    }

    private void ClearForm()
    {
        ServerIdEntry.Text = "";
        DisplayNameEntry.Text = "";
        TransportEntry.Text = "";
        EndpointEntry.Text = "";
        CommandEntry.Text = "";
        ArgumentsEntry.Text = "";
        AuthTypeEntry.Text = "";
        AuthConfigEditor.Text = "";
        MetadataEditor.Text = "";
        EnabledSwitch.IsToggled = true;
    }

    private void PopulateFromServer(McpServerDefinition server)
    {
        ServerIdEntry.Text = server.ServerId;
        DisplayNameEntry.Text = server.DisplayName;
        TransportEntry.Text = server.Transport;
        EndpointEntry.Text = server.Endpoint;
        CommandEntry.Text = server.Command;
        ArgumentsEntry.Text = string.Join(' ', server.Arguments);
        AuthTypeEntry.Text = server.AuthType;
        AuthConfigEditor.Text = server.AuthConfigJson;
        MetadataEditor.Text = server.MetadataJson;
        EnabledSwitch.IsToggled = server.Enabled;
    }

    private static IReadOnlyList<string> ParseArguments(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        var args = new List<string>();
        var buffer = new List<char>();
        var inQuotes = false;

        foreach (var c in raw)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (buffer.Count > 0)
                {
                    args.Add(new string(buffer.ToArray()));
                    buffer.Clear();
                }

                continue;
            }

            buffer.Add(c);
        }

        if (buffer.Count > 0)
            args.Add(new string(buffer.ToArray()));

        return args;
    }
}
