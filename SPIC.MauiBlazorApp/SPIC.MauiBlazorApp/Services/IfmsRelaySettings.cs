namespace SPIC.MauiBlazorApp.Services
{
	/// <summary>
	/// The handful of values the IFMS background pieces need. They live in
	/// <see cref="Preferences"/> rather than in the Blazor session, because the SMS
	/// receiver has to work at four in the morning with the app closed and no user
	/// signed in.
	/// </summary>
	public static class IfmsRelaySettings
	{
		private const string EnabledKey = "ifms_relay_enabled";
		private const string ApiBaseKey = "ifms_api_base";
		private const string DeviceKeyKey = "ifms_device_key";
		private const string DeviceTokenKey = "ifms_device_token";
		private const string DeviceIdKey = "ifms_device_id";

		/// <summary>Matches the API address the rest of the app already uses.</summary>
		public const string DefaultApiBase = "https://spicapi.apmiot.com/";

		/// <summary>
		/// Master switch. Off until somebody deliberately pairs this phone, so a
		/// normal user's handset never starts forwarding their messages.
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
		/// The shared pairing secret, matching IfmsAutomation:DeviceKey on the
		/// server. Used once, to register this handset, and never again — so the
		/// server can rotate it without breaking phones already paired.
		/// </summary>
		public static string DeviceKey
		{
			get => Preferences.Default.Get(DeviceKeyKey, string.Empty);
			set => Preferences.Default.Set(DeviceKeyKey, value);
		}

		/// <summary>
		/// This handset's own token, issued at pairing and sent on every call
		/// afterwards. Revoking the device on the server makes it stop working
		/// immediately, which is what a shared key could never do.
		/// </summary>
		public static string DeviceToken
		{
			get => Preferences.Default.Get(DeviceTokenKey, string.Empty);
			set => Preferences.Default.Set(DeviceTokenKey, value);
		}

		/// <summary>A stable label for this handset, purely for the audit trail.</summary>
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
		/// Ready to relay. That means paired — holding a token — not merely holding
		/// the pairing key, which on its own can do nothing but register.
		/// </summary>
		public static bool IsConfigured =>
			Enabled &&
			!string.IsNullOrWhiteSpace(ApiBase) &&
			!string.IsNullOrWhiteSpace(DeviceToken);
	}
}
