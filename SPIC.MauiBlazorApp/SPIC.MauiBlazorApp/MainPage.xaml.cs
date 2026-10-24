using Microsoft.AspNetCore.Components.WebView.Maui;
using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.MauiBlazorApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        /// <summary>Exposed for the Android back-button handling in MainActivity.</summary>
        public BlazorWebView WebView => blazorWebView;

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
