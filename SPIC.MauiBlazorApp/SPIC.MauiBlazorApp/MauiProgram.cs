using Microsoft.Extensions.Logging;
using SPIC.MauiBlazorApp.Services;
using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.MauiBlazorApp
{
    public static class MauiProgram
    {
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
            builder.Services.AddSingleton<IIfmsRelayHost, IfmsRelayHost>();

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddScoped<LoginState>();
            builder.Services.AddScoped<LoadingService>();
            builder.Services.AddScoped<AppSearchState>();
			builder.Services.AddScoped<LookupCacheService>();

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
                    //BaseAddress = new Uri("https://localhost:7032/")
                    BaseAddress = new Uri("https://spicapi.apmiot.com/"),
                    Timeout = TimeSpan.FromMinutes(30)
                };
            });

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}