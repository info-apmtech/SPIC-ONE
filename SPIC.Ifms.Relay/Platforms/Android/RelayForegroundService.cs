using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using SPIC.Ifms.Relay.Services;

namespace SPIC.Ifms.Relay.Platforms.Android
{
	/// <summary>
	/// Keeps the phone in touch with the server and rings when a CAPTCHA needs a
	/// person.
	///
	/// A foreground service rather than a timer: Android stops background work
	/// aggressively, HyperOS more so, and this has to survive from 04:05 until
	/// somebody wakes up. The cost is the persistent low-priority notification
	/// Android requires, which is a fair trade for not missing the one prompt
	/// that blocks the whole night's import.
	///
	/// The type is <c>specialUse</c>, not <c>dataSync</c>, deliberately. Android
	/// 15 refuses to start a dataSync service from BOOT_COMPLETED and stops one
	/// after six hours in any 24, which is exactly the window this app must
	/// cover. Neither restriction applies to specialUse. Google Play would want a
	/// justification for that type; this app is sideloaded, so it needs none.
	/// </summary>
	[Service(
		Enabled = true,
		Exported = false,
		ForegroundServiceType = ForegroundService.TypeSpecialUse)]
	public sealed class RelayForegroundService : Service
	{
		private const string OngoingChannelId = "spic_ifms_relay_status";
		private const string AlertChannelId = "spic_ifms_relay_alert";
		private const int OngoingNotificationId = 4201;
		private const int AlertNotificationId = 4202;

		/// <summary>What MainActivity looks for to jump straight to the CAPTCHA.</summary>
		public const string ShowExtra = "show";
		public const string ShowCaptcha = "captcha";

		private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);

		/// <summary>Three long buzzes: unmistakable through a bedside table.</summary>
		private static readonly long[] VibrationPattern = { 0, 600, 300, 600, 300, 600 };

		/// <summary>Consecutive failed polls before the status notification says "unreachable".</summary>
		private const int UnreachableAfterFailures = 3;

		private CancellationTokenSource? _cancellation;

		/// <summary>The challenge already notified about, so the phone rings once per CAPTCHA.</summary>
		private int _notifiedChallengeId;

		private int _consecutiveFailures;
		private DateTime? _unreachableSinceUtc;
		private string _ongoingText = string.Empty;

		/// <summary>True between OnStartCommand and OnDestroy; the Home page shows it.</summary>
		public static bool IsRunning { get; private set; }

		public override IBinder? OnBind(Intent? intent) => null;

		public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
		{
			try
			{
				CreateChannels();
				StartInForeground(BuildOngoingNotification(DescribeWatching()));

				_cancellation?.Cancel();
				_cancellation = new CancellationTokenSource();
				IsRunning = true;

				_ = Task.Run(() => PollLoopAsync(_cancellation.Token));
			}
			catch (Exception ex)
			{
				RelayLog.Fail($"Watcher could not start: {ex.Message}");
			}

			// Restart if Android kills us: missing the prompt is the failure mode
			// this whole service exists to prevent.
			return StartCommandResult.Sticky;
		}

		public override void OnDestroy()
		{
			IsRunning = false;

			_cancellation?.Cancel();
			_cancellation?.Dispose();
			_cancellation = null;

			base.OnDestroy();
		}

		/// <summary>
		/// Android 14 is where specialUse exists and where the type must be named
		/// at start; older releases take the type from the manifest.
		/// </summary>
		private void StartInForeground(Notification notification)
		{
			if (OperatingSystem.IsAndroidVersionAtLeast(34))
				StartForeground(OngoingNotificationId, notification, ForegroundService.TypeSpecialUse);
			else
				StartForeground(OngoingNotificationId, notification);
		}

		/// <summary>The compat manager, which the binding annotates as nullable but never is.</summary>
		private static NotificationManagerCompat Notifications(Context context) =>
			NotificationManagerCompat.From(context)
			?? throw new InvalidOperationException("NotificationManagerCompat is unavailable.");

