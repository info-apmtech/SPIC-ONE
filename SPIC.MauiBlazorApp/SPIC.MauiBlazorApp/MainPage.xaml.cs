using Microsoft.AspNetCore.Components.WebView.Maui;
﻿namespace SPIC.MauiBlazorApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        /// <summary>Exposed for the Android back-button handling in MainActivity.</summary>
        public BlazorWebView WebView => blazorWebView;
    }
}
