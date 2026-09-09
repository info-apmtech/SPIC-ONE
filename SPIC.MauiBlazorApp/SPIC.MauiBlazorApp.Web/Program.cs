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
        options.MaximumReceiveMessageSize = 100 * 1024 * 1024; // 100 MB

        // Keep the SignalR heartbeat in step with Blazor Server's default
        // keep-alive and let the client/server tolerate short network or
        // proxy interruptions, so a brief stall no longer tears the circuit
        // down and forces the "Rejoining the server..." reconnect flow.
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(100);
    });

// Add device-specific services used by the SPIC.MauiBlazorApp.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddSingleton<IIfmsRelayHost, SPIC.MauiBlazorApp.Web.Services.IfmsRelayHost>();
builder.Services.AddScoped<LoginState>();
builder.Services.AddScoped<LoadingService>();
builder.Services.AddScoped<AppSearchState>();
builder.Services.AddScoped<LookupCacheService>();
builder.Services.AddScoped<GuestHouseBookingState>();

// ADD THIS
builder.Services.AddSingleton(new PlatformService
{
    IsWeb = true
});
builder.Services.AddTransient<AuthHttpMessageHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthHttpMessageHandler>();
    handler.InnerHandler = new HttpClientHandler();
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["ApiBaseUrl"] ?? "https://spicapi.apmiot.com/";
    //var baseUrl = config["ApiBaseUrl"] ?? "https://previewspicapi.apmiot.com/";
    // var baseUrl = config["ApiBaseUrl"] ?? "https://localhost:7032/";
    return new HttpClient(handler)
    {
        BaseAddress = new Uri(baseUrl),
        Timeout = TimeSpan.FromMinutes(60)
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
// ============================================================
// MAINTENANCE MODE
// ============================================================

app.Use(async (context, next) =>
{
    var maintenanceEnabled =
        app.Configuration.GetValue<bool>("MaintenanceMode:Enabled");

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

            context.Response.Headers.RetryAfter = "300";

            await context.Response.SendFileAsync(filePath);
            return;
        }

        context.Response.StatusCode =
            StatusCodes.Status503ServiceUnavailable;

        await context.Response.WriteAsync(
            "Service temporarily unavailable.");

        return;
    }

    // Maintenance OFF means continue previous application normally
    await next();
});

app.UseAntiforgery();

// Explicitly enable persistent WebSocket transport for interactive server
// circuits, with a 15s ping keep-alive so the connection stays alive through
// proxy/IIS idle timeouts instead of dropping and showing the reconnect UI.
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(15) });

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(SPIC.MauiBlazorApp.Shared._Imports).Assembly);

app.Run();
