namespace RemoteAgent.App.Logic.Cqrs;

/// <summary>
/// Resolves and invokes the correct handler for a given request.
/// </summary>
public interface IRequestDispatcher
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
