using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services.Telemetry;

/// <summary>
/// Which app this is (Web / Android / iOS / Windows / MacCatalyst) and its version. One per host,
/// registered as a singleton in SPIC.MauiBlazorApp.Web/Program.cs and SPIC.MauiBlazorApp/MauiProgram.cs.
/// AuthHttpMessageHandler sends it on every API call (X-Spic-Client / X-Spic-Version).
/// </summary>
public sealed class ClientInfo
{
    public ClientInfo(TelemetryApp app, string? version)
    {
        App = app;
        Version = Trim(version);
        ClientHeader = app.ToString().ToLowerInvariant();
    }

    public TelemetryApp App { get; }

    /// <summary>"1.0.0" style; the "+commit" suffix of an informational version is dropped. Max 40 chars.</summary>
    public string Version { get; }

    /// <summary>X-Spic-Client value: web | android | ios | windows | maccatalyst.</summary>
    public string ClientHeader { get; }

    private static string Trim(string? version)
    {
        if (string.IsNullOrWhiteSpace(version)) return "1.0.0";
        var v = version.Trim();
        var plus = v.IndexOf('+');
        if (plus > 0) v = v[..plus];
        return v.Length > 40 ? v[..40] : v;
    }
}
