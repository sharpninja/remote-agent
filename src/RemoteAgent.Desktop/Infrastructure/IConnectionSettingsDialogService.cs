using Avalonia.Controls;
using RemoteAgent.Desktop.Views;

namespace RemoteAgent.Desktop.Infrastructure;

public sealed record ConnectionSettingsDefaults(
    string Host,
    string Port,
    string SelectedConnectionMode,
    string SelectedAgentId,
    string ApiKey,
    string PerRequestContext,
    IReadOnlyList<string> ConnectionModes);

public interface IConnectionSettingsDialogService
{
    Task<ConnectionSettingsDialogResult?> ShowAsync(
        Window ownerWindow,
        ConnectionSettingsDefaults defaults,
        CancellationToken cancellationToken = default);
}
