using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.AspNetCore.Components.WebView.Maui;

namespace SPIC.MauiBlazorApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        /// <summary>
        /// WebView inspection is off in Release. Launching from adb with
        /// <c>am start ... --ez webdebug true</c> turns it on for that run only, so a store
        /// build can be profiled with Chrome DevTools without shipping a debuggable app.
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();
            // After OnCreate: the BlazorWebView handler resets the flag while creating the view.
            if (Intent?.GetBooleanExtra("webdebug", false) == true)
            {
                Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
            }
        }

        /// <summary>
        /// Hardware / gesture back: step back through the Blazor page history (which also
        /// closes an open BottomSheet or More sheet, since they push a history entry) and
        /// only leave the app when there is nothing left to go back to. Without this the
        /// Android back button closed the whole app from any page.
        /// </summary>
        public override void OnBackPressed()
        {
            var page = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page as MainPage;
            if (page?.WebView?.Handler?.PlatformView is Android.Webkit.WebView webView && webView.CanGoBack())
            {
                webView.GoBack();
                return;
            }

            base.OnBackPressed();
        }
    }
}
