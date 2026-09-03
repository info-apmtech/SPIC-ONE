using System.Collections.Generic;

namespace SPIC.Ifms.Automation.Options
{
	/// <summary>
	/// Everything the automation needs to know about the IFMS portal.
	/// Deliberately configuration-driven: when the portal changes a field id or a
	/// menu path you edit appsettings.json and restart, you do not rebuild.
	/// </summary>
	public sealed class IfmsOptions
	{
		public const string SectionName = "Ifms";

		/// <summary>Portal root, e.g. https://www.urvarak.nic.in</summary>
		public string BaseUrl { get; set; } = string.Empty;

		/// <summary>Login page, relative to <see cref="BaseUrl"/> or absolute.</summary>
		public string LoginPath { get; set; } = "/";

		/// <summary>
		/// A page that exists only behind the login, used to test whether a
		/// stored session is still alive. Never the login page: opening that
		/// starts a fresh portal session and drops the one being tested.
		/// </summary>
		public string SessionProbePath { get; set; } = "/mFMS/home.action";

		/// <summary>
		/// Cheap URL hit used to decide "is the site up yet" before a real login.
		/// Defaults to the login page.
		/// </summary>
		public string? HealthCheckPath { get; set; }

		/// <summary>
		/// Credentials. Keep these OUT of appsettings.json in production: use
		/// dotnet user-secrets locally, and Ifms__UserName / Ifms__Password
		/// environment variables (or a systemd EnvironmentFile) on the server.
		/// </summary>
		public string UserName { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;

		public IfmsSelectorOptions Selectors { get; set; } = new();
		public IfmsBrowserOptions Browser { get; set; } = new();
		public IfmsCaptchaOptions Captcha { get; set; } = new();
		public IfmsOtpOptions Otp { get; set; } = new();

		/// <summary>
		/// Reuse cookies from the last successful login so that most nights need no
		/// CAPTCHA and no OTP at all. Falls back to a full login the moment the
		/// portal rejects the stored session.
		/// </summary>
		public bool ReuseStoredSession { get; set; } = true;

		/// <summary>
		/// TEMPORARY, for commissioning only. Also stores each portal password in
		/// the clear next to the encrypted one, so it can be checked by eye.
		///
		/// The run logs a warning on every start while this is on. Turn it off and
		/// drop the column once the logins are proven.
		/// </summary>
		public bool StorePlainPasswordForTesting { get; set; }

		/// <summary>
		/// Alert if no paired phone has checked in for this long. The phone polls
		/// every minute, so silence for hours means it is off, offline, or the app
		/// has been killed — all of which are better known before 04:05 than after.
		/// </summary>
		public int RelayStaleAfterHours { get; set; } = 4;

		/// <summary>Folder that keeps a dated copy of every downloaded report.</summary>
		public string DownloadRoot { get; set; } = "downloads";

		/// <summary>Delete archived downloads older than this. 0 keeps them forever.</summary>
		public int ArchiveRetentionDays { get; set; } = 120;
	}

	/// <summary>
	/// CSS or Playwright selectors for the login flow. Every one of these is a
	/// placeholder until the real login page is inspected; they live here so that
	/// correcting one is a config edit rather than a rebuild.
	/// </summary>
	public sealed class IfmsSelectorOptions
	{
		public string UserNameInput { get; set; } = "#txtUserName";
		public string PasswordInput { get; set; } = "#txtPassword";

		/// <summary>The image element holding the CAPTCHA, screenshotted for OCR.</summary>
		public string? CaptchaImage { get; set; } = "#imgCaptcha";

		/// <summary>
		/// Set this when the portal renders the CAPTCHA as readable text or a sum
		/// (for example a span containing "7 + 3 = ?"). When present it is tried
		/// first and OCR is never needed.
		/// </summary>
		public string? CaptchaText { get; set; }

		public string CaptchaInput { get; set; } = "#txtCaptcha";

		/// <summary>Link or button that swaps in a fresh CAPTCHA after a wrong answer.</summary>
		public string? CaptchaRefresh { get; set; }

		public string LoginSubmit { get; set; } = "#btnLogin";

		/// <summary>Appears only when an OTP step follows the password step.</summary>
		public string? OtpInput { get; set; } = "#txtOtp";
		public string? OtpSubmit { get; set; } = "#btnVerifyOtp";

