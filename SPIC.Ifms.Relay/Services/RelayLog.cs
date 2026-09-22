using System.Text.Json;
using System.Text.Json.Serialization;

namespace SPIC.Ifms.Relay.Services
{
	/// <summary>
	/// The last thirty things the relay did, so the person holding the phone can
	/// see at a glance whether the OTP went through at 04:06 or the server has
	/// been unreachable since midnight.
	///
	/// Entries never contain a message body. An SMS is described only by its
	/// sender, its time and whether it was forwarded or filtered out: the log is
	/// on screen in a shared office, and the OTP is nobody's business once it has
	/// been used.
	/// </summary>
	public static class RelayLog
	{
		private const string StorageKey = "relay_log";
		private const int Capacity = 30;

		private static readonly object Gate = new();
		private static List<RelayLogEntry>? _entries;

		/// <summary>Raised on whatever thread wrote the entry; marshal before touching UI.</summary>
		public static event Action? Changed;

		/// <summary>Newest first.</summary>
		public static IReadOnlyList<RelayLogEntry> Entries
		{
			get
			{
				lock (Gate)
				{
					return Load().ToList();
				}
			}
		}

		public static void Info(string summary) => Add(RelayLogKind.Info, summary);
		public static void Ok(string summary) => Add(RelayLogKind.Ok, summary);
		public static void Warn(string summary) => Add(RelayLogKind.Warn, summary);
		public static void Fail(string summary) => Add(RelayLogKind.Fail, summary);

		private static void Add(RelayLogKind kind, string summary)
		{
			try
			{
				lock (Gate)
				{
					var entries = Load();

					entries.Insert(0, new RelayLogEntry
					{
						AtUtc = DateTime.UtcNow,
						Kind = kind,
						Summary = summary
					});

					if (entries.Count > Capacity)
						entries.RemoveRange(Capacity, entries.Count - Capacity);

					Preferences.Default.Set(
						StorageKey, JsonSerializer.Serialize(entries, RelayLogJson.Default.ListRelayLogEntry));
				}

				Changed?.Invoke();
			}
			catch
			{
				// The log is a convenience. It must never take the relay down.
			}
		}

		private static List<RelayLogEntry> Load()
		{
			if (_entries is not null)
				return _entries;

			try
			{
				var json = Preferences.Default.Get(StorageKey, string.Empty);

				_entries = string.IsNullOrWhiteSpace(json)
					? new List<RelayLogEntry>()
					: JsonSerializer.Deserialize(json, RelayLogJson.Default.ListRelayLogEntry)
					  ?? new List<RelayLogEntry>();
			}
			catch
			{
				// A corrupt log is not worth keeping; start again.
				_entries = new List<RelayLogEntry>();
			}

			return _entries;
		}
	}

	public enum RelayLogKind
	{
		Info,
		Ok,
		Warn,
		Fail
	}

	public sealed class RelayLogEntry
	{
		public DateTime AtUtc { get; set; }
		public RelayLogKind Kind { get; set; }
		public string Summary { get; set; } = string.Empty;

		/// <summary>Local wall-clock time, "04:06" style, for the activity list.</summary>
		[JsonIgnore]
		public string TimeText => AtUtc.ToLocalTime().ToString("dd MMM HH:mm");

		[JsonIgnore]
		public Color Colour => Kind switch
		{
			RelayLogKind.Ok => Color.FromArgb("#166534"),
			RelayLogKind.Warn => Color.FromArgb("#B45309"),
			RelayLogKind.Fail => Color.FromArgb("#991B1B"),
			_ => Color.FromArgb("#64748B")
		};
	}

	/// <summary>
	/// Source-generated serializer: keeps the Release build free of trimming
	/// warnings and spares the reflection cost every time an entry is written.
	/// </summary>
	[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
	[JsonSerializable(typeof(List<RelayLogEntry>))]
	internal sealed partial class RelayLogJson : JsonSerializerContext
	{
	}
}
