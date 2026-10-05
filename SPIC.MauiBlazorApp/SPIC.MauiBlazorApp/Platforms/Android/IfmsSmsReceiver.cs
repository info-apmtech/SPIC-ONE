using Android.App;
using Android.Content;
using Android.Provider;
using Android.Telephony;
using SPIC.MauiBlazorApp.Services;

namespace SPIC.MauiBlazorApp.Platforms.Android
{
	/// <summary>
	/// Forwards incoming SMS to SpicAPI so the IFMS login can read its one-time
	/// password without waking anybody.
	///
	/// Registered in the manifest rather than in code, which is what lets Android
	/// start it while the app is closed — the whole point, given the run fires at
	/// 04:05. It stays inert until the phone is deliberately paired on the IFMS
	/// Relay screen, so an ordinary user's handset never forwards anything.
	/// </summary>
	[BroadcastReceiver(
		Enabled = true,
		Exported = true,
		Permission = global::Android.Manifest.Permission.BroadcastSms,
		Label = "SPIC IFMS OTP relay")]
	[IntentFilter(
		new[] { Telephony.Sms.Intents.SmsReceivedAction },
		Priority = (int)IntentFilterPriority.HighPriority)]
	public sealed class IfmsSmsReceiver : BroadcastReceiver
	{
		public override void OnReceive(Context? context, Intent? intent)
		{
			if (!IfmsRelaySettings.IsConfigured)
				return;

			if (intent?.Action != Telephony.Sms.Intents.SmsReceivedAction)
				return;

			var messages = Telephony.Sms.Intents.GetMessagesFromIntent(intent);
			if (messages is null || messages.Length == 0)
				return;

			// A long SMS arrives as several parts that must be stitched back
			// together, otherwise a code split across the boundary is lost.
			var sender = messages[0]?.OriginatingAddress ?? string.Empty;
			var body = string.Concat(messages.Select(m => m?.MessageBody ?? string.Empty));
			var receivedAt = DateTime.UtcNow;

			if (string.IsNullOrWhiteSpace(body))
				return;

			// PendingResult keeps the process alive past OnReceive returning, which
			// a plain fire-and-forget Task would not.
			var pending = GoAsync();

			_ = Task.Run(async () =>
			{
				try
				{
					// One retry: the phone may still be reconnecting to data when
					// the message lands, and the automation is only waiting a
					// couple of minutes.
					var sent = await IfmsRelayClient.RelaySmsAsync(sender, body, receivedAt);

					if (!sent)
					{
						await Task.Delay(TimeSpan.FromSeconds(5));
						await IfmsRelayClient.RelaySmsAsync(sender, body, receivedAt);
					}
				}
				catch
				{
					// Never let a relay failure crash the host app.
				}
				finally
				{
					pending?.Finish();
				}
			});
		}
	}
}
