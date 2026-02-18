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
