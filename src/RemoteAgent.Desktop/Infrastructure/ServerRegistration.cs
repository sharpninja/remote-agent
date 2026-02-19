namespace RemoteAgent.Desktop.Infrastructure;

public sealed class ServerRegistration
{
    public string ServerId { get; set; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; set; } = "";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = ServiceDefaults.Port;
    public string ApiKey { get; set; } = "";
}
