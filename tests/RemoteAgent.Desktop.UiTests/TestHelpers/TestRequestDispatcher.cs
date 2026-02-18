using RemoteAgent.App.Logic.Cqrs;

namespace RemoteAgent.Desktop.UiTests.TestHelpers;

/// <summary>
/// Test dispatcher that routes to explicitly registered handler instances.
/// Exceptions from handlers propagate up (no wrapping).
/// </summary>
public sealed class TestRequestDispatcher : IRequestDispatcher
{
    private readonly Dictionary<Type, Func<object, CancellationToken, Task<object>>> _handlers = new();

    public TestRequestDispatcher Register<TReq, TResp>(IRequestHandler<TReq, TResp> handler)
        where TReq : IRequest<TResp>
    {
        _handlers[typeof(TReq)] = async (req, ct) => (await handler.HandleAsync((TReq)req, ct))!;
        return this;
    }

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (_handlers.TryGetValue(request.GetType(), out var h))
            return (TResponse)await h(request, cancellationToken);
        // Unregistered requests are silently ignored (return default) to avoid test noise from background tasks
        return default!;
    }
}
