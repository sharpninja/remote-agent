#nullable enable
using Android.App;
using Android.Content;
using Android.OS;
using Android.Content.PM;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace RemoteAgent.App.Services;

public static partial class PlatformNotificationService
{
    private const string ChannelId = "remote_agent_notify";
    private const int NotifyId = 1;

    public static void ShowNotification(string title, string body)
    {
        var ctx = Platform.CurrentActivity ?? (Platform.AppContext as Context);
        if (ctx == null) return;

        CreateChannelIfNeeded(ctx);

        var builder = new NotificationCompat.Builder(ctx, ChannelId)
            .SetContentTitle(title)
            .SetContentText(body)
            .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
            .SetAutoCancel(true)
            .SetPriority((int)NotificationPriority.Default);

        var nm = NotificationManagerCompat.From(ctx);
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu &&
            ContextCompat.CheckSelfPermission(ctx, Android.Manifest.Permission.PostNotifications) != Permission.Granted)
            return;
        nm.Notify(NotifyId, builder.Build());
    }

    [System.Runtime.Versioning.SupportedOSPlatform("android26.0")]
    private static void CreateChannelIfNeeded(Context ctx)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        var channel = new NotificationChannel(ChannelId, "Agent notifications", NotificationImportance.Default)
        {
            Description = "Notifications when the agent sends a high-priority message.",
        };
        var nm = (NotificationManager?)ctx.GetSystemService(Context.NotificationService);
        nm?.CreateNotificationChannel(channel);
    }
}
