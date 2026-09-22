using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using SPIC.Ifms.Relay.Platforms.Android;
using SPIC.Ifms.Relay.Services;

namespace SPIC.Ifms.Relay
{
	/// <summary>
	/// The single activity. SingleTop so that tapping the CAPTCHA notification
	/// while the app is already open re-enters through OnNewIntent instead of
	/// stacking a second copy of the page.
	/// </summary>
	[Activity(
		Theme = "@style/Maui.SplashTheme",
		MainLauncher = true,
		Exported = true,
		LaunchMode = LaunchMode.SingleTop,
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
							   ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
	public class MainActivity : MauiAppCompatActivity
	{
		protected override void OnCreate(Bundle? savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Bring the watcher back up whenever the app is opened. Cheap when it
			// is already running, and it covers the case where HyperOS stopped
			// the service while the phone was idle.
			RelayForegroundService.EnsureRunning(this);
			HandleIntent(Intent);
		}

		protected override void OnNewIntent(Intent? intent)
		{
			base.OnNewIntent(intent);
			HandleIntent(intent);
		}

		private static void HandleIntent(Intent? intent)
		{
			try
			{
				if (intent?.GetStringExtra(RelayForegroundService.ShowExtra) != RelayForegroundService.ShowCaptcha)
					return;

				// Consumed by the Home page when it next appears, and raised as an
				// event for the case where it is already on screen.
				RelayEvents.RequestShowCaptcha();
				RelayEvents.RaiseChallengeChanged();

				if (Shell.Current is not null)
					MainThread.BeginInvokeOnMainThread(async () => await Shell.Current.GoToAsync("//home"));
			}
			catch
			{
				// The app must still open even if the shortcut to the CAPTCHA fails.
			}
		}
	}
}
