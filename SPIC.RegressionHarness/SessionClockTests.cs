using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.RegressionHarness;

internal static class SessionClockTests
{
    public static void Run()
    {
        Console.WriteLine("Session clock");
        var serverNow = new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);
        foreach (var skewHours in new[] { -13, 0, 13 })
        {
            var device = new DeviceTime { Now = serverNow.AddHours(skewHours) };
            var clock = new SessionClock(device);
            clock.Synchronize(serverNow);
            var expiry = serverNow.AddHours(1).UtcDateTime;
            Check.Equal($"Clock matches API with {skewHours}h skew", serverNow.UtcDateTime, clock.UtcNow);
            Check.Equal($"Fresh token remains valid with {skewHours}h skew", true, clock.UtcNow < expiry);

            var restored = new SessionClock(device);
            restored.Restore(clock.SavedOffset);
            Check.Equal("Saved clock survives app restart", clock.UtcNow, restored.UtcNow);
            device.Now = device.Now.AddHours(1);
            Check.Equal("Real expiry still occurs after one hour", true, restored.UtcNow >= expiry);

            // A changed device clock is corrected by the next API response.
            device.Now = device.Now.AddHours(-20);
            restored.Synchronize(serverNow.AddHours(1));
            Check.Equal("Next API response corrects changed device time", expiry, restored.UtcNow);
        }
        var fallbackTime = new DeviceTime { Now = serverNow };
        var fallback = new SessionClock(fallbackTime);
        fallback.Restore("invalid");
        Check.Equal("Missing server time uses device time", serverNow.UtcDateTime, fallback.UtcNow);
    }

    private sealed class DeviceTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; }
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
