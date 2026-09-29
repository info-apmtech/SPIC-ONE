using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Spic.Infrastructure.Services.Telemetry;

/// <summary>
/// DI + pipeline registration of the metrics / error telemetry (docs/metrics-telemetry-plan.md).
/// Program.cs only calls AddSpicTelemetry() and UseSpicTelemetry().
/// </summary>
public static class TelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddSpicTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        // IHttpContextAccessor (logger provider) is registered by the host (AddHttpContextAccessor).
        services.Configure<TelemetryOptions>(configuration.GetSection(TelemetryOptions.SectionName));

        // Capture side: queue, background writer, rollup / purge, API logger provider.
        services.AddSingleton<TelemetryChannel>();
        services.AddHostedService<TelemetryWriter>();
        services.AddHostedService<TelemetryRollupService>();
        services.AddSingleton<ILoggerProvider>(sp => new DbTelemetryLoggerProvider(sp));

        // Ingest (TelemetryController) and the read side (MetricsController).
        services.AddSingleton<TelemetryIngestService>();
        services.AddSingleton<TelemetryTodayCache>();
        services.AddScoped<MetricsAccess>();
        services.AddScoped<MetricsQueryService>();
        return services;
    }

    /// <summary>
    /// Call FIRST in the pipeline. Adds the exception middleware (outermost) and, directly inside it,
    /// the request middleware. The request row is written after the rest of the pipeline ran, so the
    /// JWT claims and the matched endpoint are known there, and requests that authentication /
    /// authorization / CORS short-circuit (401, 403, preflight failures) are counted too.
    /// <paramref name="routeTemplate"/> returns the matched endpoint's route template
    /// ((ctx.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText): the routing types live in the
    /// ASP.NET shared framework, which this library does not reference.
    /// </summary>
    public static IApplicationBuilder UseSpicTelemetry(this IApplicationBuilder app, Func<HttpContext, string?>? routeTemplate)
    {
        TelemetryHttp.RouteTemplateResolver = routeTemplate;

        var services = app.ApplicationServices;
        var channel = services.GetRequiredService<TelemetryChannel>();
        var options = services.GetRequiredService<IOptionsMonitor<TelemetryOptions>>();
        var logger = services.GetRequiredService<ILogger<ExceptionTelemetryMiddleware>>();

        app.Use(next => new ExceptionTelemetryMiddleware(next, channel, logger).InvokeAsync);
        app.Use(next => new RequestTelemetryMiddleware(next, channel, options).InvokeAsync);
        return app;
    }
}
