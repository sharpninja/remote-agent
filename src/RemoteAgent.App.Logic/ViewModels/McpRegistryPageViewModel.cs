using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Proto;

namespace RemoteAgent.App.Logic.ViewModels;

public sealed class McpRegistryPageViewModel : INotifyPropertyChanged
{
    private readonly IServerApiClient _apiClient;
    private readonly IAppPreferences _preferences;
    private readonly ISessionTerminationConfirmation _deleteConfirmation;

    private McpServerDefinition? _selected;
    private string _host = "";
    private string _port = "5243";
    private string _statusText = "";
    private string _serverId = "";
    private string _displayName = "";
    private string _transport = "";
    private string _endpoint = "";
    private string _command = "";
    private string _arguments = "";
    private string _authType = "";
    private string _authConfigJson = "";
    private string _metadataJson = "";
    private bool _enabled = true;

    public McpRegistryPageViewModel(
        IServerApiClient apiClient,
        IAppPreferences preferences,
        ISessionTerminationConfirmation deleteConfirmation)
    {
        _apiClient = apiClient;
        _preferences = preferences;
        _deleteConfirmation = deleteConfirmation;

        Host = preferences.Get("ServerHost", "");
        Port = preferences.Get("ServerPort", "5243");

        RefreshCommand = new RelayCommand(() => _ = RefreshAsync());
        SaveCommand = new RelayCommand(() => _ = SaveAsync());
        DeleteCommand = new RelayCommand(() => _ = DeleteAsync());
        ClearCommand = new RelayCommand(ClearForm);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<McpServerDefinition> Servers { get; } = [];

    public string Host
    {
        get => _host;
        set { if (_host != value) { _host = value; OnPropertyChanged(); } }
    }

    public string Port
    {
        get => _port;
        set { if (_port != value) { _port = value; OnPropertyChanged(); } }
    }

    public string StatusText
    {
        get => _statusText;
        set { if (_statusText != value) { _statusText = value; OnPropertyChanged(); } }
    }

    public string ServerId
    {
        get => _serverId;
        set { if (_serverId != value) { _serverId = value; OnPropertyChanged(); } }
    }

    public string DisplayName
    {
        get => _displayName;
        set { if (_displayName != value) { _displayName = value; OnPropertyChanged(); } }
    }

    public string Transport
    {
        get => _transport;
        set { if (_transport != value) { _transport = value; OnPropertyChanged(); } }
    }

    public string Endpoint
    {
        get => _endpoint;
        set { if (_endpoint != value) { _endpoint = value; OnPropertyChanged(); } }
    }

    public string Command
    {
        get => _command;
        set { if (_command != value) { _command = value; OnPropertyChanged(); } }
    }

    public string Arguments
    {
        get => _arguments;
        set { if (_arguments != value) { _arguments = value; OnPropertyChanged(); } }
    }

    public string AuthType
    {
        get => _authType;
        set { if (_authType != value) { _authType = value; OnPropertyChanged(); } }
    }

    public string AuthConfigJson
    {
        get => _authConfigJson;
        set { if (_authConfigJson != value) { _authConfigJson = value; OnPropertyChanged(); } }
    }

    public string MetadataJson
    {
        get => _metadataJson;
        set { if (_metadataJson != value) { _metadataJson = value; OnPropertyChanged(); } }
    }

    public bool Enabled
    {
        get => _enabled;
        set { if (_enabled != value) { _enabled = value; OnPropertyChanged(); } }
    }

    public ICommand RefreshCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ClearCommand { get; }

    public void SelectServer(McpServerDefinition server)
    {
        _selected = server;
        ServerId = server.ServerId;
        DisplayName = server.DisplayName;
        Transport = server.Transport;
        Endpoint = server.Endpoint;
        Command = server.Command;
        Arguments = string.Join(' ', server.Arguments);
        AuthType = server.AuthType;
        AuthConfigJson = server.AuthConfigJson;
        MetadataJson = server.MetadataJson;
        Enabled = server.Enabled;
        StatusText = $"Editing '{server.ServerId}'.";
    }

    public async Task RefreshAsync()
    {
        if (!TryGetEndpoint(out var host, out var port))
            return;

        StatusText = "Loading MCP servers...";
        var response = await _apiClient.ListMcpServersAsync(host, port);
        if (response == null)
        {
            StatusText = "Failed to load MCP servers.";
            return;
        }

        Servers.Clear();
        foreach (var server in response.Servers.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
            Servers.Add(server);

        StatusText = $"Loaded {Servers.Count} MCP server(s).";
    }

    internal async Task SaveAsync()
    {
        if (!TryGetEndpoint(out var host, out var port))
            return;

        var server = new McpServerDefinition
        {
            ServerId = (ServerId ?? "").Trim(),
            DisplayName = (DisplayName ?? "").Trim(),
            Transport = (Transport ?? "").Trim(),
            Endpoint = (Endpoint ?? "").Trim(),
            Command = (Command ?? "").Trim(),
            AuthType = (AuthType ?? "").Trim(),
            AuthConfigJson = (AuthConfigJson ?? "").Trim(),
            MetadataJson = (MetadataJson ?? "").Trim(),
            Enabled = Enabled,
        };

        foreach (var arg in ParseArguments(Arguments))
            server.Arguments.Add(arg);

        StatusText = "Saving MCP server...";
        var response = await _apiClient.UpsertMcpServerAsync(host, port, server);
        if (response == null)
        {
            StatusText = "Failed to save MCP server.";
            return;
        }

        if (!response.Success)
        {
            StatusText = response.Message;
            return;
        }

        _selected = response.Server;
        await RefreshAsync();
        if (_selected != null)
            PopulateFromServer(_selected);
        StatusText = $"Saved '{_selected?.ServerId}'.";
    }

    internal async Task DeleteAsync()
    {
        if (!TryGetEndpoint(out var host, out var port))
            return;

        var serverId = (ServerId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(serverId))
        {
            StatusText = "Select a server or enter a server id to delete.";
            return;
        }

        var confirmed = await _deleteConfirmation.ConfirmAsync(serverId);
        if (!confirmed)
            return;

        StatusText = "Deleting MCP server...";
        var response = await _apiClient.DeleteMcpServerAsync(host, port, serverId);
        if (response == null)
        {
            StatusText = "Failed to delete MCP server.";
            return;
        }

        StatusText = response.Message;
        if (response.Success)
        {
            _selected = null;
            ClearForm();
            await RefreshAsync();
        }
    }

    private void ClearForm()
    {
        _selected = null;
        ServerId = "";
        DisplayName = "";
        Transport = "";
        Endpoint = "";
        Command = "";
        Arguments = "";
        AuthType = "";
        AuthConfigJson = "";
        MetadataJson = "";
        Enabled = true;
        StatusText = "Editor cleared.";
    }

    private void PopulateFromServer(McpServerDefinition server)
    {
        ServerId = server.ServerId;
        DisplayName = server.DisplayName;
        Transport = server.Transport;
        Endpoint = server.Endpoint;
        Command = server.Command;
        Arguments = string.Join(' ', server.Arguments);
        AuthType = server.AuthType;
        AuthConfigJson = server.AuthConfigJson;
        MetadataJson = server.MetadataJson;
        Enabled = server.Enabled;
    }

    private bool TryGetEndpoint(out string host, out int port)
    {
        host = (Host ?? "").Trim();
        var portText = (Port ?? "5243").Trim();

        if (string.IsNullOrWhiteSpace(host))
        {
            StatusText = "Host is required.";
            port = 0;
            return false;
        }

        if (!int.TryParse(portText, out port) || port <= 0 || port > 65535)
        {
            StatusText = "Valid port required (1-65535).";
            return false;
        }

        return true;
    }

    internal static IReadOnlyList<string> ParseArguments(string? raw)
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