		/// <summary>Button that asks the portal to send a fresh OTP SMS.</summary>
		public string? OtpResend { get; set; }

		/// <summary>
		/// Something that exists only after a successful login (a logout link, a
		/// dashboard header). This is how the automation knows it is in.
		/// </summary>
		public string LoggedIn { get; set; } = "a:has-text(\"Logout\")";

		/// <summary>Element that carries the portal's login error text.</summary>
		public string? LoginError { get; set; } = ".alert-danger, #lblMessage";

		/// <summary>
		/// Substrings in the login error that mean "the CAPTCHA was wrong" as
		/// opposed to "the password was wrong". Only the first is worth retrying.
		/// </summary>
		public List<string> CaptchaErrorMarkers { get; set; } = new()
		{
			"captcha", "verification code", "security code", "code does not match"
		};

		/// <summary>Substrings that mean the stored session is no longer valid.</summary>
		public List<string> SessionExpiredMarkers { get; set; } = new()
		{
			"session expired", "please login", "your session has timed out"
		};
	}

	public sealed class IfmsBrowserOptions
	{
		/// <summary>Always true on a Linux server. Set false to watch it work.</summary>
		public bool Headless { get; set; } = true;

		/// <summary>chromium (default), chrome, or msedge.</summary>
		public string? Channel { get; set; }

		/// <summary>Milliseconds of artificial delay per action. Debug aid only.</summary>
		public int SlowMoMs { get; set; }

		public int NavigationTimeoutMs { get; set; } = 60_000;
		public int ActionTimeoutMs { get; set; } = 30_000;

		/// <summary>How long to wait for a download to start after clicking Export.</summary>
		public int DownloadTimeoutMs { get; set; } = 180_000;

		public string UserAgent { get; set; } =
			"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
			"(KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

		/// <summary>Ignore TLS errors. Some NIC portals still ship broken chains.</summary>
		public bool IgnoreHttpsErrors { get; set; } = true;

		/// <summary>Save a PNG of the page whenever a step fails. Worth its disk.</summary>
		public bool ScreenshotOnFailure { get; set; } = true;

		public string DiagnosticsRoot { get; set; } = "diagnostics";
	}

	public sealed class IfmsCaptchaOptions
	{
		/// <summary>
		/// Automatic solvers, tried in order on every attempt. Supported: HtmlText, Ocr.
		/// </summary>
		public List<string> Strategies { get; set; } = new() { "HtmlText", "Ocr" };

		/// <summary>
		/// Whole login retries when the portal rejects the CAPTCHA. Each retry
		/// pulls a brand new CAPTCHA image, so OCR gets several rolls of the dice.
		/// </summary>
		public int MaxAttempts { get; set; } = 5;

		/// <summary>
		/// After the automatic attempts are spent, push the CAPTCHA image to the
		/// SPIC Android app and wait for a person to read it.
		/// </summary>
		public bool OperatorFallbackEnabled { get; set; } = true;

		/// <summary>
		/// How long the first ask waits. Long by default: the run starts at 04:05
		/// and the answer realistically comes when someone wakes up.
		/// </summary>
		public int OperatorFirstWaitMinutes { get; set; } = 300;

		/// <summary>
		/// How long later asks wait. Short, because these follow a reply that
		/// arrived too late to use, and the person is holding the phone.
		/// </summary>
		public int OperatorFollowUpWaitMinutes { get; set; } = 10;

		/// <summary>
		/// How many times a fresh CAPTCHA may be pushed to the app before the run
		/// is abandoned. Round 1 is the first ask.
		/// </summary>
		public int OperatorMaxRounds { get; set; } = 5;

		/// <summary>Path to Tesseract language data. Defaults to ./tessdata.</summary>
		public string TessDataPath { get; set; } = "tessdata";
		public string TessLanguage { get; set; } = "eng";

		/// <summary>
		/// A second Tesseract model (e.g. "eng.fast" beside "eng.best") read on
		/// every image. When both models produce the same six characters the read
		/// is trusted; when they differ, the primary read is used only if it is
		/// confident. Empty disables the second opinion.
		/// </summary>
		public string SecondaryTessLanguage { get; set; } = "";

		/// <summary>
		/// Below this mean confidence a read that the second model does not
		/// confirm is not submitted: a fresh image costs nothing, a wrong submit
		/// costs an attempt and, repeated, the portal's "invalid access" refusal.
		/// </summary>
		public float MinimumConfidence { get; set; } = 0.60f;