		private async Task PollLoopAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					if (RelaySettings.IsConfigured)
						await PollOnceAsync(cancellationToken);
				}
				catch (System.OperationCanceledException)
				{
					return;
				}
				catch (Exception ex)
				{
					// A failed poll is not worth stopping the loop over.
					RelayLog.Fail($"Poll failed: {ex.Message}");
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

		/// <summary>
		/// One round trip. Doubles as the heartbeat: every poll updates LastSeenAt
		/// on the server, so silence genuinely means the phone is gone.
		/// </summary>
		private async Task PollOnceAsync(CancellationToken cancellationToken)
		{
			var result = await RelayClient.GetPendingChallengeAsync(cancellationToken);

			if (!result.Success)
			{
				_consecutiveFailures++;

				if (_consecutiveFailures == UnreachableAfterFailures)
				{
					_unreachableSinceUtc = DateTime.UtcNow;
					RelayLog.Warn($"Server unreachable: {result.Message}");
				}

				UpdateOngoing();
				return;
			}

			if (_consecutiveFailures >= UnreachableAfterFailures)
				RelayLog.Ok("Server reachable again");

			_consecutiveFailures = 0;
			_unreachableSinceUtc = null;

			var challenge = result.Value;

			if (challenge is not null && challenge.Id != _notifiedChallengeId)
			{
				_notifiedChallengeId = challenge.Id;
				RelayLog.Warn($"CAPTCHA prompt shown (round {challenge.Round})");
				RaiseAlert(challenge);
				RelayEvents.RaiseChallengeChanged();
			}
			else if (challenge is null && _notifiedChallengeId != 0)
			{
				_notifiedChallengeId = 0;
				CancelAlert();
				RelayEvents.RaiseChallengeChanged();
			}

			UpdateOngoing();
		}

		private void RaiseAlert(PendingChallenge challenge)
		{
			var launch = new Intent(this, typeof(MainActivity));
			launch.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
			launch.PutExtra(ShowExtra, ShowCaptcha);

			var pending = PendingIntent.GetActivity(
				this,
				0,
				launch,
				PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

			var text = challenge.Round <= 1
				? "The automation could not read today's CAPTCHA. Tap to type it in."
				: "That code arrived too late. Tap for a fresh CAPTCHA.";

			var builder = new NotificationCompat.Builder(this, AlertChannelId);
			builder.SetContentTitle("IFMS needs the CAPTCHA");
			builder.SetContentText(text);
			builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(text));
			builder.SetSmallIcon(global::Android.Resource.Drawable.StatSysWarning);
			builder.SetPriority(NotificationCompat.PriorityHigh);
			builder.SetCategory(NotificationCompat.CategoryReminder);
			builder.SetVibrate(VibrationPattern);
			builder.SetContentIntent(pending);
			builder.SetAutoCancel(true);

			Notifications(this).Notify(AlertNotificationId, builder.Build());
		}

		private void CancelAlert() =>
			Notifications(this).Cancel(AlertNotificationId);

		/// <summary>Re-posts the ongoing notification only when its text has changed.</summary>
		private void UpdateOngoing()
		{
			var text = _unreachableSinceUtc is { } since
				? $"Server unreachable since {since.ToLocalTime():HH:mm}"
				: DescribeWatching();

			if (text == _ongoingText)
				return;

			_ongoingText = text;
			Notifications(this).Notify(OngoingNotificationId, BuildOngoingNotification(text));
		}

		private static string DescribeWatching()
		{
			var last = RelaySettings.LastContactUtc;

			return last is null
				? "Watching for the IFMS OTP"
				: $"Watching for the IFMS OTP · last contact {last.Value.ToLocalTime():HH:mm}";
		}

		private Notification BuildOngoingNotification(string text)
		{
			_ongoingText = text;

			var open = PendingIntent.GetActivity(
				this,
				1,
				new Intent(this, typeof(MainActivity)).SetFlags(ActivityFlags.SingleTop),
				PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

			var builder = new NotificationCompat.Builder(this, OngoingChannelId);
			builder.SetContentTitle("SPIC IFMS Relay");
			builder.SetContentText(text);
			builder.SetSmallIcon(global::Android.Resource.Drawable.StatNotifySync);
			builder.SetPriority(NotificationCompat.PriorityMin);
			builder.SetContentIntent(open);
			builder.SetOngoing(true);
			builder.SetSilent(true);

			return builder.Build()
				?? throw new InvalidOperationException("NotificationCompat.Builder returned no notification.");
		}

		private void CreateChannels()
		{
			var manager = (NotificationManager?)GetSystemService(NotificationService);
			if (manager is null)
				return;

			// Deliberately silent: this one is only there to keep the service alive.
			var ongoing = new NotificationChannel(
				OngoingChannelId,
				"Relay status",
				NotificationImportance.Min)
			{
				Description = "Always on while the phone is paired. Shows the last contact with the server."
			};

			// This one must be able to wake somebody up.
			var alert = new NotificationChannel(
				AlertChannelId,
				"IFMS CAPTCHA prompts",
				NotificationImportance.High)
			{
				Description = "Raised when the IFMS automation needs a CAPTCHA typed in."
			};

			alert.EnableVibration(true);
			alert.SetVibrationPattern(VibrationPattern);

			manager.CreateNotificationChannel(ongoing);
			manager.CreateNotificationChannel(alert);
		}

		/// <summary>
		/// Starts the watcher if this phone has been paired. Safe to call often:
		/// starting an already-running service just re-enters OnStartCommand.
		/// </summary>
		public static void EnsureRunning(Context context)
		{
			if (!RelaySettings.IsConfigured)
				return;

			try
			{
				context.StartForegroundService(new Intent(context, typeof(RelayForegroundService)));
			}
			catch (Exception ex)
			{
				// Android 12+ refuses a foreground start from some background
				// contexts. The next time the app is opened it will try again.
				RelayLog.Fail($"Watcher could not be started: {ex.Message}");
			}
		}

		public static void Stop(Context context)
		{
			try
			{
				context.StopService(new Intent(context, typeof(RelayForegroundService)));
				Notifications(context).Cancel(AlertNotificationId);
			}
			catch (Exception ex)
			{
				RelayLog.Fail($"Watcher could not be stopped: {ex.Message}");
			}
		}
	}
}
