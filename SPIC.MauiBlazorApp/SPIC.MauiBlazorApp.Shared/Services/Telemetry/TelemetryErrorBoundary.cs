using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SPIC.MauiBlazorApp.Shared.Services.Telemetry;

// In the Services namespace (imported by _Imports.razor) so both layouts can use the tag as is.
namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// The layouts' error boundary (MainLayout, EmptyLayout): same UI as ErrorBoundary, but a page
/// render / event-handler exception is reported through ClientTelemetry. The base class is not
/// called on purpose: it only logs, and the host logger provider would then record it twice.
/// </summary>
public class TelemetryErrorBoundary : ErrorBoundary
{
    [Inject] private ClientTelemetry Telemetry { get; set; } = default!;

    protected override Task OnErrorAsync(Exception exception)
    {
        try
        {
            return Telemetry.ReportAsync(exception, "ErrorBoundary");
        }
        catch
        {
            return Task.CompletedTask;
        }
    }
}
