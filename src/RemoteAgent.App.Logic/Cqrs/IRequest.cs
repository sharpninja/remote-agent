namespace RemoteAgent.App.Logic.Cqrs;

/// <summary>
/// Base interface for all CQRS request types. Every request carries a
/// <see cref="CorrelationId"/> for end-to-end tracing of UI interactions.
/// </summary>
/// <typeparam name="TResponse">The handler return type.</typeparam>
public interface IRequest<TResponse>
{
    Guid CorrelationId { get; }
}
