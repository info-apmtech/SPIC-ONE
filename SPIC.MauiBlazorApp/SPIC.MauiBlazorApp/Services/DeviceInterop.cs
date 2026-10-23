using Microsoft.JSInterop;

namespace SPIC.MauiBlazorApp.Services
{
    /// <summary>
    /// Native replacements for the browser-only JavaScript the shared UI relies on.
    /// wwwroot/maui-interop.js overrides window.downloadFileFromBytes, window.open,
    /// window.getCurrentPosition and friends to call these methods, so none of the
    /// 150+ shared pages needs to know it is running inside a WebView.
    ///
    /// Static [JSInvokable] methods are reachable from JS as
    /// DotNet.invokeMethodAsync('SPIC.MauiBlazorApp', '&lt;MethodName&gt;', ...).
    /// </summary>
    public static class DeviceInterop
    {
        /// <summary>
        /// Writes the bytes to the app cache and opens them with the system viewer
        /// (PDF viewer, Excel, image viewer...). Falls back to the share sheet when no
        /// app can open the type. Replaces the &lt;a download&gt; and window.open(blob)
        /// patterns, which mobile WebViews ignore.
        /// </summary>
        [JSInvokable]
        public static async Task<bool> SaveAndOpenFile(string fileName, string contentType, string base64Data)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64Data);

                var folder = Path.Combine(FileSystem.Current.CacheDirectory, "downloads");
                Directory.CreateDirectory(folder);

