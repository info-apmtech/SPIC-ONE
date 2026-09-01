using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using SPIC.Ifms.Automation.Options;
using SPIC.Ifms.Automation.Portal.Challenges;

namespace SPIC.Ifms.Automation.Portal
{
	public sealed class LoginResult
	{
		public required bool Success { get; init; }
		public string? FailureReason { get; init; }

		/// <summary>StoredSession, HtmlText, Ocr or Operator.</summary>
		public string? CaptchaMethod { get; init; }
		public int CaptchaAttempts { get; init; }
		public string? OtpMethod { get; init; }
	}

	public sealed class DownloadedReport
	{
		public required string FileName { get; init; }
		public required string FilePath { get; init; }
		public required long Bytes { get; init; }
		public required string Extension { get; init; }
	}

	/// <summary>
	/// Drives a real Chromium against the IFMS portal: signs in, walks the menu
	/// path for each report, applies its filters and captures the download.
	///
	/// One instance owns one browser session and is not thread-safe; the
	/// orchestrator creates one per run and disposes it at the end.
	/// </summary>
	public sealed class IfmsPortalClient : IAsyncDisposable
	{
		private readonly IfmsOptions _options;
		private readonly IEnumerable<ICaptchaSolver> _captchaSolvers;
		private readonly OperatorCaptchaSolver _operatorSolver;
		private readonly IOtpProvider _otpProvider;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly ILogger<IfmsPortalClient> _logger;

		/// <summary>
		/// Which company this browser is signed in as. Set by LoginAsync; the run
		/// creates one client per account because the sessions are separate.
		/// </summary>
		private IfmsAccountCredentials? _account;

		private IPlaywright? _playwright;
		private IBrowser? _browser;
		private IBrowserContext? _context;
		private IPage? _page;

		/// <summary>Steps can move into an iframe; this is what they act on.</summary>
		private IFrame? _activeFrame;

		public IfmsPortalClient(
			IOptions<IfmsOptions> options,
			IEnumerable<ICaptchaSolver> captchaSolvers,
			OperatorCaptchaSolver operatorSolver,
			IOtpProvider otpProvider,
			IServiceScopeFactory scopeFactory,
			ILogger<IfmsPortalClient> logger)
		{
			_options = options.Value;
			_captchaSolvers = captchaSolvers;
			_operatorSolver = operatorSolver;
			_otpProvider = otpProvider;
			_scopeFactory = scopeFactory;
			_logger = logger;
		}

		private IPage Page => _page ?? throw new InvalidOperationException("The browser is not open.");

		private IfmsAccountCredentials Account =>
			_account ?? throw new InvalidOperationException("No portal account has been selected.");

		/// <summary>The frame steps act on: the page itself unless inside an iframe.</summary>
		private IFrame Frame => _activeFrame ?? Page.MainFrame;

		// ---------------------------------------------------------------- login

		public async Task<LoginResult> LoginAsync(
			IfmsAccountCredentials account,
			int runId,
			CancellationToken cancellationToken)
		{
			_account = account;

			_logger.LogInformation(
				"Signing in as {Company} ({UserName}).", account.CompanyName, account.UserName);

			await StartBrowserAsync(cancellationToken);

			if (_options.ReuseStoredSession)
			{
				var reused = await TryStoredSessionAsync(cancellationToken);
				if (reused)
				{
						_logger.LogInformation(
						"Signed in as {Company} with the stored session; no CAPTCHA or OTP needed.",
						account.CompanyName);
					return new LoginResult
					{
						Success = true,
						CaptchaMethod = "StoredSession",
						OtpMethod = "StoredSession"
					};
				}
			}

			var automatic = await TryAutomaticLoginAsync(runId, cancellationToken);
			if (automatic.Success)
				return automatic;

			if (!_options.Captcha.OperatorFallbackEnabled)
				return automatic;

			// Only a CAPTCHA we could not read is worth asking a human about.
			// A wrong password will not get better with a second pair of eyes.
			if (automatic.FailureReason is not null &&
				!automatic.FailureReason.Contains("captcha", StringComparison.OrdinalIgnoreCase))
			{
				return automatic;
			}

			return await TryOperatorLoginAsync(runId, automatic.CaptchaAttempts, cancellationToken);
		}

