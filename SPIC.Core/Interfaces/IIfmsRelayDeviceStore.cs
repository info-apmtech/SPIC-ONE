using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SPIC.Core.Interfaces
{
	public sealed class IfmsRelayDeviceDto
	{
		public int Id { get; set; }
		public string DeviceId { get; set; } = string.Empty;
		public string DeviceName { get; set; } = string.Empty;

		public DateTime RegisteredAt { get; set; }
		public string? RegisteredBy { get; set; }

		public DateTime? LastSeenAt { get; set; }
		public string? LastSeenAction { get; set; }

		public int MessagesRelayed { get; set; }

		public bool IsActive { get; set; }
		public DateTime? RevokedAt { get; set; }
		public string? RevokedBy { get; set; }

		public string? AppVersion { get; set; }
		public string? Platform { get; set; }

		/// <summary>Null when never seen; otherwise how long since it last spoke.</summary>
		public double? MinutesSinceLastSeen { get; set; }

		/// <summary>True when this phone has gone quiet for longer than expected.</summary>
		public bool IsStale { get; set; }
	}

	public sealed class IfmsRelayRegistration
	{
		public required int DeviceRecordId { get; init; }

		/// <summary>
		/// Returned exactly once, at pairing. The server keeps only a hash, so this
		/// cannot be recovered later — a lost token means pairing the phone again.
		/// </summary>
		public required string Token { get; init; }

		public required bool ReplacedExisting { get; init; }
	}

	public interface IIfmsRelayDeviceStore
	{
		/// <summary>
		/// Pairs a handset and issues it a token of its own. Re-pairing the same
		/// DeviceId rotates the token rather than creating a duplicate row.
		/// </summary>
		Task<IfmsRelayRegistration> RegisterAsync(
			string deviceId,
			string deviceName,
			string? appVersion,
			string? platform,
			string? registeredBy,
			CancellationToken cancellationToken);

		/// <summary>
		/// Resolves a token to an active device, and records that it was seen.
		/// Returns null when the token is unknown or the device has been revoked —
		/// which is what makes revoking a replaced phone take effect immediately.
		/// </summary>
		Task<IfmsRelayDeviceDto?> AuthenticateAsync(
			string token,
			string action,
			CancellationToken cancellationToken);

		Task<IReadOnlyList<IfmsRelayDeviceDto>> ListAsync(
			int staleAfterHours,
			CancellationToken cancellationToken);

		Task<bool> RevokeAsync(int id, string? revokedBy, CancellationToken cancellationToken);

		Task NoteMessageRelayedAsync(int deviceRecordId, CancellationToken cancellationToken);

		/// <summary>
		/// The most recent check-in across all active devices, or null when none is
		/// paired. This is what the staleness alert is built on.
		/// </summary>
		Task<DateTime?> LastSeenAcrossActiveAsync(CancellationToken cancellationToken);
	}
}
