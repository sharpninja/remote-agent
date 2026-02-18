using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace RemoteAgent.Desktop.Infrastructure;

/// <summary>Avalonia implementation of IFileSaveDialogService using the main window's StorageProvider.</summary>
public sealed class AvaloniaFileSaveDialogService : IFileSaveDialogService
{
    public async Task<string?> GetSaveFilePathAsync(
        string suggestedName,
        string extension,
        string filterDescription)
    {
        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var mainWindow = lifetime?.MainWindow;
        if (mainWindow == null)
            return null;

        var topLevel = TopLevel.GetTopLevel(mainWindow);
        if (topLevel == null)
            return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = suggestedName,
            DefaultExtension = extension,
            FileTypeChoices =
            [
                new FilePickerFileType(filterDescription) { Patterns = [$"*.{extension}"] }
            ]
        });

        return file?.Path.LocalPath;
    }
}
