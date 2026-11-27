using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SPIC.Core.Interfaces;

namespace Spic.Infrastructure.Services.Sms;

public static class SmsServiceCollectionExtensions
{
    /// <summary>
    /// Binds "Sms" and registers the ISmsSender that Sms:Provider selects ("Log" by default).
    /// MSG91 calls use a named HttpClient with a 10 second timeout.
    /// </summary>
    public static IServiceCollection AddSpicSms(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SmsOptions.SectionName);
        services.Configure<SmsOptions>(section);
        services.AddHttpClient(Msg91SmsSender.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(10));

        var provider = section.Get<SmsOptions>()?.EffectiveProvider ?? SmsOptions.LogProvider;
        if (provider == SmsOptions.Msg91Provider)
            services.AddSingleton<ISmsSender, Msg91SmsSender>();
        else
            services.AddSingleton<ISmsSender, LogSmsSender>();

        return services;
    }
}
