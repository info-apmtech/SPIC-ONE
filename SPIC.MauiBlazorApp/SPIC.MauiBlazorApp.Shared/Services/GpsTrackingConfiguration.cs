using System;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// Holds the runtime and cached GPS tracking interval for JMDO tracking.
/// Fallback default is 20 seconds. Platform hosts (e.g. MAUI) can bind OnGetNativeInterval
/// and OnSaveNativeInterval to persistent Preferences so the interval survives app restarts
/// and offline sessions.
/// </summary>
public static class GpsTrackingConfiguration
{
    public const int DefaultIntervalSeconds = 20;
    public const int MinIntervalSeconds = 1;
    public const int MaxIntervalSeconds = 100;

    private static int _intervalSeconds = DefaultIntervalSeconds;

    public static Func<int>? OnGetNativeInterval { get; set; }
    public static Action<int>? OnSaveNativeInterval { get; set; }

    public static int IntervalSeconds
    {
        get
        {
            if (OnGetNativeInterval != null)
            {
                try
                {
                    var val = OnGetNativeInterval();
                    if (val is >= MinIntervalSeconds and <= MaxIntervalSeconds)
                    {
                        _intervalSeconds = val;
                        return val;
                    }
                }
                catch { }
            }
            return _intervalSeconds;
        }
        set
        {
            if (value is >= MinIntervalSeconds and <= MaxIntervalSeconds)
            {
                _intervalSeconds = value;
                try
                {
                    OnSaveNativeInterval?.Invoke(value);
                }
                catch { }
            }
        }
    }
}
