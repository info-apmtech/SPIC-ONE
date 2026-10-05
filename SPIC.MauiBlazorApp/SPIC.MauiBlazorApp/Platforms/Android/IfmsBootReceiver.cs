using Android.App;
using Android.Content;

namespace SPIC.MauiBlazorApp.Platforms.Android
{
	/// <summary>
	/// Restarts the CAPTCHA watcher after a reboot. Without this, a phone that
	/// restarted overnight would silently stop watching, and the first anyone
	/// would know is a missing morning import.
	/// </summary>
	[BroadcastReceiver(Enabled = true, Exported = true, DirectBootAware = false)]
	[IntentFilter(new[]
	{
		Intent.ActionBootCompleted,
		"android.intent.action.QUICKBOOT_POWERON"
	})]
	public sealed class IfmsBootReceiver : BroadcastReceiver
	{
		public override void OnReceive(Context? context, Intent? intent)
		{
			if (context is null)
				return;

			if (intent?.Action is not (Intent.ActionBootCompleted or "android.intent.action.QUICKBOOT_POWERON"))
				return;

			try
			{
				IfmsWatchService.EnsureRunning(context);
			}
			catch
			{
				// A phone that refuses to start the service on boot still works
				// once the app is opened; never crash the boot broadcast over it.
			}
		}
	}
}
