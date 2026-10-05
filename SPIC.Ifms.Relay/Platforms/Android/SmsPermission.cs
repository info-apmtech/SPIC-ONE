namespace SPIC.Ifms.Relay.Platforms.Android
{
	/// <summary>
	/// MAUI has no built-in SMS-read permission, so this declares the two Android
	/// permissions the relay needs and lets the standard Permissions API request
	/// them together.
	/// </summary>
	public sealed class SmsPermission : Permissions.BasePlatformPermission
	{
		public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
			new[]
			{
				(global::Android.Manifest.Permission.ReceiveSms, true),
				(global::Android.Manifest.Permission.ReadSms, true)
			};
	}
}
