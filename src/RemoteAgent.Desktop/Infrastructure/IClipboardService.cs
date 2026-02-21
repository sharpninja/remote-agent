namespace RemoteAgent.Desktop.Infrastructure;

/// <summary>Abstraction for clipboard write access, enabling testability of clipboard-writing operations.</summary>
public interface IClipboardService
{
    Task SetTextAsync(string text);
}
