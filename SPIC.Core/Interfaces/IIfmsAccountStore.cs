using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SPIC.Core.Interfaces
{
	/// <summary>
	/// One portal login, decrypted and ready to use. Deliberately a short-lived
	/// object rather than something cached: the password can change between runs.
	/// </summary>
	public sealed class IfmsAccountCredentials
	{
		public required int AccountId { get; init; }
		public required string AccountKey { get; init; }
		public required string CompanyName { get; init; }
		public required string UserName { get; init; }
		public required string Password { get; init; }

		public DateTime PasswordExpiresAt { get; init; }

		public int DaysUntilPasswordExpires =>
			(int)Math.Floor((PasswordExpiresAt - DateTime.UtcNow).TotalDays);

		public bool PasswordExpired => DateTime.UtcNow >= PasswordExpiresAt;
	}

	public interface IIfmsAccountStore
	{
		/// <summary>Active logins in sign-in order, decrypted.</summary>
		Task<IReadOnlyList<IfmsAccountCredentials>> GetActiveAsync(CancellationToken cancellationToken);

		Task<IfmsAccountCredentials?> GetAsync(string accountKey, CancellationToken cancellationToken);

		/// <summary>
		/// Creates or updates a login. Setting a password restarts the 80-day clock
		/// and writes an audit row.
		/// </summary>
		Task SetCredentialsAsync(
			string accountKey,
			string companyName,
			string userName,
			string password,
			string changedBy,
			string reason,
			CancellationToken cancellationToken);

		Task RecordLoginAsync(
			int accountId,
			bool succeeded,
			string? message,
			CancellationToken cancellationToken);
	}
}
