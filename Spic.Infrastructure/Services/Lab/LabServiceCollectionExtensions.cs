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
        // registrations are added by the API-Lab workstream
        return services;
    }
}
