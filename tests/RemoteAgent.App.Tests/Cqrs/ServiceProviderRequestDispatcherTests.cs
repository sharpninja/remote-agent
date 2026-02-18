using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteAgent.App.Logic.Cqrs;

namespace RemoteAgent.App.Tests.Cqrs;

public sealed record StubRequest(Guid CorrelationId, string Value) : IRequest<string>;

public sealed class StubHandler : IRequestHandler<StubRequest, string>
{
    public CancellationToken CapturedToken { get; private set; }

    public Task<string> HandleAsync(StubRequest request, CancellationToken cancellationToken = default)
    {
        CapturedToken = cancellationToken;
        return Task.FromResult($"echo:{request.Value}");
    }
}

public sealed record ThrowingRequest(Guid CorrelationId) : IRequest<string>;

public sealed class ThrowingHandler : IRequestHandler<ThrowingRequest, string>
{
    public Task<string> HandleAsync(ThrowingRequest request, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("boom");
    }
}

public sealed record UnregisteredRequest(Guid CorrelationId) : IRequest<string>;

public class ServiceProviderRequestDispatcherTests
{
    private static (ServiceProviderRequestDispatcher Dispatcher, CapturingLogger<ServiceProviderRequestDispatcher> Logger) CreateDispatcher(
        Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        configure?.Invoke(services);
        var logger = new CapturingLogger<ServiceProviderRequestDispatcher>();
        services.AddSingleton<ILogger<ServiceProviderRequestDispatcher>>(logger);
        var sp = services.BuildServiceProvider();
        return (new ServiceProviderRequestDispatcher(sp, logger), logger);
    }

    [Fact]
    public async Task SendAsync_WhenHandlerRegistered_ShouldResolveAndReturnResult()
    {
        var (dispatcher, _) = CreateDispatcher(s =>
            s.AddTransient<IRequestHandler<StubRequest, string>, StubHandler>());

        var result = await dispatcher.SendAsync(new StubRequest(Guid.NewGuid(), "hello"));

        result.Should().Be("echo:hello");
    }

    [Fact]
    public async Task SendAsync_WhenNoHandlerRegistered_ShouldThrowInvalidOperationException()
    {
        var (dispatcher, _) = CreateDispatcher();

        var act = () => dispatcher.SendAsync(new UnregisteredRequest(Guid.NewGuid()));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SendAsync_ShouldPassCancellationTokenToHandler()
    {
        var handler = new StubHandler();
        var (dispatcher, _) = CreateDispatcher(s =>
            s.AddSingleton<IRequestHandler<StubRequest, string>>(handler));
        using var cts = new CancellationTokenSource();

        await dispatcher.SendAsync(new StubRequest(Guid.NewGuid(), "test"), cts.Token);

        handler.CapturedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task SendAsync_WhenHandlerThrows_ShouldPropagateException()
    {
        var (dispatcher, _) = CreateDispatcher(s =>
            s.AddTransient<IRequestHandler<ThrowingRequest, string>, ThrowingHandler>());

        var act = () => dispatcher.SendAsync(new ThrowingRequest(Guid.NewGuid()));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
    }

    [Fact]
    public async Task SendAsync_WhenCorrelationIdIsEmpty_ShouldThrowArgumentException()
    {
        var (dispatcher, _) = CreateDispatcher(s =>
            s.AddTransient<IRequestHandler<StubRequest, string>, StubHandler>());

        var act = () => dispatcher.SendAsync(new StubRequest(Guid.Empty, "test"));

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*CorrelationId*");
    }

    [Fact]
    public async Task SendAsync_ShouldLogDebugEntryWithRequestTypeCorrelationIdAndParameters()
    {
        var (dispatcher, logger) = CreateDispatcher(s =>
            s.AddTransient<IRequestHandler<StubRequest, string>, StubHandler>());
        var correlationId = Guid.NewGuid();

        await dispatcher.SendAsync(new StubRequest(correlationId, "hello"));

        var entryLog = logger.Entries
            .Where(e => e.Level == LogLevel.Debug && e.Message.Contains("CQRS Enter"))
            .Should().ContainSingle().Subject;

        entryLog.Message.Should().Contain("StubRequest");
        entryLog.Message.Should().Contain(correlationId.ToString());
        entryLog.Message.Should().Contain("hello");
    }

    [Fact]
    public async Task SendAsync_OnSuccess_ShouldLogDebugExitWithRequestTypeCorrelationIdAndResult()
    {
        var (dispatcher, logger) = CreateDispatcher(s =>
            s.AddTransient<IRequestHandler<StubRequest, string>, StubHandler>());
        var correlationId = Guid.NewGuid();

        await dispatcher.SendAsync(new StubRequest(correlationId, "world"));

        var exitLog = logger.Entries
            .Where(e => e.Level == LogLevel.Debug && e.Message.Contains("CQRS Leave"))
            .Should().ContainSingle().Subject;

        exitLog.Message.Should().Contain("StubRequest");
        exitLog.Message.Should().Contain(correlationId.ToString());
        exitLog.Message.Should().Contain("echo:world");
    }

    [Fact]
    public async Task SendAsync_OnException_ShouldLogDebugExitWithRequestTypeCorrelationIdAndExceptionMessage()
    {
        var (dispatcher, logger) = CreateDispatcher(s =>
            s.AddTransient<IRequestHandler<ThrowingRequest, string>, ThrowingHandler>());
        var correlationId = Guid.NewGuid();

        try { await dispatcher.SendAsync(new ThrowingRequest(correlationId)); }
        catch (InvalidOperationException) { }

        var exitLog = logger.Entries
            .Where(e => e.Level == LogLevel.Debug && e.Message.Contains("CQRS Leave"))
            .Should().ContainSingle().Subject;

        exitLog.Message.Should().Contain("ThrowingRequest");
        exitLog.Message.Should().Contain(correlationId.ToString());
        exitLog.Message.Should().Contain("boom");
        exitLog.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task SendAsync_CorrelationIdInEntryLog_ShouldMatchRequestCorrelationId()
    {
        var (dispatcher, logger) = CreateDispatcher(s =>
            s.AddTransient<IRequestHandler<StubRequest, string>, StubHandler>());
        var correlationId = Guid.NewGuid();

        await dispatcher.SendAsync(new StubRequest(correlationId, "trace"));

        var entryLog = logger.Entries.First(e => e.Message.Contains("CQRS Enter"));
        entryLog.Message.Should().Contain($"[{correlationId}]");
    }
}
