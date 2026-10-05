using Android.App;
using Android.Content.PM;
using Android.OS;
using SPIC.MauiBlazorApp.Platforms.Android;

namespace SPIC.MauiBlazorApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Bring the IFMS CAPTCHA watcher back up whenever the app is opened.
            // Cheap when it is already running, and it covers the case where
            // Android stopped the service while the phone was idle.
            try
            {
                IfmsWatchService.EnsureRunning(this);
            }
            catch
            {
                // The app must still start even if the watcher cannot.
            }
        }
    }
}
