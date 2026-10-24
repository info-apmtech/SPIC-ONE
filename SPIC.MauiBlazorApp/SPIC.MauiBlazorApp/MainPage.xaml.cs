using Microsoft.AspNetCore.Components.WebView.Maui;
using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.MauiBlazorApp
{
    public partial class MainPage : ContentPage
    {
        /// <summary>Bridge for DeviceInterop.NotifyContentReady to reach this page without DI -
        /// same pattern as NavHistoryService.Current / MainActivity.Instance.</summary>
        internal static MainPage? Current { get; private set; }

        public MainPage()
        {
            InitializeComponent();
            Current = this;
        }

        /// <summary>Exposed for the Android back-button handling in MainActivity.</summary>
        public BlazorWebView WebView => blazorWebView;

        /// <summary>
        /// Called once Blazor's first real content has rendered (Routes.razor's OnAfterRenderAsync,
        /// via DeviceInterop.NotifyContentReady). Fades out and hides BootOverlay - the native
        /// icon+spinner shown from this page's very first frame, bridging the gap between the
        /// native Android splash screen dismissing and the WebView actually having content to show
        /// (see MainPage.xaml's own comment on BootOverlay for why that gap exists).
        /// </summary>
        internal void HideBootOverlay()
        {
            if (BootOverlay is null || !BootOverlay.IsVisible) return;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await BootOverlay.FadeToAsync(0, 200, Easing.CubicOut);
                BootOverlay.IsVisible = false;
            });
        }

        /// <summary>
        /// Intercepts hardware / gesture back navigation on Android.
        /// When the Blazor application has history to step back into (or is on an inner screen),
        /// navigates back in Blazor and returns true so the native activity does NOT close.
        /// Returns false only when already at the landing page / root, allowing the system to exit the app.
        /// </summary>
        protected override bool OnBackButtonPressed()
        {
            if (NavHistoryService.Current != null && NavHistoryService.Current.CanGoBack)
            {
                return NavHistoryService.Current.TryGoBack();
            }

            return base.OnBackButtonPressed();
        }
    }
}
