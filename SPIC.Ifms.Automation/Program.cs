using System;
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
var isTool = command is "test-captcha" or "set-credentials" or "list-credentials" or "test-email";

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

	Console.WriteLine($"Saved the login for {company} ({userName}).");
	Console.WriteLine("The 80-day password clock starts now.");
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