		/// <summary>
		/// The unattended path: up to Ifms:Captcha:MaxAttempts complete login
		/// attempts, each one starting from a freshly loaded page so the portal
		/// hands out a brand new CAPTCHA every time.
		/// </summary>
		private async Task<LoginResult> TryAutomaticLoginAsync(int runId, CancellationToken cancellationToken)
		{
			var maxAttempts = Math.Max(1, _options.Captcha.MaxAttempts);
			var guesses = new List<string>();
			string? lastFailure = null;
			string? method = null;

			for (var attempt = 1; attempt <= maxAttempts; attempt++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				_logger.LogInformation("Login attempt {Attempt} of {Max}.", attempt, maxAttempts);

				await GotoLoginAsync(cancellationToken);

				var challenge = await CaptureCaptchaAsync(attempt, runId, guesses, cancellationToken);
				CaptchaAnswer? answer = null;

				if (challenge is not null)
				{
					answer = await SolveAutomaticallyAsync(challenge, cancellationToken);

					if (answer is null)
					{
						lastFailure = "The CAPTCHA could not be read automatically.";
						_logger.LogWarning("No automatic solver could read attempt {Attempt}'s CAPTCHA.", attempt);
						continue;
					}

					guesses.Add(answer.Value);
					method = answer.Method;
				}

				var outcome = await SubmitCredentialsAsync(runId, answer?.Value, cancellationToken);

				if (outcome.Success)
				{
					await SaveSessionAsync(cancellationToken);
					return new LoginResult
					{
						Success = true,
						CaptchaMethod = method ?? "NotRequired",
						CaptchaAttempts = attempt,
						OtpMethod = outcome.OtpMethod
					};
				}

				lastFailure = outcome.FailureReason;

				if (!outcome.RetryWorthwhile)
				{
					_logger.LogError("Login failed and retrying will not help: {Reason}", lastFailure);
					return new LoginResult
					{
						Success = false,
						FailureReason = lastFailure,
						CaptchaMethod = method,
						CaptchaAttempts = attempt
					};
				}

				_logger.LogWarning("Attempt {Attempt} rejected: {Reason}", attempt, lastFailure);
			}

			return new LoginResult
			{
				Success = false,
				FailureReason =
					$"The CAPTCHA was not solved in {maxAttempts} automatic attempts. " +
					$"Last response from the portal: {lastFailure}",
				CaptchaMethod = method,
				CaptchaAttempts = maxAttempts
			};
		}

		/// <summary>
		/// The human path, entered only after every automatic attempt is spent.
		/// Each round loads a fresh login page, pushes that CAPTCHA to the app and
		/// waits. If the answer arrives after the portal has expired the page, the
		/// next round simply asks again with a new image — which is what makes a
		/// reply at 7am still useful.
		/// </summary>
		private async Task<LoginResult> TryOperatorLoginAsync(
			int runId,
			int automaticAttempts,
			CancellationToken cancellationToken)
		{
			var maxRounds = Math.Max(1, _options.Captcha.OperatorMaxRounds);
			string? lastFailure = null;

			for (var round = 1; round <= maxRounds; round++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				await GotoLoginAsync(cancellationToken);

				var challenge = await CaptureCaptchaAsync(round, runId, new List<string>(), cancellationToken);
				if (challenge is null)
				{
					lastFailure = "The CAPTCHA element disappeared from the login page.";
					break;
				}

				var answer = await _operatorSolver.SolveAsync(challenge, round, cancellationToken);
				if (answer is null)
				{
					lastFailure = round == 1
						? "Nobody answered the CAPTCHA prompt in the app."
						: "The follow-up CAPTCHA prompt went unanswered.";
					break;
				}

				var outcome = await SubmitCredentialsAsync(runId, answer.Value, cancellationToken);

				if (answer.ChallengeRequestId is int requestId)
					await _operatorSolver.ReportOutcomeAsync(requestId, outcome.Success, cancellationToken);

				if (outcome.Success)
				{
					await SaveSessionAsync(cancellationToken);
					_logger.LogInformation("Signed in using the CAPTCHA answered from the app (round {Round}).", round);

					return new LoginResult
					{
						Success = true,
						CaptchaMethod = "Operator",
						CaptchaAttempts = automaticAttempts + round,
						OtpMethod = outcome.OtpMethod
					};
				}

				lastFailure = outcome.FailureReason;

				if (!outcome.RetryWorthwhile)
					break;

				_logger.LogWarning(
					"The CAPTCHA answered from the app was rejected (round {Round}); asking again with a fresh image.",
					round);
			}

			return new LoginResult
			{
				Success = false,
				FailureReason = lastFailure ?? "The CAPTCHA could not be solved.",
				CaptchaMethod = "Operator",
				CaptchaAttempts = automaticAttempts + maxRounds
			};
		}