                var safeName = string.Join("_", (string.IsNullOrWhiteSpace(fileName) ? "file" : fileName)
                    .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
                var path = Path.Combine(folder, safeName);
                await File.WriteAllBytesAsync(path, bytes);

                return await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var opened = await Launcher.Default.OpenAsync(
                        new OpenFileRequest(safeName, new ReadOnlyFile(path, contentType)));

                    if (!opened)
                    {
                        await Share.Default.RequestAsync(new ShareFileRequest(safeName, new ShareFile(path, contentType)));
                    }

                    return true;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveAndOpenFile failed: {ex}");
                return false;
            }
        }

        /// <summary>Native share sheet for a title / text / link (window.spic.share in the WebView).</summary>
        [JSInvokable]
        public static async Task<bool> ShareText(string title, string text, string url)
        {
            try
            {
                return await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var request = new ShareTextRequest
                    {
                        Title = string.IsNullOrWhiteSpace(title) ? "SPIC ONE" : title,
                        Subject = string.IsNullOrWhiteSpace(title) ? null : title,
                        Text = string.IsNullOrWhiteSpace(text) ? null : text,
                        Uri = string.IsNullOrWhiteSpace(url) ? null : url
                    };
                    await Share.Default.RequestAsync(request);
                    return true;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShareText failed: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Replaces window.open. http(s) goes to the system browser; tel:, sms:,
        /// mailto:, whatsapp: and any other scheme go to whichever app handles it.
        /// </summary>
        [JSInvokable]
        public static async Task<bool> OpenExternal(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return false;
            }

            // Internal payment gateway URLs or checkout frames should not open external browser
            if (uri.Host.Contains("razorpay.com", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                return await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (uri.Scheme is "http" or "https")
                    {
                        await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
                        return true;
                    }

                    return await Launcher.Default.TryOpenAsync(uri);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenExternal failed: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Replaces navigator.geolocation. Asks for the location permission through
        /// the OS prompt, then returns the same shape location.js resolves with.
        /// </summary>
        [JSInvokable]
        public static async Task<GeoPosition> GetCurrentPosition()
        {
            try
            {
                return await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    }

                    if (status != PermissionStatus.Granted)
                    {
                        return new GeoPosition(0, 0, "Location permission was not granted.");
                    }

                    var location = await Geolocation.Default.GetLocationAsync(
                        new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(15)));

                    if (location is null)
                    {
                        return new GeoPosition(0, 0, "Location is currently unavailable.");
                    }

                    return new GeoPosition(location.Latitude, location.Longitude, null);
                });
            }
            catch (FeatureNotEnabledException)
            {
                return new GeoPosition(0, 0, "Location services are turned off on this device.");
            }
            catch (Exception ex)
            {
                return new GeoPosition(0, 0, ex.Message);
            }
        }

        public sealed record GeoPosition(double Latitude, double Longitude, string? Error);

        /// <summary>
        /// Starts native background tracking for the given session - window.startBackgroundTracking
        /// in the WebView, called right after JMDOTrackingApi.StartAsync succeeds (FieldDashboard.
        /// razor). Requests background location ("Allow all the time" on Android, "Always" on iOS)
        /// and, on Android, the notification permission the persistent status notification needs -
        /// both with the OS's own prompt, so the JMDO is explicitly asked and informed, never
        /// enabled silently. Returns false (never throws) if a permission was declined or the
        /// platform-specific service failed to start - Live Tracking itself (the server-side
        /// session) is unaffected either way; this only controls whether it can keep running once
        /// the app is backgrounded.
        /// </summary>
        [JSInvokable]
        public static async Task<bool> StartBackgroundTracking(int sessionId)
        {
            if (sessionId <= 0) return false;

            try
            {
                return await MainThread.InvokeOnMainThreadAsync(async () =>
                {
#if IOS
                    // Prime the token cache while device is unlocked and in foreground
                    await JmdoTrackingBackgroundClient.PrimeTokenCacheAsync();

                    // On iOS, background location is fully supported under "When In Use"
                    // with AllowsBackgroundLocationUpdates = true and ShowsBackgroundLocationIndicator = true.
                    // Requesting only LocationWhenInUse gives a single, clear permission prompt to the user
                    // without confusing back-to-back system dialogs.
                    var whenInUse = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                    if (whenInUse != PermissionStatus.Granted)
                    {
                        whenInUse = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    }

                    if (whenInUse != PermissionStatus.Granted)
                    {
                        return false;
                    }

                    SPIC.MauiBlazorApp.Platforms.iOS.JmdoLocationManager.Start(sessionId);
                    return true;
#elif ANDROID
                    var status = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
                    if (status != PermissionStatus.Granted)
                    {
                        status = await Permissions.RequestAsync<Permissions.LocationAlways>();
                    }

                    if (status != PermissionStatus.Granted)
                    {
                        // Live Tracking still works in the foreground (the client-side loop in
                        // JmdoTrackingClientLoop keeps posting points while the app is open) - it
                        // just won't survive backgrounding without this permission.
                        return false;
                    }

                    var notifStatus = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                    if (notifStatus != PermissionStatus.Granted)
                    {
                        notifStatus = await Permissions.RequestAsync<Permissions.PostNotifications>();
                    }
                    // Notification permission is for the persistent status indicator only - Android
                    // 13+ will simply run the foreground service without a visible notification if
                    // declined, which still works but is less transparent to the JMDO, so this is
                    // requested but not required to proceed.

                    SPIC.MauiBlazorApp.Platforms.Android.JmdoTrackingForegroundService.EnsureRunning(
                        global::Android.App.Application.Context, sessionId);
                    return true;
#else
                    return false;
#endif
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StartBackgroundTracking failed: {ex}");
                return false;
            }
        }

        /// <summary>Stops native background tracking - window.stopBackgroundTracking, called right
        /// after JMDOTrackingApi.StopAsync succeeds. Safe to call even if background tracking was
        /// never started (e.g. the permission was declined).</summary>
        [JSInvokable]
        public static Task<bool> StopBackgroundTracking()
        {
            try
            {
#if ANDROID
                SPIC.MauiBlazorApp.Platforms.Android.JmdoTrackingForegroundService.Stop(global::Android.App.Application.Context);
#elif IOS
                SPIC.MauiBlazorApp.Platforms.iOS.JmdoLocationManager.Stop();
#endif
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StopBackgroundTracking failed: {ex}");
                return Task.FromResult(false);
            }
        }
    }
}
