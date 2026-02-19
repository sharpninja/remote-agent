using Microsoft.Extensions.Logging;

namespace RemoteAgent.App.Tests.Cqrs;

/// <summary>Test helper that captures <see cref="ILogger{T}"/> entries for assertion in dispatcher tests. TR-18.1, TR-18.2.</summary>
internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception), exception));
    }
}
