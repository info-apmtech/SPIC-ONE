using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreLocation;
using Foundation;
using UIKit;
using SPIC.MauiBlazorApp.Services;

namespace SPIC.MauiBlazorApp.Platforms.iOS
{
    /// <summary>
    /// iOS counterpart to Android's JmdoTrackingForegroundService.
    /// Manages continuous background location tracking using CoreLocation, with
    /// UIBackgroundModes: location and showsBackgroundLocationIndicator enabled.
    /// Uses BeginBackgroundTask to ensure HTTP upload requests are not killed prematurely
    /// when iOS attempts to suspend the backgrounded app.
    /// </summary>
    public sealed class JmdoLocationManager : CLLocationManagerDelegate
    {
        private static JmdoLocationManager? _instance;

        private readonly CLLocationManager _manager;
        private int _sessionId;
        private CancellationTokenSource? _cts;
        private DateTime _lastReportTime = DateTime.MinValue;
        private CLLocation? _lastReportedLocation;

        private JmdoLocationManager()
        {
            _manager = new CLLocationManager
            {
                DesiredAccuracy = CLLocation.AccuracyBest,
                DistanceFilter = 10, // 10 meters for responsive field updates
                AllowsBackgroundLocationUpdates = true,
                PausesLocationUpdatesAutomatically = false,
                ShowsBackgroundLocationIndicator = true,
                ActivityType = CLActivityType.OtherNavigation
            };
            _manager.Delegate = this;
        }

        public static void Start(int sessionId)
        {
            _instance ??= new JmdoLocationManager();
            _instance._sessionId = sessionId;
            _instance._lastReportTime = DateTime.MinValue;
            _instance._lastReportedLocation = null;

            _instance._cts?.Cancel();
            _instance._cts?.Dispose();
            _instance._cts = new CancellationTokenSource();

            // Ensure authorization is determined using standard WhenInUse (single prompt)
            if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0))
            {
                var status = _instance._manager.AuthorizationStatus;
                if (status == CLAuthorizationStatus.NotDetermined)
                {
                    _instance._manager.RequestWhenInUseAuthorization();
                }
            }

            _instance._manager.StartUpdatingLocation();

            // Also monitor significant location changes as backup if iOS suspends fine updates
            try
            {
                if (CLLocationManager.SignificantLocationChangeMonitoringAvailable)
                {
                    _instance._manager.StartMonitoringSignificantLocationChanges();
                }
            }
            catch { }

            // Request background execution time to flush any backlog
            nint bgTaskId = UIApplication.SharedApplication.BeginBackgroundTask("JmdoStartFlush", () => { });
            _ = Task.Run(async () =>
            {
                try
                {
                    await JmdoTrackingBackgroundClient.FlushQueueAsync(sessionId, _instance._cts.Token);
                }
                finally
                {
                    if (bgTaskId != UIApplication.BackgroundTaskInvalid)
                    {
                        UIApplication.SharedApplication.EndBackgroundTask(bgTaskId);
                    }
                }
            });
        }

        public static void Stop()
        {
            if (_instance is null) return;

            try
            {
                _instance._manager.StopUpdatingLocation();
                if (CLLocationManager.SignificantLocationChangeMonitoringAvailable)
                {
                    _instance._manager.StopMonitoringSignificantLocationChanges();
                }
            }
            catch { }

            _instance._cts?.Cancel();
            _instance._cts?.Dispose();
            _instance._cts = null;
        }

        private static int GetIntervalSeconds()
        {
            try
            {
                var s = Microsoft.Maui.Storage.Preferences.Default.Get("GpsTrackingIntervalSeconds", 20);
                return s is >= 1 and <= 100 ? s : 20;
            }
            catch
            {
                return 20;
            }
        }

        public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
        {
            if (_cts is not { IsCancellationRequested: false } cts) return;
            var latest = locations.LastOrDefault();
            if (latest is null) return;

            // Throttle duplicate stationary fixes proportionally to the configured tracking interval
            // to save battery and bandwidth, while ensuring points are sent periodically even if stationary.
            var interval = GetIntervalSeconds();
            var throttleThreshold = Math.Max(1, (int)(interval * 0.75));
            var now = DateTime.UtcNow;
            var timeSinceLast = (now - _lastReportTime).TotalSeconds;
            var distanceMoved = _lastReportedLocation != null ? latest.DistanceFrom(_lastReportedLocation) : double.MaxValue;

            if (_lastReportedLocation != null && timeSinceLast < throttleThreshold && distanceMoved < 10)
            {
                return;
            }

            _lastReportTime = now;
            _lastReportedLocation = latest;

            var sessionId = _sessionId;
            var lat = latest.Coordinate.Latitude;
            var lng = latest.Coordinate.Longitude;
            double? accuracy = latest.HorizontalAccuracy >= 0 ? latest.HorizontalAccuracy : null;

            // Request background execution time so iOS does NOT suspend the app before HTTP upload finishes!
            nint bgTaskId = UIApplication.SharedApplication.BeginBackgroundTask("JmdoReportLocation", () => { });
            _ = Task.Run(async () =>
            {
                try
                {
                    await JmdoTrackingBackgroundClient.ReportPointAsync(sessionId, lat, lng, accuracy, cts.Token);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[JmdoLocationManager] ReportPointAsync error: {ex.Message}");
                }
                finally
                {
                    if (bgTaskId != UIApplication.BackgroundTaskInvalid)
                    {
                        UIApplication.SharedApplication.EndBackgroundTask(bgTaskId);
                    }
                }
            });
        }

        public override void Failed(CLLocationManager manager, NSError error)
        {
            System.Diagnostics.Debug.WriteLine($"[JmdoLocationManager] CLLocationManager failed: {error.LocalizedDescription}");
        }
    }
}
