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
    });

// Add device-specific services used by the SPIC.MauiBlazorApp.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddScoped<LoginState>();
builder.Services.AddScoped<LoadingService>();
builder.Services.AddScoped<AppSearchState>();
builder.Services.AddScoped<LookupCacheService>();

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
   // var baseUrl = config["ApiBaseUrl"] ?? "https://spicapi.apmiot.com/";
    var baseUrl = config["ApiBaseUrl"] ?? "https://localhost:7032/";
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

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(SPIC.MauiBlazorApp.Shared._Imports).Assembly);

app.Run();