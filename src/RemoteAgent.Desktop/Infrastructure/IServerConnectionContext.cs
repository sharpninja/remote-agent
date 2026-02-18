namespace RemoteAgent.Desktop.Infrastructure;

public interface IServerConnectionContext
{
    string Host { get; }
    string Port { get; }
    string ApiKey { get; }
    string SelectedAgentId { get; }
    string SelectedConnectionMode { get; }
    string PerRequestContext { get; }
    string ServerId { get; }
    string ServerDisplayName { get; }
}
