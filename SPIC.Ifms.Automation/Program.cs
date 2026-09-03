using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services;
using SPIC.Core.Interfaces;
using SPIC.Ifms.Automation.Alerts;
using SPIC.Ifms.Automation.Options;
using SPIC.Ifms.Automation.Portal;
using SPIC.Ifms.Automation.Portal.Challenges;
using SPIC.Ifms.Automation.Reports;
using SPIC.Ifms.Automation.Scheduling;
using SPIC.Ifms.Automation.Tools;

// Matches SpicAPI: the existing entities store naive DateTimes and Npgsql 6+
// would otherwise reject every write.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient();

// ------------------------------------------------------------------ options

builder.Services.Configure<IfmsOptions>(
	builder.Configuration.GetSection(IfmsOptions.SectionName));
builder.Services.Configure<ScheduleOptions>(
	builder.Configuration.GetSection(ScheduleOptions.SectionName));
builder.Services.Configure<ReportJobsOptions>(
	builder.Configuration.GetSection(ReportJobsOptions.SectionName));
builder.Services.Configure<AlertOptions>(
	builder.Configuration.GetSection(AlertOptions.SectionName));
builder.Services.Configure<UploadOptions>(
	builder.Configuration.GetSection(UploadOptions.SectionName));

// ----------------------------------------------------------------- database
//
// Refuse to start without a connection string rather than falling back to
// something plausible. Two databases are in play here - spicone for the portal
// and spiconeifms for the automation - and quietly choosing the wrong one
// produces the worst kind of bug: commands that report success against a
// database nobody is looking at.

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
	Console.Error.WriteLine(
		"ConnectionStrings:DefaultConnection is not set.\n\n" +
		"The service gets it from /opt/spic-ifms/secrets.env via systemd.\n" +
		"To run a command by hand, load the same file first:\n\n" +
		"  set -a; . /opt/spic-ifms/secrets.env; set +a\n" +
		"  dotnet SPIC.Ifms.Automation.dll list-credentials\n");

	return 1;
}

builder.Services.AddDbContext<IfmsDbContext>(options =>
	options.UseNpgsql(
		connectionString,
		npgsql =>
		{
			npgsql.MigrationsAssembly("Spic.Infrastructure");
			npgsql.CommandTimeout(600);
		}));

// Portal passwords are encrypted with Data Protection, and this host has to be
// able to read what SpicAPI wrote. Three things must match on both sides or the
// two cannot exchange a single password:
//
//   - the same key store, which is why the keys live in the database rather
//     than in a folder neither machine shares
//   - the same application name, because it is mixed into the key derivation
//   - the same DbContext and database, so both look in the same table
//
// Without this the host starts happily and only fails when it first tries to
// read a credential, which is exactly the wrong time to find out.
builder.Services
	.AddDataProtection()
	.SetApplicationName("SPIC.Ifms")
	.PersistKeysToDbContext<IfmsDbContext>();

// No IExcelBulkUploadService here any more. Downloaded files are posted to
// SpicAPI's upload endpoint, exactly as a person would from the Excel Upload
// page — so the import runs in one place, against the portal's own database,
// and this service never needs to know the SPIC schema at all.
builder.Services.AddScoped<IIfmsAccountStore, IfmsAccountStore>();
builder.Services.AddScoped<IIfmsRelayDeviceStore, IfmsRelayDeviceStore>();

// ---------------------------------------------------------------- automation

builder.Services.AddSingleton<ISiteProbe, SiteProbe>();

builder.Services.AddSingleton<ICaptchaSolver, HtmlTextCaptchaSolver>();
builder.Services.AddSingleton<ICaptchaSolver, OcrCaptchaSolver>();
builder.Services.AddSingleton<OperatorCaptchaSolver>();

builder.Services.AddSingleton<IOtpProvider>(sp =>
{
	var strategy = builder.Configuration["Ifms:Otp:Strategy"] ?? "SmsRelay";

	return strategy.Equals("NotRequired", StringComparison.OrdinalIgnoreCase)
		? ActivatorUtilities.CreateInstance<NoOtpProvider>(sp)
		: ActivatorUtilities.CreateInstance<SmsRelayOtpProvider>(sp);
});

