using Microsoft.Extensions.Logging;
using RemoteAgent.Desktop.Infrastructure;

namespace RemoteAgent.Desktop.Logging;

/// <summary>
/// ILoggerProvider that captures every log call into IAppLogStore so the
/// Management App Log view (FR-12.12) can display live app-process logging.
/// </summary>
public sealed class AppLoggerProvider(IAppLogStore store) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new AppLoggerCategory(categoryName, store);

    public void Dispose() { }
}

internal sealed class AppLoggerCategory(string category, IAppLogStore store) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        store.Add(new AppLogEntry(
            Timestamp: DateTimeOffset.UtcNow,
            Level: logLevel,
            Category: category,
            Message: formatter(state, exception),
            ExceptionMessage: exception?.ToString()));
    }
}
