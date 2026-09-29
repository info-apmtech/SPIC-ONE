using Microsoft.AspNetCore.Components.Server.Circuits;
using SPIC.MauiBlazorApp.Shared.Services.Telemetry;

namespace SPIC.MauiBlazorApp.Web.Services;

/// <summary>
/// Gives the circuit's ClientTelemetry its session id (the circuit id) and keeps
/// TelemetryCircuitRegistry in step, so an error the host logs with a circuit id can be put on
/// a user and a route (docs/metrics-telemetry-plan.md row 4). Scoped: one per circuit.
/// </summary>
public sealed class TelemetryCircuitHandler : CircuitHandler
{
    private readonly ClientTelemetry _telemetry;
    private readonly TelemetryCircuitRegistry _registry;

    public TelemetryCircuitHandler(ClientTelemetry telemetry, TelemetryCircuitRegistry registry)
    {
        _telemetry = telemetry;
        _registry = registry;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        try
        {
            _registry.Touch(circuit.Id);
            _telemetry.SessionId = circuit.Id;
        }
        catch
        {
            // telemetry must never stop a circuit from opening
        }

        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        try
        {
            _registry.Remove(circuit.Id);
        }
        catch
        {
        }

        return Task.CompletedTask;
    }
}
