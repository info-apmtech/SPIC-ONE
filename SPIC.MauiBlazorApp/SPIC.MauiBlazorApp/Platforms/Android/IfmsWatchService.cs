using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using SPIC.MauiBlazorApp.Services;

namespace SPIC.MauiBlazorApp.Platforms.Android
{
	/// <summary>
	/// Watches for a CAPTCHA the automation could not read and raises a
	/// notification so the phone rings rather than waiting to be checked.
	///
	/// A foreground service rather than a background timer: Android stops
	/// background work aggressively, and this has to survive from 04:05 until
	/// somebody wakes up. The cost is the persistent low-priority notification
	/// Android requires, which is a fair trade for not missing the one prompt
	/// that blocks the whole night's import.
	/// </summary>
	[Service(
		Enabled = true,
		Exported = false,
		ForegroundServiceType = ForegroundService.TypeDataSync)]
	public sealed class IfmsWatchService : Service
	{
		private const string OngoingChannelId = "spic_ifms_watch";
		private const string AlertChannelId = "spic_ifms_alert";
		private const int OngoingNotificationId = 4101;
		private const int AlertNotificationId = 4102;

		private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);

		private CancellationTokenSource? _cancellation;

		/// <summary>The challenge we have already notified about, so it rings once.</summary>
		private int _notifiedChallengeId;

		public override IBinder? OnBind(Intent? intent) => null;

		public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
		{
			CreateChannels();
			StartForeground(OngoingNotificationId, BuildOngoingNotification());

			_cancellation?.Cancel();
			_cancellation = new CancellationTokenSource();

			_ = Task.Run(() => PollLoopAsync(_cancellation.Token));

			// Restart if Android kills us: missing the prompt is the failure mode
			// this whole service exists to prevent.
			return StartCommandResult.Sticky;
		}

		public override void OnDestroy()
		{
			_cancellation?.Cancel();
			_cancellation?.Dispose();
			_cancellation = null;

			base.OnDestroy();
		}

		private async Task PollLoopAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					if (IfmsRelaySettings.IsConfigured)
					{
						var challenge = await IfmsRelayClient.GetPendingChallengeAsync(cancellationToken);

						if (challenge is not null && challenge.Id != _notifiedChallengeId)
						{
							_notifiedChallengeId = challenge.Id;
							RaiseAlert(challenge);
						}
						else if (challenge is null)
						{
							_notifiedChallengeId = 0;
							CancelAlert();
						}
					}
				}
				catch
				{
					// A failed poll is not worth stopping the loop over.
				}

				try
				{
					await Task.Delay(PollInterval, cancellationToken);
				}
				catch (System.OperationCanceledException)
				{
					return;
				}
			}
		}

		private void RaiseAlert(IfmsRelayClient.PendingChallenge challenge)
		{
			var launch = new Intent(this, typeof(MainActivity));
			launch.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
			launch.PutExtra("navigate", "/IfmsAutoImport");

			var pending = PendingIntent.GetActivity(
				this,
				0,
				launch,
				PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

			var text = challenge.Round <= 1
				? "The automation could not read today's CAPTCHA. Tap to type it in."
				: "That code arrived too late. Tap for a fresh CAPTCHA.";

			var notification = new NotificationCompat.Builder(this, AlertChannelId)
				.SetContentTitle("IFMS needs the CAPTCHA")
				.SetContentText(text)
				.SetStyle(new NotificationCompat.BigTextStyle().BigText(text))
				.SetSmallIcon(global::Android.Resource.Drawable.StatSysWarning)
				.SetPriority(NotificationCompat.PriorityHigh)
				.SetCategory(NotificationCompat.CategoryReminder)
				.SetContentIntent(pending)
				.SetAutoCancel(true)
				.Build();

			NotificationManagerCompat.From(this).Notify(AlertNotificationId, notification);
		}

		private void CancelAlert() =>
			NotificationManagerCompat.From(this).Cancel(AlertNotificationId);

		private Notification BuildOngoingNotification() =>
			new NotificationCompat.Builder(this, OngoingChannelId)
				.SetContentTitle("SPIC IFMS relay")
				.SetContentText("Watching for the IFMS OTP and CAPTCHA.")
				.SetSmallIcon(global::Android.Resource.Drawable.StatNotifySync)
				.SetPriority(NotificationCompat.PriorityMin)
				.SetOngoing(true)
				.Build();

		private void CreateChannels()
		{
			if (!OperatingSystem.IsAndroidVersionAtLeast(26))
				return;

			var manager = (NotificationManager?)GetSystemService(NotificationService);
			if (manager is null)
				return;

			// Deliberately silent: this one is only there to keep the service alive.
			var ongoing = new NotificationChannel(
				OngoingChannelId,
				"IFMS relay status",
				NotificationImportance.Min);

			// This one must be able to wake somebody up.
			var alert = new NotificationChannel(
				AlertChannelId,
				"IFMS CAPTCHA prompts",
				NotificationImportance.High)
			{
				Description = "Raised when the IFMS automation needs a CAPTCHA typed in."
			};

			alert.EnableVibration(true);

			manager.CreateNotificationChannel(ongoing);
			manager.CreateNotificationChannel(alert);
		}

		/// <summary>Starts the watcher, if this phone has been paired.</summary>
		public static void EnsureRunning(Context context)
		{
			if (!IfmsRelaySettings.IsConfigured)
				return;

			var intent = new Intent(context, typeof(IfmsWatchService));

			if (OperatingSystem.IsAndroidVersionAtLeast(26))
				context.StartForegroundService(intent);
			else
				context.StartService(intent);
		}

		public static void Stop(Context context) =>
			context.StopService(new Intent(context, typeof(IfmsWatchService)));
	}
}
