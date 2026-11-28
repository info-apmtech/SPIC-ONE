using SkiaSharp;
using SPIC.Core.FarmerPortal;

namespace SpicAPI.Services
{
    /// <summary>A leaf photo after <see cref="LeafImageProcessor.Process"/>: the re-encoded JPEG (at most 1024 px, no
    /// EXIF / GPS) and the small RGB copy the colour metrics are measured on.</summary>
    public sealed record LeafImage(byte[] Jpeg, int Width, int Height, byte[] MetricsRgb, int MetricsWidth, int MetricsHeight);

    /// <summary>
    /// Farmer portal Task 6: decodes a leaf photo with SkiaSharp (JPEG / PNG / WebP; the caller has checked the magic
    /// bytes), turns it upright from its EXIF orientation, puts transparent parts on white, shrinks it to at most 1024 px
    /// on the long side and re-encodes it as JPEG quality 80. Re-encoding writes no metadata, so EXIF (camera, GPS) is
    /// gone. Size limits and the decode gate: see <see cref="Process"/> / <see cref="ProcessAsync"/>. Never throws for a bad file.
    /// </summary>
    public static class LeafImageProcessor
    {
        /// <summary>The affine map (x' = Sx*x + Kx*y + Tx, y' = Ky*x + Sy*y + Ty) that turns an image stored with EXIF
        /// orientation <paramref name="origin"/> (1-8) upright, and the upright size.</summary>
        public static (float Sx, float Kx, float Tx, float Ky, float Sy, float Ty, int Width, int Height) OrientTransform(int origin, int w, int h) => origin switch
        {
            2 => (-1, 0, w, 0, 1, 0, w, h),        // mirrored
            3 => (-1, 0, w, 0, -1, h, w, h),       // upside down
            4 => (1, 0, 0, 0, -1, h, w, h),        // mirrored upside down
            5 => (0, 1, 0, 1, 0, 0, h, w),         // transposed
            6 => (0, -1, h, 1, 0, 0, h, w),        // turned 90 deg (phone held upright)
            7 => (0, -1, h, -1, 0, w, h, w),       // transverse
            8 => (0, 1, 0, -1, 0, w, h, w),        // turned 270 deg
            _ => (1, 0, 0, 0, 1, 0, w, h)
        };

        /// <summary>The size that fits <paramref name="max"/> on the long side, keeping the shape (never larger).</summary>
        public static (int Width, int Height) Fit(int w, int h, int max)
        {
            var side = Math.Max(w, h);
            if (side <= max) return (w, h);
            var scale = (double)max / side;
            return (Math.Max(1, (int)Math.Round(w * scale)), Math.Max(1, (int)Math.Round(h * scale)));
        }

