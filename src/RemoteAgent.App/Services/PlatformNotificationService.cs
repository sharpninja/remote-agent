namespace RemoteAgent.App.Services;

/// <summary>Shows a system notification (FR-3.2, FR-3.3, TR-5.4). On Android: creates a channel and posts a notification; tapping opens the app so the message is visible in the chat.</summary>
/// <remarks>Platform-specific implementation in partials (e.g. Android). Other platforms no-op.</remarks>
/// <see href="https://sharpninja.github.io/remote-agent/functional-requirements.html">Functional requirements (FR-3)</see>
/// <see href="https://sharpninja.github.io/remote-agent/technical-requirements.html">Technical requirements (TR-5)</see>
public static partial class PlatformNotificationService
{
#if !ANDROID
    /// <summary>Shows a notification (no-op on this platform). On Android, tap opens the app.</summary>
    /// <param name="title">Notification title.</param>
    /// <param name="body">Notification body (e.g. message preview).</param>
    public static void ShowNotification(string title, string body) { }
#endif
}
