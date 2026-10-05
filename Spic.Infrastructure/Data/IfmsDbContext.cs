using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Data
{
	/// <summary>
	/// The automation's own tables, and nothing else.
	///
	/// Deliberately separate from <see cref="AppDbContext"/> and pointed at its own
	/// database. The automation does not write report data any more — it uploads
	/// files to SpicAPI exactly as a person would from the Excel Upload page — so
	/// it has no reason to know about dealers, welfare schemes or identity, and no
	/// reason to be able to reach them.
	///
	/// Nine tables: what ran, what it downloaded, which logins it uses, which
	/// phones may relay an OTP, and the keys that encrypt the passwords.
	/// </summary>
	public class IfmsDbContext : DbContext, IDataProtectionKeyContext
	{
		public IfmsDbContext(DbContextOptions<IfmsDbContext> options) : base(options)
		{
		}

		public DbSet<IfmsAutomationRun> IfmsAutomationRuns { get; set; }
		public DbSet<IfmsAutomationReportRun> IfmsAutomationReportRuns { get; set; }
		public DbSet<IfmsOtpMessage> IfmsOtpMessages { get; set; }
		public DbSet<IfmsPortalSession> IfmsPortalSessions { get; set; }
		public DbSet<IfmsChallengeRequest> IfmsChallengeRequests { get; set; }
		public DbSet<IfmsPortalAccount> IfmsPortalAccounts { get; set; }
		public DbSet<IfmsPasswordChange> IfmsPasswordChanges { get; set; }
		public DbSet<IfmsRelayDevice> IfmsRelayDevices { get; set; }

		/// <summary>
		/// The Data Protection keys that encrypt the portal passwords.
		///
		/// They live here rather than on disk because SpicAPI and the automation run
		/// on different machines and both must read the same passwords — a shared
		/// folder cannot span two hosts, a shared database already does.
		/// </summary>
		public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.Entity<IfmsAutomationReportRun>()
				.HasOne(r => r.Run)
				.WithMany(r => r.Reports)
				.HasForeignKey(r => r.RunId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<IfmsAutomationRun>()
				.HasIndex(r => new { r.ReportDate, r.StartedAt });

			builder.Entity<IfmsAutomationReportRun>()
				.HasIndex(r => new { r.RunId, r.JobKey });

			// The OTP poller reads unconsumed messages newest-first every second.
			builder.Entity<IfmsOtpMessage>()
				.HasIndex(o => new { o.ConsumedAt, o.ReceivedAt });

			builder.Entity<IfmsPortalSession>()
				.HasIndex(s => new { s.PortalUserName, s.IsActive });

			builder.Entity<IfmsChallengeRequest>()
				.HasIndex(c => new { c.Status, c.CreatedAt });

			builder.Entity<IfmsPortalAccount>()
				.HasIndex(a => a.AccountKey)
				.IsUnique();

			builder.Entity<IfmsPasswordChange>()
				.HasIndex(c => new { c.AccountId, c.ChangedAt });

			builder.Entity<IfmsRelayDevice>()
				.HasIndex(d => d.DeviceId)
				.IsUnique();

			// The SMS relay looks a device up by its token on every call.
			builder.Entity<IfmsRelayDevice>()
				.HasIndex(d => new { d.TokenHash, d.IsActive });
		}
	}
}
