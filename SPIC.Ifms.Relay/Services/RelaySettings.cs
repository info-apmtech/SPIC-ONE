namespace SPIC.Ifms.Relay.Services
{
	/// <summary>
	/// Everything the relay needs to remember between launches. It all lives in
	/// <see cref="Preferences"/> because the SMS receiver and the watcher run at
	/// four in the morning with no page open and nobody signed in — there is no
	/// session to hold state in.
	///
	/// The pairing key is deliberately absent. It is typed once, sent once to
	/// register, and forgotten: only the per-device token it buys is kept.
	/// </summary>
	public static class RelaySettings
	{
		private const string EnabledKey = "relay_enabled";
		private const string ApiBaseKey = "relay_api_base";
		private const string DeviceTokenKey = "relay_device_token";
		private const string DeviceIdKey = "relay_device_id";
		private const string ForwardOnlyIfmsKey = "relay_forward_only_ifms";
		private const string AcceptedSendersKey = "relay_accepted_senders";
		private const string LastContactKey = "relay_last_contact_utc";
		private const string LastSmsRelayedKey = "relay_last_sms_relayed_utc";

		/// <summary>The production API, the same one SPIC ONE talks to.</summary>
		public const string DefaultApiBase = "https://spicapi.apmiot.com/";

		/// <summary>The IFMS portal's OTP sender, as seen on the SIM so far.</summary>
		public const string DefaultAcceptedSenders = "7305430555";

		/// <summary>
		/// Master switch. Off until somebody deliberately pairs this phone, so
		/// merely installing the app never forwards anything.
		/// </summary>
		public static bool Enabled
		{
			get => Preferences.Default.Get(EnabledKey, false);
			set => Preferences.Default.Set(EnabledKey, value);
		}

		public static string ApiBase
		{
			get => Preferences.Default.Get(ApiBaseKey, DefaultApiBase);
			set => Preferences.Default.Set(ApiBaseKey, value);
		}

		/// <summary>
		/// This handset's own token, issued at pairing and sent on every call
		/// afterwards. Revoking the device on the server stops it immediately,
		/// which a shared key never could.
		/// </summary>
		public static string DeviceToken
		{
			get => Preferences.Default.Get(DeviceTokenKey, string.Empty);
			set => Preferences.Default.Set(DeviceTokenKey, value);
		}

		/// <summary>
		/// A stable label for this handset in the server's audit trail. Generated
		/// once and kept, so re-pairing rotates the token rather than creating a
		/// second device row.
		/// </summary>
		public static string DeviceId
		{
			get
			{
				var existing = Preferences.Default.Get(DeviceIdKey, string.Empty);

				if (!string.IsNullOrEmpty(existing))
					return existing;

				var generated = $"{DeviceInfo.Current.Manufacturer}-{DeviceInfo.Current.Model}-" +
								$"{Guid.NewGuid().ToString("N")[..6]}";

				Preferences.Default.Set(DeviceIdKey, generated);
				return generated;
			}
		}

		/// <summary>
		/// When true (the default) only messages that look like IFMS traffic leave
		/// the phone: the body mentions IFMS, or the sender is one of
		/// <see cref="AcceptedSenders"/>. Everything else is dropped on the phone.
		/// </summary>
		public static bool ForwardOnlyIfms
		{
			get => Preferences.Default.Get(ForwardOnlyIfmsKey, true);
			set => Preferences.Default.Set(ForwardOnlyIfmsKey, value);
		}

		/// <summary>Comma or space separated sender numbers that are always forwarded.</summary>
		public static string AcceptedSenders
		{
			get => Preferences.Default.Get(AcceptedSendersKey, DefaultAcceptedSenders);
			set => Preferences.Default.Set(AcceptedSendersKey, value);
		}

		/// <summary>The last time any call to the server succeeded. Null if never.</summary>
		public static DateTime? LastContactUtc
		{
			get => ReadUtc(LastContactKey);
			set => WriteUtc(LastContactKey, value);
		}

		/// <summary>The last time an SMS was accepted by the server. Null if never.</summary>
		public static DateTime? LastSmsRelayedUtc
		{
			get => ReadUtc(LastSmsRelayedKey);
			set => WriteUtc(LastSmsRelayedKey, value);
		}

		/// <summary>
		/// Ready to relay: switched on and holding a token. Holding an API address
		/// alone means nothing — only pairing earns the right to talk to the server.
		/// </summary>
		public static bool IsConfigured =>
			Enabled &&
			!string.IsNullOrWhiteSpace(ApiBase) &&
			!string.IsNullOrWhiteSpace(DeviceToken);

		/// <summary>
		/// The accepted senders reduced to digits, so "+91 73054 30555" and
		/// "7305430555" compare equal. Empty entries are dropped.
		/// </summary>
		public static IReadOnlyList<string> AcceptedSenderDigits()
		{
			return AcceptedSenders
				.Split(new[] { ',', ';', ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(NormaliseNumber)
				.Where(s => s.Length > 0)
				.Distinct()
				.ToList();
		}

		/// <summary>
		/// Digits only, with a leading country code 91 dropped so the comparison
		/// is on the ten-digit subscriber number either side may or may not carry.
		/// </summary>
		public static string NormaliseNumber(string value)
		{
			var digits = new string(value.Where(char.IsDigit).ToArray());

			if (digits.Length > 10 && digits.StartsWith("91", StringComparison.Ordinal))
				digits = digits[2..];

			return digits;
		}

		private static DateTime? ReadUtc(string key)
		{
			var ticks = Preferences.Default.Get(key, 0L);
			return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
		}

		private static void WriteUtc(string key, DateTime? value)
		{
			if (value is null)
				Preferences.Default.Remove(key);
			else
				Preferences.Default.Set(key, value.Value.ToUniversalTime().Ticks);
		}
	}
}