builder.Services.AddScoped<IfmsPortalClient>();
builder.Services.AddSingleton<IReportImporter, ReportImporter>();
builder.Services.AddSingleton<INightlyRunService, NightlyRunService>();

// -------------------------------------------------------------------- alerts

builder.Services.AddSingleton<PushAlertSink>();
builder.Services.AddSingleton<IAlertSink, EmailAlertSink>();
builder.Services.AddSingleton<IAlertSink>(sp => sp.GetRequiredService<PushAlertSink>());
builder.Services.AddSingleton<IAlertSink, WhatsAppAlertSink>();

// The CAPTCHA prompt rides the push channel when it is on; otherwise it is a
// no-op and the app's polling is the only way the prompt is seen.
builder.Services.AddSingleton<IChallengeNotifier>(sp =>
{
	var push = sp.GetRequiredService<PushAlertSink>();
	return push.Enabled ? push : new NullChallengeNotifier();
});

builder.Services.AddSingleton<IAlertDispatcher, AlertDispatcher>();

// ------------------------------------------------------------------- workers
//
// The calibration tool runs instead of the schedule, not alongside it, so that
// "dotnet run -- test-captcha" never fires a real download.

var command = args.Length > 0 ? args[0].ToLowerInvariant() : string.Empty;
var isTool = command is "test-captcha" or "set-credentials" or "list-credentials"
	or "test-email" or "otp" or "run-now" or "test-login" or "dump-page" or "test-job";

if (!isTool)
{
	builder.Services.AddHostedService<DailyScheduleWorker>();
	builder.Services.AddHostedService<ManualTriggerWorker>();
	builder.Services.AddHostedService<RelayPresenceWorker>();
}

var host = builder.Build();

if (isTool && command != "test-captcha")
{
	var target = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
	Console.WriteLine($"Database: {target.Database} on {target.Host}:{target.Port}");
	Console.WriteLine();
}

if (command is "set-credentials" or "list-credentials")
	return await RunCredentialsCommandAsync(host.Services, command, args);

// Email settings are the sort of thing that is wrong three times before it is
// right, and waiting for 04:05 to find out is no way to iterate.
if (command == "test-email")
	return await RunTestEmailAsync(host.Services);

// Hand the login an OTP without a phone. This is the commissioning path, and it
// stays useful afterwards as the fallback for the morning the handset is flat.
if (command == "otp")
	return await RunOtpCommandAsync(host.Services, args);

// Queue a run for the service to pick up within twenty seconds, rather than
// waiting for 04:05 to find out whether anything works.
if (command == "run-now")
	return await RunNowCommandAsync(host.Services, args);

// Sign in and stop. A normal run skips the login when no reports are enabled -
// sensibly, since there would be nothing to do with it - so proving the login
// needs a command that does nothing else.
if (command == "test-login")
	return await RunTestLoginAsync(host.Services, args);

if (command == "dump-page")
	return await RunDumpPageAsync(host.Services, args);

if (command == "test-job")
	return await RunTestJobAsync(host.Services, args);

if (command == "test-captcha")
{
	// test-captcha replay <folder>   re-reads images captured by an earlier run
	// test-captcha [n]               fetches n fresh images from the portal
	if (args.Length > 2 && args[1].Equals("replay", StringComparison.OrdinalIgnoreCase))
	{
		return await CaptchaCalibrationTool.ReplayAsync(
			host.Services,
			args[2],
			CancellationToken.None);
	}

	var sampleSize = args.Length > 1 && int.TryParse(args[1], out var parsed) ? parsed : 25;

	return await CaptchaCalibrationTool.RunAsync(
		host.Services,
		Math.Clamp(sampleSize, 1, 200),
		CancellationToken.None);
}

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("SPIC IFMS automation starting.");

ValidateConfiguration(host.Services, logger);

host.Run();
return 0;

