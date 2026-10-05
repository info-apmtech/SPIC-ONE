using Spic.Infrastructure.Data;

namespace Spic.Infrastructure.Services.Telemetry;

/// <summary>
/// Today's usage / route rows computed live from the raw tables (TelemetryAggregator), shared by the
/// Metrics calls of one page load. Recomputed when older than 30 s or as soon as the writer has saved
/// new rows on this replica, so a fresh event shows up on the next call.
/// </summary>
public sealed class TelemetryTodayCache
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromSeconds(30);

    private readonly TelemetryChannel _channel;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private TelemetryAggregator.DayResult? _value;
    private DateTime _computedAt;
    private long _writtenAt = -1;

    public TelemetryTodayCache(TelemetryChannel channel) => _channel = channel;

    public async Task<TelemetryAggregator.DayResult> GetAsync(AppDbContext db, CancellationToken ct)
    {
        if (TryFresh(out var fresh)) return fresh!;

        await _gate.WaitAsync(ct);
        try
        {
            if (TryFresh(out fresh)) return fresh!;
            var written = _channel.WrittenSinceStart;
            var value = await TelemetryAggregator.ComputeDayAsync(db, DateTime.UtcNow.Date, ct);
            _value = value;
            _computedAt = DateTime.UtcNow;
            _writtenAt = written;
            return value;
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool TryFresh(out TelemetryAggregator.DayResult? value)
    {
        value = _value;
        return value is not null
            && value.Day == DateTime.UtcNow.Date
            && DateTime.UtcNow - _computedAt < MaxAge
            && _writtenAt == _channel.WrittenSinceStart;
    }
}
