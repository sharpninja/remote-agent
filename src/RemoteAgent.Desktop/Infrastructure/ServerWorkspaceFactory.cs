using Microsoft.Extensions.DependencyInjection;
using RemoteAgent.Desktop.ViewModels;

namespace RemoteAgent.Desktop.Infrastructure;

public sealed class CurrentServerContext
{
    public ServerRegistration? Registration { get; set; }
}

public sealed class ServerWorkspaceLease(IServiceScope scope, ServerWorkspaceViewModel viewModel) : IDisposable
{
    public IServiceScope Scope { get; } = scope;
    public ServerWorkspaceViewModel ViewModel { get; } = viewModel;

    public void Dispose()
    {
        Scope.Dispose();
    }
}

public interface IServerWorkspaceFactory
{
    ServerWorkspaceLease Create(ServerRegistration registration);
}

public sealed class ServerWorkspaceFactory(IServiceScopeFactory scopeFactory) : IServerWorkspaceFactory
{
    public ServerWorkspaceLease Create(ServerRegistration registration)
    {
        var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CurrentServerContext>();
        context.Registration = registration;
        var vm = scope.ServiceProvider.GetRequiredService<ServerWorkspaceViewModel>();
        return new ServerWorkspaceLease(scope, vm);
    }
}