/// <summary>
/// Manages the portal logins from the command line, which is how they get set on
/// a server with no browser to hand:
///
///   dotnet run -- set-credentials spic 1000249825 "the-password" "SPIC"
///   dotnet run -- list-credentials
///
/// The password is encrypted before it touches the database, and never printed.
/// </summary>
static async Task<int> RunCredentialsCommandAsync(
	IServiceProvider services,
	string command,
	string[] args)
{
	await using var scope = services.CreateAsyncScope();
	var store = scope.ServiceProvider.GetRequiredService<IIfmsAccountStore>();

	if (command == "list-credentials")
	{
		var accounts = await store.GetActiveAsync(CancellationToken.None);

		if (accounts.Count == 0)
		{
			Console.WriteLine("No portal logins are configured.");
			Console.WriteLine("Add one with: dotnet run -- set-credentials <key> <username> <password> [company]");
			return 0;
		}

		Console.WriteLine($"{"KEY",-14}{"COMPANY",-20}{"USERNAME",-16}PASSWORD EXPIRES");

		foreach (var account in accounts)
		{
			var expiry = account.PasswordExpired
				? $"EXPIRED {account.PasswordExpiresAt:dd MMM yyyy}"
				: $"{account.PasswordExpiresAt:dd MMM yyyy} ({account.DaysUntilPasswordExpires} days)";

			Console.WriteLine(
				$"{account.AccountKey,-14}{account.CompanyName,-20}{account.UserName,-16}{expiry}");
		}

		return 0;
	}

	if (args.Length < 3)
	{
		Console.WriteLine("Usage: dotnet SPIC.Ifms.Automation.dll set-credentials <key> <username> [company]");
		Console.WriteLine();
		Console.WriteLine("  dotnet SPIC.Ifms.Automation.dll set-credentials greenstar 1000249825 Greenstar");
		Console.WriteLine();
		Console.WriteLine("It asks for the password, so it never appears in your shell history");
		Console.WriteLine("or in the process list. Paste it at the prompt.");
		return 1;
	}

	var key = args[1];
	var userName = args[2];
	var company = args.Length > 3 ? args[3] : key.ToUpperInvariant();

	// Prompting rather than taking the password as an argument. On the command
	// line it would sit in ~/.bash_history and be visible in `ps` to every user
	// on the box for as long as the process ran - and this is a password that
	// unlocks a government portal.
	var password = ReadPasswordFromConsole($"Password for {userName} ({company}): ");

	if (string.IsNullOrEmpty(password))
	{
		Console.WriteLine("Nothing entered; no change made.");
		return 1;
	}

	var confirm = ReadPasswordFromConsole("Type it again to confirm      : ");

	if (!string.Equals(password, confirm, StringComparison.Ordinal))
	{
		// Worth the second prompt: a mistyped password here is only discovered
		// at 04:05, as a login failure that looks like a portal problem.
		Console.WriteLine("Those do not match; no change made.");
		return 1;
	}

	await store.SetCredentialsAsync(
		key, company, userName, password,
		changedBy: Environment.UserName,
		reason: "Manual",
		CancellationToken.None);

	// Read it back rather than trusting the write. This command reported success
	// for two days while silently storing nothing, and the only reason that was
	// survivable is that it fails at 04:05 rather than corrupting anything — but
	// a confirmation that came from the database would have caught it at once.
	var saved = await store.GetActiveAsync(CancellationToken.None);
	var stored = saved.FirstOrDefault(a =>
		string.Equals(a.AccountKey, key, StringComparison.OrdinalIgnoreCase));

	if (stored is null)
	{
		Console.WriteLine(
			$"WROTE NOTHING. '{key}' is not in the database after saving. " +
			"Check the connection string and try again.");
		return 1;
	}

	Console.WriteLine(
		$"Saved the login for {stored.CompanyName} ({stored.UserName}), " +
		$"read back from the database.");
	Console.WriteLine(
		$"The password expires on {stored.PasswordExpiresAt:dd MMM yyyy} " +
		$"({stored.DaysUntilPasswordExpires} days).");

	return 0;
}

