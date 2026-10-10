using System.Text.Json;

namespace SPIC.Ifms.Relay.Services
{
	/// <summary>
	/// An IFMS message the phone received but could not hand to the server.
	///
	/// One retry five seconds after the SMS was not enough: on 9 Oct 2026 the
	/// phone received ten OTPs while the server was answering errors, and all
	/// but the first were lost. The watcher drains this queue every ten seconds
	/// until the message is five minutes old, by which time the code it carried
	/// has expired anyway.
	/// </summary>
	public sealed class PendingSms
	{
		public string Sender { get; set; } = string.Empty;
		public string Body { get; set; } = string.Empty;
		public DateTime ReceivedAtUtc { get; set; }
		public int Attempts { get; set; }
	}

	public static class PendingSmsQueue
	{
		private const string Key = "relay_pending_sms";
		private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(5);
		private static readonly object Gate = new();

		public static int Count
		{
			get { lock (Gate) return Load().Count; }
		}

		public static void Enqueue(string sender, string body, DateTime receivedAtUtc)
		{
			lock (Gate)
			{
				var items = Load();
				items.Add(new PendingSms { Sender = sender, Body = body, ReceivedAtUtc = receivedAtUtc, Attempts = 1 });
				Save(items);
			}
		}

		/// <summary>
		/// Tries every queued message once. Delivered and expired messages leave
		/// the queue; the rest wait for the next pass.
		/// </summary>
		public static async Task DrainAsync(CancellationToken cancellationToken)
		{
			List<PendingSms> items;
			lock (Gate) items = Load();

			if (items.Count == 0)
				return;

			var remaining = new List<PendingSms>();

			foreach (var item in items)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var label = string.IsNullOrWhiteSpace(item.Sender) ? "unknown sender" : item.Sender;

				if (DateTime.UtcNow - item.ReceivedAtUtc > MaxAge)
				{
					RelayLog.Fail($"SMS from {label} given up after {item.Attempts} attempts; the code has expired");
					continue;
				}

				item.Attempts++;
				var result = await RelayClient.RelaySmsAsync(item.Sender, item.Body, item.ReceivedAtUtc, cancellationToken);

				if (result.Success)
				{
					RelaySettings.LastSmsRelayedUtc = DateTime.UtcNow;
					RelayLog.Ok($"SMS from {label} forwarded on attempt {item.Attempts}");
				}
				else
				{
					if (item.Attempts <= 3 || item.Attempts % 6 == 0)
						RelayLog.Warn($"SMS from {label} still not forwarded (attempt {item.Attempts}): {result.Message}");
					remaining.Add(item);
				}
			}

			lock (Gate) Save(remaining);
		}

		private static List<PendingSms> Load()
		{
			try
			{
				var json = Preferences.Default.Get(Key, string.Empty);
				if (string.IsNullOrWhiteSpace(json))
					return new List<PendingSms>();

				return JsonSerializer.Deserialize(json, RelayJson.Default.ListPendingSms) ?? new List<PendingSms>();
			}
			catch
			{
				return new List<PendingSms>();
			}
		}

		private static void Save(List<PendingSms> items)
		{
			try
			{
				if (items.Count == 0)
					Preferences.Default.Remove(Key);
				else
					Preferences.Default.Set(Key, JsonSerializer.Serialize(items, RelayJson.Default.ListPendingSms));
			}
			catch
			{
				// Losing the queue is no worse than not having one.
			}
		}
	}
}
