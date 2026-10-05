using Android.App;
using Android.Content;
using Android.Provider;
using SPIC.Ifms.Relay.Services;

namespace SPIC.Ifms.Relay.Platforms.Android
{
	/// <summary>
	/// Forwards the IFMS one-time password to SpicAPI so the 04:05 login can read
	/// it without waking anybody.
	///
	/// Registered in the manifest rather than in code, which is what lets Android
	/// start it while the app is closed. It stays inert until the phone is paired,
	/// and by default it lets only IFMS-looking messages leave the phone: the
	/// body mentions IFMS or the sender is on the accepted list. Everything else
	/// is dropped here, on the handset, and only its sender is logged.
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
			try
			{
				if (!RelaySettings.IsConfigured)
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

				var senderLabel = string.IsNullOrWhiteSpace(sender) ? "unknown sender" : sender;

				if (!ShouldForward(sender, body))
				{
					RelayLog.Info($"SMS from {senderLabel} skipped by the IFMS filter");
					return;
				}

				// PendingResult keeps the process alive past OnReceive returning,
				// which a plain fire-and-forget Task would not.
				var pending = GoAsync();

				_ = Task.Run(async () =>
				{
					try
					{
						// One retry: the phone may still be reconnecting to data
						// when the message lands, and the automation is only
						// waiting a couple of minutes.
						var result = await RelayClient.RelaySmsAsync(sender, body, receivedAt);

						if (!result.Success)
						{
							await Task.Delay(TimeSpan.FromSeconds(5));
							result = await RelayClient.RelaySmsAsync(sender, body, receivedAt);
						}

						if (result.Success)
						{
							RelaySettings.LastSmsRelayedUtc = DateTime.UtcNow;
							RelayLog.Ok($"SMS from {senderLabel} forwarded");
						}
						else
						{
							RelayLog.Fail($"SMS from {senderLabel} not forwarded: {result.Message}");
						}
					}
					catch (Exception ex)
					{
						RelayLog.Fail($"SMS from {senderLabel} not forwarded: {ex.Message}");
					}
					finally
					{
						pending?.Finish();
					}
				});
			}
			catch
			{
				// Nothing in a broadcast receiver may crash the process.
			}
		}

		/// <summary>
		/// The privacy filter. With the switch off everything is forwarded, which
		/// is only meant for a SIM that receives nothing but IFMS traffic.
		/// </summary>
		internal static bool ShouldForward(string sender, string body)
		{
			if (!RelaySettings.ForwardOnlyIfms)
				return true;

			if (body.Contains("ifms", StringComparison.OrdinalIgnoreCase))
				return true;

			var senderDigits = RelaySettings.NormaliseNumber(sender);

			if (senderDigits.Length == 0)
				return false;

			return RelaySettings.AcceptedSenderDigits()
				.Any(accepted => senderDigits.Contains(accepted, StringComparison.Ordinal));
		}
	}
}
