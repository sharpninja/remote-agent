using RemoteAgent.Proto;

namespace RemoteAgent.App.Logic;

public interface IConnectionModeSelector
{
    Task<string?> SelectAsync();
}

public interface IAgentSelector
{
    Task<string?> SelectAsync(ServerInfoResponse serverInfo);
}

public sealed record PickedAttachment(byte[] Content, string ContentType, string FileName);

public interface IAttachmentPicker
{
    Task<PickedAttachment?> PickAsync();
}

public interface IPromptTemplateSelector
{
    Task<PromptTemplateDefinition?> SelectAsync(IReadOnlyList<PromptTemplateDefinition> templates);
}

public interface IPromptVariableProvider
{
    Task<string?> GetValueAsync(string variableName);
}

public interface ISessionTerminationConfirmation
{
    Task<bool> ConfirmAsync(string sessionLabel);
}

public interface INotificationService
{
    void Show(string title, string body);
}

/// <summary>Scans a QR code (or lets the user enter a pairing URL) and returns the raw URI string, or null if cancelled.</summary>
public interface IQrCodeScanner
{
    Task<string?> ScanAsync();
}

/// <summary>Routes deep-link URIs arriving from the OS (e.g. remoteagent://pair?…) to subscribers. Queues one URI if dispatched before any subscriber registers.</summary>
public interface IDeepLinkService
{
    /// <summary>Subscribe and immediately receive any queued pending URI.</summary>
    void Subscribe(Action<string> handler);
    /// <summary>Dispatch a URI to all subscribers; queues it if there are none yet.</summary>
    void Dispatch(string rawUri);
}