/// <summary>
/// Reads a password without echoing it.
///
/// Falls back to a plain read when there is no console — piped input, or a
/// systemd context — because throwing there would make the command unusable
/// from a script for no gain.
/// </summary>
static string ReadPasswordFromConsole(string prompt)
{
	Console.Write(prompt);

	if (Console.IsInputRedirected)
	{
		Console.WriteLine("(input is not a terminal; it will not be hidden)");
		return Console.ReadLine() ?? string.Empty;
	}

	var builder = new System.Text.StringBuilder();

	while (true)
	{
		var key = Console.ReadKey(intercept: true);

		if (key.Key == ConsoleKey.Enter)
		{
			Console.WriteLine();
			return builder.ToString();
		}

		if (key.Key == ConsoleKey.Backspace)
		{
			if (builder.Length > 0)
				builder.Length--;

			continue;
		}

		if (key.Key == ConsoleKey.Escape)
		{
			Console.WriteLine();
			return string.Empty;
		}

		if (!char.IsControl(key.KeyChar))
			builder.Append(key.KeyChar);
	}
}

/// <summary>
/// Signs in to the portal and stops, reporting exactly what happened at each
/// step. This is the commissioning tool: it exercises the CAPTCHA solver against
/// live images, the OTP handoff and the logged-in selector, and touches no
/// reports at all.
///
///   dotnet SPIC.Ifms.Automation.dll test-login greenstar
///
/// With no phone paired, feed it the code from another terminal:
///   dotnet SPIC.Ifms.Automation.dll otp 123456
/// </summary>
static async Task<int> RunTestLoginAsync(IServiceProvider services, string[] args)
{
	var key = args.Length > 1 ? args[1] : null;

	await using var scope = services.CreateAsyncScope();
	var store = scope.ServiceProvider.GetRequiredService<IIfmsAccountStore>();

	var accounts = await store.GetActiveAsync(CancellationToken.None);

	if (accounts.Count == 0)
	{
		Console.WriteLine("No portal logins are configured.");
		return 1;
	}

	var account = key is null
		? accounts[0]
		: accounts.FirstOrDefault(a => string.Equals(a.AccountKey, key, StringComparison.OrdinalIgnoreCase));

	if (account is null)
	{
		Console.WriteLine($"No login found for '{key}'. Known keys: " +
			string.Join(", ", accounts.Select(a => a.AccountKey)));
		return 1;
	}

	Console.WriteLine($"Signing in as {account.CompanyName} ({account.UserName}).");
	Console.WriteLine();
	Console.WriteLine("If the portal asks for an OTP, paste it from another terminal:");
	Console.WriteLine("  dotnet SPIC.Ifms.Automation.dll otp <code>");
	Console.WriteLine();

	var portal = scope.ServiceProvider.GetRequiredService<IfmsPortalClient>();

	try
	{
		var result = await portal.LoginAsync(account, runId: 0, CancellationToken.None);

		Console.WriteLine();
		Console.WriteLine("---------------------------------------------");
		Console.WriteLine($"Signed in       : {(result.Success ? "YES" : "NO")}");
		Console.WriteLine($"CAPTCHA solved  : {result.CaptchaMethod ?? "n/a"} " +
						  $"after {result.CaptchaAttempts} attempt(s)");
		Console.WriteLine($"OTP             : {result.OtpMethod ?? "not requested"}");

		if (!result.Success)
		{
			Console.WriteLine($"Why not         : {result.FailureReason}");
			Console.WriteLine();
			Console.WriteLine("A screenshot and the page HTML are in /opt/spic-ifms/diagnostics/<today>/.");
			return 1;
		}

		Console.WriteLine();
		Console.WriteLine("The login works end to end. Enable a report job next.");
		return 0;
	}
	finally
	{
		await portal.DisposeAsync();
	}
}

