using CoreLocation;
using Foundation;
using SPIC.MauiBlazorApp.Services;

namespace SPIC.MauiBlazorApp.Platforms.iOS
{
    /// <summary>
    /// iOS counterpart to Android's JmdoTrackingForegroundService. iOS has no foreground-service/
    /// persistent-notification concept of its own - once AllowsBackgroundLocationUpdates is on, iOS
    /// shows its own automatic "location in use" indicator (the status-bar pill / blue bar) while
    /// background updates are active, which already satisfies "clearly inform the JMDO tracking
    /// continues in the background" without any custom notification code here.
    ///
    /// Started/stopped from DeviceInterop.StartBackgroundTracking/StopBackgroundTracking, the exact
    /// same entry points the Android implementation uses - FieldDashboard.razor's Blazor code calls
    /// one JS bridge either way and never knows which platform handled it.
    /// </summary>
    public sealed class JmdoLocationManager : CLLocationManagerDelegate
    {
        // Lazily created once and reused - CLLocationManager is meant to be held for the life of
        // the tracking session, not recreated per update.
        private static JmdoLocationManager? _instance;

        private readonly CLLocationManager _manager;
        private int _sessionId;
        private CancellationTokenSource? _cts;

        private JmdoLocationManager()
        {
            _manager = new CLLocationManager
            {
                // iOS throttles background delivery more aggressively than Android and doesn't give
                // exact timer control while backgrounded - this requests the highest practical
                // accuracy/cadence and a configuration that favors regular (not just significant-
                // change) updates, but the actual delivered interval is a best-effort target on iOS,
                // not a guarantee, especially on older OS versions or under Low Power Mode.
                DesiredAccuracy = CLLocation.AccuracyBest,
                DistanceFilter = 25, // meters - matches Android's own MinDistanceMeters
                AllowsBackgroundLocationUpdates = true,
                PausesLocationUpdatesAutomatically = false,
                ShowsBackgroundLocationIndicator = true
            };
            _manager.Delegate = this;
        }

        public static void Start(int sessionId)
        {
            _instance ??= new JmdoLocationManager();
            _instance._sessionId = sessionId;

            _instance._cts?.Cancel();
            _instance._cts?.Dispose();
            _instance._cts = new CancellationTokenSource();

            _instance._manager.StartUpdatingLocation();

            // Flush anything left over from a previous run as soon as tracking (re)starts, instead
            // of waiting for the next fix.
            _ = JmdoTrackingBackgroundClient.FlushQueueAsync(sessionId, _instance._cts.Token);
        }

        public static void Stop()
        {
            if (_instance is null) return;

            _instance._manager.StopUpdatingLocation();
            _instance._cts?.Cancel();
            _instance._cts?.Dispose();
            _instance._cts = null;
        }

        public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
        {
            if (_cts is not { IsCancellationRequested: false } cts) return;
            var latest = locations.LastOrDefault();
            if (latest is null) return;

            var sessionId = _sessionId;
            var lat = latest.Coordinate.Latitude;
            var lng = latest.Coordinate.Longitude;
            double? accuracy = latest.HorizontalAccuracy >= 0 ? latest.HorizontalAccuracy : null;

            _ = JmdoTrackingBackgroundClient.ReportPointAsync(sessionId, lat, lng, accuracy, cts.Token);
        }

        public override void Failed(CLLocationManager manager, NSError error)
        {
            // A single failed fix is not worth stopping the session over - the next update (or the
            // server-side Stale/Offline status, once enough of them fail silently) reflects it.
        }

        // ------------------------------------------------------------------------------------
        // Honesty note (matches JmdoTrackingForegroundService's own, Android side): iOS can still
        // suspend background location delivery - under Low Power Mode, if the user denies "Always"
        // and only grants "While Using", after a force-quit (iOS does NOT relaunch a force-quit app
        // for location updates the way it does for a backgrounded one), or OS-level throttling that
        // varies by device/iOS version. None of that is bypassed here. A session this can no longer
        // report for correctly shows Stale -> Offline server-side, never a false "Live".
        // ------------------------------------------------------------------------------------
    }
}
