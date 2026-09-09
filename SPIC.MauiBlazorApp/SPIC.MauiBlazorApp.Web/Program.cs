using SPIC.MauiBlazorApp.Shared.Services;
using SPIC.MauiBlazorApp.Web.Components;
using SPIC.MauiBlazorApp.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataProtection()
    .SetApplicationName("SPIC");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 100 * 1024 * 1024;

        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(100);
    });

// Device-specific services
builder.Services.AddSingleton<IFormFactor, FormFactor>();

builder.Services.AddSingleton<IIfmsRelayHost,
    SPIC.MauiBlazorApp.Web.Services.IfmsRelayHost>();

builder.Services.AddScoped<LoginState>();
builder.Services.AddScoped<LoadingService>();
builder.Services.AddScoped<AppSearchState>();
builder.Services.AddScoped<LookupCacheService>();
builder.Services.AddScoped<GuestHouseBookingState>();

builder.Services.AddSingleton(new PlatformService
{
    IsWeb = true
});

builder.Services.AddTransient<AuthHttpMessageHandler>();

builder.Services.AddScoped(sp =>
{
    var handler =
        sp.GetRequiredService<AuthHttpMessageHandler>();

    handler.InnerHandler =
        new HttpClientHandler();

    var config =
        sp.GetRequiredService<IConfiguration>();

    var baseUrl =
        config["ApiBaseUrl"]
        ?? "https://spicapi.apmiot.com/";

    return new HttpClient(handler)
    {
        BaseAddress = new Uri(baseUrl),
        Timeout = TimeSpan.FromMinutes(60)
    };
});


var app = builder.Build();


// ============================================================
// PRODUCTION CONFIGURATION
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Error",
        createScopeForErrors: true);

    app.UseHsts();
}


app.UseHttpsRedirection();


// ============================================================
// MAINTENANCE MODE
// ============================================================

app.Use(async (context, next) =>
{
    var maintenanceEnabled =
        app.Configuration.GetValue<bool>(
            "MaintenanceMode:Enabled");

    if (maintenanceEnabled)
    {
        var maintenancePage =
            app.Configuration["MaintenanceMode:Page"]
            ?? "unavailable.html";

        var filePath = Path.Combine(
            app.Environment.WebRootPath,
            maintenancePage);

        if (File.Exists(filePath))
        {
            context.Response.StatusCode =
                StatusCodes.Status503ServiceUnavailable;

            context.Response.ContentType =
                "text/html; charset=utf-8";

            context.Response.Headers.RetryAfter =
                "300";

            await context.Response.SendFileAsync(
                filePath);

            return;
        }

        // Fallback if maintenance HTML
        // is accidentally missing.
        context.Response.StatusCode =
            StatusCodes.Status503ServiceUnavailable;

        context.Response.ContentType =
            "text/plain; charset=utf-8";

        await context.Response.WriteAsync(
            "Service temporarily unavailable.");

        return;
    }

    await next();
});


// ============================================================
// NORMAL APPLICATION
// ============================================================

app.UseStatusCodePagesWithReExecute(
    "/not-found",
    createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.UseWebSockets(
    new WebSocketOptions
    {
        KeepAliveInterval =
            TimeSpan.FromSeconds(15)
    });

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(
            SPIC.MauiBlazorApp.Shared._Imports
        ).Assembly);

app.Run();