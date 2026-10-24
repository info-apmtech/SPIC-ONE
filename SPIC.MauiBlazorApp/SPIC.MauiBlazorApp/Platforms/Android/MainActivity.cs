using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.AspNetCore.Components.WebView.Maui;
using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.MauiBlazorApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        internal static MainActivity? Instance { get; private set; }

        public MainActivity()
        {
            Instance = this;
        }

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
        /// Hardware / gesture back button handler. Calls NavHistoryService directly - it's a plain
        /// static C# reference (Current), not a JS round-trip, since MainActivity and the Blazor
        /// circuit share the same process. Must go through TryGoBackAsync (not the sync TryGoBack),
        /// because the one case that matters most here - nowhere left to go, i.e. the app's root
        /// page - is exactly the case where it needs to await the "Exit SPIC ONE?" ConfirmDialog
        /// before deciding whether to actually let the app close.
        /// </summary>
        public override void OnBackPressed() => _ = HandleBackPressedAsync();

        private async Task HandleBackPressedAsync()
        {
            System.Diagnostics.Debug.WriteLine($"[BACKDBG] OnBackPressed fired. Current={(NavHistoryService.Current is null ? "null" : "set")} CanGoBack={NavHistoryService.Current?.CanGoBack}");
            try
            {
                if (NavHistoryService.Current != null && await NavHistoryService.Current.TryGoBackAsync())
                {
                    System.Diagnostics.Debug.WriteLine("[BACKDBG] TryGoBackAsync returned true (handled).");
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BACKDBG] HandleBackPressedAsync threw: {ex}");
            }

            System.Diagnostics.Debug.WriteLine("[BACKDBG] Falling through to base.OnBackPressed (exit).");
            base.OnBackPressed();
        }
    }
}
