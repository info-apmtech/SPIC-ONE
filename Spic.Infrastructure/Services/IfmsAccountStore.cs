using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace Spic.Infrastructure.Services
{
	/// <summary>
	/// Stores portal passwords in the database, encrypted with ASP.NET Data
	/// Protection.
	///
	/// They are in the database rather than in configuration because the portal
	/// expires them every 80 days: they have to be changeable without a redeploy,
	/// and their age has to be tracked so somebody is warned before a 4am run
	/// fails on an expired password.
	///
	/// The encryption is only as durable as the Data Protection key ring. That
	/// directory must be persisted and backed up — if it is lost, every stored
	/// password becomes unreadable and has to be entered again. Program.cs pins it
	/// to an explicit path for exactly this reason.
	/// </summary>
	public sealed class IfmsAccountStore : IIfmsAccountStore
	{
		/// <summary>
		/// Changing this string orphans every existing ciphertext, so it is fixed.
		/// </summary>
		private const string ProtectorPurpose = "SPIC.Ifms.Automation.PortalPassword.v1";

		private readonly AppDbContext _db;
		private readonly IDataProtector _protector;
		private readonly IConfiguration _config;
		private readonly ILogger<IfmsAccountStore> _logger;

		public IfmsAccountStore(
			AppDbContext db,
			IDataProtectionProvider dataProtection,
			IConfiguration config,
			ILogger<IfmsAccountStore> logger)
		{
			_db = db;
			_config = config;
			_protector = dataProtection.CreateProtector(ProtectorPurpose);
			_logger = logger;
		}

		public async Task<IReadOnlyList<IfmsAccountCredentials>> GetActiveAsync(
			CancellationToken cancellationToken)
		{
			var accounts = await _db.IfmsPortalAccounts
				.AsNoTracking()
				.Where(a => a.IsActive)
				.OrderBy(a => a.Order)
				.ThenBy(a => a.Id)
				.ToListAsync(cancellationToken);

			var result = new List<IfmsAccountCredentials>(accounts.Count);

			foreach (var account in accounts)
			{
				var credentials = Unprotect(account);

				if (credentials is not null)
					result.Add(credentials);
			}

			return result;
		}

		public async Task<IfmsAccountCredentials?> GetAsync(
			string accountKey,
			CancellationToken cancellationToken)
		{
			var account = await _db.IfmsPortalAccounts
				.AsNoTracking()
				.FirstOrDefaultAsync(a => a.AccountKey == accountKey, cancellationToken);

			return account is null ? null : Unprotect(account);
		}

		public async Task SetCredentialsAsync(
			string accountKey,
			string companyName,
			string userName,
			string password,
			string changedBy,
			string reason,
			CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(accountKey))
				throw new ArgumentException("An account key is required.", nameof(accountKey));

			if (string.IsNullOrWhiteSpace(password))
				throw new ArgumentException("A password is required.", nameof(password));

			var account = await _db.IfmsPortalAccounts
				.FirstOrDefaultAsync(a => a.AccountKey == accountKey, cancellationToken);

			var now = DateTime.UtcNow;
			var isNew = account is null;

			if (account is null)
			{
				account = new IfmsPortalAccount
				{
					AccountKey = accountKey.Trim(),
					CreatedAt = now,
					Order = await _db.IfmsPortalAccounts.CountAsync(cancellationToken) * 10
				};

				_db.IfmsPortalAccounts.Add(account);
			}

			account.CompanyName = string.IsNullOrWhiteSpace(companyName)
				? account.CompanyName
				: companyName.Trim();

			account.UserName = userName.Trim();
			account.ProtectedPassword = _protector.Protect(password);

			// Commissioning aid, switched off by default. Kept in step with the
			// encrypted copy so the two can never disagree, and cleared the moment
			// the flag goes off rather than lingering until somebody remembers.
			var keepPlain = _config.GetValue<bool>("Ifms:StorePlainPasswordForTesting");

			account.PlainPasswordForTesting = keepPlain ? password : null;

			if (keepPlain)
			{
				_logger.LogWarning(
					"Ifms:StorePlainPasswordForTesting is ON, so the password for {AccountKey} " +
					"is also stored in the clear. Turn it off and drop the column once the " +
					"logins are proven.",
					account.AccountKey);
			}
			account.PasswordSetAt = now;
			account.PasswordExpiresAt = now.AddDays(Math.Max(1, account.PasswordRotationDays));

			// A fresh password means the old warning no longer applies.
			account.ExpiryWarningSentAt = null;

			account.IsActive = true;
			account.UpdatedAt = now;
			account.UpdatedBy = changedBy;

			await _db.SaveChangesAsync(cancellationToken);

			_db.IfmsPasswordChanges.Add(new IfmsPasswordChange
			{
				AccountId = account.Id,
				ChangedAt = now,
				Reason = reason,
				ChangedBy = changedBy
			});

			await _db.SaveChangesAsync(cancellationToken);

			_logger.LogInformation(
				"{Action} portal login {AccountKey} ({UserName}); password valid until {Expires:dd MMM yyyy}.",
				isNew ? "Created" : "Updated", account.AccountKey, account.UserName,
				account.PasswordExpiresAt);
		}

		public async Task RecordLoginAsync(
			int accountId,
			bool succeeded,
			string? message,
			CancellationToken cancellationToken)
		{
			var account = await _db.IfmsPortalAccounts
				.FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

			if (account is null)
				return;

			account.LastLoginAt = DateTime.UtcNow;
			account.LastLoginSucceeded = succeeded;
			account.LastLoginMessage = message is null || message.Length <= 400
				? message
				: message[..400];

			if (succeeded)
			{
				// Mark the most recent password change as proven. Until a login
				// works, a newly typed password is only a claim.
				var pending = await _db.IfmsPasswordChanges
					.Where(c => c.AccountId == accountId && !c.VerifiedByLogin)
					.OrderByDescending(c => c.ChangedAt)
					.FirstOrDefaultAsync(cancellationToken);

				if (pending is not null)
					pending.VerifiedByLogin = true;
			}

			await _db.SaveChangesAsync(cancellationToken);
		}

		private IfmsAccountCredentials? Unprotect(IfmsPortalAccount account)
		{
			if (string.IsNullOrWhiteSpace(account.ProtectedPassword))
			{
				_logger.LogError(
					"Portal login {AccountKey} has no password set. Set one with " +
					"\"dotnet run -- set-credentials {AccountKey} <username> <password>\" " +
					"or from the IFMS Logins page.",
					account.AccountKey, account.AccountKey);

				return null;
			}

			try
			{
				return new IfmsAccountCredentials
				{
					AccountId = account.Id,
					AccountKey = account.AccountKey,
					CompanyName = account.CompanyName,
					UserName = account.UserName,
					Password = _protector.Unprotect(account.ProtectedPassword),
					PasswordExpiresAt = account.PasswordExpiresAt
				};
			}
			catch (Exception ex)
			{
				// Almost always a lost or changed Data Protection key ring rather
				// than corrupt data, so say that rather than "decryption failed".
				_logger.LogError(
					ex,
					"Could not decrypt the password for {AccountKey}. This usually means the Data " +
					"Protection key directory was lost or changed. Set the password again to fix it.",
					account.AccountKey);

				return null;
			}
		}
	}
}
