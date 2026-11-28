using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.MauiBlazorApp.Services
{
    /// <summary>
    /// Recheck fix U2: the leaf check's "Take a photo" on Android. Android's BlazorWebView file chooser ignores
    /// <c>capture</c> and opens the document picker, so on Android the page asks this service instead, which opens
    /// the camera with <c>MediaPicker.CapturePhotoAsync</c> (CAMERA permission and the IMAGE_CAPTURE query are in
    /// Platforms/Android/AndroidManifest.xml). iOS, Mac and Windows report <see cref="IsAvailable"/> false and keep the
    /// file input (WKWebView honours <c>capture</c>). The photo goes up as taken: the server turns it upright from
    /// EXIF, shrinks and re-encodes it (no EXIF or GPS is stored).
    /// </summary>
    public sealed class MauiCamera : INativeCamera
    {
        /// <summary>Largest photo read into memory (the server's own cap is 8 MB; a bigger photo gets a plain error).</summary>
        private const long MaxBytes = 12L * 1024 * 1024;

        public bool IsAvailable
        {
            get
            {
#if ANDROID
                try { return MediaPicker.Default.IsCaptureSupported; }
                catch { return false; }
#else
                return false;
#endif
            }
        }

        public async Task<NativePhoto?> CaptureAsync(CancellationToken ct = default)
        {
#if ANDROID
            try
            {
                var granted = await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                    if (status != PermissionStatus.Granted) status = await Permissions.RequestAsync<Permissions.Camera>();
                    return status == PermissionStatus.Granted;
                });
                if (!granted)
                    return NativePhoto.Failed("The camera permission was not given. Allow the camera for SPIC ONE in your phone settings, or choose a photo from the gallery.");

                var file = await MainThread.InvokeOnMainThreadAsync(() => MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions { Title = "Leaf photo" }));
                if (file is null) return null; // cancelled

                await using var stream = await file.OpenReadAsync();
                using var ms = new MemoryStream();
                var buffer = new byte[81920];
                int read;
                while ((read = await stream.ReadAsync(buffer, ct)) > 0)
                {
                    if (ms.Length + read > MaxBytes)
                        return NativePhoto.Failed("This photo is too large. Please take it again, or choose a smaller one from the gallery.");
                    ms.Write(buffer, 0, read);
                }
                var type = string.IsNullOrWhiteSpace(file.ContentType) ? "image/jpeg" : file.ContentType;
                return new NativePhoto(ms.ToArray(), type, "Camera photo", null);
            }
            catch (FeatureNotSupportedException)
            {
                return NativePhoto.Failed("This phone has no camera we can open. Please choose a photo from the gallery.");
            }
            catch (PermissionException)
            {
                return NativePhoto.Failed("The camera permission was not given. Allow the camera for SPIC ONE in your phone settings, or choose a photo from the gallery.");
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CapturePhoto failed: {ex.GetType().Name}");
                return NativePhoto.Failed("We could not open the camera. Please try again, or choose a photo from the gallery.");
            }
#else
            await Task.CompletedTask;
            return null;
#endif
        }
    }
}