		private async Task<CaptchaAnswer?> SolveAutomaticallyAsync(
			CaptchaChallenge challenge,
			CancellationToken cancellationToken)
		{
			foreach (var name in _options.Captcha.Strategies)
			{
				var solver = _captchaSolvers.FirstOrDefault(
					s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

				if (solver is null)
				{
					_logger.LogWarning("No CAPTCHA solver is registered under the name {Name}.", name);
					continue;
				}

				var answer = await solver.SolveAsync(challenge, cancellationToken);
				if (answer is not null)
					return answer;
			}

			return null;
		}

		private sealed record SubmitOutcome(
			bool Success,
			bool RetryWorthwhile,
			string? FailureReason,
			string? OtpMethod);

		private async Task<SubmitOutcome> SubmitCredentialsAsync(
			int runId,
			string? captcha,
			CancellationToken cancellationToken)
		{
			var selectors = _options.Selectors;

			await Frame.FillAsync(selectors.UserNameInput, Account.UserName);
			await Frame.FillAsync(selectors.PasswordInput, Account.Password);

			if (captcha is not null && !string.IsNullOrWhiteSpace(selectors.CaptchaInput))
				await Frame.FillAsync(selectors.CaptchaInput, captcha);

			// The portal sends the OTP SMS as a side effect of this click, so the
			// clock for "which SMS is ours" starts here.
			var otpRequestedAt = DateTime.UtcNow;

			await Frame.ClickAsync(selectors.LoginSubmit);
			await WaitForSettleAsync(cancellationToken);

			if (await IsLoggedInAsync(cancellationToken))
				return new SubmitOutcome(true, false, null, "NotRequired");

			var otpMethod = await HandleOtpStepAsync(runId, otpRequestedAt, cancellationToken);

			if (otpMethod is not null)
			{
				if (await IsLoggedInAsync(cancellationToken))
					return new SubmitOutcome(true, false, null, otpMethod);

				// The code was entered and still no dashboard. Worth one more whole
				// attempt, but say plainly what happened rather than blaming the
				// CAPTCHA for it.
				await CaptureDiagnosticAsync("otp-rejected", cancellationToken);

				var otpError = await ReadLoginErrorAsync();

				return new SubmitOutcome(
					Success: false,
					RetryWorthwhile: true,
					FailureReason: otpError is null
						? "The OTP was submitted but the portal did not sign us in. The relayed code " +
						  "may have been the wrong one, or the OTP submit button was not clicked."
						: $"OTP rejected: {otpError}",
					OtpMethod: otpMethod);
			}

			var error = await ReadLoginErrorAsync();
			var isCaptchaError = error is not null && selectors.CaptchaErrorMarkers.Any(
				marker => error.Contains(marker, StringComparison.OrdinalIgnoreCase));

			await CaptureDiagnosticAsync("login-failed", cancellationToken);

			// Never reaching the OTP screen is itself diagnostic on this portal:
			// the OTP always follows a good username, password and CAPTCHA, so if
			// it did not appear, one of those three was rejected.
			var missedOtpStep =
				_options.Otp.Required &&
				otpMethod is null &&
				!string.Equals(_options.Otp.Strategy, "NotRequired", StringComparison.OrdinalIgnoreCase);

			string reason;

			if (error is not null)
			{
				reason = isCaptchaError ? $"captcha rejected: {error}" : error;
			}
			else if (missedOtpStep)
			{
				reason =
					"The portal did not reach the OTP step and showed no error. The CAPTCHA was " +
					"most likely wrong; if this repeats on every attempt, check the password.";
			}
			else
			{
				reason =
					"The portal neither signed us in nor showed an error. The 'LoggedIn' selector " +
					"may be wrong.";
			}

			return new SubmitOutcome(
				Success: false,
				RetryWorthwhile: isCaptchaError || missedOtpStep || error is null,
				FailureReason: reason,
				OtpMethod: otpMethod);
		}

		/// <summary>
		/// Handles the one-time password step. Returns the method used, or null
		/// when no OTP step appeared at all.
		///
		/// On this portal the OTP is asked for on every fresh login — username,
		/// password and CAPTCHA first, then the code — so this is the normal path,
		/// not an edge case. The paired Android phone forwards the SMS and the
		/// login continues with nobody awake.
		/// </summary>
		private async Task<string?> HandleOtpStepAsync(
			int runId,
			DateTime requestedAtUtc,
			CancellationToken cancellationToken)
		{
			if (string.Equals(_options.Otp.Strategy, "NotRequired", StringComparison.OrdinalIgnoreCase))
				return null;

			var otpBox = await FindOtpFieldAsync(cancellationToken);

			if (otpBox is null)
			{
				// Either the credentials were rejected before the OTP step, or the
				// field could not be identified. The caller reads the page error to
				// tell those apart.
				return null;
			}

			// Report the field's real id and name, so the guessing can stop. Until
			// Ifms:Selectors:OtpInput is set this is auto-detected on every login,
			// and auto-detection is a thing to retire rather than rely on.
			var otpId = await otpBox.GetAttributeAsync("id");
			var otpName = await otpBox.GetAttributeAsync("name");

			_logger.LogInformation(
				"The portal is asking for an OTP. The field is id='{Id}' name='{Name}' — " +
				"put \"#{Id}\" in Ifms:Selectors:OtpInput and it will stop guessing.",
				otpId ?? "(none)", otpName ?? "(none)", otpId ?? "");

			var otp = await _otpProvider.WaitForOtpAsync(
				requestedAtUtc, runId, cancellationToken, Account.AccountKey);

			if (otp is null && _options.Otp.AllowResend && !string.IsNullOrWhiteSpace(_options.Selectors.OtpResend))
			{
				_logger.LogWarning("No OTP arrived; asking the portal to resend once.");

				var resentAt = DateTime.UtcNow;
				await Frame.ClickAsync(_options.Selectors.OtpResend);
				otp = await _otpProvider.WaitForOtpAsync(
					resentAt, runId, cancellationToken, Account.AccountKey);
			}

			if (otp is null)
			{
				await CaptureDiagnosticAsync("otp-timeout", cancellationToken);
				return null;
			}

			await otpBox.FillAsync(otp);
			await SubmitOtpAsync(otpBox, cancellationToken);

			// Verifying raises the UIDAI alert, which the dialog handler accepts.
			// The navigation to the landing page only happens after that, so give
			// it a moment rather than testing for "logged in" too early.
			await WaitForSettleAsync(cancellationToken);

			return "SmsRelay";
		}

		/// <summary>
		/// Locates the OTP box, preferring the configured selector and falling back
		/// to finding it on the page.
		///
		/// The fallback exists because the OTP screen only appears after a real
		/// successful login, so its markup could not be inspected in advance. Rather
		/// than block on that, this looks for an input whose id, name or placeholder
		/// says "otp" and, failing that, for the single empty text box on a page
		/// that is clearly the OTP step. Fill in Ifms:Selectors:OtpInput once you
		/// know it and none of this guessing runs.
		/// </summary>
		private async Task<ILocator?> FindOtpFieldAsync(CancellationToken cancellationToken)
		{
			var configured = _options.Selectors.OtpInput;

			if (!string.IsNullOrWhiteSpace(configured))
			{
				var located = Frame.Locator(configured).First;

				try
				{
					await located.WaitForAsync(new LocatorWaitForOptions
					{
						State = WaitForSelectorState.Visible,
						Timeout = _options.Otp.StepTimeoutMs
					});

					return located;
				}
				catch (TimeoutException)
				{
					_logger.LogWarning(
						"The configured OTP selector '{Selector}' did not appear; trying to find the field.",
						configured);
				}
			}

			// Give the OTP page a moment to render before hunting for it.
			try
			{
				await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded,
					new PageWaitForLoadStateOptions { Timeout = _options.Otp.StepTimeoutMs });
			}
			catch (TimeoutException)
			{
				// Not fatal; the page may already be settled.
			}

			// Named like an OTP field. Covers otp, userOtp, txtOTP, one_time_password.
			var byName = Frame.Locator(
				"input[id*='otp' i], input[name*='otp' i], input[placeholder*='otp' i], " +
				"input[id*='onetime' i], input[name*='onetime' i], " +
				"input[id*='one_time' i], input[name*='one_time' i]");

			if (await FirstVisibleAsync(byName) is { } named)
			{
				_logger.LogInformation("Found the OTP field by name. Put its selector in Ifms:Selectors:OtpInput.");
				return named;
			}

			// Nothing named helpfully: fall back to the shape of the page. An OTP
			// step is characteristically a single empty text box, so exactly one
			// visible candidate is a safe bet — and more than one is not, so this
			// deliberately refuses rather than guessing wrong.
			var candidates = Frame.Locator(
				"input[type='text']:visible, input[type='tel']:visible, " +
				"input[type='number']:visible, input[type='password']:visible");

			var count = await candidates.CountAsync();
			var visible = new List<ILocator>();

			for (var i = 0; i < count; i++)
			{
				var candidate = candidates.Nth(i);

				if (await candidate.IsVisibleAsync() &&
					string.IsNullOrEmpty(await candidate.InputValueAsync()))
				{
					visible.Add(candidate);
				}
			}

			if (visible.Count == 1)
			{
				_logger.LogWarning(
					"Guessed the OTP field as the only empty box on the page. Confirm it and set " +
					"Ifms:Selectors:OtpInput so this guess is never needed again.");

				return visible[0];
			}

			if (visible.Count > 1)
			{
				_logger.LogError(
					"Found {Count} possible OTP fields and will not guess between them. " +
					"Set Ifms:Selectors:OtpInput.", visible.Count);

				await CaptureDiagnosticAsync("otp-ambiguous", cancellationToken);
			}

			return null;
		}

