using System.Globalization;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>Uses the API clock for session expiry even when the device clock is incorrect.</summary>
public sealed class SessionClock
{
    private readonly TimeProvider _timeProvider;
    private TimeSpan _offset;

    public SessionClock() : this(TimeProvider.System) { }

    public SessionClock(TimeProvider timeProvider) => _timeProvider = timeProvider;

    public DateTime UtcNow => (_timeProvider.GetUtcNow() + _offset).UtcDateTime;

    public void Synchronize(DateTimeOffset serverTime) =>
        _offset = serverTime - _timeProvider.GetUtcNow();

    public string SavedOffset => _offset.Ticks.ToString(CultureInfo.InvariantCulture);

    public void Restore(string? savedOffset)
    {
        if (long.TryParse(savedOffset, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
            _offset = TimeSpan.FromTicks(ticks);
    }
}
