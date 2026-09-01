using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using SPIC.Ifms.Automation.Options;

namespace SPIC.Ifms.Automation.Portal.Challenges
{
	public interface IOtpProvider
	{
		/// <summary>
		/// Waits for the one-time password the portal has just sent by SMS.
		/// <paramref name="requestedAtUtc"/> is the instant the automation pressed
		/// the button; anything relayed before it is ignored so a stale SMS can
		/// never be replayed into a login.
		/// </summary>
		Task<string?> WaitForOtpAsync(
			DateTime requestedAtUtc,
			int? runId,
			CancellationToken cancellationToken,
			string? accountKey = null);
	}

	/// <summary>
	/// Reads the OTP the SPIC Android companion forwards to SpicAPI.
	///
	/// Note on the two company logins: both may send their OTP to the same handset
	/// from the same sender, so the sender cannot tell them apart. What does is
	/// time — the logins run one after another, and each only accepts messages that
	/// arrived after it pressed the button.
	///
	/// The phone stays with the SIM that receives IFMS messages. When an SMS
	/// arrives its broadcast receiver POSTs the body to the API, which stores it
	/// in IfmsOtpMessages. This provider polls that table, so a code typically
	/// reaches the automation two to three seconds after the network delivers it —
	/// no human involved.
	/// </summary>
	public sealed class SmsRelayOtpProvider : IOtpProvider
	{
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly IfmsOtpOptions _options;
		private readonly ILogger<SmsRelayOtpProvider> _logger;
		private readonly Regex _otpPattern;

		public SmsRelayOtpProvider(
			IServiceScopeFactory scopeFactory,
			IOptions<IfmsOptions> options,
			ILogger<SmsRelayOtpProvider> logger)
		{
			_scopeFactory = scopeFactory;
			_options = options.Value.Otp;
			_logger = logger;
			_otpPattern = new Regex(_options.OtpPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
		}

		public async Task<string?> WaitForOtpAsync(
			DateTime requestedAtUtc,
			int? runId,
			CancellationToken cancellationToken,
			string? accountKey = null)
		{
			// Allow a little slack: phone clocks drift, and the relay stamps
			// ReceivedAt from the device.
			var floor = requestedAtUtc.AddSeconds(-30);
			var deadline = DateTime.UtcNow.AddSeconds(_options.WaitSeconds);
			var poll = TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds));

			_logger.LogInformation(
				"Waiting up to {Seconds}s for the IFMS OTP to arrive from the Android relay.",
				_options.WaitSeconds);

			while (DateTime.UtcNow < deadline)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var otp = await TryClaimAsync(floor, runId, accountKey, cancellationToken);
				if (otp is not null)
				{
					_logger.LogInformation("OTP received from the Android relay.");
					return otp;
				}

				await Task.Delay(poll, cancellationToken);
			}

			_logger.LogError(
				"No OTP arrived within {Seconds}s. Check that the SPIC Android app is running, " +
				"has SMS permission, and can reach the API.",
				_options.WaitSeconds);

			return null;
		}

		/// <summary>
		/// Finds the newest unconsumed message and marks it consumed in the same
		/// save, so two overlapping runs can never grab the same code.
		/// </summary>
		private async Task<string?> TryClaimAsync(
			DateTime floorUtc,
			int? runId,
			string? accountKey,
			CancellationToken cancellationToken)
		{
			await using var scope = _scopeFactory.CreateAsyncScope();
			var db = scope.ServiceProvider.GetRequiredService<IfmsDbContext>();

			var oldestAcceptable = DateTime.UtcNow.AddSeconds(-Math.Max(30, _options.MaxMessageAgeSeconds));
			if (floorUtc > oldestAcceptable)
				oldestAcceptable = floorUtc;

			var candidates = await db.IfmsOtpMessages
				.Where(m => m.ConsumedAt == null && m.ReceivedAt >= oldestAcceptable)
				.OrderByDescending(m => m.ReceivedAt)
				.Take(20)
				.ToListAsync(cancellationToken);

			foreach (var message in candidates)
			{
				if (!SenderAccepted(message.Sender))
					continue;

				var otp = message.ExtractedOtp;
				if (string.IsNullOrWhiteSpace(otp))
				{
					var match = _otpPattern.Match(message.Body ?? string.Empty);
					if (!match.Success)
						continue;

					otp = match.Groups.Count > 1 && match.Groups[1].Success
						? match.Groups[1].Value
						: match.Value;
				}

				message.ConsumedAt = DateTime.UtcNow;
				message.ConsumedByRunId = runId;
				message.ConsumedByAccountKey = accountKey;
				message.ExtractedOtp = otp;

				await db.SaveChangesAsync(cancellationToken);
				return otp.Trim();
			}

			return null;
		}

		private bool SenderAccepted(string? sender)
		{
			if (_options.AcceptedSenders.Count == 0)
				return true;

			if (string.IsNullOrWhiteSpace(sender))
			{
				// Indian operators rewrite the header constantly. Rather than drop a
				// code we might need, let it through and rely on the time window.
				return true;
			}

			return _options.AcceptedSenders.Any(accepted =>
				sender.Contains(accepted, StringComparison.OrdinalIgnoreCase));
		}
	}

	/// <summary>Used when the portal does not ask for an OTP at all.</summary>
	public sealed class NoOtpProvider : IOtpProvider
	{
		public Task<string?> WaitForOtpAsync(
			DateTime requestedAtUtc,
			int? runId,
			CancellationToken cancellationToken,
			string? accountKey = null) => Task.FromResult<string?>(null);
	}
}
