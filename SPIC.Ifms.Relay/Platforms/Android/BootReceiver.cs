using Android.App;
using Android.Content;
using SPIC.Ifms.Relay.Services;

namespace SPIC.Ifms.Relay.Platforms.Android
{
	/// <summary>
	/// Restarts the watcher after a reboot, and after this app is updated.
	/// Without the first, a phone that restarted overnight would silently stop
	/// watching; without the second, every sideloaded update killed the service
	/// and it stayed dead until somebody opened the app — two nights of missed
	/// OTPs in September came from exactly that.
	/// </summary>
	[BroadcastReceiver(Enabled = true, Exported = true, DirectBootAware = false)]
	[IntentFilter(new[]
	{
		Intent.ActionBootCompleted,
		"android.intent.action.QUICKBOOT_POWERON"
	})]
	[IntentFilter(new[] { Intent.ActionMyPackageReplaced })]
	public sealed class BootReceiver : BroadcastReceiver
	{
		public override void OnReceive(Context? context, Intent? intent)
		{
			if (context is null)
				return;

			var action = intent?.Action;

			if (action is not (Intent.ActionBootCompleted
				or "android.intent.action.QUICKBOOT_POWERON"
				or Intent.ActionMyPackageReplaced))
			{
				return;
			}

			try
			{
				RelayLog.Info(action == Intent.ActionMyPackageReplaced
					? "App updated; starting the watcher"
					: "Phone restarted; starting the watcher");
				RelayForegroundService.EnsureRunning(context);
			}
			catch
			{
				// A phone that refuses to start the service on boot still works
				// once the app is opened; never crash the boot broadcast over it.
			}
		}
	}
}
