namespace RemoteAgent.App.Logic.Cqrs;

/// <summary>
/// Handler contract. One handler per request type.
/// </summary>
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
