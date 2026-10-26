using Android.App;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using SPIC.MauiBlazorApp.Services;

namespace SPIC.MauiBlazorApp.Platforms.Android
{
    /// <summary>
    /// Keeps posting the JMDO's GPS location to the server while Live Tracking is on, independently
    /// of whether the app itself is in the foreground, backgrounded, or the screen is locked - to
    /// the extent Android actually allows (see the class-level honesty note at the bottom).
    ///
    /// A foreground service, not a Blazor-side timer: Android aggressively suspends background work
    /// the moment the app isn't visible, which is exactly the scenario this has to survive. The
    /// persistent, low-priority notification this requires is the trade-off Android imposes for
    /// that - and it doubles as the easy, always-visible way back to Stop Live Tracking the privacy
    /// requirement calls for.
    ///
    /// ForegroundServiceType.TypeLocation (not TypeSpecialUse, unlike SPIC.Ifms.Relay's own
    /// foreground service in this same solution) - this is a genuine, Play-Store-compliant location
    /// use case, so it gets the purpose-built type instead of the "no better category exists" one.
    /// </summary>
    [Service(
        Enabled = true,
        Exported = false,
        ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
    public sealed class JmdoTrackingForegroundService : Service, ILocationListener
    {
        public const string SessionIdExtra = "session_id";

        private const string ChannelId = "spic_jmdo_tracking_status";
        private const int NotificationId = 4301;

        // Android's own throttling/batching inside RequestLocationUpdates, not a separate timer -
        // lets the OS coalesce updates for battery life instead of this service forcing a fix every
        private static TimeSpan GetRequestInterval()
        {
            try
            {
                var seconds = Microsoft.Maui.Storage.Preferences.Default.Get("GpsTrackingIntervalSeconds", 20);
                if (seconds is < 1 or > 100) seconds = 20;
                return TimeSpan.FromSeconds(seconds);
            }
            catch
            {
                return TimeSpan.FromSeconds(20);
            }
        }
        private const float MinDistanceMeters = 25f;

        private LocationManager? _locationManager;
        private int _sessionId;
        private CancellationTokenSource? _cts;

        public static bool IsRunning { get; private set; }

        public override IBinder? OnBind(Intent? intent) => null;

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            _sessionId = intent?.GetIntExtra(SessionIdExtra, 0) ?? 0;
            if (_sessionId <= 0)
            {
                StopSelf();
                return StartCommandResult.NotSticky;
            }

            try
            {
                CreateChannel();
                StartInForeground(BuildNotification());

                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                IsRunning = true;

                StartLocationUpdates();

                // Flush anything left over from a previous run (app was killed mid-upload, etc.)
                // as soon as the service is back up, instead of waiting for the next fix.
                _ = JmdoTrackingBackgroundClient.FlushQueueAsync(_sessionId, _cts.Token);
            }
            catch (Exception)
            {
                StopSelf();
            }

            // Sticky, but see the class-level honesty note: Android can and does still kill this
            // under memory pressure, battery-saver policies, or OEM-specific background limits - a
            // restart attempt is not a guarantee of continuous coverage.
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            IsRunning = false;

            try { _locationManager?.RemoveUpdates(this); } catch { }

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            base.OnDestroy();
        }

        private void StartLocationUpdates()
        {
            _locationManager = (LocationManager?)GetSystemService(LocationService);
            if (_locationManager is null) return;

            var provider = _locationManager.IsProviderEnabled(LocationManager.GpsProvider)
                ? LocationManager.GpsProvider
                : LocationManager.NetworkProvider;

            if (!_locationManager.IsProviderEnabled(provider)) return; // location services off on the device - nothing to do

            _locationManager.RequestLocationUpdates(
                provider,
                (long)GetRequestInterval().TotalMilliseconds,
                MinDistanceMeters,
                this);
        }

        // ILocationListener - Location is ambiguous with Microsoft.Maui.Devices.Sensors.Location
        // (implicit MAUI usings), so every reference here is fully qualified.
        public void OnLocationChanged(global::Android.Locations.Location location)
        {
            if (_cts is not { IsCancellationRequested: false } cts) return;

            var sessionId = _sessionId;
            var lat = location.Latitude;
            var lng = location.Longitude;
            double? accuracy = location.HasAccuracy ? location.Accuracy : null;

            _ = JmdoTrackingBackgroundClient.ReportPointAsync(sessionId, lat, lng, accuracy, cts.Token);
        }

        public void OnLocationChanged(IList<global::Android.Locations.Location> locations)
        {
            foreach (var location in locations) OnLocationChanged(location);
        }

        public void OnProviderDisabled(string provider) { }
        public void OnProviderEnabled(string provider) { }
        public void OnStatusChanged(string? provider, [GeneratedEnum] Availability status, Bundle? extras) { }

        private void StartInForeground(Notification notification)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
                StartForeground(NotificationId, notification, global::Android.Content.PM.ForegroundService.TypeLocation);
            else
                StartForeground(NotificationId, notification);
        }

        private Notification BuildNotification()
        {
            var open = PendingIntent.GetActivity(
                this,
                0,
                new Intent(this, typeof(MainActivity)).SetFlags(ActivityFlags.SingleTop),
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var builder = new NotificationCompat.Builder(this, ChannelId);
            builder.SetContentTitle("SPIC ONE - Live Tracking active");
            builder.SetContentText("Your location is being tracked. Tap to open and stop tracking.");
            builder.SetSmallIcon(global::Android.Resource.Drawable.StatNotifySync);
            builder.SetPriority(NotificationCompat.PriorityLow);
            builder.SetContentIntent(open);
            builder.SetOngoing(true);
            builder.SetSilent(true);

            return builder.Build()
                ?? throw new InvalidOperationException("NotificationCompat.Builder returned no notification.");
        }

        // Notification channels are an API 26+ (Oreo) concept - this app's own min SDK is 24
        // (AndroidManifest/SupportedOSPlatformVersion), unlike SPIC.Ifms.Relay's (26+), so this
        // guard is required here, not just defensive: calling CreateNotificationChannel on an
        // actual API 24/25 device without it would be calling a method the OS doesn't have.
        private void CreateChannel()
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;

            var manager = (NotificationManager?)GetSystemService(NotificationService);
            if (manager is null) return;

            var channel = new NotificationChannel(
                ChannelId,
                "Live tracking status",
                NotificationImportance.Low)
            {
                Description = "Shown while SPIC ONE is tracking your location in the background."
            };

            manager.CreateNotificationChannel(channel);
        }

        /// <summary>Starts (or re-enters, if already running) the service for the given session.
        /// Safe to call every time Live Tracking is started - OnStartCommand just re-runs with the
        /// new session id. A no-op on API &lt; 26: Context.StartForegroundService doesn't exist that
        /// far back, and a background-tracking feature built on 26+-only APIs (notification
        /// channels, foreground service types) isn't meaningfully supportable below it anyway -
        /// Live Tracking still works foreground-only there via JmdoTrackingClientLoop.</summary>
        public static void EnsureRunning(Context context, int sessionId)
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;

            try
            {
                var intent = new Intent(context, typeof(JmdoTrackingForegroundService));
                intent.PutExtra(SessionIdExtra, sessionId);
                context.StartForegroundService(intent);
            }
            catch (Exception)
            {
                // Android 12+ refuses a foreground start from some background contexts - Live
                // Tracking was still started server-side, it just won't survive backgrounding until
                // the app is reopened and this is retried.
            }
        }

        public static void Stop(Context context)
        {
            try
            {
                context.StopService(new Intent(context, typeof(JmdoTrackingForegroundService)));
            }
            catch (Exception) { }
        }

        // ------------------------------------------------------------------------------------
        // Honesty note (matches the plan this service was built from): this is a best-effort,
        // not a guarantee. Android can still kill this service - under memory pressure, Doze/
        // App Standby on some OEM skins, "unrestricted battery usage" not being granted by the
        // JMDO, or the app being force-stopped from Settings. None of that is bypassed here.
        // What IS guaranteed: a session this service can no longer report for will correctly
        // show Stale -> Offline server-side (JMDOTrackingController.ResolveLiveStatus), never a
        // false "Live", and nothing except an explicit Stop Live Tracking ever marks it Stopped.
        // ------------------------------------------------------------------------------------
    }
}
