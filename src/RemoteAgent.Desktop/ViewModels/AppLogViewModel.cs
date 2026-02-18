using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using RemoteAgent.App.Logic.Cqrs;
using RemoteAgent.Desktop.Infrastructure;
using RemoteAgent.Desktop.Requests;

namespace RemoteAgent.Desktop.ViewModels;

/// <summary>ViewModel for the Management App Log view (FR-12.12).</summary>
public sealed class AppLogViewModel : INotifyPropertyChanged
{
    private readonly IRequestDispatcher _dispatcher;
    private readonly IFileSaveDialogService _saveDialog;
    private string _statusText = "";
    private string _filterText = "";

    public AppLogViewModel(IRequestDispatcher dispatcher, IFileSaveDialogService saveDialog)
    {
        _dispatcher = dispatcher;
        _saveDialog = saveDialog;

        ClearCommand = new RelayCommand(() => _ = RunAsync(ClearAsync));
        SaveAsTxtCommand  = new RelayCommand(() => _ = RunAsync(() => SaveAsync("txt",  "app-log.txt",  "txt",  "Text files")));
        SaveAsJsonCommand = new RelayCommand(() => _ = RunAsync(() => SaveAsync("json", "app-log.json", "json", "JSON files")));
        SaveAsCsvCommand  = new RelayCommand(() => _ = RunAsync(() => SaveAsync("csv",  "app-log.csv",  "csv",  "CSV files")));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AppLogEntry> Entries { get; } = [];

    public string FilterText
    {
        get => _filterText;
        set { _filterText = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public RelayCommand ClearCommand { get; }
    public RelayCommand SaveAsTxtCommand { get; }
    public RelayCommand SaveAsJsonCommand { get; }
    public RelayCommand SaveAsCsvCommand { get; }

    /// <summary>Appends a new entry from the ILogger pipeline and respects the active filter.</summary>
    public void Append(AppLogEntry entry)
    {
        Entries.Add(entry);
    }

    private async Task ClearAsync()
    {
        await _dispatcher.SendAsync(new ClearAppLogRequest(Guid.NewGuid(), Workspace: this));
    }

    private async Task SaveAsync(string format, string suggestedName, string extension, string filterDescription)
    {
        var path = await _saveDialog.GetSaveFilePathAsync(suggestedName, extension, filterDescription);
        if (path == null)
            return;

        var entries = Entries.ToList();
        var result = await _dispatcher.SendAsync(
            new SaveAppLogRequest(Guid.NewGuid(), entries, format, path, Workspace: this));

        if (!result.Success)
            StatusText = result.ErrorMessage ?? "Save failed.";
    }

    private async Task RunAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            StatusText = $"Command failed: {ex.Message}";
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
