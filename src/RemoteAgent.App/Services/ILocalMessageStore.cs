namespace RemoteAgent.App.Services;

/// <summary>Local storage of chat messages (TR-11.1).</summary>
public interface ILocalMessageStore
{
    IReadOnlyList<ChatMessage> Load();
    void Add(ChatMessage message);
    void SetArchived(Guid messageId, bool archived);
}
