namespace RemoteAgent.App.Logic.Cqrs;

/// <summary>
/// Represents a void return for side-effect-only handlers.
/// </summary>
public readonly record struct Unit
{
    public static readonly Unit Value = default;
    public static readonly Task<Unit> TaskValue = Task.FromResult(Value);
}