		private async Task SubmitOtpAsync(ILocator otpBox, CancellationToken cancellationToken)
		{
			var configured = _options.Selectors.OtpSubmit;

			if (!string.IsNullOrWhiteSpace(configured))
			{
				try
				{
					await Frame.ClickAsync(configured, new FrameClickOptions { Timeout = 10_000 });
					return;
				}
				catch (TimeoutException)
				{
					_logger.LogWarning(
						"The configured OTP submit '{Selector}' was not clickable; pressing Enter instead.",
						configured);
				}
			}
			else
			{
				// The obvious submit on the OTP form, if there is one.
				var submit = Frame.Locator(
					"input[type='submit']:visible, button[type='submit']:visible, " +
					"button:has-text('Verify'), button:has-text('Submit'), " +
					"input[value='Verify' i], input[value='Submit' i]");

				if (await FirstVisibleAsync(submit) is { } button)
				{
					await button.ClickAsync();
					return;
				}
			}

			await otpBox.PressAsync("Enter");
		}

		private static async Task<ILocator?> FirstVisibleAsync(ILocator locator)
		{
			var count = await locator.CountAsync();

			for (var i = 0; i < count; i++)
			{
				var candidate = locator.Nth(i);

				if (await candidate.IsVisibleAsync())
					return candidate;
			}

			return null;
		}