/// <summary>
/// Signs in (reusing the stored session when the portal still honours it),
/// opens each given path, and saves its HTML and screenshot under
/// diagnostics/, printing every link on the page. This is how the real
/// report URLs and field names are read off the portal instead of guessed.
///
///   dotnet SPIC.Ifms.Automation.dll dump-page greenstar /mFMS/welcome.action /mFMS/other.action
///
/// With no paths, only the landing page after login is captured.
/// </summary>
static async Task<int> RunDumpPageAsync(IServiceProvider services, string[] args)
{
	var key = args.Length > 1 ? args[1] : null;
	var paths = args.Skip(2).ToList();

	await using var scope = services.CreateAsyncScope();
	var store = scope.ServiceProvider.GetRequiredService<IIfmsAccountStore>();
	var accounts = await store.GetActiveAsync(CancellationToken.None);

	var account = key is null
		? accounts.FirstOrDefault()
		: accounts.FirstOrDefault(a => string.Equals(a.AccountKey, key, StringComparison.OrdinalIgnoreCase));

	if (account is null)
	{
		Console.WriteLine($"No login found for '{key}'. Known keys: " +
			string.Join(", ", accounts.Select(a => a.AccountKey)));
		return 1;
	}

	var portal = scope.ServiceProvider.GetRequiredService<IfmsPortalClient>();

	try
	{
		var result = await portal.LoginAsync(account, runId: 0, CancellationToken.None);
		if (!result.Success)
		{
			Console.WriteLine($"Not signed in: {result.FailureReason}");
			return 1;
		}

		Console.WriteLine($"Signed in ({result.CaptchaMethod}, OTP {result.OtpMethod ?? "not requested"}).");

		var captured = await portal.CapturePageAsync(null, CancellationToken.None);
		Console.WriteLine($"Landing page -> {captured}");

		foreach (var path in paths)
		{
			captured = await portal.CapturePageAsync(path, CancellationToken.None);
			Console.WriteLine($"{path} -> {captured}");
		}

		return 0;
	}
	finally
	{
		await portal.DisposeAsync();
	}
}

/// <summary>
/// Runs one report job's steps on this machine, with a screenshot and the
/// page HTML saved after every step, and the file kept under downloads/.
/// Nothing is uploaded. Loop tokens can be pinned on the command line:
///
///   dotnet SPIC.Ifms.Automation.dll test-job company-sales-greenstar
///   dotnet SPIC.Ifms.Automation.dll test-job retail-stocks-greenstar state="TAMIL NADU"
/// </summary>
static async Task<int> RunTestJobAsync(IServiceProvider services, string[] args)
{
	if (args.Length < 2)
	{
		Console.WriteLine("usage: test-job <jobKey> [token=value ...]");
		return 1;
	}

	var jobKey = args[1];
	var pinned = args.Skip(2)
		.Select(a => a.Split('=', 2))
		.Where(p => p.Length == 2)
		.ToDictionary(p => p[0], p => p[1], StringComparer.OrdinalIgnoreCase);

	await using var scope = services.CreateAsyncScope();
	var jobs = scope.ServiceProvider.GetRequiredService<IOptions<ReportJobOptions>>().Value.Jobs;
	var job = jobs.FirstOrDefault(j => string.Equals(j.Key, jobKey, StringComparison.OrdinalIgnoreCase));
	if (job is null)
	{
		Console.WriteLine($"No job '{jobKey}'. Known: {string.Join(", ", jobs.Select(j => j.Key))}");
		return 1;
	}

	var store = scope.ServiceProvider.GetRequiredService<IIfmsAccountStore>();
	var accounts = await store.GetActiveAsync(CancellationToken.None);
	var account = accounts.FirstOrDefault(a => string.IsNullOrWhiteSpace(job.AccountKey) ||
		string.Equals(a.AccountKey, job.AccountKey, StringComparison.OrdinalIgnoreCase));
	if (account is null)
	{
		Console.WriteLine($"No login for account '{job.AccountKey}'.");
		return 1;
	}

	var portal = scope.ServiceProvider.GetRequiredService<IfmsPortalClient>();
	portal.CaptureEveryStep = true;

	try
	{
		var login = await portal.LoginAsync(account, runId: 0, CancellationToken.None);
		if (!login.Success)
		{
			Console.WriteLine($"Not signed in: {login.FailureReason}");
			return 1;
		}
		Console.WriteLine($"Signed in ({login.CaptchaMethod}, OTP {login.OtpMethod ?? "not requested"}).");

		var reportDate = DateTime.Today.AddDays(job.ReportDateOffsetDays ?? -1);
		var tokens = new RunTokens(reportDate, DateTime.Now, account.UserName)
			.WithLiteral("company", account.CompanyName)
			.WithLiteral("accountKey", account.AccountKey);

		foreach (var loop in job.ForEach)
		{
			string value;
			if (pinned.TryGetValue(loop.TokenName, out var given))
				value = given;
			else if (loop.Values.Count > 0)
				value = loop.Values[0];
			else
			{
				var found = await portal.DiscoverLoopValuesAsync(job, loop, tokens, CancellationToken.None);
				Console.WriteLine($"  {loop.TokenName}: {found.Count} value(s) discovered: {string.Join(" | ", found.Take(12))}{(found.Count > 12 ? " ..." : "")}");
				if (found.Count == 0) { Console.WriteLine($"Nothing to loop over for '{loop.TokenName}'."); return 1; }
				value = found[0];
			}
			Console.WriteLine($"  using {loop.TokenName} = {value}");
			tokens.WithLiteral(loop.TokenName, value);
		}

		var dir = Path.Combine(AppContext.BaseDirectory, "downloads", DateTime.Now.ToString("yyyy-MM-dd"));
		var file = await portal.DownloadReportAsync(job, tokens, dir, CancellationToken.None);
		Console.WriteLine();
		Console.WriteLine($"Downloaded: {file.FilePath} ({file.Bytes:N0} bytes, {file.Extension})");
		return 0;
	}
	catch (Exception ex)
	{
		Console.WriteLine();
		Console.WriteLine($"FAILED: {ex.GetType().Name}: {ex.Message.Split(Environment.NewLine)[0]}");
		Console.WriteLine("Look at the newest files under diagnostics/<today>/ for the page it was on.");
		return 1;
	}
	finally
	{
		await portal.DisposeAsync();
	}
}

