namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// A host camera for "Take a photo" (recheck fix U2). Android's BlazorWebView sends every
/// <c>&lt;input type=file&gt;</c> to the document picker and ignores <c>capture</c>, so the leaf check could not open the
/// camera in the Play Store app. The MAUI host registers a MediaPicker-based camera that is available on Android
/// only; the web host and iOS (WKWebView honours <c>capture</c>) register <see cref="NoNativeCamera"/> and keep
/// the <c>InputFile capture="environment"</c> path unchanged.
/// </summary>
public interface INativeCamera
{
    /// <summary>True when this host should open its own camera instead of the file input's camera.</summary>
    bool IsAvailable { get; }

    /// <summary>Opens the camera. Null when the farmer cancelled; otherwise the photo, or an <see cref="NativePhoto.Error"/>
    /// in plain words (permission refused, no camera, unreadable photo). Never throws.</summary>
    Task<NativePhoto?> CaptureAsync(CancellationToken ct = default);
}

/// <summary>A photo from <see cref="INativeCamera"/>: the original bytes (the server turns it upright and re-encodes it).</summary>
public sealed record NativePhoto(byte[]? Bytes, string ContentType, string FileName, string? Error)
{
    public static NativePhoto Failed(string error) => new(null, "image/jpeg", "", error);
}

/// <summary>The web host and every platform without a native camera path: the file input does the work.</summary>
public sealed class NoNativeCamera : INativeCamera
{
    public bool IsAvailable => false;

    public Task<NativePhoto?> CaptureAsync(CancellationToken ct = default) => Task.FromResult<NativePhoto?>(null);
}