		private async Task<CaptchaChallenge?> CaptureCaptchaAsync(
			int attempt,
			int runId,
			List<string> previousGuesses,
			CancellationToken cancellationToken)
		{
			var selectors = _options.Selectors;
			string? text = null;
			byte[]? image = null;

			if (!string.IsNullOrWhiteSpace(selectors.CaptchaText))
			{
				try
				{
					text = await Frame.Locator(selectors.CaptchaText).First.InnerTextAsync(
						new LocatorInnerTextOptions { Timeout = 5_000 });
				}
				catch (TimeoutException)
				{
					// Falls through to the image path.
				}
			}

			if (!string.IsNullOrWhiteSpace(selectors.CaptchaImage))
			{
				try
				{
					var element = Frame.Locator(selectors.CaptchaImage).First;
					await element.WaitForAsync(new LocatorWaitForOptions
					{
						State = WaitForSelectorState.Visible,
						Timeout = 10_000
					});

					image = await element.ScreenshotAsync(new LocatorScreenshotOptions
					{
						Type = ScreenshotType.Png
					});
				}
				catch (TimeoutException)
				{
					// Falls through: some portals only show the CAPTCHA on retry.
				}
			}

			if (text is null && image is null)
			{
				_logger.LogInformation("No CAPTCHA is present on the login page.");
				return null;
			}

			return new CaptchaChallenge
			{
				Text = text,
				ImagePng = image,
				Attempt = attempt,
				RunId = runId,
				AccountKey = Account.AccountKey,
				CompanyName = Account.CompanyName,
				FailedGuesses = previousGuesses.Count == 0
					? null
					: string.Join(", ", previousGuesses.TakeLast(5))
			};
		}

		/// <summary>
		/// Whether the portal still considers us signed in.
		///
		/// Worth asking mid-run now that fetching every state for both companies
		/// takes hours rather than minutes: a session that lapses at 05:30 would
		/// otherwise fail every remaining report in silence, and the morning would
		/// show a hundred identical errors with no clue that one expiry caused
		/// them all.
		/// </summary>
		public Task<bool> IsSignedInAsync(CancellationToken cancellationToken) =>
			IsLoggedInAsync(cancellationToken);

		private async Task<bool> IsLoggedInAsync(CancellationToken cancellationToken)
		{
			try
			{
				await Page.Locator(_options.Selectors.LoggedIn).First.WaitForAsync(
					new LocatorWaitForOptions
					{
						State = WaitForSelectorState.Visible,
						Timeout = 15_000
					});

				return true;
			}
			catch (TimeoutException)
			{
				return false;
			}
		}

		private async Task<string?> ReadLoginErrorAsync()
		{
			if (string.IsNullOrWhiteSpace(_options.Selectors.LoginError))
				return null;

			try
			{
				var locator = Page.Locator(_options.Selectors.LoginError);
				var count = await locator.CountAsync();

				for (var i = 0; i < count; i++)
				{
					var text = (await locator.Nth(i).InnerTextAsync()).Trim();
					if (!string.IsNullOrWhiteSpace(text))
						return text;
				}
			}
			catch (Exception ex)
			{
				_logger.LogDebug("Could not read the login error text: {Message}", ex.Message);
			}

			return null;
		}

		// -------------------------------------------------------------- reports

		/// <summary>
		/// Walks a job's configured steps and captures the file its download step
		/// produces. Everything portal-specific lives in configuration, so a menu
		/// change on the IFMS side is fixed without touching this method.
		/// </summary>
		/// <summary>
		/// Reads the values a job should loop over off a dropdown on the report
		/// page. Used only when the configuration does not list them, so that a job
		/// can be written once without spelling out every state.
		/// </summary>
		public async Task<IReadOnlyList<string>> DiscoverLoopValuesAsync(
			ReportJob job,
			ReportJobLoop loop,
			RunTokens tokens,
			CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(loop.DiscoverFromSelector))
				return Array.Empty<string>();

			var entry = job.Steps.FirstOrDefault(
				s => s.Action.Equals("goto", StringComparison.OrdinalIgnoreCase));

			if (entry is null)
			{
				throw new InvalidOperationException(
					$"Job '{job.Key}' asks for discovered loop values but has no goto step " +
					$"to find the dropdown on.");
			}

			_activeFrame = null;
			await ExecuteStepAsync(entry, tokens, cancellationToken);

			var options = await Frame.Locator($"{loop.DiscoverFromSelector} option").AllInnerTextsAsync();

			var values = options
				.Select(o => o.Trim())
				.Where(o => o.Length > 0)
				.Where(o => !loop.ExcludeLabels.Any(
					x => o.Contains(x, StringComparison.OrdinalIgnoreCase)))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			_logger.LogInformation(
				"Discovered {Count} values for {JobKey} from {Selector}.",
				values.Count, job.Key, loop.DiscoverFromSelector);

			return values;
		}

