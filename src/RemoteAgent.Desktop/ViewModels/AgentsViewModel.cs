using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RemoteAgent.App.Logic;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.ViewModels;

public sealed class AgentsViewModel : INotifyPropertyChanged
{
    private readonly IRequestDispatcher _dispatcher;
    private readonly IServerConnectionContext _context;
    private readonly Action<string> _setDefaultAgentId;
    private AgentSnapshot? _selectedAgent;
    private string _agentsStatus = "Agents not loaded.";
    private string _serverVersion = "";
    private string _editCommand = "";
    private string _editArguments = "";
    private string _editMaxSessions = "";

    public AgentsViewModel(
        IRequestDispatcher dispatcher,
        IServerConnectionContext context,
        Action<string> setDefaultAgentId)
    {
        _dispatcher = dispatcher;
        _context = context;
        _setDefaultAgentId = setDefaultAgentId;

        RefreshAgentsCommand = new RelayCommand(
            () => _ = RunCommandAsync(RefreshAgentsAsync));
        SetDefaultAgentCommand = new RelayCommand(
            () => SetDefaultAgent(),
            () => SelectedAgent != null);
        SaveAgentCommand = new RelayCommand(
            () => _ = RunCommandAsync(SaveAgentAsync),
            () => SelectedAgent != null);

        ObserveBackgroundTask(RefreshAgentsAsync(), "initial agents refresh");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AgentSnapshot> Agents { get; } = [];

    public AgentSnapshot? SelectedAgent
    {
        get => _selectedAgent;
        set
        {
            if (_selectedAgent == value) return;
            _selectedAgent = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedAgentCapacityText));
            OnPropertyChanged(nameof(HasSelectedAgent));
            ((RelayCommand)SetDefaultAgentCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SaveAgentCommand).RaiseCanExecuteChanged();
            // Populate editable fields from selected agent
            if (value != null)
            {
                EditCommand = value.Command;
                EditArguments = value.Arguments;
                EditMaxSessions = value.MaxConcurrentSessions?.ToString() ?? "";
            }
            else
            {
                EditCommand = "";
                EditArguments = "";
                EditMaxSessions = "";
            }
        }
    }

    public bool HasSelectedAgent => SelectedAgent != null;

    public string EditCommand
    {
        get => _editCommand;
        set { if (_editCommand != value) { _editCommand = value; OnPropertyChanged(); } }
    }

    public string EditArguments
    {
        get => _editArguments;
        set { if (_editArguments != value) { _editArguments = value; OnPropertyChanged(); } }
    }

    public string EditMaxSessions
    {
        get => _editMaxSessions;
        set { if (_editMaxSessions != value) { _editMaxSessions = value; OnPropertyChanged(); } }
    }

    public string SelectedAgentCapacityText
    {
        get
        {
            var agent = SelectedAgent;
            if (agent == null) return "";
            var parts = new List<string> { $"Agent: {agent.AgentId}" };
            if (!string.IsNullOrEmpty(agent.RunnerType))
                parts.Add($"Runner Type: {agent.RunnerType}");
            parts.Add($"Active Sessions: {agent.ActiveSessionCount}");
            if (agent.MaxConcurrentSessions.HasValue)
                parts.Add($"Max Concurrent: {agent.MaxConcurrentSessions.Value}");
            else
                parts.Add("Max Concurrent: (no limit)");
            if (agent.RemainingCapacity.HasValue)
                parts.Add($"Remaining Capacity: {agent.RemainingCapacity.Value}");
            else
                parts.Add("Remaining Capacity: (unlimited)");
            if (agent.IsDefault)
                parts.Add("★ Default Agent");
            return string.Join(Environment.NewLine, parts);
        }
    }

    public string AgentsStatus
    {
        get => _agentsStatus;
        set
        {
            if (_agentsStatus == value) return;
            _agentsStatus = value;
            OnPropertyChanged();
        }
    }

    public string ServerVersion
    {
        get => _serverVersion;
        set
        {
            if (_serverVersion == value) return;
            _serverVersion = value;
            OnPropertyChanged();
        }
    }

    public string DefaultAgentId => _context.SelectedAgentId;

    public ICommand RefreshAgentsCommand { get; }
    public ICommand SetDefaultAgentCommand { get; }
    public ICommand SaveAgentCommand { get; }

    private void SetDefaultAgent()
    {
        var agent = SelectedAgent;
        if (agent == null) return;

        _setDefaultAgentId(agent.AgentId);

        // Rebuild the list to update IsDefault flags
        var updatedAgents = Agents.Select(a =>
            new AgentSnapshot(
                a.AgentId,
                a.ActiveSessionCount,
                a.MaxConcurrentSessions,
                a.RemainingCapacity,
                string.Equals(a.AgentId, agent.AgentId, StringComparison.OrdinalIgnoreCase),
                a.RunnerType,
                a.Command,
                a.Arguments,
                a.Description))
            .ToList();

        Agents.Clear();
        foreach (var a in updatedAgents)
            Agents.Add(a);

        SelectedAgent = Agents.FirstOrDefault(a => a.IsDefault);
        AgentsStatus = $"Default agent set to '{agent.AgentId}'.";
    }

    private async Task SaveAgentAsync()
    {
        var agent = SelectedAgent;
        if (agent == null) { AgentsStatus = "No agent selected."; return; }

        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { AgentsStatus = "Host is required."; return; }
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { AgentsStatus = "Port must be 1-65535."; return; }

        int maxSessions = 0;
        if (!string.IsNullOrWhiteSpace(EditMaxSessions) && !int.TryParse(EditMaxSessions.Trim(), out maxSessions))
        {
            AgentsStatus = "Max sessions must be a number.";
            return;
        }

        AgentsStatus = $"Saving agent '{agent.AgentId}' configuration...";
        try
        {
            var response = await ServerApiClient.UpdateAgentRunnerAsync(
                host, port, agent.AgentId,
                command: EditCommand?.Trim(),
                arguments: EditArguments?.Trim(),
                maxConcurrentSessions: maxSessions,
                setAsDefault: agent.IsDefault,
                apiKey: _context.ApiKey,
                throwOnError: true);

            if (response != null && response.Success)
            {
                AgentsStatus = response.Message;
                // Refresh to show latest
                await RefreshAgentsAsync();
            }
            else
            {
                AgentsStatus = response?.Message ?? "Save failed (no response from server).";
            }
        }
        catch (Exception ex)
        {
            AgentsStatus = $"Save failed: {ex.Message}";
        }
    }

    private async Task RefreshAgentsAsync()
    {
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { AgentsStatus = "Host is required."; return; }
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { AgentsStatus = "Port must be 1-65535."; return; }

        AgentsStatus = "Loading agents from server...";
        await _dispatcher.SendAsync(new RefreshAgentsRequest(
            Guid.NewGuid(), host, port, _context.ApiKey, _context.SelectedAgentId, Workspace: this));
    }

    private async Task RunCommandAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            AgentsStatus = $"Command failed: {ex.Message}";
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
