using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RemoteAgent.App.Logic.Cqrs;

/// <summary>
/// Reflection-based dispatcher that resolves handlers from an <see cref="IServiceProvider"/>
/// and provides cross-cutting Debug-level entry/exit logging with CorrelationId tracing.
/// </summary>
public sealed class ServiceProviderRequestDispatcher : IRequestDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ServiceProviderRequestDispatcher> _logger;

    public ServiceProviderRequestDispatcher(
        IServiceProvider serviceProvider,
        ILogger<ServiceProviderRequestDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.CorrelationId == Guid.Empty)
            throw new ArgumentException("CorrelationId must not be Guid.Empty.", nameof(request));

        var requestType = request.GetType();
        var correlationId = request.CorrelationId;

        _logger.LogDebug("CQRS Enter {RequestType} [{CorrelationId}]: {Request}",
            requestType.Name, correlationId, request);

        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handler = _serviceProvider.GetRequiredService(handlerType);
        var method = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.HandleAsync))!;

        try
        {
            var result = await (Task<TResponse>)method.Invoke(handler, [request, cancellationToken])!;
            _logger.LogDebug("CQRS Leave {RequestType} [{CorrelationId}]: {Result}",
                requestType.Name, correlationId, result);
            return result;
        }
        catch (TargetInvocationException tie) when (tie.InnerException is not null)
        {
            _logger.LogDebug(tie.InnerException, "CQRS Leave {RequestType} [{CorrelationId}] with exception: {ExceptionMessage}",
                requestType.Name, correlationId, tie.InnerException.Message);
            throw tie.InnerException;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "CQRS Leave {RequestType} [{CorrelationId}] with exception: {ExceptionMessage}",
                requestType.Name, correlationId, ex.Message);
            throw;
        }
    }
}
