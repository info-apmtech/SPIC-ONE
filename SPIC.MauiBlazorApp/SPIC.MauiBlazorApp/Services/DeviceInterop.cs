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
    /// DotNet.invokeMethodAsync('SPIC.MauiBlazorApp', '<MethodName>', ...).
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
    }
}