		/// <summary>
		/// Characters the CAPTCHA can contain. Restricting this lifts OCR accuracy
		/// dramatically. Empty means "no restriction".
		/// </summary>
		public string CharacterWhitelist { get; set; } =
			"ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

		/// <summary>Expected CAPTCHA length. 0 disables the length check.</summary>
		public int ExpectedLength { get; set; } = 6;

		/// <summary>
		/// Cut the image into single glyphs and read each one separately, rather
		/// than handing Tesseract the whole strip.
		///
		/// This is the difference between working and not working on the mFMS
		/// CAPTCHA: its characters sit at staggered heights, and every Tesseract
		/// line mode assumes one baseline, so it quietly drops the ones that do
		/// not fit. Turn it off only for a conventional flat CAPTCHA.
		/// </summary>
		public bool SegmentCharacters { get; set; } = true;

		/// <summary>Upscale, clean and threshold the image before OCR.</summary>
		public bool PreprocessImage { get; set; } = true;
		public int UpscaleFactor { get; set; } = 4;
		public int BinarizeThreshold { get; set; } = 140;

		/// <summary>
		/// The mFMS CAPTCHA is light text on a dark panel, which is the opposite of
		/// what Tesseract expects. Inverting first is what makes it readable at all.
		/// </summary>
		public bool InvertImage { get; set; } = true;

		/// <summary>
		/// Keep only strongly coloured pixels and throw the rest away.
		///
		/// This is the single biggest accuracy win on the mFMS CAPTCHA: the
		/// characters are saturated orange while the noisy gradient behind them is
		/// grey, so selecting on colour separates them cleanly in a way no
		/// brightness threshold can — a dark grey background pixel and a dark
		/// orange text pixel have nearly the same brightness but nothing like the
		/// same saturation.
		///
		/// Turn this off if the portal ever switches to plain black-on-white.
		/// </summary>
		public bool IsolateColouredText { get; set; } = true;

		/// <summary>
		/// How colourful a pixel must be to count as text, 0-255 measured as
		/// max(R,G,B) - min(R,G,B). Raise it if background survives the mask,
		/// lower it if strokes come out broken.
		/// </summary>
		public int SaturationThreshold { get; set; } = 40;
	}

	public sealed class IfmsOtpOptions
	{
		/// <summary>
		/// SmsRelay reads the OTP the Android companion forwards.
		/// NotRequired skips the OTP step entirely.
		/// </summary>
		public string Strategy { get; set; } = "SmsRelay";

		/// <summary>
		/// The portal asks for an OTP on every fresh login, so a login that never
		/// reaches the OTP step has failed rather than skipped it. Leave this true
		/// and the run says so plainly instead of reporting a vague timeout.
		/// </summary>
		public bool Required { get; set; } = true;

		/// <summary>How long to wait for the OTP screen to appear after submitting.</summary>
		public int StepTimeoutMs { get; set; } = 20_000;

		/// <summary>
		/// How long to wait for the SMS after the portal sends it.
		///
		/// The code lives about five minutes. The 60-second countdown shown on the
		/// OTP page is only the delay before Regenerate OTP becomes clickable, so
		/// do not mistake it for an expiry and set this too low.
		/// </summary>
		public int WaitSeconds { get; set; } = 120;

		public int PollIntervalSeconds { get; set; } = 2;

		/// <summary>
		/// Ignore relayed messages older than this at the moment the OTP is
		/// requested, so yesterday's SMS can never be replayed.
		/// </summary>
		public int MaxMessageAgeSeconds { get; set; } = 300;

		/// <summary>Only accept SMS whose sender contains one of these. Empty accepts any.</summary>
		public List<string> AcceptedSenders { get; set; } = new()
		{
			"IFMS", "URVRK", "NICSMS", "DBTFMS"
		};

		/// <summary>Regex whose first capture group is the OTP.</summary>
		public string OtpPattern { get; set; } = @"\b(\d{4,8})\b";

		/// <summary>Ask the portal to resend once if the first SMS never arrives.</summary>
		public bool AllowResend { get; set; } = true;

		/// <summary>How many times to press Regenerate before giving up on the login attempt.</summary>
		public int MaxResends { get; set; } = 4;
	}
}
