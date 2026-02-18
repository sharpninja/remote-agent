using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.ViewModels;

public sealed class PluginsViewModel : INotifyPropertyChanged
{
    private readonly IRequestDispatcher _dispatcher;
    private readonly IServerConnectionContext _context;
    private string _pluginAssembliesText = "";
    private string _pluginStatus = "Plugin configuration not loaded.";

    public PluginsViewModel(IRequestDispatcher dispatcher, IServerConnectionContext context)
    {
        _dispatcher = dispatcher;
        _context = context;

        RefreshPluginsCommand = new RelayCommand(
            () => _ = RunCommandAsync(RefreshPluginsAsync));
        SavePluginsCommand = new RelayCommand(
            () => _ = RunCommandAsync(SavePluginsAsync));

        ObserveBackgroundTask(RefreshPluginsAsync(), "initial plugins refresh");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> ConfiguredPluginAssemblies { get; } = [];
    public ObservableCollection<string> LoadedPluginRunnerIds { get; } = [];

    public string PluginAssembliesText
    {
        get => _pluginAssembliesText;
        set
        {
            if (_pluginAssembliesText == value) return;
            _pluginAssembliesText = value;
            OnPropertyChanged();
        }
    }

    public string PluginStatus
    {
        get => _pluginStatus;
        set
        {
            if (_pluginStatus == value) return;
            _pluginStatus = value;
            OnPropertyChanged();
        }
    }

    public ICommand RefreshPluginsCommand { get; }
    public ICommand SavePluginsCommand { get; }

    private async Task RefreshPluginsAsync()
    {
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) return;
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) return;
        await _dispatcher.SendAsync(new RefreshPluginsRequest(Guid.NewGuid(), host, port, _context.ApiKey, Workspace: this));
    }

    private async Task SavePluginsAsync()
    {
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { PluginStatus = "Host is required."; return; }
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { PluginStatus = "Port must be 1-65535."; return; }
        var assemblies = (PluginAssembliesText ?? "")
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        await _dispatcher.SendAsync(new SavePluginsRequest(Guid.NewGuid(), host, port, assemblies, _context.ApiKey, Workspace: this));
    }

    private async Task RunCommandAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            PluginStatus = $"Command failed: {ex.Message}";
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