		public async Task<DownloadedReport> DownloadReportAsync(
			ReportJob job,
			RunTokens tokens,
			string targetDirectory,
			CancellationToken cancellationToken)
		{
			_activeFrame = null;

			foreach (var step in job.Steps)
				await ExecuteStepAsync(step, tokens, cancellationToken);

			Directory.CreateDirectory(targetDirectory);

			if (!string.IsNullOrWhiteSpace(job.DirectDownloadUrl))
				return await FetchDirectAsync(job, tokens, targetDirectory, cancellationToken);

			if (job.DownloadStep is null)
			{
				throw new InvalidOperationException(
					$"Report job '{job.Key}' has neither a DownloadStep nor a DirectDownloadUrl.");
			}

			var waitForDownload = Page.WaitForDownloadAsync(new PageWaitForDownloadOptions
			{
				Timeout = _options.Browser.DownloadTimeoutMs
			});

			await ExecuteStepAsync(job.DownloadStep, tokens, cancellationToken);

			var download = await waitForDownload;

			var suggested = download.SuggestedFilename;
			var extension = Path.GetExtension(suggested);
			if (string.IsNullOrWhiteSpace(extension))
			{
				extension = job.ExpectedExtension;
				suggested = $"{job.Key}{extension}";
			}

			var fileName = BuildFileName(job, tokens, extension);
			var path = Path.Combine(targetDirectory, fileName);

			await download.SaveAsAsync(path);

			var bytes = new FileInfo(path).Length;

			_logger.LogInformation(
				"Downloaded {Suggested} for {JobKey} ({Bytes:N0} bytes).",
				suggested, job.Key, bytes);

			return new DownloadedReport
			{
				FileName = suggested,
				FilePath = path,
				Bytes = bytes,
				Extension = extension.ToLowerInvariant()
			};
		}

		/// <summary>
		/// Pulls a file straight from a URL using the browser's own session, for
		/// portals that expose the export as a plain link once filters are set.
		/// </summary>
		private async Task<DownloadedReport> FetchDirectAsync(
			ReportJob job,
			RunTokens tokens,
			string targetDirectory,
			CancellationToken cancellationToken)
		{
			var url = Absolute(tokens.Resolve(job.DirectDownloadUrl)!);

			var response = await Page.APIRequest.GetAsync(url, new APIRequestContextOptions
			{
				Timeout = _options.Browser.DownloadTimeoutMs
			});

			if (!response.Ok)
			{
				throw new InvalidOperationException(
					$"Direct download of '{job.Key}' returned HTTP {response.Status} from {url}.");
			}

			var body = await response.BodyAsync();
			var extension = job.ExpectedExtension;
			var fileName = $"{job.Key}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
			var path = Path.Combine(targetDirectory, fileName);

			await File.WriteAllBytesAsync(path, body, cancellationToken);

			return new DownloadedReport
			{
				FileName = Path.GetFileName(new Uri(url).LocalPath) is { Length: > 0 } n ? n : fileName,
				FilePath = path,
				Bytes = body.Length,
				Extension = extension.ToLowerInvariant()
			};
		}

		private async Task ExecuteStepAsync(
			PortalStep step,
			RunTokens tokens,
			CancellationToken cancellationToken)
		{
			var selector = tokens.Resolve(step.Selector);
			var value = tokens.Resolve(step.Value);
			var timeout = (float)(step.TimeoutMs ?? _options.Browser.ActionTimeoutMs);

			var label = step.Description ?? $"{step.Action} {selector ?? value}";
			_logger.LogDebug("Step: {Label}", label);

			try
			{
				switch (step.Action.ToLowerInvariant())
				{
					case "goto":
						await Page.GotoAsync(Absolute(value!), new PageGotoOptions
						{
							Timeout = _options.Browser.NavigationTimeoutMs,
							WaitUntil = WaitUntilState.DOMContentLoaded
						});
						_activeFrame = null;
						break;

					case "click":
						await Frame.ClickAsync(selector!, new FrameClickOptions { Timeout = timeout });
						break;

					case "fill":
						await Frame.FillAsync(selector!, value ?? string.Empty,
							new FrameFillOptions { Timeout = timeout });
						break;

					case "select":
						await Frame.SelectOptionAsync(selector!, new SelectOptionValue { Value = value },
							new FrameSelectOptionOptions { Timeout = timeout });
						break;

					case "selecttext":
						await Frame.SelectOptionAsync(selector!, new SelectOptionValue { Label = value },
							new FrameSelectOptionOptions { Timeout = timeout });
						break;

					case "check":
						await Frame.CheckAsync(selector!, new FrameCheckOptions { Timeout = timeout });
						break;

					case "uncheck":
						await Frame.UncheckAsync(selector!, new FrameUncheckOptions { Timeout = timeout });
						break;

					case "press":
						await Frame.PressAsync(selector!, value!, new FramePressOptions { Timeout = timeout });
						break;

					case "waitfor":
						await Frame.WaitForSelectorAsync(selector!, new FrameWaitForSelectorOptions
						{
							State = WaitForSelectorState.Visible,
							Timeout = timeout
						});
						break;

					case "waithidden":
						await Frame.WaitForSelectorAsync(selector!, new FrameWaitForSelectorOptions
						{
							State = WaitForSelectorState.Hidden,
							Timeout = timeout
						});
						break;

					case "wait":
						await Task.Delay(step.TimeoutMs ?? 1_000, cancellationToken);
						break;

					case "frame":
						_activeFrame = Page.FrameLocator(selector!) is not null
							? Page.Frames.FirstOrDefault(f => f.Name == selector || f.Url.Contains(selector!))
							: null;

						if (_activeFrame is null)
						{
							throw new InvalidOperationException(
								$"No iframe matched '{selector}' on {Page.Url}.");
						}
						break;

					case "mainframe":
						_activeFrame = null;
						break;

					case "eval":
						await Frame.EvaluateAsync(value!);
						break;

					default:
						throw new InvalidOperationException($"Unknown portal step action '{step.Action}'.");
				}
			}
			catch (Exception ex) when (step.Optional)
			{
				_logger.LogDebug("Optional step '{Label}' did not apply: {Message}", label, ex.Message);
			}
			catch (Exception)
			{
				await CaptureDiagnosticAsync($"step-{step.Action}", cancellationToken);
				throw;
			}
		}

