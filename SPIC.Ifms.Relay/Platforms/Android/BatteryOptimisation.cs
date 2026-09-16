using Android.Content;
using Android.OS;
using Android.Provider;
using SPIC.Ifms.Relay.Services;
using AndroidApp = Android.App.Application;
using AndroidUri = Android.Net.Uri;

namespace SPIC.Ifms.Relay.Platforms.Android
{
	/// <summary>
	/// The battery-optimisation escape hatches. Doze would otherwise stop the
	/// watcher's network calls for most of the night, and HyperOS adds its own
	/// killer on top that only its Settings app can switch off.
	/// </summary>
	public static class BatteryOptimisation
	{
		public static bool IsIgnored()
		{
			try
			{
				var context = AndroidApp.Context;
				var power = (PowerManager?)context.GetSystemService(Context.PowerService);

				return power?.IsIgnoringBatteryOptimizations(context.PackageName ?? string.Empty) == true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// The system dialog that asks the person to exempt this app. Returns the
		/// sentence to show; the dialog itself is Android's.
		/// </summary>
		public static string RequestIgnore()
		{
			if (IsIgnored())
				return "Battery optimisation is already switched off for this app.";

			try
			{
				var context = AndroidApp.Context;

				var intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations)
					.SetData(AndroidUri.Parse($"package:{context.PackageName}"))
					.AddFlags(ActivityFlags.NewTask);

				context.StartActivity(intent);
				return "Choose Allow in the dialog Android just opened.";
			}
			catch (Exception ex)
			{
				RelayLog.Warn($"Could not open the battery dialog: {ex.Message}");
				return "Android would not open the dialog. Use 'Open battery settings' instead.";
			}
		}

		/// <summary>
		/// The app's own Settings page: the fallback on HyperOS, whose "Battery
		/// saver" and "Autostart" switches live there and nowhere an intent can
		/// reach directly.
		/// </summary>
		public static string OpenAppSettings()
		{
			try
			{
				var context = AndroidApp.Context;

				var intent = new Intent(Settings.ActionApplicationDetailsSettings)
					.SetData(AndroidUri.Parse($"package:{context.PackageName}"))
					.AddFlags(ActivityFlags.NewTask);

				context.StartActivity(intent);
				return "Look for Battery saver (set No restrictions) and Autostart (on).";
			}
			catch (Exception ex)
			{
				RelayLog.Warn($"Could not open app settings: {ex.Message}");
				return "Android would not open the settings page.";
			}
		}
	}
}
