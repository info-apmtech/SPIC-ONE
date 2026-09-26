using Microsoft.Extensions.DependencyInjection;

namespace Spic.Infrastructure.Services.Lab;

/// <summary>
/// DI registrations for the lab workstream (auto-result engine, activity writer, code
/// generators). Owned by the API-Lab agent; Program.cs only calls AddSasLab().
/// </summary>
public static class LabServiceCollectionExtensions
{
    public static IServiceCollection AddSasLab(this IServiceCollection services)
    {
        // API-Lab workstream (LabController): caller access resolved once per request, the
        // activity writer that stamps the caller on every log row, and the stateless auto-result
        // engine. LabCodes and LabV1Sync are static helpers.
        services.AddScoped<LabAccess>();
        services.AddScoped<LabActivityWriter>();
        services.AddSingleton<LabAutoResultEngine>();
        return services;
    }
}
