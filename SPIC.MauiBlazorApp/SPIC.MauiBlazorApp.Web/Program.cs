using SPIC.MauiBlazorApp.Shared.Services;
using SPIC.MauiBlazorApp.Web.Components;
using SPIC.MauiBlazorApp.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

// Add device-specific services used by the SPIC.MauiBlazorApp.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddScoped<LoginState>();

// ADD THIS
builder.Services.AddSingleton(new PlatformService
{
	IsWeb = true
});
builder.Services.AddScoped(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["ApiBaseUrl"] ?? "https://spicapi.apmtechnologies.in/";
    return new HttpClient { BaseAddress = new Uri(baseUrl) };
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