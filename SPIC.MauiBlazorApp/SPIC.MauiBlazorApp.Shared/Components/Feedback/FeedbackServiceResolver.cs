using Microsoft.Extensions.DependencyInjection;

namespace SPIC.MauiBlazorApp.Shared.Components.Feedback;

/// <summary>
/// Feedback components take their service from a <c>CascadingValue</c> when one is
/// supplied (dev demo, tests) and from DI otherwise. Resolving lazily lets the components
/// give a clear error message instead of a bare "no service registered" exception.
/// </summary>
internal static class FeedbackServiceResolver
{
    public static T Resolve<T>(T? cascaded, IServiceProvider services) where T : class
        => cascaded
           ?? services.GetService<T>()
           ?? throw new InvalidOperationException(
               $"{typeof(T).Name} is not registered. Add services.AddScoped<{typeof(T).Name}>() " +
               "in Program.cs and MauiProgram.cs (see Components/Feedback/README.md).");
}
