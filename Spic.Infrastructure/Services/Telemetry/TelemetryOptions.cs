namespace Spic.Infrastructure.Services.Telemetry;

/// <summary>
/// Config section "Telemetry" (docs/metrics-telemetry-plan.md section 3). IngestKey comes from the
/// environment (Telemetry__IngestKey, Key Vault secret telemetry-ingest-key in Azure); empty = no
/// server-attributed ingest, the web host's logger provider then posts anonymously.
/// </summary>
public sealed class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    /// <summary>Master switch: false = nothing is captured or written (the Metrics page still reads).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Shared secret of X-Telemetry-Key (server-attributed batches). Empty = disabled.</summary>
    public string? IngestKey { get; set; }

    public TelemetryRetentionOptions Retention { get; set; } = new();

    /// <summary>"/" is an exact match; every other entry is a path-segment prefix (/health, /swagger).
    /// Empty = the defaults (the binder appends to a pre-filled list, so defaults live below).</summary>
    public List<string>? ExcludePaths { get; set; }

    public static readonly string[] DefaultExcludePaths = { "/health", "/swagger", "/" };

    public IReadOnlyList<string> EffectiveExcludePaths =>
        ExcludePaths is { Count: > 0 } ? ExcludePaths : DefaultExcludePaths;

    /// <summary>Log level from which the API logger provider records an error row.</summary>
    public string MinimumLogLevel { get; set; } = "Error";

    public int RollupMinutes { get; set; } = 15;

    public int ChannelCapacity { get; set; } = 10000;

    public bool HasIngestKey => !string.IsNullOrWhiteSpace(IngestKey);
}

/// <summary>Days each table keeps (TelemetryRollupService purge, 03:00 UTC).</summary>
public sealed class TelemetryRetentionOptions
{
    public int RequestDays { get; set; } = 30;
    public int PageViewDays { get; set; } = 30;
    public int ErrorDays { get; set; } = 90;
    public int DailyDays { get; set; } = 400;
}
