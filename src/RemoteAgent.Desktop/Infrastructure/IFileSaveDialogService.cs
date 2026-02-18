namespace RemoteAgent.Desktop.Infrastructure;

/// <summary>Abstracts the platform save-file dialog so ViewModels remain testable.</summary>
public interface IFileSaveDialogService
{
    /// <summary>
    /// Prompts the user to choose a save location.
    /// Returns the selected local file path, or <c>null</c> if the user cancelled.
    /// </summary>
    Task<string?> GetSaveFilePathAsync(string suggestedName, string extension, string filterDescription);
}