/// <summary>
/// Injects a one-time password as though the Android relay had forwarded it.
///
///   dotnet SPIC.Ifms.Automation.dll otp 123456
///
/// The waiting login picks it up within two seconds. It is written exactly like
/// a relayed SMS, so nothing downstream can tell the difference — which is the
/// point: this is the same path the phone uses, exercised by hand.
/// </summary>
static async Task<int> RunOtpCommandAsync(IServiceProvider services, string[] args)
{
	if (args.Length < 2)
	{
		Console.WriteLine("Usage: dotnet SPIC.Ifms.Automation.dll otp <code>");
		return 1;
	}

	var code = new string(args[1].Where(char.IsDigit).ToArray());

	if (code.Length is < 4 or > 8)
	{
		Console.WriteLine($"'{args[1]}' does not look like an OTP.");
		return 1;
	}

	await using var scope = services.CreateAsyncScope();
	var db = scope.ServiceProvider.GetRequiredService<IfmsDbContext>();

	db.IfmsOtpMessages.Add(new SPIC.Core.Entities.IfmsOtpMessage
	{
		DeviceId = "manual",
		Sender = "MANUAL",
		Body = $"Your IFMS OTP is {code}",
		ExtractedOtp = code,
		ReceivedAt = DateTime.UtcNow,
		CreatedAt = DateTime.UtcNow
	});

	await db.SaveChangesAsync();

	Console.WriteLine($"OTP {code} queued. A login waiting for one will use it within two seconds.");
	Console.WriteLine("It can only be used once, and only by a login that asked after it arrived.");
	return 0;
}

/// <summary>
/// Queues a run for the service to collect, so a change can be tested now rather
/// than at four tomorrow morning.
/// </summary>
static async Task<int> RunNowCommandAsync(IServiceProvider services, string[] args)
{
	await using var scope = services.CreateAsyncScope();
	var db = scope.ServiceProvider.GetRequiredService<IfmsDbContext>();

	var busy = await db.IfmsAutomationRuns.AnyAsync(r =>
		r.Status == SPIC.Core.Entities.IfmsRunStatus.Pending ||
		r.Status == SPIC.Core.Entities.IfmsRunStatus.Running);

	if (busy)
	{
		Console.WriteLine("A run is already queued or in progress; not queueing another.");
		return 1;
	}

	var reportDate = args.Length > 1 && DateTime.TryParse(args[1], out var parsed)
		? parsed.Date
		: DateTime.Today.AddDays(-1);

	var run = new SPIC.Core.Entities.IfmsAutomationRun
	{
		ReportDate = reportDate,
		StartedAt = DateTime.UtcNow,
		Status = SPIC.Core.Entities.IfmsRunStatus.Pending,
		Trigger = SPIC.Core.Entities.IfmsRunTrigger.Manual,
		Attempt = 1,
		CreatedAt = DateTime.UtcNow,
		UpdatedAt = DateTime.UtcNow,
		UpdatedBy = Environment.UserName
	};

	db.IfmsAutomationRuns.Add(run);
	await db.SaveChangesAsync();

	Console.WriteLine($"Queued run {run.Id} for report date {reportDate:dd MMM yyyy}.");
	Console.WriteLine("The service picks it up within twenty seconds. Watch it with:");
	Console.WriteLine("  sudo journalctl -u spic-ifms -f");
	return 0;
}