        /// <summary>A small JPEG (at most <paramref name="max"/> px, quality 70) of a stored photo for the history list;
        /// null when it cannot be read.</summary>
        public static byte[]? Thumbnail(byte[] jpeg, int max = 240)
        {
            try
            {
                using var bmp = SKBitmap.Decode(jpeg);
                if (bmp is null) return null;
                var (w, h) = Fit(bmp.Width, bmp.Height, max);
                using var small = bmp.Resize(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
                if (small is null) return null;
                using var image = SKImage.FromBitmap(small);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 70);
                return data.ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }


        /// <summary>The answer when no decode slot frees up in 30 s (the controller turns it into 503, final fix wave #50).</summary>
        public const string BusyMessage = "Many farmers are checking leaves right now. Please try again in a minute.";

        private static readonly SemaphoreSlim Gate = new(LeafCheckRules.MaxConcurrentDecodes, LeafCheckRules.MaxConcurrentDecodes);

        /// <summary>
        /// <see cref="Process"/> behind a static gate of <see cref="LeafCheckRules.MaxConcurrentDecodes"/> decodes per replica
        /// (fix round 1, review I2), so many parallel uploads cannot each allocate a decoded photo at once. Waits at most
        /// 30 s for a slot ("busy" message after that).
        /// </summary>
        public static async Task<(LeafImage? Image, string? Error)> ProcessAsync(byte[] data, CancellationToken ct)
        {
            if (!await Gate.WaitAsync(TimeSpan.FromSeconds(30), ct))
                return (null, BusyMessage);
            try
            {
                var image = Process(data, out var error);
                return (image, error);
            }
            finally
            {
                Gate.Release();
            }
        }

        private const string Unreadable = "We could not read this photo. Please take a new photo and try again.";

        /// <summary>
        /// Decodes, turns upright, shrinks and re-encodes one photo. Refused before any pixel is decoded: under 64 px; JPEG
        /// over 60 MP; PNG / WebP over 20 MP (they cannot be decoded at a reduced scale); PNG / WebP over 4 MP with fewer than
        /// 10 KB per megapixel (a header-only or flat bomb file). Incomplete images are refused after decoding. The decoded
        /// image (JPEG at a reduced scale near 2048 px) is drawn straight into the 1024 px result, turned upright on the way,
        /// so no second full-size copy is made.
        /// </summary>
        public static LeafImage? Process(byte[] data, out string? error)
        {
            error = null;
            try
            {
                using var stream = new SKMemoryStream(data);
                using var codec = SKCodec.Create(stream, out var created);
                if (codec is null || created != SKCodecResult.Success)
                {
                    error = Unreadable;
                    return null;
                }

                var info = codec.Info;
                var pixels = (long)info.Width * info.Height;
                var jpeg = codec.EncodedFormat == SKEncodedImageFormat.Jpeg;
                if (pixels > (jpeg ? LeafCheckRules.MaxPixels : LeafCheckRules.MaxPixelsOther))
                {
                    error = "This photo is too large. Please take a new photo with your phone camera.";
                    return null;
                }
                if (Math.Min(info.Width, info.Height) < LeafCheckRules.MinSide)
                {
                    error = "This photo is too small. Please take a closer photo of the leaf.";
                    return null;
                }
                if (!jpeg && pixels > LeafCheckRules.SparseCheckFromPixels
                    && data.LongLength < pixels / 1_000_000.0 * LeafCheckRules.MinBytesPerMegapixelOther)
                {
                    error = Unreadable;
                    return null;
                }

                // Big JPEGs decode faster and smaller at a reduced scale (1/2 .. 1/8); keep about 2048 px on the long side.
                var desired = Math.Min(1f, 2048f / Math.Max(info.Width, info.Height));
                var size = desired < 1f ? codec.GetScaledDimensions(desired) : new SKSizeI(info.Width, info.Height);
                if (size.Width < LeafCheckRules.MinSide || size.Height < LeafCheckRules.MinSide) size = new SKSizeI(info.Width, info.Height);

                var decodeInfo = new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var decoded = new SKBitmap(decodeInfo);
                if (codec.GetPixels(decodeInfo, decoded.GetPixels()) != SKCodecResult.Success)
                {
                    error = Unreadable;
                    return null;
                }

                // Upright and shrunk in one draw, on white (a PNG's transparent parts become plain background).
                var t = OrientTransform((int)codec.EncodedOrigin, size.Width, size.Height);
                var (sw, sh) = Fit(t.Width, t.Height, LeafCheckRules.MaxStoredSide);
                var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
                using var stored = new SKBitmap(new SKImageInfo(sw, sh, SKColorType.Rgba8888, SKAlphaType.Premul));
                using (var canvas = new SKCanvas(stored))
                using (var source = SKImage.FromBitmap(decoded))
                {
                    canvas.Clear(SKColors.White);
                    var orient = new SKMatrix(t.Sx, t.Kx, t.Tx, t.Ky, t.Sy, t.Ty, 0, 0, 1);
                    canvas.SetMatrix(orient.PostConcat(SKMatrix.CreateScale((float)sw / t.Width, (float)sh / t.Height)));
                    canvas.DrawImage(source, 0, 0, sampling);
                }

                byte[] jpegBytes;
                using (var image = SKImage.FromBitmap(stored))
                using (var encoded = image.Encode(SKEncodedImageFormat.Jpeg, LeafCheckRules.JpegQuality))
                    jpegBytes = encoded.ToArray();

                var (mw, mh) = Fit(sw, sh, LeafCheckRules.MetricsSide);
                using var small = stored.Resize(new SKImageInfo(mw, mh, SKColorType.Rgba8888, SKAlphaType.Premul), sampling) ?? stored.Copy();
                var rgba = small.Bytes;
                var rgb = new byte[small.Width * small.Height * 3];
                for (int i = 0, j = 0; i + 3 < rgba.Length && j < rgb.Length; i += 4, j += 3)
                {
                    rgb[j] = rgba[i];
                    rgb[j + 1] = rgba[i + 1];
                    rgb[j + 2] = rgba[i + 2];
                }
                return new LeafImage(jpegBytes, sw, sh, rgb, small.Width, small.Height);
            }
            catch (Exception)
            {
                error = Unreadable;
                return null;
            }
        }
    }
}
