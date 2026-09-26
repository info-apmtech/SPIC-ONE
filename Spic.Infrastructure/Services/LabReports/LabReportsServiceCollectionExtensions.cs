using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SPIC.Core.Interfaces;

namespace Spic.Infrastructure.Services.LabReports;

/// <summary>
/// DI registrations for the reports workstream (report rows, PDF / Excel rendering,
/// translations, fonts). Owned by the API-Reports agent; Program.cs only calls AddSasLabReports().
/// </summary>
public static class LabReportsServiceCollectionExtensions
{
    public static IServiceCollection AddSasLabReports(this IServiceCollection services)
    {
        services.AddScoped<ILabReportService, LabReportService>();
        services.AddScoped<LabReportReader>();
        services.AddScoped<LabReportFiles>();
        services.AddSingleton<LabTranslations>();
        services.AddSingleton<LabFonts>();
        services.AddSingleton<LabReportRenderer>();
        services.AddHostedService<LabReportsStartup>();
        return services;
    }
}

/// <summary>
/// Startup work that must not delay or break the API start: registers the report fonts (and logs
/// which languages they cover) and seeds the LabTranslation rows. Runs in the background; the
/// translations are also seeded lazily on first use when this failed (e.g. database not ready).
/// </summary>
public sealed class LabReportsStartup : IHostedService
{
    private readonly LabFonts _fonts;
    private readonly LabTranslations _translations;
    private readonly ILogger<LabReportsStartup> _logger;

    public LabReportsStartup(LabFonts fonts, LabTranslations translations, ILogger<LabReportsStartup> logger)
    {
        _fonts = fonts;
        _translations = translations;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                _fonts.EnsureLoaded();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LabReportsStartup: font registration failed.");
            }

            try
            {
                await _translations.EnsureSeededAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LabReportsStartup: translation seeding failed; it is retried on first use.");
            }
        }, CancellationToken.None);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
