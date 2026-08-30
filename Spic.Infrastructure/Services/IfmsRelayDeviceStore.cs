using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace Spic.Infrastructure.Services
{
	/// <summary>
	/// Keeps track of which phones are allowed to relay the IFMS one-time password.
	///
	/// One row per handset with its own token, rather than a single shared key.
	/// The difference only matters on the day a phone is replaced: with a shared
	/// key the retired handset relays forever and nobody notices, and with this it
	/// stops the moment it is revoked.
	/// </summary>
	public sealed class IfmsRelayDeviceStore : IIfmsRelayDeviceStore
	{
		private readonly AppDbContext _db;
		private readonly ILogger<IfmsRelayDeviceStore> _logger;

		public IfmsRelayDeviceStore(AppDbContext db, ILogger<IfmsRelayDeviceStore> logger)
		{
			_db = db;
			_logger = logger;
		}

		public async Task<IfmsRelayRegistration> RegisterAsync(
			string deviceId,
			string deviceName,
			string? appVersion,
			string? platform,
			string? registeredBy,
			CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(deviceId))
				throw new ArgumentException("A device id is required.", nameof(deviceId));

			var token = GenerateToken();
			var now = DateTime.UtcNow;

			var device = await _db.IfmsRelayDevices
				.FirstOrDefaultAsync(d => d.DeviceId == deviceId, cancellationToken);

			var replaced = device is not null;

			if (device is null)
			{
				device = new IfmsRelayDevice
				{
					DeviceId = deviceId.Trim(),
					RegisteredAt = now
				};

				_db.IfmsRelayDevices.Add(device);
			}

			device.DeviceName = string.IsNullOrWhiteSpace(deviceName)
				? device.DeviceName
				: deviceName.Trim();

			device.TokenHash = Hash(token);
			device.AppVersion = appVersion;
			device.Platform = platform;
			device.RegisteredBy = registeredBy;
			device.RegisteredAt = now;
			device.IsActive = true;
			device.RevokedAt = null;
			device.RevokedBy = null;
			device.LastSeenAt = now;
			device.LastSeenAction = "register";

			await _db.SaveChangesAsync(cancellationToken);

			_logger.LogInformation(
				"{Action} relay device {Name} ({DeviceId}).",
				replaced ? "Re-paired" : "Paired", device.DeviceName, device.DeviceId);

			return new IfmsRelayRegistration
			{
				DeviceRecordId = device.Id,
				Token = token,
				ReplacedExisting = replaced
			};
		}

		public async Task<IfmsRelayDeviceDto?> AuthenticateAsync(
			string token,
			string action,
			CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(token))
				return null;

			var hash = Hash(token);

			var device = await _db.IfmsRelayDevices
				.FirstOrDefaultAsync(d => d.TokenHash == hash && d.IsActive, cancellationToken);

			if (device is null)
				return null;

			device.LastSeenAt = DateTime.UtcNow;
			device.LastSeenAction = action.Length > 60 ? action[..60] : action;

			await _db.SaveChangesAsync(cancellationToken);

			return ToDto(device, staleAfterHours: 0);
		}

		public async Task<IReadOnlyList<IfmsRelayDeviceDto>> ListAsync(
			int staleAfterHours,
			CancellationToken cancellationToken)
		{
			var devices = await _db.IfmsRelayDevices
				.AsNoTracking()
				.OrderByDescending(d => d.IsActive)
				.ThenByDescending(d => d.LastSeenAt)
				.ToListAsync(cancellationToken);

			return devices.Select(d => ToDto(d, staleAfterHours)).ToList();
		}

		public async Task<bool> RevokeAsync(int id, string? revokedBy, CancellationToken cancellationToken)
		{
			var device = await _db.IfmsRelayDevices
				.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

			if (device is null || !device.IsActive)
				return false;

			device.IsActive = false;
			device.RevokedAt = DateTime.UtcNow;
			device.RevokedBy = revokedBy;

			// Clearing the hash as well as the flag means a revoked token cannot be
			// resurrected by flipping IsActive back on in a database tool.
			device.TokenHash = string.Empty;

			await _db.SaveChangesAsync(cancellationToken);

			_logger.LogWarning(
				"Relay device {Name} ({DeviceId}) revoked by {By}.",
				device.DeviceName, device.DeviceId, revokedBy ?? "unknown");

			return true;
		}

		public async Task NoteMessageRelayedAsync(int deviceRecordId, CancellationToken cancellationToken)
		{
			var device = await _db.IfmsRelayDevices
				.FirstOrDefaultAsync(d => d.Id == deviceRecordId, cancellationToken);

			if (device is null)
				return;

			device.MessagesRelayed++;
			await _db.SaveChangesAsync(cancellationToken);
		}

		public async Task<DateTime?> LastSeenAcrossActiveAsync(CancellationToken cancellationToken) =>
			await _db.IfmsRelayDevices
				.AsNoTracking()
				.Where(d => d.IsActive && d.LastSeenAt != null)
				.MaxAsync(d => (DateTime?)d.LastSeenAt, cancellationToken);

		private static IfmsRelayDeviceDto ToDto(IfmsRelayDevice device, int staleAfterHours)
		{
			double? minutes = device.LastSeenAt.HasValue
				? (DateTime.UtcNow - device.LastSeenAt.Value).TotalMinutes
				: null;

			return new IfmsRelayDeviceDto
			{
				Id = device.Id,
				DeviceId = device.DeviceId,
				DeviceName = device.DeviceName,
				RegisteredAt = device.RegisteredAt,
				RegisteredBy = device.RegisteredBy,
				LastSeenAt = device.LastSeenAt,
				LastSeenAction = device.LastSeenAction,
				MessagesRelayed = device.MessagesRelayed,
				IsActive = device.IsActive,
				RevokedAt = device.RevokedAt,
				RevokedBy = device.RevokedBy,
				AppVersion = device.AppVersion,
				Platform = device.Platform,
				MinutesSinceLastSeen = minutes,
				IsStale = device.IsActive &&
						  staleAfterHours > 0 &&
						  (minutes is null || minutes > staleAfterHours * 60)
			};
		}

		/// <summary>256 bits from the cryptographic RNG, URL-safe so it survives a header.</summary>
		private static string GenerateToken() =>
			Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
				.Replace('+', '-')
				.Replace('/', '_')
				.TrimEnd('=');

		private static string Hash(string token) =>
			Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
	}
}
