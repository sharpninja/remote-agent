using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;
using RemoteAgent.Proto;

namespace RemoteAgent.Desktop.ViewModels;

public sealed class PromptTemplatesViewModel : INotifyPropertyChanged
{
    private readonly IRequestDispatcher _dispatcher;
    private readonly IServerConnectionContext _context;
    private PromptTemplateDefinition? _selectedPromptTemplate;
    private string _promptTemplateId = "";
    private string _promptTemplateName = "";
    private string _promptTemplateDescription = "";
    private string _promptTemplateContent = "";
    private string _promptTemplateStatus = "Prompt templates not loaded.";
    private string _seedSessionId = "";
    private string _seedContextType = "system";
    private string _seedContent = "";
    private string _seedSource = "";
    private string _seedStatus = "";

    public PromptTemplatesViewModel(IRequestDispatcher dispatcher, IServerConnectionContext context)
    {
        _dispatcher = dispatcher;
        _context = context;

        RefreshPromptTemplatesCommand = new RelayCommand(
            () => _ = RunCommandAsync(RefreshPromptTemplatesAsync));
        SavePromptTemplateCommand = new RelayCommand(
            () => _ = RunCommandAsync(SavePromptTemplateAsync));
        DeletePromptTemplateCommand = new RelayCommand(
            () => _ = RunCommandAsync(DeleteSelectedPromptTemplateAsync),
            () => SelectedPromptTemplate != null);
        SeedContextCommand = new RelayCommand(
            () => _ = RunCommandAsync(SeedContextAsync));

        ObserveBackgroundTask(RefreshPromptTemplatesAsync(), "initial prompt templates refresh");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<PromptTemplateDefinition> PromptTemplates { get; } = [];

    public PromptTemplateDefinition? SelectedPromptTemplate
    {
        get => _selectedPromptTemplate;
        set
        {
            if (_selectedPromptTemplate == value) return;
            _selectedPromptTemplate = value;
            OnPropertyChanged();
            ((RelayCommand)DeletePromptTemplateCommand).RaiseCanExecuteChanged();
            if (_selectedPromptTemplate != null)
            {
                PromptTemplateId = _selectedPromptTemplate.TemplateId;
                PromptTemplateName = _selectedPromptTemplate.DisplayName;
                PromptTemplateDescription = _selectedPromptTemplate.Description;
                PromptTemplateContent = _selectedPromptTemplate.TemplateContent;
            }
        }
    }

    public string PromptTemplateId
    {
        get => _promptTemplateId;
        set
        {
            if (_promptTemplateId == value) return;
            _promptTemplateId = value;
            OnPropertyChanged();
        }
    }

    public string PromptTemplateName
    {
        get => _promptTemplateName;
        set
        {
            if (_promptTemplateName == value) return;
            _promptTemplateName = value;
            OnPropertyChanged();
        }
    }

    public string PromptTemplateDescription
    {
        get => _promptTemplateDescription;
        set
        {
            if (_promptTemplateDescription == value) return;
            _promptTemplateDescription = value;
            OnPropertyChanged();
        }
    }

    public string PromptTemplateContent
    {
        get => _promptTemplateContent;
        set
        {
            if (_promptTemplateContent == value) return;
            _promptTemplateContent = value;
            OnPropertyChanged();
        }
    }

    public string PromptTemplateStatus
    {
        get => _promptTemplateStatus;
        set
        {
            if (_promptTemplateStatus == value) return;
            _promptTemplateStatus = value;
            OnPropertyChanged();
        }
    }

    public string SeedSessionId
    {
        get => _seedSessionId;
        set
        {
            if (_seedSessionId == value) return;
            _seedSessionId = value;
            OnPropertyChanged();
        }
    }

    public string SeedContextType
    {
        get => _seedContextType;
        set
        {
            if (_seedContextType == value) return;
            _seedContextType = value;
            OnPropertyChanged();
        }
    }

    public string SeedContent
    {
        get => _seedContent;
        set
        {
            if (_seedContent == value) return;
            _seedContent = value;
            OnPropertyChanged();
        }
    }

    public string SeedSource
    {
        get => _seedSource;
        set
        {
            if (_seedSource == value) return;
            _seedSource = value;
            OnPropertyChanged();
        }
    }

    public string SeedStatus
    {
        get => _seedStatus;
        set
        {
            if (_seedStatus == value) return;
            _seedStatus = value;
            OnPropertyChanged();
        }
    }

    public ICommand RefreshPromptTemplatesCommand { get; }
    public ICommand SavePromptTemplateCommand { get; }
    public ICommand DeletePromptTemplateCommand { get; }
    public ICommand SeedContextCommand { get; }

    private async Task RefreshPromptTemplatesAsync()
    {
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) return;
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) return;
        await _dispatcher.SendAsync(new RefreshPromptTemplatesRequest(Guid.NewGuid(), host, port, _context.ApiKey, Workspace: this));
    }

    private async Task SavePromptTemplateAsync()
    {
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { PromptTemplateStatus = "Host is required."; return; }
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { PromptTemplateStatus = "Port must be 1-65535."; return; }
        var template = new PromptTemplateDefinition
        {
            TemplateId = PromptTemplateId ?? "",
            DisplayName = PromptTemplateName ?? "",
            Description = PromptTemplateDescription ?? "",
            TemplateContent = PromptTemplateContent ?? ""
        };
        await _dispatcher.SendAsync(new SavePromptTemplateRequest(Guid.NewGuid(), host, port, template, _context.ApiKey, Workspace: this));
    }

    private async Task DeleteSelectedPromptTemplateAsync()
    {
        var selected = SelectedPromptTemplate;
        if (selected == null) return;
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { PromptTemplateStatus = "Host is required."; return; }
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { PromptTemplateStatus = "Port must be 1-65535."; return; }
        await _dispatcher.SendAsync(new Requests.DeletePromptTemplateRequest(Guid.NewGuid(), host, port, selected.TemplateId, _context.ApiKey, Workspace: this));
    }

    private async Task SeedContextAsync()
    {
        var host = (_context.Host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(host)) { SeedStatus = "Host is required."; return; }
        if (!int.TryParse((_context.Port ?? "").Trim(), out var port) || port <= 0 || port > 65535) { SeedStatus = "Port must be 1-65535."; return; }
        if (string.IsNullOrWhiteSpace(SeedSessionId)) { SeedStatus = "Seed session id is required."; return; }
        if (string.IsNullOrWhiteSpace(SeedContent)) { SeedStatus = "Seed content is required."; return; }
        await _dispatcher.SendAsync(new Requests.SeedSessionContextRequest(Guid.NewGuid(), host, port, SeedSessionId, SeedContextType, SeedContent, SeedSource, _context.ApiKey, Workspace: this));
    }

    private async Task RunCommandAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            PromptTemplateStatus = $"Command failed: {ex.Message}";
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
