using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.ViewModels;

public sealed class McpRegistryDesktopViewModel : INotifyPropertyChanged
{
    private readonly IRequestDispatcher _dispatcher;
    private readonly IServerConnectionContext _context;
    private McpServerDefinition? _selectedMcpServer;
    private string _mcpServerId = "";
    private string _mcpDisplayName = "";
    private string _mcpTransport = "stdio";
    private string _mcpEndpoint = "";
    private string _mcpCommand = "";
    private string _mcpArguments = "";
    private bool _mcpEnabled = true;
    private string _agentMcpServerIdsText = "";
    private string _mcpStatus = "MCP registry not loaded.";

    public McpRegistryDesktopViewModel(IRequestDispatcher dispatcher, IServerConnectionContext context)
    {
        _dispatcher = dispatcher;
        _context = context;

        RefreshMcpCommand = new RelayCommand(
            () => _ = RunCommandAsync(RefreshMcpAsync));
        SaveMcpServerCommand = new RelayCommand(
            () => _ = RunCommandAsync(SaveMcpServerAsync));
        DeleteMcpServerCommand = new RelayCommand(
            () => _ = RunCommandAsync(DeleteSelectedMcpServerAsync),
            () => SelectedMcpServer != null);
        SaveAgentMcpMappingCommand = new RelayCommand(
            () => _ = RunCommandAsync(SaveAgentMcpMappingAsync));

        ObserveBackgroundTask(RefreshMcpAsync(), "initial MCP refresh");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<McpServerDefinition> McpServers { get; } = [];

    public string SelectedAgentId => _context.SelectedAgentId;

    public McpServerDefinition? SelectedMcpServer
    {
        get => _selectedMcpServer;
        set
        {
            if (_selectedMcpServer == value) return;
            _selectedMcpServer = value;
            OnPropertyChanged();
            ((RelayCommand)DeleteMcpServerCommand).RaiseCanExecuteChanged();
            if (_selectedMcpServer != null)
            {
                McpServerId = _selectedMcpServer.ServerId;
                McpDisplayName = _selectedMcpServer.DisplayName;
                McpTransport = string.IsNullOrWhiteSpace(_selectedMcpServer.Transport) ? "stdio" : _selectedMcpServer.Transport;
                McpEndpoint = _selectedMcpServer.Endpoint;
                McpCommand = _selectedMcpServer.Command;
                McpArguments = string.Join(' ', _selectedMcpServer.Arguments);
                McpEnabled = _selectedMcpServer.Enabled;
            }
        }
    }

    public string McpServerId
    {
        get => _mcpServerId;
        set
        {
            if (_mcpServerId == value) return;
            _mcpServerId = value;
            OnPropertyChanged();
        }
    }

    public string McpDisplayName
    {
        get => _mcpDisplayName;
        set
        {
            if (_mcpDisplayName == value) return;
            _mcpDisplayName = value;
            OnPropertyChanged();
        }
    }

    public string McpTransport
    {
        get => _mcpTransport;
        set
        {
            if (_mcpTransport == value) return;
            _mcpTransport = value;
            OnPropertyChanged();
        }
    }

    public string McpEndpoint
    {
        get => _mcpEndpoint;
        set
        {
            if (_mcpEndpoint == value) return;
            _mcpEndpoint = value;
            OnPropertyChanged();
        }
    }

    public string McpCommand
    {
        get => _mcpCommand;
        set
        {
            if (_mcpCommand == value) return;
            _mcpCommand = value;
            OnPropertyChanged();
        }
    }

    public string McpArguments
    {
        get => _mcpArguments;
        set
        {
            if (_mcpArguments == value) return;
            _mcpArguments = value;
            OnPropertyChanged();
        }
    }

    public bool McpEnabled
    {
        get => _mcpEnabled;
        set
        {
            if (_mcpEnabled == value) return;
            _mcpEnabled = value;
            OnPropertyChanged();
        }
    }

    public string AgentMcpServerIdsText
    {
        get => _agentMcpServerIdsText;
        set
        {
            if (_agentMcpServerIdsText == value) return;
            _agentMcpServerIdsText = value;
            OnPropertyChanged();
        }
    }

    public string McpStatus
    {
        get => _mcpStatus;
        set
        {
            if (_mcpStatus == value) return;
            _mcpStatus = value;
            OnPropertyChanged();
        }
    }

    public ICommand RefreshMcpCommand { get; }
    public ICommand SaveMcpServerCommand { get; }
    public ICommand DeleteMcpServerCommand { get; }
    public ICommand SaveAgentMcpMappingCommand { get; }

    private async Task RefreshMcpAsync()
    {
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) return;
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) return;
        await _dispatcher.SendAsync(new RefreshMcpRegistryRequest(Guid.NewGuid(), host, port, _context.ApiKey, Workspace: this));
    }

    private async Task SaveMcpServerAsync()
    {
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { McpStatus = "Host is required."; return; }
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { McpStatus = "Port must be 1-65535."; return; }
        var definition = new McpServerDefinition
        {
            ServerId = McpServerId?.Trim() ?? "",
            DisplayName = McpDisplayName ?? "",
            Transport = string.IsNullOrWhiteSpace(McpTransport) ? "stdio" : McpTransport.Trim(),
            Endpoint = McpEndpoint ?? "",
            Command = McpCommand ?? "",
            AuthType = "none",
            Enabled = McpEnabled
        };
        definition.Arguments.AddRange((McpArguments ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        await _dispatcher.SendAsync(new SaveMcpServerRequest(Guid.NewGuid(), host, port, definition, _context.ApiKey, Workspace: this));
    }

    private async Task DeleteSelectedMcpServerAsync()
    {
        var selected = SelectedMcpServer;
        if (selected == null) return;
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { McpStatus = "Host is required."; return; }
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { McpStatus = "Port must be 1-65535."; return; }
        await _dispatcher.SendAsync(new Requests.DeleteMcpServerRequest(Guid.NewGuid(), host, port, selected.ServerId, _context.ApiKey, Workspace: this));
    }

    private async Task SaveAgentMcpMappingAsync()
    {
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { McpStatus = "Host is required."; return; }
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { McpStatus = "Port must be 1-65535."; return; }
        var ids = (AgentMcpServerIdsText ?? "")
            .Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        await _dispatcher.SendAsync(new SaveAgentMcpMappingRequest(Guid.NewGuid(), host, port, _context.SelectedAgentId, ids, _context.ApiKey, Workspace: this));
    }

    private async Task RunCommandAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            McpStatus = $"Command failed: {ex.Message}";
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static void ObserveBackgroundTask(Task task, string operation)
    {
        _ = task.ContinueWith(
            completed =>
            {
                if (completed.IsCanceled || completed.Exception == null)
                    return;
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }
}
