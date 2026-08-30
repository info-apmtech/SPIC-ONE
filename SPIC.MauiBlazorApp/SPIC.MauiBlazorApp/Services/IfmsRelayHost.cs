using SPIC.MauiBlazorApp.Shared.Services;

#if ANDROID
using SPIC.MauiBlazorApp.Platforms.Android;
#endif

namespace SPIC.MauiBlazorApp.Services
{
	/// <summary>
	/// The device half of the IFMS relay: pairing, permissions and the watcher.
	/// Everything meaningful here is Android-only; on other heads it reports
	/// unsupported so the shared UI can say so instead of failing.
	/// </summary>
	public sealed class IfmsRelayHost : IIfmsRelayHost
	{
#if ANDROID
		public bool IsSupported => true;
#else
		public bool IsSupported => false;
#endif

		public IfmsRelayStatus GetStatus()
		{
#if ANDROID
			return new IfmsRelayStatus
			{
				Supported = true,
				Enabled = IfmsRelaySettings.Enabled,
				ApiBase = IfmsRelaySettings.ApiBase,
				DeviceId = IfmsRelaySettings.DeviceId,
				HasDeviceKey = !string.IsNullOrWhiteSpace(IfmsRelaySettings.DeviceKey),
				SmsPermissionGranted =
					Permissions.CheckStatusAsync<IfmsSmsPermission>().GetAwaiter().GetResult()
						== PermissionStatus.Granted,
				NotificationPermissionGranted = true,
				WatcherRunning = IfmsRelaySettings.IsConfigured
			};
#else
			return IfmsRelayStatus.Unsupported;
#endif
		}

		public async Task<IfmsRelayPairResult> PairAsync(string apiBase, string deviceKey)
		{
#if ANDROID
			if (string.IsNullOrWhiteSpace(deviceKey))
				return Fail("Enter the device key from the server's IfmsAutomation:DeviceKey setting.");

			if (string.IsNullOrWhiteSpace(apiBase) ||
				!Uri.TryCreate(apiBase, UriKind.Absolute, out _))
			{
				return Fail("That API address is not a valid URL.");
			}

			var sms = await Permissions.RequestAsync<IfmsSmsPermission>();

			if (sms != PermissionStatus.Granted)
			{
				return Fail(
					"Android did not grant permission to read SMS, so the OTP cannot be " +
					"forwarded. Allow it under Settings, Apps, SPIC, Permissions, SMS.");
			}

			// Android 13+ silently drops notifications without this, which would
			// hide the CAPTCHA prompt entirely.
			if (OperatingSystem.IsAndroidVersionAtLeast(33))
				await Permissions.RequestAsync<Permissions.PostNotifications>();

			IfmsRelaySettings.ApiBase = apiBase.Trim();
			IfmsRelaySettings.DeviceKey = deviceKey.Trim();
			IfmsRelaySettings.Enabled = true;

			var context = global::Android.App.Application.Context;
			IfmsWatchService.EnsureRunning(context);

			return new IfmsRelayPairResult
			{
				Success = true,
				Message = $"Paired as {IfmsRelaySettings.DeviceId}. " +
						  "This phone will now forward the IFMS OTP and alert you if a CAPTCHA needs typing."
			};
#else
			await Task.CompletedTask;
			return Fail("The OTP relay runs on the Android app, not here.");
#endif
		}

		public async Task UnpairAsync()
		{
#if ANDROID
			IfmsRelaySettings.Enabled = false;
			IfmsRelaySettings.DeviceKey = string.Empty;

			IfmsWatchService.Stop(global::Android.App.Application.Context);
#endif
			await Task.CompletedTask;
		}

		private static IfmsRelayPairResult Fail(string message) =>
			new() { Success = false, Message = message };
	}

#if ANDROID
	/// <summary>
	/// MAUI has no built-in SMS-read permission, so this declares the two Android
	/// permissions the relay needs and lets the standard Permissions API request
	/// them together.
	/// </summary>
	public sealed class IfmsSmsPermission : Permissions.BasePlatformPermission
	{
		public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
			new[]
			{
				(global::Android.Manifest.Permission.ReceiveSms, true),
				(global::Android.Manifest.Permission.ReadSms, true)
			};
	}
#endif
}
