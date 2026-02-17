using Microsoft.Extensions.DependencyInjection;

namespace RemoteAgent.Desktop.ViewModels;

public interface IDesktopSessionViewModelFactory
{
    DesktopSessionViewModel Create(string title, string connectionMode, string agentId);
}

public sealed class DesktopSessionViewModelFactory(IServiceProvider services) : IDesktopSessionViewModelFactory
{
    public DesktopSessionViewModel Create(string title, string connectionMode, string agentId)
    {
        var session = services.GetRequiredService<DesktopSessionViewModel>();
        session.Title = title;
        session.ConnectionMode = connectionMode;
        session.AgentId = agentId;
        session.SessionId = Guid.NewGuid().ToString("N")[..12];
        return session;
    }
}
