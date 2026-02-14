// Nullable disabled for this file only: Android/AndroidX bindings produce CS8602 at our call sites
// despite null checks (see docs/REPOSITORY_RULES.md "Explicit allowance").
#nullable disable
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
    private const string PermissionPostNotifications = "android.permission.POST_NOTIFICATIONS";

    public static void ShowNotification(string title, string body)
    {
        var ctx = Platform.CurrentActivity ?? (Platform.AppContext as Context);
        if (ctx == null) return;

        ShowNotificationWithContext(ctx, title, body);
    }

    private static void ShowNotificationWithContext(Context context, string title, string body)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            CreateChannelIfNeeded(context);

        var builder = new NotificationCompat.Builder(context, ChannelId);
        if (builder is not null)
        {
            builder
                .SetContentTitle(title)
                .SetContentText(body)
                .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
                .SetAutoCancel(true)
                .SetPriority((int)NotificationPriority.Default);

            var nm = NotificationManagerCompat.From(context);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu &&
                ContextCompat.CheckSelfPermission(context, PermissionPostNotifications) != Permission.Granted)
                return;
            var notification = builder.Build();
            if (nm is not null && notification is not null)
                nm.Notify(NotifyId, notification);
        }
    }

    private static void CreateChannelIfNeeded(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        var channel = new NotificationChannel(ChannelId, "Agent notifications", NotificationImportance.Default)
        {
            Description = "Notifications when the agent sends a high-priority message.",
            EnableLights = false,
            EnableVibration = false,
            LockscreenVisibility = NotificationVisibility.Public,
        };
        var nm = (NotificationManager)context.GetSystemService(Context.NotificationService);
        if (nm != null)
            nm.CreateNotificationChannel(channel);
    }
}
