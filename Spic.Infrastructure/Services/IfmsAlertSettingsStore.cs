using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services
{
	/// <summary>
	/// The single alert-email settings row, with its SMTP password encrypted.
	/// Shared by SpicAPI (which edits it) and the automation (which sends with it).
	/// </summary>
	public interface IIfmsAlertSettingsStore
	{
		/// <summary>Null when nobody has saved settings yet.</summary>
		Task<IfmsAlertSettings?> GetAsync(CancellationToken cancellationToken);

		/// <summary>Creates or replaces the one row. An empty password keeps the stored one.</summary>
		Task<IfmsAlertSettings> SaveAsync(
			IfmsSetAlertSettingsDto dto,
			string updatedBy,
			CancellationToken cancellationToken);

		/// <summary>Null when no password is stored or it can no longer be read.</summary>
		string? DecryptPassword(IfmsAlertSettings row);

		Task RequestTestAsync(string requestedBy, CancellationToken cancellationToken);

		/// <summary>Stamps the test as done: sets LastTestAt and clears the request.</summary>
		Task RecordTestResultAsync(string result, CancellationToken cancellationToken);

		/// <summary>Outcome of the last real alert, as opposed to a test.</summary>
		Task RecordSendResultAsync(string result, CancellationToken cancellationToken);
	}

	/// <summary>
	/// Keeps the alert email settings in spiconeifms rather than appsettings.json,
	/// so an SMTP password or recipient list can change from the phone without a
	/// redeploy of the automation host.
	///
	/// The password uses the same isolated IFMS key ring as the portal passwords:
	/// SpicAPI writes it, the automation reads it, and neither host's own Data
	/// Protection is involved.
	/// </summary>
	public sealed class IfmsAlertSettingsStore : IIfmsAlertSettingsStore
	{
		/// <summary>The only row there is.</summary>
		public const int SingletonId = 1;

		/// <summary>Changing this string orphans the stored ciphertext, so it is fixed.</summary>
		private const string ProtectorPurpose = "SPIC.Ifms.AlertSettings.SmtpPassword";

		private static readonly char[] AddressSeparators = { ';', ',', '\n', '\r' };

		private readonly IfmsDbContext _db;
		private readonly IDataProtector _protector;
		private readonly ILogger<IfmsAlertSettingsStore> _logger;

		public IfmsAlertSettingsStore(
			IfmsDbContext db,
			IIfmsDataProtection dataProtection,
			ILogger<IfmsAlertSettingsStore> logger)
		{
			_db = db;
			_protector = dataProtection.Provider.CreateProtector(ProtectorPurpose);
			_logger = logger;
		}

		public Task<IfmsAlertSettings?> GetAsync(CancellationToken cancellationToken) =>
			_db.IfmsAlertSettings
				.AsNoTracking()
				.FirstOrDefaultAsync(s => s.Id == SingletonId, cancellationToken);

		public async Task<IfmsAlertSettings> SaveAsync(
			IfmsSetAlertSettingsDto dto,
			string updatedBy,
			CancellationToken cancellationToken)
		{
			var row = await _db.IfmsAlertSettings
				.FirstOrDefaultAsync(s => s.Id == SingletonId, cancellationToken);

			if (row is null)
			{
				row = new IfmsAlertSettings { Id = SingletonId };
				_db.IfmsAlertSettings.Add(row);
			}

			row.EmailEnabled = dto.EmailEnabled;
			row.SmtpHost = Clean(dto.SmtpHost, 200);
			row.SmtpPort = dto.SmtpPort;
			row.UseStartTls = dto.UseStartTls;
			row.UserName = Clean(dto.UserName, 200);
			row.FromAddress = Clean(dto.FromAddress, 200);
			row.FromName = Clean(dto.FromName, 120);
			row.ToAddresses = Clean(dto.ToAddresses, 2000);
			row.CcAddresses = Clean(dto.CcAddresses, 2000);
			row.FailuresOnly = dto.FailuresOnly;
			row.AttachReports = dto.AttachReports;

			// Blank means "leave it alone": the phone never sees the stored
			// password, so it cannot send it back, and a save that silently
			// wiped it would only be noticed at 04:05.
			if (!string.IsNullOrEmpty(dto.Password))
				row.ProtectedPassword = _protector.Protect(dto.Password);

			row.UpdatedAt = DateTime.UtcNow;
			row.UpdatedBy = Clean(updatedBy, 120);

			await _db.SaveChangesAsync(cancellationToken);

			_logger.LogInformation(
				"Alert email settings saved by {UpdatedBy}: enabled={Enabled}, host={Host}:{Port}, to={To}.",
				row.UpdatedBy, row.EmailEnabled, row.SmtpHost, row.SmtpPort, row.ToAddresses);

			return row;
		}

		public string? DecryptPassword(IfmsAlertSettings row)
		{
			if (string.IsNullOrWhiteSpace(row.ProtectedPassword))
				return null;

			try
			{
				return _protector.Unprotect(row.ProtectedPassword);
			}
			catch (Exception ex)
			{
				// Almost always a lost or changed key ring rather than corrupt
				// data; say so, and let the send fail on authentication rather
				// than here, so the SMTP error lands in LastSendResult.
				_logger.LogError(
					ex,
					"Could not decrypt the alert email password. This usually means the IFMS Data " +
					"Protection keys were lost or changed. Enter the password again to fix it.");

				return null;
			}
		}

		public async Task RequestTestAsync(string requestedBy, CancellationToken cancellationToken)
		{
			var row = await _db.IfmsAlertSettings
				.FirstOrDefaultAsync(s => s.Id == SingletonId, cancellationToken);

			if (row is null)
				throw new InvalidOperationException("Save the alert email settings before requesting a test.");

			row.TestRequestedAt = DateTime.UtcNow;
			row.TestRequestedBy = Clean(requestedBy, 120);

			await _db.SaveChangesAsync(cancellationToken);
		}

		public async Task RecordTestResultAsync(string result, CancellationToken cancellationToken)
		{
			var row = await _db.IfmsAlertSettings
				.FirstOrDefaultAsync(s => s.Id == SingletonId, cancellationToken);

			if (row is null)
				return;

			row.LastTestAt = DateTime.UtcNow;
			row.LastTestResult = Clean(result, 1000);
			row.TestRequestedAt = null;

			await _db.SaveChangesAsync(cancellationToken);
		}

		public async Task RecordSendResultAsync(string result, CancellationToken cancellationToken)
		{
			var row = await _db.IfmsAlertSettings
				.FirstOrDefaultAsync(s => s.Id == SingletonId, cancellationToken);

			if (row is null)
				return;

			row.LastSentAt = DateTime.UtcNow;
			row.LastSendResult = Clean(result, 1000);

			await _db.SaveChangesAsync(cancellationToken);
		}

		public static IfmsAlertSettingsDto ToDto(IfmsAlertSettings row) =>
			new()
			{
				Id = row.Id,
				EmailEnabled = row.EmailEnabled,
				SmtpHost = row.SmtpHost,
				SmtpPort = row.SmtpPort,
				UseStartTls = row.UseStartTls,
				UserName = row.UserName,
				HasPassword = !string.IsNullOrWhiteSpace(row.ProtectedPassword),
				FromAddress = row.FromAddress,
				FromName = row.FromName,
				ToAddresses = row.ToAddresses,
				CcAddresses = row.CcAddresses,
				FailuresOnly = row.FailuresOnly,
				AttachReports = row.AttachReports,
				TestRequestedAt = row.TestRequestedAt,
				TestRequestedBy = row.TestRequestedBy,
				LastTestAt = row.LastTestAt,
				LastTestResult = row.LastTestResult,
				LastSentAt = row.LastSentAt,
				LastSendResult = row.LastSendResult,
				UpdatedAt = row.UpdatedAt,
				UpdatedBy = row.UpdatedBy
			};

		/// <summary>Splits on ; , and newlines, trims, and drops blanks.</summary>
		public static IReadOnlyList<string> SplitAddresses(string? addresses) =>
			string.IsNullOrWhiteSpace(addresses)
				? Array.Empty<string>()
				: addresses
					.Split(AddressSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.Where(a => a.Length > 0)
					.ToList();

		private static string Clean(string? value, int max)
		{
			var trimmed = (value ?? string.Empty).Trim();
			return trimmed.Length <= max ? trimmed : trimmed[..max];
		}
	}
}
