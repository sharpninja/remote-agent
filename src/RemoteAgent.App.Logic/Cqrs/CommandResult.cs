namespace RemoteAgent.App.Logic.Cqrs;

/// <summary>
/// Result type for handlers that can succeed or fail with a message.
/// </summary>
public record CommandResult(bool Success, string? ErrorMessage = null)
{
    public static CommandResult Ok() => new(true);
    public static CommandResult Fail(string message) => new(false, message);
}

/// <summary>
/// Result type for handlers that return data on success or fail with a message.
/// </summary>
public record CommandResult<T>(bool Success, T? Data = default, string? ErrorMessage = null)
    where T : class
{
    public static CommandResult<T> Ok(T data) => new(true, data);
    public static CommandResult<T> Fail(string message) => new(false, ErrorMessage: message);
}
