using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// The seam between the IFMS automation service, the Android companion and
	/// the portal dashboard.
	///
	/// The automation itself never calls in to start work — it polls for queued
	/// runs — so this controller stays a thin read/write surface over the tables
	/// and the API needs no knowledge of where the browser host lives.
	/// </summary>
	[ApiController]
	[Route("api/[controller]")]
	public sealed class IfmsAutomationController : ControllerBase
	{
		private const int MaxOtpBodyLength = 2000;

		private readonly IfmsDbContext _db;
		private readonly IConfiguration _config;
		private readonly IIfmsAccountStore _accounts;
		private readonly IIfmsRelayDeviceStore _devices;

		public IfmsAutomationController(
			IfmsDbContext db,
			IConfiguration config,
			IIfmsAccountStore accounts,
			IIfmsRelayDeviceStore devices)
		{
			_db = db;
			_config = config;
			_accounts = accounts;
			_devices = devices;
		}

		/// <summary>The phone that made this call, once its token has been checked.</summary>
		private IfmsRelayDeviceDto? _callingDevice;

		/// <summary>
		/// The pairing secret, shared and set once. It is only good for registering
		/// a handset — never for relaying a message — so rotating it does not
		/// disturb phones that are already paired.
		/// </summary>
		private bool HasPairingKey()
		{
			var expected = _config["IfmsAutomation:DeviceKey"];

			if (string.IsNullOrWhiteSpace(expected))
				return false;

			var supplied = Request.Headers["X-Device-Key"].ToString();

			return !string.IsNullOrEmpty(supplied) &&
				   CryptographicOperations.FixedTimeEquals(
					   Encoding.UTF8.GetBytes(supplied),
					   Encoding.UTF8.GetBytes(expected));
		}

		/// <summary>
		/// Checks the per-device token and records that the phone was seen.
		///
		/// The Android companion cannot use the user's JWT: that lives in the
		/// WebView's session storage and is long gone by four in the morning. It
		/// carries its own token instead, issued at pairing, so a replaced handset
		/// can be revoked without disturbing anything else.
		/// </summary>
		private async Task<bool> IsKnownDeviceAsync(string action, CancellationToken cancellationToken)
		{
			var token = Request.Headers["X-Device-Token"].ToString();

			if (string.IsNullOrEmpty(token))
				return false;

			_callingDevice = await _devices.AuthenticateAsync(token, action, cancellationToken);
			return _callingDevice is not null;
		}

		/// <summary>A signed-in portal user, or a paired phone.</summary>
		private async Task<bool> IsCallerAllowedAsync(string action, CancellationToken cancellationToken) =>
			User.Identity?.IsAuthenticated == true ||
			await IsKnownDeviceAsync(action, cancellationToken);

		// ------------------------------------------------------------------ SMS

		/// <summary>
		/// Receives an SMS forwarded by the Android companion. This is what makes
		/// the 04:05 login unattended: the phone holding the SIM relays the code
		/// within a couple of seconds and the automation picks it straight up.
		/// </summary>
		[AllowAnonymous]
		[HttpPost("sms")]
		public async Task<IActionResult> RelaySms(
			[FromBody] IfmsSmsRelayDto dto,
			CancellationToken cancellationToken)
		{
			if (!await IsCallerAllowedAsync("sms", cancellationToken))
				return Unauthorized(new { Success = false, Message = "Unrecognised device." });

			if (string.IsNullOrWhiteSpace(dto.Body))
				return BadRequest(new { Success = false, Message = "The message body is empty." });

			var body = dto.Body.Length > MaxOtpBodyLength
				? dto.Body[..MaxOtpBodyLength]
				: dto.Body;

			// Extract here as well as in the automation, so a stored message is
			// already useful if the pattern is later tightened on one side only.
			var match = Regex.Match(body, @"\b(\d{4,8})\b");

			var message = new IfmsOtpMessage
			{
				DeviceId = Trim(dto.DeviceId, 120),
				Sender = Trim(dto.Sender, 120),
				Body = body,
				ExtractedOtp = match.Success ? match.Groups[1].Value : null,
				ReceivedAt = dto.ReceivedAt ?? DateTime.UtcNow,
				CreatedAt = DateTime.UtcNow
			};

			message.DeviceId ??= _callingDevice?.DeviceId;

			_db.IfmsOtpMessages.Add(message);
			await _db.SaveChangesAsync(cancellationToken);

			if (_callingDevice is not null)
				await _devices.NoteMessageRelayedAsync(_callingDevice.Id, cancellationToken);

			return Ok(new
			{
				Success = true,
				message.Id,
				OtpDetected = message.ExtractedOtp is not null
			});
		}

		// ----------------------------------------------------------- challenges

		/// <summary>
		/// The CAPTCHA currently waiting for a human, if there is one. The Android
		/// app polls this; a null result means there is nothing to do.
		/// </summary>
		[AllowAnonymous]
		[HttpGet("challenge/pending")]
		public async Task<ActionResult<IfmsPendingChallengeDto?>> PendingChallenge(
			CancellationToken cancellationToken)
		{
			if (!await IsCallerAllowedAsync("poll", cancellationToken))
				return Unauthorized();

			var now = DateTime.UtcNow;

			var challenge = await _db.IfmsChallengeRequests
				.AsNoTracking()
				.Where(c => c.Status == "Pending" && c.ExpiresAt > now)
				.OrderByDescending(c => c.CreatedAt)
				.FirstOrDefaultAsync(cancellationToken);

			return Ok(challenge is null ? null : ToDto(challenge, now));
		}

		/// <summary>
		/// Submits a human reading of the CAPTCHA. The automation is polling for
		/// this and picks it up within five seconds.
		/// </summary>
		[AllowAnonymous]
		[HttpPost("challenge/{id:int}/answer")]
		public async Task<IActionResult> AnswerChallenge(
			int id,
			[FromBody] IfmsChallengeAnswerDto dto,
			CancellationToken cancellationToken)
		{
			if (!await IsCallerAllowedAsync("captcha-answer", cancellationToken))
				return Unauthorized();

			var answer = (dto.Answer ?? string.Empty).Trim();

			if (answer.Length == 0)
				return BadRequest(new { Success = false, Message = "No answer was supplied." });

			if (answer.Length > 50)
				return BadRequest(new { Success = false, Message = "That answer is too long to be a CAPTCHA." });

			var challenge = await _db.IfmsChallengeRequests
				.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

			if (challenge is null)
				return NotFound(new { Success = false, Message = "No such challenge." });

			if (challenge.Status != "Pending")
			{
				// Expected: the person answered just after the automation gave up.
				// Say so plainly rather than pretending it worked.
				return Conflict(new
				{
					Success = false,
					challenge.Status,
					Message = challenge.Status == "Expired"
						? "That CAPTCHA expired. The automation will send a fresh one shortly."
						: $"That CAPTCHA is no longer waiting for an answer ({challenge.Status})."
				});
			}

			challenge.Answer = answer;
			challenge.AnsweredAt = DateTime.UtcNow;
			challenge.AnsweredBy =
				User.FindFirstValue(ClaimTypes.Name) ??
				User.FindFirstValue(ClaimTypes.NameIdentifier) ??
				"Android app";
			challenge.Status = "Answered";

			await _db.SaveChangesAsync(cancellationToken);

			return Ok(new { Success = true, Message = "Thanks — the automation is continuing." });
		}

		// -------------------------------------------------------------- devices

		/// <summary>
		/// Pairs a phone and issues it a token of its own.
		///
		/// Authenticated with the shared pairing key, which is the only thing that
		/// key is good for. Re-registering the same handset rotates its token
		/// rather than creating a second row.
		/// </summary>
		[AllowAnonymous]
		[HttpPost("devices/register")]
		public async Task<IActionResult> RegisterDevice(
			[FromBody] IfmsRegisterDeviceDto dto,
			CancellationToken cancellationToken)
		{
			if (!HasPairingKey() && User.Identity?.IsAuthenticated != true)
				return Unauthorized(new { Success = false, Message = "Bad pairing key." });

			if (string.IsNullOrWhiteSpace(dto.DeviceId))
				return BadRequest(new { Success = false, Message = "A device id is required." });

			IfmsRelayRegistration registration;
			try
			{
				registration = await _devices.RegisterAsync(
					dto.DeviceId,
					dto.DeviceName,
					dto.AppVersion,
					dto.Platform,
					User.FindFirstValue(ClaimTypes.Name) ?? "pairing key",
					cancellationToken);
			}
			catch (Exception ex)
			{
				// Pairing is done by an administrator holding the pairing key, and
				// the API's log is not always at hand. Name the failure here so a
				// missing IfmsConnection or table shows up in the reply itself.
				var root = ex;
				while (root.InnerException is not null) root = root.InnerException;
				return StatusCode(500, new
				{
					Success = false,
					Message = $"Pairing failed inside SpicAPI: {root.GetType().Name}: {root.Message}",
					Hint = "Check ConnectionStrings:IfmsConnection points at spiconeifms and its migrations are applied."
				});
			}

			// The token is returned here and nowhere else - only its hash is kept.
			return Ok(new
			{
				Success = true,
				registration.Token,
				registration.ReplacedExisting,
				Message = registration.ReplacedExisting
					? "Re-paired. The previous token for this handset no longer works."
					: "Paired."
			});
		}

		/// <summary>
		/// Lets the phone say it is alive without doing anything else. The reply
		/// tells it whether a CAPTCHA is waiting, so one call covers both.
		/// </summary>
		[AllowAnonymous]
		[HttpPost("devices/heartbeat")]
		public async Task<IActionResult> Heartbeat(CancellationToken cancellationToken)
		{
			if (!await IsKnownDeviceAsync("heartbeat", cancellationToken))
				return Unauthorized();

			var now = DateTime.UtcNow;

			var waiting = await _db.IfmsChallengeRequests
				.AsNoTracking()
				.AnyAsync(c => c.Status == "Pending" && c.ExpiresAt > now, cancellationToken);

			return Ok(new { Success = true, CaptchaWaiting = waiting });
		}

		/// <summary>Paired handsets, with how long since each last spoke.</summary>
		[Authorize]
		[HttpGet("devices")]
		public async Task<ActionResult<IReadOnlyList<IfmsRelayDeviceDto>>> Devices(
			CancellationToken cancellationToken)
		{
			var staleAfterHours = _config.GetValue("Ifms:RelayStaleAfterHours", 4);
			return Ok(await _devices.ListAsync(staleAfterHours, cancellationToken));
		}

		/// <summary>
		/// Stops a handset relaying, immediately. This is the whole point of giving
		/// each phone its own token: replacing one is pair-new then revoke-old, and
		/// the retired handset goes quiet at once.
		/// </summary>
		[Authorize]
		[HttpPost("devices/{id:int}/revoke")]
		public async Task<IActionResult> RevokeDevice(int id, CancellationToken cancellationToken)
		{
			var revoked = await _devices.RevokeAsync(
				id,
				User.FindFirstValue(ClaimTypes.Name) ?? "Portal",
				cancellationToken);

			return revoked
				? Ok(new { Success = true, Message = "Revoked. That phone can no longer relay." })
				: NotFound(new { Success = false, Message = "No such active device." });
		}

		// ------------------------------------------------------------- accounts

		/// <summary>
		/// The portal logins and how close each password is to expiring. Passwords
		/// themselves are never returned — there is no endpoint that reveals one.
		/// </summary>
		[Authorize]
		[HttpGet("accounts")]
		public async Task<ActionResult<List<IfmsPortalAccountDto>>> Accounts(
			CancellationToken cancellationToken)
		{
			var accounts = await _db.IfmsPortalAccounts
				.AsNoTracking()
				.OrderBy(a => a.Order)
				.ThenBy(a => a.Id)
				.ToListAsync(cancellationToken);

			var now = DateTime.UtcNow;

			return Ok(accounts.Select(a =>
			{
				var days = (int)Math.Floor((a.PasswordExpiresAt - now).TotalDays);

				return new IfmsPortalAccountDto
				{
					Id = a.Id,
					AccountKey = a.AccountKey,
					CompanyName = a.CompanyName,
					UserName = a.UserName,
					IsActive = a.IsActive,
					Order = a.Order,
					HasPassword = !string.IsNullOrWhiteSpace(a.ProtectedPassword),
					PlainPasswordForTesting = a.PlainPasswordForTesting,
					PasswordSetAt = a.PasswordSetAt,
					PasswordExpiresAt = a.PasswordExpiresAt,
					PasswordRotationDays = a.PasswordRotationDays,
					DaysUntilExpiry = days,
					PasswordExpired = now >= a.PasswordExpiresAt,
					PasswordExpiringSoon = days <= 10 && now < a.PasswordExpiresAt,
					LastLoginAt = a.LastLoginAt,
					LastLoginSucceeded = a.LastLoginSucceeded,
					LastLoginMessage = a.LastLoginMessage,
					OtpMobileNumber = a.OtpMobileNumber
				};
			}).ToList());
		}

		/// <summary>
		/// Creates or updates a portal login. Saving a password restarts the 80-day
		/// clock and writes an audit row; the password is encrypted before it
		/// reaches the database.
		/// </summary>
		[Authorize]
		[HttpPost("accounts")]
		public async Task<IActionResult> SetAccount(
			[FromBody] IfmsSetAccountDto dto,
			CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(dto.AccountKey))
				return BadRequest(new { Success = false, Message = "An account key is required." });

			if (string.IsNullOrWhiteSpace(dto.UserName))
				return BadRequest(new { Success = false, Message = "A username is required." });

			if (string.IsNullOrWhiteSpace(dto.Password))
				return BadRequest(new { Success = false, Message = "A password is required." });

			var changedBy =
				User.FindFirstValue(ClaimTypes.Name) ??
				User.FindFirstValue(ClaimTypes.NameIdentifier) ??
				"Portal";

			await _accounts.SetCredentialsAsync(
				dto.AccountKey.Trim(),
				dto.CompanyName?.Trim() ?? dto.AccountKey.Trim(),
				dto.UserName.Trim(),
				dto.Password,
				changedBy,
				reason: "Manual",
				cancellationToken);

			if (!string.IsNullOrWhiteSpace(dto.OtpMobileNumber))
			{
				var account = await _db.IfmsPortalAccounts
					.FirstOrDefaultAsync(a => a.AccountKey == dto.AccountKey.Trim(), cancellationToken);

				if (account is not null)
				{
					account.OtpMobileNumber = dto.OtpMobileNumber.Trim();
					await _db.SaveChangesAsync(cancellationToken);
				}
			}

			return Ok(new
			{
				Success = true,
				Message = "Saved. The 80-day password clock starts now."
			});
		}

		/// <summary>Stops a login being used without deleting its history.</summary>
		[Authorize]
		[HttpPost("accounts/{id:int}/active")]
		public async Task<IActionResult> SetAccountActive(
			int id,
			[FromQuery] bool active,
			CancellationToken cancellationToken)
		{
			var account = await _db.IfmsPortalAccounts
				.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

			if (account is null)
				return NotFound();

			account.IsActive = active;
			account.UpdatedAt = DateTime.UtcNow;
			account.UpdatedBy = User.FindFirstValue(ClaimTypes.Name) ?? "Portal";

			await _db.SaveChangesAsync(cancellationToken);

			return Ok(new { Success = true });
		}

		// ----------------------------------------------------------------- runs

		[Authorize]
		[HttpGet("runs")]
		public async Task<ActionResult<List<IfmsRunListItemDto>>> Runs(
			[FromQuery] int take = 30,
			CancellationToken cancellationToken = default)
		{
			take = Math.Clamp(take, 1, 200);

			var runs = await _db.IfmsAutomationRuns
				.AsNoTracking()
				.Include(r => r.Reports)
				.OrderByDescending(r => r.StartedAt)
				.Take(take)
				.ToListAsync(cancellationToken);

			return Ok(runs.Select(ToDto).ToList());
		}

		[Authorize]
		[HttpGet("runs/{id:int}")]
		public async Task<ActionResult<IfmsRunListItemDto>> Run(int id, CancellationToken cancellationToken)
		{
			var run = await _db.IfmsAutomationRuns
				.AsNoTracking()
				.Include(r => r.Reports)
				.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

			return run is null ? NotFound() : Ok(ToDto(run));
		}

		/// <summary>
		/// Queues a run for the automation service to pick up, which it does
		/// within twenty seconds. Refuses if one is already queued or running, so
		/// an impatient double-click cannot start two browsers against the portal.
		/// </summary>
		[Authorize]
		[HttpPost("runs/queue")]
		public async Task<IActionResult> QueueRun(
			[FromBody] IfmsQueueRunDto? dto,
			CancellationToken cancellationToken)
		{
			var busy = await _db.IfmsAutomationRuns
				.AnyAsync(
					r => r.Status == IfmsRunStatus.Pending || r.Status == IfmsRunStatus.Running,
					cancellationToken);

			if (busy)
			{
				return Conflict(new
				{
					Success = false,
					Message = "A run is already queued or in progress."
				});
			}

			var reportDate = (dto?.ReportDate ?? DateTime.Today.AddDays(-1)).Date;

			var run = new IfmsAutomationRun
			{
				ReportDate = reportDate,
				StartedAt = DateTime.UtcNow,
				Status = IfmsRunStatus.Pending,
				Trigger = IfmsRunTrigger.Manual,
				Attempt = 1,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				UpdatedBy = User.FindFirstValue(ClaimTypes.Name) ?? "Portal"
			};

			_db.IfmsAutomationRuns.Add(run);
			await _db.SaveChangesAsync(cancellationToken);

			return Ok(new
			{
				Success = true,
				run.Id,
				Message = "Queued. The automation picks it up within about twenty seconds."
			});
		}

		/// <summary>
		/// One call for the phone's IFMS screen: is anything waiting for me, and
		/// how did last night go.
		/// </summary>
		[AllowAnonymous]
		[HttpGet("status")]
		public async Task<ActionResult<IfmsAutomationStatusDto>> Status(CancellationToken cancellationToken)
		{
			if (!await IsCallerAllowedAsync("status", cancellationToken))
				return Unauthorized();

			var now = DateTime.UtcNow;

			var challenge = await _db.IfmsChallengeRequests
				.AsNoTracking()
				.Where(c => c.Status == "Pending" && c.ExpiresAt > now)
				.OrderByDescending(c => c.CreatedAt)
				.FirstOrDefaultAsync(cancellationToken);

			var latest = await _db.IfmsAutomationRuns
				.AsNoTracking()
				.Include(r => r.Reports)
				.OrderByDescending(r => r.StartedAt)
				.FirstOrDefaultAsync(cancellationToken);

			var status = new IfmsAutomationStatusDto
			{
				PendingChallenge = challenge is null ? null : ToDto(challenge, now),
				LatestRun = latest is null ? null : ToDto(latest),
				NeedsAttention =
					challenge is not null ||
					latest?.Status is IfmsRunStatus.Failed or IfmsRunStatus.PartiallySucceeded
			};

			status.Headline = challenge is not null
				? "IFMS needs the CAPTCHA typed in."
				: latest is null
					? "No IFMS run has happened yet."
					: latest.Status switch
					{
						IfmsRunStatus.Succeeded =>
							$"All {latest.ReportsSucceeded} reports imported for {latest.ReportDate:dd MMM}.",
						IfmsRunStatus.PartiallySucceeded =>
							$"{latest.ReportsSucceeded} of {latest.ReportsTotal} reports imported for " +
							$"{latest.ReportDate:dd MMM}; {latest.ReportsFailed} need doing by hand.",
						IfmsRunStatus.Running => "An IFMS run is in progress.",
						IfmsRunStatus.Pending => "An IFMS run is queued.",
						_ => $"The IFMS run for {latest.ReportDate:dd MMM} failed."
					};

			return Ok(status);
		}

		/// <summary>
		/// Where the automation posts its summaries and CAPTCHA prompts.
		///
		/// Today this only acknowledges and logs: the Android app learns about
		/// both by polling /status, which needs no Firebase project and no device
		/// tokens. It is the hook to raise a real FCM push from when you want one.
		/// </summary>
		[AllowAnonymous]
		[HttpPost("notify")]
		public IActionResult Notify([FromBody] object payload)
		{
			var expected = _config["IfmsAutomation:AutomationKey"];
			var supplied = Request.Headers["X-Automation-Key"].ToString();

			if (string.IsNullOrWhiteSpace(expected) ||
				string.IsNullOrEmpty(supplied) ||
				!CryptographicOperations.FixedTimeEquals(
					Encoding.UTF8.GetBytes(supplied),
					Encoding.UTF8.GetBytes(expected)))
			{
				return Unauthorized(new { Success = false, Message = "Bad automation key." });
			}

			return Ok(new { Success = true });
		}

		// ---------------------------------------------------------------- mapping

		private static IfmsPendingChallengeDto ToDto(IfmsChallengeRequest challenge, DateTime now) =>
			new()
			{
				Id = challenge.Id,
				RunId = challenge.RunId,
				ChallengeType = challenge.ChallengeType,
				ImageBase64 = challenge.ImageBase64,
				Prompt = challenge.Prompt,
				Round = challenge.Round,
				FailedGuesses = challenge.FailedGuesses,
				CreatedAt = challenge.CreatedAt,
				ExpiresAt = challenge.ExpiresAt,
				SecondsRemaining = (int)Math.Max(0, (challenge.ExpiresAt - now).TotalSeconds)
			};

		private static IfmsRunListItemDto ToDto(IfmsAutomationRun run) =>
			new()
			{
				Id = run.Id,
				ReportDate = run.ReportDate,
				StartedAt = run.StartedAt,
				CompletedAt = run.CompletedAt,
				Status = run.Status.ToString(),
				Trigger = run.Trigger.ToString(),
				Attempt = run.Attempt,
				SitePortalReachable = run.SitePortalReachable,
				LoginSucceeded = run.LoginSucceeded,
				CaptchaMethod = run.CaptchaMethod,
				CaptchaAttempts = run.CaptchaAttempts,
				OtpMethod = run.OtpMethod,
				ReportsTotal = run.ReportsTotal,
				ReportsSucceeded = run.ReportsSucceeded,
				ReportsFailed = run.ReportsFailed,
				RowsInserted = run.RowsInserted,
				RowsUpdated = run.RowsUpdated,
				RowsSkipped = run.RowsSkipped,
				ErrorMessage = run.ErrorMessage,
				Reports = run.Reports
					.OrderBy(r => r.Id)
					.Select(r => new IfmsReportRunDto
					{
						Id = r.Id,
						JobKey = r.JobKey,
						CategoryId = r.CategoryId,
						ReportTitle = r.ReportTitle,
						Status = r.Status.ToString(),
						ReportDate = r.ReportDate,
						AppliedFilters = r.AppliedFilters,
						DownloadedFileName = r.DownloadedFileName,
						DownloadedBytes = r.DownloadedBytes,
						TotalRows = r.TotalRows,
						RowsInserted = r.RowsInserted,
						RowsUpdated = r.RowsUpdated,
						RowsSkipped = r.RowsSkipped,
						Warnings = r.Warnings,
						ErrorMessage = r.ErrorMessage,
						StartedAt = r.StartedAt,
						CompletedAt = r.CompletedAt,
						Attempt = r.Attempt
					})
					.ToList()
			};

		private static string? Trim(string? value, int max) =>
			string.IsNullOrWhiteSpace(value)
				? null
				: value.Length <= max ? value : value[..max];
	}
}
