using System.Reflection;
using Microsoft.Extensions.Logging;
using SPIC.MauiBlazorApp.Services;
using SPIC.Core.Entities;
using SPIC.MauiBlazorApp.Shared.Services;
using SPIC.MauiBlazorApp.Shared.Services.Telemetry;

namespace SPIC.MauiBlazorApp
{
    public static class MauiProgram
    {
        /// <summary>
        /// Set at build time with -p:SpicApiBaseUrl=... (see the csproj); defaults to production.
        /// </summary>
        public static string ApiBaseUrl { get; } =
            typeof(MauiProgram).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "SpicApiBaseUrl")?.Value
            ?? "https://api.spicone.in/";

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Add device-specific services used by the SPIC.MauiBlazorApp.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();
            builder.Services.AddSingleton<ISessionStore, SecureSessionStore>();
            builder.Services.AddScoped<FileDownloadService>();
            builder.Services.AddScoped<ToastService>();
            builder.Services.AddScoped<ConnectivityState>();

            builder.Services.AddMauiBlazorWebView();
			builder.Services.AddScoped<LoginState>();
			// Shell state shared by MainLayout and the parameterless Components/Shell components.
			builder.Services.AddScoped<ShellSession>();
			builder.Services.AddScoped<LoadingService>();
			builder.Services.AddScoped<AppSearchState>();
			builder.Services.AddScoped<LookupCacheService>();
			builder.Services.AddScoped<GuestHouseBookingState>();
			builder.Services.AddScoped<DigitalLibraryApi>();
			builder.Services.AddScoped<CommunityApi>();
			builder.Services.AddScoped<SasApi>();
			builder.Services.AddScoped<SasPaymentApi>();
			builder.Services.AddScoped<LabApi>();
			builder.Services.AddScoped<MetricsApi>();

			// Usage + error telemetry (docs/metrics-telemetry-plan.md): the app / version header on
			// every API call, page views and client errors, and anything the app logs at Error+.
			builder.Services.AddSingleton(new ClientInfo(CurrentApp(), AppInfo.Current.VersionString));
			builder.Services.AddSingleton<TelemetryCircuitRegistry>();
			builder.Services.AddScoped<ClientTelemetry>();
			builder.Logging.Services.AddSingleton<ILoggerProvider>(sp => new TelemetryLoggerProvider(
				sp.GetRequiredService<ClientInfo>(),
				sp.GetRequiredService<TelemetryCircuitRegistry>(),
				ApiBaseUrl,
				TelemetrySource.Client));

			// ADD THIS
			builder.Services.AddSingleton(new PlatformService
            {
                IsWeb = false
            });
            builder.Services.AddTransient<AuthHttpMessageHandler>();
            builder.Services.AddScoped(sp =>
            {
                var handler = sp.GetRequiredService<AuthHttpMessageHandler>();
                handler.InnerHandler = new HttpClientHandler();
                return new HttpClient(handler)
                {
                    // Build-time setting: -p:SpicApiBaseUrl=... (see csproj); defaults to production.
                    BaseAddress = new Uri(ApiBaseUrl),
                    Timeout = TimeSpan.FromMinutes(30)
                };
            });

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static TelemetryApp CurrentApp()
        {
            var platform = DeviceInfo.Platform;
            if (platform == DevicePlatform.Android) return TelemetryApp.Android;
            if (platform == DevicePlatform.iOS) return TelemetryApp.iOS;
            if (platform == DevicePlatform.WinUI) return TelemetryApp.Windows;
            if (platform == DevicePlatform.MacCatalyst || platform == DevicePlatform.macOS) return TelemetryApp.MacCatalyst;
            return TelemetryApp.Unknown;
        }
    }
}