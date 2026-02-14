namespace RemoteAgent.App.Services;

/// <summary>Shows a system notification. On Android: creates a channel and posts a notification; tap opens the app.</summary>
public static partial class PlatformNotificationService
{
#if !ANDROID
    public static void ShowNotification(string title, string body) { }
#endif
}
