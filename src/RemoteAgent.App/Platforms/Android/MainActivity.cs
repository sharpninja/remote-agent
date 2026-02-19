using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using RemoteAgent.App.Logic;

namespace RemoteAgent.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "remoteagent",
    DataHost = "pair")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleDeepLinkIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        HandleDeepLinkIntent(intent);
    }

    private static void HandleDeepLinkIntent(Intent? intent)
    {
        if (intent?.Action != Intent.ActionView || intent.Data == null) return;
        var rawUri = intent.Data.ToString();
        if (string.IsNullOrEmpty(rawUri)) return;

        var services = IPlatformApplication.Current?.Services;
        services?.GetService<IDeepLinkService>()?.Dispatch(rawUri);
    }
}