/// <summary>
/// Sends one message through the configured SMTP settings and reports exactly
/// what the server said, rather than leaving it buried in a run's alert failure.
/// </summary>
static async Task<int> RunTestEmailAsync(IServiceProvider services)
{
	var options = services.GetRequiredService<IOptions<AlertOptions>>().Value.Email;

	Console.WriteLine($"Host      : {options.Host}:{options.Port}");
	Console.WriteLine($"StartTls  : {options.UseStartTls}");
	Console.WriteLine($"From      : {options.FromAddress}");
	Console.WriteLine($"To        : {string.Join(", ", options.To)}");
	Console.WriteLine($"Password  : {(string.IsNullOrEmpty(options.Password) ? "NOT SET" : "set")}");
	Console.WriteLine();

	if (!options.Enabled)
	{
		Console.WriteLine("Alerts:Email:Enabled is false; nothing to test.");
		return 1;
	}

	var sink = services.GetServices<IAlertSink>().FirstOrDefault(s => s.Name == "Email");

	if (sink is null)
	{
		Console.WriteLine("The email sink is not registered.");
		return 1;
	}

	try
	{
		await sink.SendNoticeAsync(
			"IFMS automation test message",
			"If you are reading this, the alert email is working.\n\n" +
			"Sent by: dotnet SPIC.Ifms.Automation.dll test-email",
			urgent: false,
			CancellationToken.None);

		Console.WriteLine("SENT. Check the inbox.");
		return 0;
	}
	catch (Exception ex)
	{
		Console.WriteLine($"FAILED: {ex.GetType().Name}");
		Console.WriteLine($"        {ex.Message}");

		if (ex.InnerException is not null)
			Console.WriteLine($"        inner: {ex.InnerException.Message}");

		Console.WriteLine();
		Console.WriteLine("Common causes:");
		Console.WriteLine("  does not support STARTTLS  -> set Alerts:Email:UseStartTls false");
		Console.WriteLine("  timed out                  -> that host/port is not reachable from here");
		Console.WriteLine("  authentication failed      -> wrong Alerts__Email__Password in secrets.env");

		return 1;
	}
}

/// <summary>
/// Fails loudly at startup rather than at 04:05. A missing password discovered
/// now is a two-minute fix; discovered in the small hours it costs a day of data.
/// </summary>
static void ValidateConfiguration(IServiceProvider services, ILogger logger)
{
	var config = services.GetRequiredService<IConfiguration>();
	var problems = new List<string>();

	if (string.IsNullOrWhiteSpace(config.GetConnectionString("DefaultConnection")))
		problems.Add("ConnectionStrings:DefaultConnection is not set.");

	if (string.IsNullOrWhiteSpace(config["Ifms:BaseUrl"]))
		problems.Add("Ifms:BaseUrl is not set.");

	if (!config.GetSection("ReportJobs:Jobs").GetChildren().Any())
		problems.Add("ReportJobs:Jobs is empty, so there is nothing to download.");

	foreach (var problem in problems)
		logger.LogError("Configuration problem: {Problem}", problem);

	if (problems.Count > 0)
	{
		logger.LogError(
			"{Count} configuration problem(s) found. The service will keep running so you can fix " +
			"appsettings.json and restart, but the scheduled run will fail until they are resolved.",
			problems.Count);
	}
}
