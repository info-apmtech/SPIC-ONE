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
                var seconds = Microsoft.Maui.Storage.Preferences.Default.Get("GpsTrackingIntervalSeconds", 10);
                if (seconds is < 1 or > 100) seconds = 10;
                return TimeSpan.FromSeconds(seconds);
            }
            catch
            {
                return TimeSpan.FromSeconds(10);
            }
        }
        // 0 - purely time-gated (minTime alone), matching the foreground polling loop
        // (JmdoTrackingClientLoop) and the admin-configured GPS Settings interval. A nonzero value
        // here (previously 25m) made RequestLocationUpdates withhold EVERY fix until the device moved
        // that far since the last one - invisible while the app is foregrounded (the JS/getCurrentPosition
        // loop has no such filter and masks it), but it meant a stationary JMDO closing the app saw
        // zero location updates reach the server at all: the foreground service notification stayed
        // up (the service itself was alive) while OnLocationChanged simply never fired again.
        private const float MinDistanceMeters = 0f;

        private LocationManager? _locationManager;
        private int _sessionId;
        private CancellationTokenSource? _cts;

        public static bool IsRunning { get; private set; }

        // Last-known-active session id, outliving process death - without this, a Sticky restart
        // (OS recreates the process after a kill) calls OnStartCommand with a null Intent, finds no
        // SessionIdExtra, and stops itself right back down: Sticky's redelivery was a no-op in
        // practice. Cleared only by an explicit Stop() call, never by OnDestroy, so it survives
        // exactly the kill scenarios it exists for.
        private const string PersistedSessionKey = "JmdoTrackingActiveSessionId";

        private static int GetPersistedSessionId()
        {
            try { return Microsoft.Maui.Storage.Preferences.Default.Get(PersistedSessionKey, 0); }
            catch { return 0; }
        }

        private static void SavePersistedSessionId(int sessionId)
        {
            try { Microsoft.Maui.Storage.Preferences.Default.Set(PersistedSessionKey, sessionId); }
            catch { }
        }

        private static void ClearPersistedSessionId()
        {
            try { Microsoft.Maui.Storage.Preferences.Default.Remove(PersistedSessionKey); }
            catch { }
        }

        // Backstop for OEM kills that bypass OnTaskRemoved entirely - some "clear all"/task-manager
        // kills (MIUI/ColorOS/etc.) SIGKILL the whole process without running any of its components'
        // normal shutdown lifecycle, so OnTaskRemoved's restart-on-swipe never gets a chance to run.
        // An inexact, Doze-aware alarm (SetAndAllowWhileIdle, not SetExactAndAllowWhileIdle) needs no
        // special permission and re-checks every WatchdogInterval whether a session is still meant to
        // be active; if so and the service isn't running, it restarts it and reschedules itself -
        // Stop() is the only thing that breaks the chain (CancelWatchdog).
        private const int WatchdogRequestCode = 4302;
        private static readonly TimeSpan WatchdogInterval = TimeSpan.FromMinutes(10);

        private static PendingIntent? WatchdogPendingIntent(Context context)
        {
            var intent = new Intent(context, typeof(TrackingWatchdogReceiver));
            return PendingIntent.GetBroadcast(context, WatchdogRequestCode, intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        }

        private static void ScheduleWatchdog(Context context)
        {
            try
            {
                if ((AlarmManager?)context.GetSystemService(AlarmService) is not { } alarmManager) return;
                var triggerAt = SystemClock.ElapsedRealtime() + (long)WatchdogInterval.TotalMilliseconds;
                alarmManager.SetAndAllowWhileIdle(AlarmType.ElapsedRealtimeWakeup, triggerAt, WatchdogPendingIntent(context));
            }
            catch (Exception) { }
        }

        private static void CancelWatchdog(Context context)
        {
            try
            {
                ((AlarmManager?)context.GetSystemService(AlarmService))?.Cancel(WatchdogPendingIntent(context));
            }
            catch (Exception) { }
        }

        /// <summary>Called by <see cref="TrackingWatchdogReceiver"/> on each alarm fire.</summary>
        public static void WatchdogCheck(Context context)
        {
            var sessionId = GetPersistedSessionId();
            if (sessionId <= 0) return; // tracking was explicitly stopped - nothing to watch for

            if (!IsRunning)
                EnsureRunning(context, sessionId);

            ScheduleWatchdog(context); // always re-arm while a session is persisted
        }

        public override IBinder? OnBind(Intent? intent) => null;

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            var incoming = intent?.GetIntExtra(SessionIdExtra, 0) ?? 0;
            _sessionId = incoming > 0 ? incoming : GetPersistedSessionId();
            if (_sessionId <= 0)
            {
                StopSelf();
                return StartCommandResult.NotSticky;
            }

            try
            {
                CreateChannel();
                StartInForeground(BuildNotification());
                SavePersistedSessionId(_sessionId);
                ScheduleWatchdog(this);

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

        // Fires when the user swipes the app away from Recents while this service is running (the
        // exact scenario reported as "closing the app from Recents stops tracking"). Restarting the
        // foreground service from here is explicitly exempt from Android 8+'s background-start
        // restrictions - unlike a cold/external start attempt, this process still has a live
        // component (this service) at the moment it runs. This does not override an OEM's own
        // process-kill behavior (MIUI/ColorOS/etc. can still kill the whole process outright - see
        // the class-level honesty note), but it fixes the stock-Android gap where nothing attempted
        // a restart at all.
        public override void OnTaskRemoved(Intent? rootIntent)
        {
            base.OnTaskRemoved(rootIntent);

            if (_sessionId <= 0 || ApplicationContext is not { } appContext) return;

            try
            {
                var intent = new Intent(appContext, typeof(JmdoTrackingForegroundService));
                intent.PutExtra(SessionIdExtra, _sessionId);
                if (OperatingSystem.IsAndroidVersionAtLeast(26))
                    appContext.StartForegroundService(intent);
                else
                    appContext.StartService(intent);
            }
            catch (Exception) { }

            // Also (re-)arm the watchdog here, not just in OnStartCommand - if the OEM kills the
            // process again right after this restart attempt (before OnStartCommand's own
            // ScheduleWatchdog call runs), the watchdog alarm scheduled below still survives the
            // process death and can recover it later.
            ScheduleWatchdog(appContext);
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

        // The interval RequestLocationUpdates was last registered with - compared against the live
        // Preference value on every fix (OnLocationChanged) so an admin changing GPS Settings mid-
        // session actually takes effect, instead of only applying to the NEXT Start Live Tracking.
        // Android has no API to change minTime on an active registration; RemoveUpdates + re-request
        // is the only way.
        private TimeSpan _registeredInterval;

        private void StartLocationUpdates()
        {
            _locationManager = (LocationManager?)GetSystemService(LocationService);
            if (_locationManager is null) return;

            var provider = _locationManager.IsProviderEnabled(LocationManager.GpsProvider)
                ? LocationManager.GpsProvider
                : LocationManager.NetworkProvider;

            if (!_locationManager.IsProviderEnabled(provider)) return; // location services off on the device - nothing to do

            _registeredInterval = GetRequestInterval();
            _locationManager.RequestLocationUpdates(
                provider,
                (long)_registeredInterval.TotalMilliseconds,
                MinDistanceMeters,
                this);
        }

        private void RestartLocationUpdatesIfIntervalChanged()
        {
            if (_locationManager is null) return;

            var latest = GetRequestInterval();
            if (latest == _registeredInterval) return;

            try { _locationManager.RemoveUpdates(this); } catch { }
            StartLocationUpdates(); // re-reads the Preference and re-registers with the new interval
        }

        // ILocationListener - Location is ambiguous with Microsoft.Maui.Devices.Sensors.Location
        // (implicit MAUI usings), so every reference here is fully qualified.
        public void OnLocationChanged(global::Android.Locations.Location location)
        {
            if (_cts is not { IsCancellationRequested: false } cts) return;

            RestartLocationUpdatesIfIntervalChanged();

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
            // The "route" extra is how MainActivity (cold start via OnCreate -> PendingDeepLink, or
            // warm via OnNewIntent -> NavHistoryService.Current directly) knows to land on
            // FieldDashboard specifically, instead of wherever the app would otherwise open to -
            // see MainActivity's OnCreate/OnNewIntent for the two paths that consume this.
            var openIntent = new Intent(this, typeof(MainActivity))
                .SetFlags(ActivityFlags.SingleTop)
                .PutExtra("route", "/FieldDashboard");
            var open = PendingIntent.GetActivity(
                this,
                0,
                openIntent,
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
            ClearPersistedSessionId();
            CancelWatchdog(context);

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
