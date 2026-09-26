using Microsoft.Extensions.DependencyInjection;
using SPIC.Core.Interfaces;

namespace Spic.Infrastructure.Services.LabReports;

/// <summary>
/// DI registrations for the reports workstream (report rows, PDF / Excel rendering,
/// translations, fonts). Owned by the API-Reports agent; Program.cs only calls AddSasLabReports().
/// Until that workstream lands, a placeholder implementation keeps batch completion working
/// without creating report rows.
/// </summary>
public static class LabReportsServiceCollectionExtensions
{
    public static IServiceCollection AddSasLabReports(this IServiceCollection services)
    {
        services.AddScoped<ILabReportService, PlaceholderLabReportService>();
        return services;
    }

    private sealed class PlaceholderLabReportService : ILabReportService
    {
        public Task GenerateForBatchAsync(int batchId, string? byUserId, string? byName, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