		// --------------------------------------------------------------- browser

		private async Task StartBrowserAsync(CancellationToken cancellationToken)
		{
			if (_page is not null)
				return;

			_playwright = await Microsoft.Playwright.Playwright.CreateAsync();

			_browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
			{
				Headless = _options.Browser.Headless,
				Channel = string.IsNullOrWhiteSpace(_options.Browser.Channel) ? null : _options.Browser.Channel,
				SlowMo = _options.Browser.SlowMoMs == 0 ? null : _options.Browser.SlowMoMs,
				Args = new[]
				{
					// Required inside most containers, harmless on a plain VM.
					"--disable-dev-shm-usage",
					"--no-sandbox"
				}
			});

			_context = await NewContextAsync(storageState: null);
			_page = await _context.NewPageAsync();

			ConfigurePage(_page);
		}

		/// <summary>
		/// Wires up a page the way every page in this session needs to behave.
		///
		/// The dialog handler is the part that matters: after the OTP is verified
		/// the portal throws a native alert about UIDAI L1-PoS devices, and until
		/// it is dismissed the page does not move on. Playwright would auto-dismiss
		/// it, but relying on that leaves no trace in the log when a new dialog
		/// appears, so this accepts explicitly and says what it said.
		/// </summary>
		private void ConfigurePage(IPage page)
		{
			page.SetDefaultTimeout(_options.Browser.ActionTimeoutMs);
			page.SetDefaultNavigationTimeout(_options.Browser.NavigationTimeoutMs);

			page.Dialog += async (_, dialog) =>
			{
				_logger.LogInformation(
					"Dismissing a {Type} dialog from the portal: {Message}",
					dialog.Type, dialog.Message);

				try
				{
					await dialog.AcceptAsync();
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Could not dismiss the dialog.");
				}
			};
		}

		private Task<IBrowserContext> NewContextAsync(string? storageState) =>
			_browser!.NewContextAsync(new BrowserNewContextOptions
			{
				AcceptDownloads = true,
				IgnoreHTTPSErrors = _options.Browser.IgnoreHttpsErrors,
				UserAgent = _options.Browser.UserAgent,
				ViewportSize = new ViewportSize { Width = 1600, Height = 1000 },
				StorageState = storageState
			});

		private async Task GotoLoginAsync(CancellationToken cancellationToken)
		{
			_activeFrame = null;

			await Page.GotoAsync(Absolute(_options.LoginPath), new PageGotoOptions
			{
				Timeout = _options.Browser.NavigationTimeoutMs,
				WaitUntil = WaitUntilState.DOMContentLoaded
			});

			await WaitForSettleAsync(cancellationToken);
		}

		/// <summary>
		/// Waits for the network to go quiet, but never fails a run over it —
		/// portals with long-polling widgets never reach a truly idle network.
		/// </summary>
		private async Task WaitForSettleAsync(CancellationToken cancellationToken)
		{
			try
			{
				await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
				{
					Timeout = 10_000
				});
			}
			catch (TimeoutException)
			{
				await Task.Delay(500, cancellationToken);
			}
		}

		// --------------------------------------------------------------- session

		private async Task<bool> TryStoredSessionAsync(CancellationToken cancellationToken)
		{
			string? storageState;

			await using (var scope = _scopeFactory.CreateAsyncScope())
			{
				var db = scope.ServiceProvider.GetRequiredService<IfmsDbContext>();

				storageState = await db.IfmsPortalSessions
					.AsNoTracking()
					.Where(s => s.IsActive && s.PortalUserName == Account.UserName)
					.OrderByDescending(s => s.CapturedAt)
					.Select(s => s.StorageStateJson)
					.FirstOrDefaultAsync(cancellationToken);
			}

			if (string.IsNullOrWhiteSpace(storageState))
				return false;

			try
			{
				await _page!.CloseAsync();
				await _context!.CloseAsync();

				_context = await NewContextAsync(storageState);
				_page = await _context.NewPageAsync();
				ConfigurePage(_page);

				await GotoLoginAsync(cancellationToken);

				if (await IsLoggedInAsync(cancellationToken))
				{
					await TouchSessionAsync(cancellationToken);
					return true;
				}

				_logger.LogInformation("The stored session has expired; falling back to a full login.");
				await InvalidateSessionAsync("Portal no longer accepts the stored cookies.", cancellationToken);
				return false;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Could not reuse the stored session; falling back to a full login.");
				await InvalidateSessionAsync(ex.Message, cancellationToken);
				return false;
			}
		}

		private async Task SaveSessionAsync(CancellationToken cancellationToken)
		{
			if (!_options.ReuseStoredSession || _context is null)
				return;

			try
			{
				var state = await _context.StorageStateAsync();

				await using var scope = _scopeFactory.CreateAsyncScope();
				var db = scope.ServiceProvider.GetRequiredService<IfmsDbContext>();

				var existing = await db.IfmsPortalSessions
					.Where(s => s.PortalUserName == Account.UserName && s.IsActive)
					.ToListAsync(cancellationToken);

				foreach (var session in existing)
				{
					session.IsActive = false;
					session.InvalidatedAt = DateTime.UtcNow;
					session.InvalidationReason = "Replaced by a newer login.";
				}

				db.IfmsPortalSessions.Add(new IfmsPortalSession
				{
					PortalUserName = Account.UserName,
					StorageStateJson = state,
					CapturedAt = DateTime.UtcNow,
					LastValidatedAt = DateTime.UtcNow,
					IsActive = true
				});

				await db.SaveChangesAsync(cancellationToken);
			}
			catch (Exception ex)
			{
				// Losing the session cache costs one CAPTCHA tomorrow, nothing more.
				_logger.LogWarning(ex, "Could not store the portal session.");
			}
		}

		private async Task TouchSessionAsync(CancellationToken cancellationToken)
		{
			await using var scope = _scopeFactory.CreateAsyncScope();
			var db = scope.ServiceProvider.GetRequiredService<IfmsDbContext>();

			var session = await db.IfmsPortalSessions
				.Where(s => s.IsActive && s.PortalUserName == Account.UserName)
				.OrderByDescending(s => s.CapturedAt)
				.FirstOrDefaultAsync(cancellationToken);

			if (session is null)
				return;

			session.LastValidatedAt = DateTime.UtcNow;
			await db.SaveChangesAsync(cancellationToken);
		}

		private async Task InvalidateSessionAsync(string reason, CancellationToken cancellationToken)
		{
			await using var scope = _scopeFactory.CreateAsyncScope();
			var db = scope.ServiceProvider.GetRequiredService<IfmsDbContext>();

			var sessions = await db.IfmsPortalSessions
				.Where(s => s.IsActive && s.PortalUserName == Account.UserName)
				.ToListAsync(cancellationToken);

			foreach (var session in sessions)
			{
				session.IsActive = false;
				session.InvalidatedAt = DateTime.UtcNow;
				session.InvalidationReason = reason.Length > 400 ? reason[..400] : reason;
			}

			await db.SaveChangesAsync(cancellationToken);
		}

		// ----------------------------------------------------------- diagnostics

		/// <summary>
		/// A screenshot plus the raw HTML at the moment something broke. Without
		/// these, debugging a headless failure on a server is guesswork.
		/// </summary>
		private async Task CaptureDiagnosticAsync(string label, CancellationToken cancellationToken)
		{
			if (!_options.Browser.ScreenshotOnFailure || _page is null)
				return;

			try
			{
				var root = _options.Browser.DiagnosticsRoot;
				if (!Path.IsPathRooted(root))
					root = Path.Combine(AppContext.BaseDirectory, root);

				var folder = Path.Combine(root, DateTime.Now.ToString("yyyy-MM-dd"));
				Directory.CreateDirectory(folder);

				var stamp = DateTime.Now.ToString("HHmmss");
				var pngPath = Path.Combine(folder, $"{stamp}_{label}.png");
				var htmlPath = Path.Combine(folder, $"{stamp}_{label}.html");

				await _page.ScreenshotAsync(new PageScreenshotOptions { Path = pngPath, FullPage = true });
				await File.WriteAllTextAsync(htmlPath, await _page.ContentAsync(), cancellationToken);

				_logger.LogInformation("Saved failure diagnostics to {Path}.", pngPath);
			}
			catch (Exception ex)
			{
				_logger.LogDebug("Could not capture diagnostics: {Message}", ex.Message);
			}
		}

		/// <summary>
		/// The name the file is archived under. A predictable name matters: these
		/// are kept for months, and "report (7).csv" tells nobody which state it
		/// was. Anything unusable in a path is replaced rather than rejected.
		/// </summary>
		private static string BuildFileName(ReportJob job, RunTokens tokens, string extension)
		{
			var template = job.FileNameTemplate;

			var stem = string.IsNullOrWhiteSpace(template)
				? $"{job.Key}_{DateTime.Now:yyyyMMdd_HHmmss}"
				: tokens.Resolve(template)!;

			foreach (var bad in Path.GetInvalidFileNameChars())
				stem = stem.Replace(bad, '_');

			stem = stem.Replace(' ', '_').Trim('_');

			if (stem.Length == 0)
				stem = job.Key;

			return stem + extension;
		}

		private string Absolute(string pathOrUrl) =>
			IfmsUrl.Absolute(_options.BaseUrl, pathOrUrl);

		public async ValueTask DisposeAsync()
		{
			try
			{
				if (_context is not null)
					await _context.CloseAsync();

				if (_browser is not null)
					await _browser.CloseAsync();
			}
			catch (Exception ex)
			{
				_logger.LogDebug("Browser teardown reported: {Message}", ex.Message);
			}
			finally
			{
				_playwright?.Dispose();
				_playwright = null;
				_browser = null;
				_context = null;
				_page = null;
			}
		}
	}
}
