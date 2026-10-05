namespace SPIC.MauiBlazorApp.Shared.Services
{
	/// <summary>
	/// Pairing and status for the IFMS OTP relay, which only a phone can do.
	///
	/// Follows the same shape as <see cref="IFormFactor"/>: the shared Blazor UI
	/// talks to this interface, the Android head implements it for real, and the
	/// web head returns <see cref="IfmsRelayStatus.Unsupported"/> so the setup card
	/// simply explains that this part runs on the phone.
	/// </summary>
	public interface IIfmsRelayHost
	{
		bool IsSupported { get; }

		IfmsRelayStatus GetStatus();

		/// <summary>
		/// Saves the pairing, asks for the SMS and notification permissions, and
		/// starts the watcher. Returns what actually happened, including the case
		/// where the user declined a permission.
		/// </summary>
		Task<IfmsRelayPairResult> PairAsync(string apiBase, string deviceKey);

		/// <summary>Stops relaying and forgets the key.</summary>
		Task UnpairAsync();
	}

	public sealed class IfmsRelayStatus
	{
		public bool Supported { get; init; }
		public bool Enabled { get; init; }
		public bool SmsPermissionGranted { get; init; }
		public bool NotificationPermissionGranted { get; init; }
		public bool WatcherRunning { get; init; }

		public string ApiBase { get; init; } = string.Empty;
		public string DeviceId { get; init; } = string.Empty;

		/// <summary>True when the key is set; the key itself is never handed back.</summary>
		public bool HasDeviceKey { get; init; }

		public static IfmsRelayStatus Unsupported => new() { Supported = false };
	}

	public sealed class IfmsRelayPairResult
	{
		public bool Success { get; init; }
		public string Message { get; init; } = string.Empty;
	}
}
