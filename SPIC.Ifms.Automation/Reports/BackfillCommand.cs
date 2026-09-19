using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SPIC.Core.Interfaces;
using SPIC.Ifms.Automation.Options;
using SPIC.Ifms.Automation.Portal;

namespace SPIC.Ifms.Automation.Reports
{
	/// <summary>
	/// One-time historical download of the date-ranged reports, download only:
	///
	///   dotnet SPIC.Ifms.Automation.dll backfill company-sales-greenstar from=2024-04-01 to=2026-09-17
	///   dotnet SPIC.Ifms.Automation.dll backfill all from=2024-04-01 account=greenstar
	///   dotnet SPIC.Ifms.Automation.dll backfill retail-stocks-greenstar from=2024-04-01 dry=true
	///   dotnet SPIC.Ifms.Automation.dll backfill wholesale-sales-greenstar from=2024-04-01 step=90
	///
	/// Logs in once, then for every period chunk and every loop value (state, plant/product)
	/// downloads the report into downloads/backfill/&lt;job&gt;/&lt;from&gt;_&lt;to&gt;/&lt;loop values&gt;/.
	/// Nothing is uploaded.
	///
	/// Chunk size: step=day | week | month | &lt;N&gt; (a number of days, e.g. 90). When step is not
	/// given it is chosen per job: 90 days for the invoice reports (the ones that use
	/// {{quarterStart}}, whose rows carry their own dates and whose portal filter allows up to
	/// 90 days), and one download per date for everything else, because a stock report is a
	/// position as on a date and a range would only give the position at its end.
	///
	/// Every result is appended to downloads/backfill/progress.jsonl, and a chunk that already
	/// has a line with status ok or empty is skipped, so the command can be re-run after a
	/// portal timeout, an expired session or a killed process and only does what is left.
	/// A session that dies mid-way is re-established, up to 40 logins per job.
	///
	/// The portal allows ONE session per login: a second sign-in on the same account silently
	/// ends the first (seen 2026-09-18, two backfill processes). So never run two backfills for
	/// the same account, and the command pauses during the nightly service's window
	/// (pause=04:00-05:45 by default, server local time) so the two do not sign each other out.
	/// After the pause it signs in again, since the nightly run has taken the session.
	///
	/// The date tokens are overridden per chunk: everything a job uses as the start of its
	/// range (fromDate, quarterStart, monthStart, yesterday) becomes the chunk start; everything
	/// it uses as the end (toDate, today, reportDate, monthEnd) becomes the chunk end.
	/// </summary>
	public static class BackfillCommand
	{
		private const int MaxLoginsPerJob = 40;   // a daily-step run lasts days and outlives many portal sessions; each login needs an OTP via the relay

		public static async Task<int> RunAsync(IServiceProvider services, string[] args)
		{
			var keys = args.Skip(1).Where(a => !a.Contains('=')).ToList();
			var options = args.Skip(1)
				.Where(a => a.Contains('='))
				.Select(a => a.Split('=', 2))
				.ToDictionary(p => p[0], p => p[1], StringComparer.OrdinalIgnoreCase);

			if (keys.Count == 0 || !options.TryGetValue("from", out var fromText))
			{
				Console.WriteLine("usage: backfill <jobKey|all> from=YYYY-MM-DD [to=YYYY-MM-DD] [step=day|week|month|<days>] [account=key] [pause=HH:mm-HH:mm] [dry=true] [token=value ...]");
				Console.WriteLine("       pause defaults to 04:00-05:45 (the nightly run's window; the portal allows one session per login).");
				Console.WriteLine("       step defaults per job: 90 (days) for the invoice reports that use {{quarterStart}}, day for the stock reports.");
				return 1;
			}

			var from = DateTime.ParseExact(fromText, "yyyy-MM-dd", CultureInfo.InvariantCulture);
			var to = options.TryGetValue("to", out var toText)
				? DateTime.ParseExact(toText, "yyyy-MM-dd", CultureInfo.InvariantCulture)
				: DateTime.Today.AddDays(-1);
			var stepOverride = options.TryGetValue("step", out var stepText) ? stepText.ToLowerInvariant().TrimEnd('d') : null;
			if (stepOverride is not null && stepOverride is not ("day" or "week" or "month") && !(int.TryParse(stepOverride, out var n) && n > 0))
			{
				Console.WriteLine($"step must be day, week, month or a number of days, not '{stepText}'.");
				return 1;
			}
			var dry = options.TryGetValue("dry", out var dryText) && dryText.Equals("true", StringComparison.OrdinalIgnoreCase);
			var pauseText = options.TryGetValue("pause", out var pt) ? pt : "04:00-05:45";
			var pause = ParsePause(pauseText);
			if (pause is null) { Console.WriteLine($"pause must look like 04:00-05:45, not '{pauseText}'."); return 1; }
			var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "from", "to", "step", "account", "dry", "pause" };
			var pinned = options.Where(kv => !reserved.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

			await using var scope = services.CreateAsyncScope();
			var allJobs = scope.ServiceProvider.GetRequiredService<IOptions<ReportJobsOptions>>().Value.Jobs;
			var store = scope.ServiceProvider.GetRequiredService<IIfmsAccountStore>();
			var accounts = await store.GetActiveAsync(CancellationToken.None);

			var selected = keys.Contains("all", StringComparer.OrdinalIgnoreCase)
				? allJobs.ToList()
				: keys.Select(k => allJobs.FirstOrDefault(j => string.Equals(j.Key, k, StringComparison.OrdinalIgnoreCase))).ToList();
			if (selected.Count == 0 || selected.Any(j => j is null))
			{
				Console.WriteLine($"Unknown job key. Known: {string.Join(", ", allJobs.Select(j => j.Key))}, or 'all'.");
				return 1;
			}

			var accountKey = options.TryGetValue("account", out var acc) ? acc : selected[0]!.AccountKey;
			var account = accounts.FirstOrDefault(a => string.Equals(a.AccountKey, accountKey, StringComparison.OrdinalIgnoreCase));
			if (account is null)
			{
				Console.WriteLine($"No login for account '{accountKey}'.");
				return 1;
			}
			// With account= given explicitly the jobs run under that login whatever their own
			// AccountKey says: that is how several Greenstar user IDs share the report set.
			var explicitAccount = options.ContainsKey("account");
			var jobs = selected
				.Where(j => explicitAccount || string.IsNullOrWhiteSpace(j!.AccountKey) || string.Equals(j.AccountKey, account.AccountKey, StringComparison.OrdinalIgnoreCase))
				.OrderBy(j => j!.Order)
				.Select(j => j!)
				.ToList();

			var root = Path.Combine(AppContext.BaseDirectory, "downloads", "backfill");
			Directory.CreateDirectory(root);
			var progressPath = Path.Combine(root, "progress.jsonl");
			var done = LoadDone(progressPath);

			Console.WriteLine($"Backfill {account.CompanyName}: {jobs.Count} job(s) from {from:yyyy-MM-dd} to {to:yyyy-MM-dd}.");
			foreach (var j in jobs)
			{
				var st = stepOverride ?? DefaultStep(j);
				Console.WriteLine($"  {j.Key,-34} step {StepLabel(st),-8} {Chunks(from, to, st).Count,5} chunk(s){(j.ForEach.Count > 0 ? " x " + string.Join(" x ", j.ForEach.Select(l => l.TokenName)) : "")}");
			}
			Console.WriteLine($"Files under {root}; progress in {progressPath}. Nothing is uploaded.");
			if (dry)
			{
				foreach (var j in jobs)
					foreach (var (start, end) in Chunks(from, to, stepOverride ?? DefaultStep(j)))
						Console.WriteLine($"  {j.Key} {start:yyyy-MM-dd}..{end:yyyy-MM-dd} {(done.Contains(Key(j.Key, start, end, "")) ? "(done)" : "")}");
				return 0;
			}

			var portal = scope.ServiceProvider.GetRequiredService<IfmsPortalClient>();
			var summary = new Dictionary<string, (int ok, int empty, int failed, int skipped)>();
			var comboCache = new Dictionary<string, List<List<(string Name, string Value)>>>(StringComparer.OrdinalIgnoreCase);
			var capturedEmpty = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var loggedIn = false;

			try
			{
				foreach (var job in jobs)
				{
					var logins = 0;
					var tally = (ok: 0, empty: 0, failed: 0, skipped: 0);
					var chunks = Chunks(from, to, stepOverride ?? DefaultStep(job));
					Console.WriteLine();
					Console.WriteLine($"=== {job.Key} : {job.Title} — {chunks.Count} chunk(s) of {StepLabel(stepOverride ?? DefaultStep(job))} ===");

					foreach (var (start, end) in chunks)
					{
						var chunkDir = Path.Combine(root, job.Key, $"{start:yyyy-MM-dd}_{end:yyyy-MM-dd}");
						List<List<(string Name, string Value)>>? combinations = null;
						if (job.ForEach.Count == 0) combinations = new List<List<(string, string)>> { new() };
						else if (!job.ForEach.Any(l => l.TokenName.Equals("state", StringComparison.OrdinalIgnoreCase)) && comboCache.TryGetValue(job.Key, out var cached))
							combinations = cached;   // plants and products do not change from one day to the next; states are cheap and left as they are
						for (var discovery = 1; combinations is null; discovery++)
						{
							try
							{
								if (await PauseForNightlyAsync(pause.Value)) loggedIn = false;
								if (!loggedIn) { logins++; loggedIn = await LoginAsync(portal, account); if (!loggedIn) return 1; }
								var baseTokens = Tokens(account, start, end, pinned);
								combinations = await ExpandAsync(portal, job, baseTokens, 0, new List<(string, string)>(), pinned, CancellationToken.None);
								if (!job.ForEach.Any(l => l.TokenName.Equals("state", StringComparison.OrdinalIgnoreCase)))
								{
									comboCache[job.Key] = combinations;
									Console.WriteLine($"  {combinations.Count} {string.Join("/", job.ForEach.Select(l => l.TokenName))} combination(s) found; reused for every day of this report.");
								}
								if (_sessionSuspect)
								{
									Console.WriteLine("  the portal error during discovery breaks the session; signing in again before downloading");
									_sessionSuspect = false;
									loggedIn = false;
								}
							}
							catch (Exception ex)
							{
								var reason = ex.Message.Split(Environment.NewLine)[0];
								if (discovery < 3)
								{
									// The portal throws "Internal Server Error" dialogs now and then while the
									// product list loads; a second look, and then a fresh sign-in, usually clears it.
									Console.WriteLine($"  {start:yyyy-MM-dd}..{end:yyyy-MM-dd}  discovering loop values failed ({reason}); {(discovery == 1 ? "trying again" : "signing in again")}");
									if (discovery == 2 && logins < MaxLoginsPerJob) loggedIn = false;
									await Task.Delay(TimeSpan.FromSeconds(15));
									continue;
								}
								Console.WriteLine($"  {start:yyyy-MM-dd}..{end:yyyy-MM-dd}  FAILED discovering loop values: {reason}");
								Append(progressPath, job.Key, start, end, "", "failed", 0, ex.Message);
								tally.failed++;
								break;
							}
						}
						if (combinations is null) continue;

						foreach (var combo in combinations)
						{
							var loopText = string.Join("|", combo.Select(c => c.Value));
							var key = Key(job.Key, start, end, loopText);
							if (done.Contains(key)) { tally.skipped++; continue; }

							var dir = combo.Count == 0 ? chunkDir : Path.Combine(chunkDir, Sanitize(string.Join("_", combo.Select(c => c.Value))));
							var attempt = 0;
							while (true)
							{
								attempt++;
								try
								{
									if (await PauseForNightlyAsync(pause.Value)) loggedIn = false;
									if (!loggedIn) { logins++; loggedIn = await LoginAsync(portal, account); if (!loggedIn) return 1; }
									var tokens = Tokens(account, start, end, pinned);
									foreach (var (name, value) in combo) tokens.WithLiteral(name, value);
									Directory.CreateDirectory(dir);
									var file = await portal.DownloadReportAsync(job, tokens, dir, CancellationToken.None);
									if (file.Bytes == 0)
									{
										Console.WriteLine($"  {start:yyyy-MM-dd}..{end:yyyy-MM-dd} {loopText,-28} empty");
										Append(progressPath, job.Key, start, end, loopText, "empty", 0, null);
										tally.empty++;
									}
									else
									{
										Console.WriteLine($"  {start:yyyy-MM-dd}..{end:yyyy-MM-dd} {loopText,-28} {file.Bytes,12:N0} bytes  {file.FileName}");
										Append(progressPath, job.Key, start, end, loopText, "ok", file.Bytes, file.FilePath);
										tally.ok++;
									}
									done.Add(key);
									break;
								}
								catch (Exception ex)
								{
									var reason = ex.Message.Split(Environment.NewLine)[0];
									var exportNeverAppeared = job.DownloadStep?.TimeoutMs is int dl
										&& reason.StartsWith("Timeout", StringComparison.OrdinalIgnoreCase)
										&& reason.Contains($"{dl}ms", StringComparison.Ordinal);
									if (exportNeverAppeared && job.ForEach.Any(l => l.AllowEmptyPerValue))
									{
										// The Global Stock pages show no export button at all when the plant/product
										// has nothing on that date; that is an empty result, not a failure to retry.
										if (capturedEmpty.Add(job.Key))
										{
											try { Console.WriteLine($"  (page kept for reference: {await portal.CapturePageAsync(null, CancellationToken.None)})"); }
											catch { /* diagnostics only */ }
										}
										Console.WriteLine($"  {start:yyyy-MM-dd}..{end:yyyy-MM-dd} {loopText,-28} empty (no export offered)");
										Append(progressPath, job.Key, start, end, loopText, "empty", 0, "no export button");
										tally.empty++;
										done.Add(key);
										break;
									}
									var sessionLost = reason.Contains("authoris", StringComparison.OrdinalIgnoreCase)
										|| reason.Contains("login", StringComparison.OrdinalIgnoreCase)
										|| reason.Contains("session", StringComparison.OrdinalIgnoreCase);
									if (sessionLost && logins < MaxLoginsPerJob && attempt < 3)
									{
										Console.WriteLine($"  {start:yyyy-MM-dd}..{end:yyyy-MM-dd} {loopText,-28} session lost, signing in again");
										loggedIn = false;
										continue;
									}
									if (attempt == 1 && !sessionLost)
									{
										Console.WriteLine($"  {start:yyyy-MM-dd}..{end:yyyy-MM-dd} {loopText,-28} retry after: {reason}");
										await Task.Delay(TimeSpan.FromSeconds(10));
										continue;
									}
									if (attempt == 2 && !sessionLost && logins < MaxLoginsPerJob)
									{
										// A session the portal ended silently (another login on the same account, a
										// portal restart) shows up as an export that never downloads, not as a login
										// page - so the last resort is a fresh sign-in, not another retry.
										Console.WriteLine($"  {start:yyyy-MM-dd}..{end:yyyy-MM-dd} {loopText,-28} still failing ({reason}); signing in again in case the session expired");
										loggedIn = false;
										continue;
									}
									Console.WriteLine($"  {start:yyyy-MM-dd}..{end:yyyy-MM-dd} {loopText,-28} FAILED: {reason}");
									Append(progressPath, job.Key, start, end, loopText, "failed", 0, reason);
									tally.failed++;
									break;
								}
							}
							await Task.Delay(TimeSpan.FromSeconds(2));
						}
					}
					summary[job.Key] = tally;
				}
			}
			finally
			{
				await portal.DisposeAsync();
			}

			Console.WriteLine();
			Console.WriteLine($"{"JOB",-34}{"OK",6}{"EMPTY",7}{"FAILED",8}{"SKIPPED",9}");
			foreach (var (k, t) in summary)
				Console.WriteLine($"{k,-34}{t.ok,6}{t.empty,7}{t.failed,8}{t.skipped,9}");
			Console.WriteLine("Re-run the same command to retry the failed ones; finished chunks are skipped.");
			return summary.Values.Any(t => t.failed > 0) ? 1 : 0;
		}

		/// <summary>
		/// Signs in under a lock shared by every backfill process on this machine. All the
		/// portal logins send their OTP to the same handset and the relay hands out the newest
		/// code, so two logins in flight at once swap codes and both fail; one at a time, they
		/// do not. The nightly service does not take the lock, which is what the pause is for.
		/// </summary>
		private static async Task<bool> LoginAsync(IfmsPortalClient portal, IfmsAccountCredentials account)
		{
			// A login that fails for a passing reason (no OTP because the handset or the portal's
			// SMS gateway is down, portal offline) is tried again every ten minutes for up to two
			// hours rather than ending the run in the middle of the night.
			for (var round = 1; ; round++)
			{
				if (await LoginOnceAsync(portal, account)) return true;
				if (round >= 12) return false;
				Console.WriteLine($"  sign-in failed; trying again in 10 minutes ({round} of 12)");
				await Task.Delay(TimeSpan.FromMinutes(10));
			}
		}

		private static async Task<bool> LoginOnceAsync(IfmsPortalClient portal, IfmsAccountCredentials account)
		{
			var lockPath = Path.Combine(AppContext.BaseDirectory, "login.lock");
			FileStream? held = null;
			var waited = false;
			while (held is null)
			{
				try { held = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
				catch (IOException)
				{
					if (!waited) { Console.WriteLine("  another run is signing in; waiting for it to finish first"); waited = true; }
					await Task.Delay(TimeSpan.FromSeconds(5));
				}
			}
			try
			{
				var loginTask = portal.LoginAsync(account, runId: 0, CancellationToken.None);
				// The automatic path (8 CAPTCHA reads, one OTP) is over within four minutes. Past
				// that the login is waiting for a person to answer the CAPTCHA in the app, which
				// can take hours, and the other runs must not queue behind it.
				if (await Task.WhenAny(loginTask, Task.Delay(TimeSpan.FromMinutes(4))) != loginTask)
				{
					Console.WriteLine("  this login is waiting on the app; the login turn is released to the other runs meanwhile");
					held.Dispose();
					held = null;
				}
				var login = await loginTask;
				if (!login.Success)
				{
					Console.WriteLine($"Not signed in: {login.FailureReason}");
					return false;
				}
				Console.WriteLine($"Signed in ({login.CaptchaMethod}, OTP {login.OtpMethod ?? "not requested"}).");
				// A moment for the portal to settle before the next process starts its own login.
				if (held is not null) await Task.Delay(TimeSpan.FromSeconds(10));
				return true;
			}
			finally
			{
				held?.Dispose();
			}
		}

		/// <summary>Set when a discovery hit the portal's Internal Server Error, which leaves the session broken.</summary>
		private static bool _sessionSuspect;

		private static RunTokens Tokens(IfmsAccountCredentials account, DateTime start, DateTime end, Dictionary<string, string> pinned)
		{
			var tokens = new RunTokens(end, DateTime.Now, account.UserName)
				.WithLiteral("company", account.CompanyName)
				.WithLiteral("accountKey", account.AccountKey)
				.WithDate("fromDate", start).WithDate("quarterStart", start).WithDate("monthStart", start).WithDate("yesterday", start)
				.WithDate("toDate", end).WithDate("today", end).WithDate("reportDate", end).WithDate("monthEnd", end);
			foreach (var (k, v) in pinned) tokens.WithLiteral(k, v);
			return tokens;
		}

		/// <summary>Every combination of the job's loops, discovering inner values per outer value (plant -> product).</summary>
		private static async Task<List<List<(string Name, string Value)>>> ExpandAsync(
			IfmsPortalClient portal, ReportJob job, RunTokens tokens, int index, List<(string Name, string Value)> chosen,
			Dictionary<string, string> pinned, CancellationToken ct)
		{
			if (index >= job.ForEach.Count) return new List<List<(string, string)>> { new(chosen) };
			var loop = job.ForEach[index];
			IReadOnlyList<string> values;
			if (pinned.TryGetValue(loop.TokenName, out var given)) values = new[] { given };
			else if (loop.Values.Count > 0) values = loop.Values;
			else
			{
				foreach (var (n, v) in chosen) tokens.WithLiteral(n, v);
				try
				{
					values = await portal.DiscoverLoopValuesAsync(job, loop, tokens, ct);
				}
				catch (Exception ex) when (loop.ContinueOnFailure && chosen.Count > 0)
				{
					// An inner list that never loads (the portal answers "Internal Server Error" for
					// one plant's products, every time) must not sink the whole day: skip that branch.
					Console.WriteLine($"  {string.Join("/", chosen.Select(c => c.Value))}: could not read the {loop.TokenName} list ({ex.Message.Split(Environment.NewLine)[0]}); skipped");
					_sessionSuspect = true;   // the next page after that error comes back blank, then the login page
					return new List<List<(string, string)>>();
				}
			}
			var result = new List<List<(string, string)>>();
			foreach (var value in values)
			{
				var next = new List<(string, string)>(chosen) { (loop.TokenName, value) };
				result.AddRange(await ExpandAsync(portal, job, tokens, index + 1, next, pinned, ct));
			}
			return result;
		}

		/// <summary>
		/// 90 days for a job whose steps use {{quarterStart}} (Company Sale, Wholesaler Sale: dated
		/// invoice rows, portal accepts up to 90 days); otherwise one download per date, because a
		/// stock report is a position as on a date.
		/// </summary>
		private static string DefaultStep(ReportJob job) =>
			job.Steps.Any(s => (s.Value ?? "").Contains("{{quarterStart}}", StringComparison.OrdinalIgnoreCase)) ? "90" : "day";

		private static string StepLabel(string step) => int.TryParse(step, out var n) ? $"{n} days" : step;

		private static List<(DateTime Start, DateTime End)> Chunks(DateTime from, DateTime to, string step)
		{
			var list = new List<(DateTime, DateTime)>();
			var cursor = from.Date;
			while (cursor <= to.Date)
			{
				DateTime end = step switch
				{
					"day" => cursor,
					"week" => cursor.AddDays(6),
					"month" => new DateTime(cursor.Year, cursor.Month, DateTime.DaysInMonth(cursor.Year, cursor.Month)),
					_ => cursor.AddDays(int.Parse(step) - 1)
				};
				if (end > to.Date) end = to.Date;
				list.Add((cursor, end));
				cursor = end.AddDays(1);
			}
			return list;
		}

		private static (TimeSpan From, TimeSpan To)? ParsePause(string text)
		{
			var parts = text.Split('-', 2);
			if (parts.Length != 2) return null;
			if (!TimeSpan.TryParseExact(parts[0].Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out var from)) return null;
			if (!TimeSpan.TryParseExact(parts[1].Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out var to)) return null;
			return (from, to);
		}

		/// <summary>Waits out the nightly service's window (local time). True when it waited, so the caller signs in again.</summary>
		private static async Task<bool> PauseForNightlyAsync((TimeSpan From, TimeSpan To) pause)
		{
			if (pause.From == pause.To) return false;
			var now = DateTime.Now;
			var t = now.TimeOfDay;
			var inside = pause.From < pause.To ? (t >= pause.From && t < pause.To) : (t >= pause.From || t < pause.To);
			if (!inside) return false;
			var resume = now.Date + pause.To;
			if (resume <= now) resume = resume.AddDays(1);
			Console.WriteLine($"  paused until {resume:HH:mm} for the nightly run (it takes the portal session); signing in again after.");
			await Task.Delay(resume - now);
			return true;
		}

		private static string Key(string job, DateTime start, DateTime end, string loops) => $"{job}|{start:yyyy-MM-dd}|{end:yyyy-MM-dd}|{loops}";

		private static string Sanitize(string s) => string.Concat(s.Select(c => Path.GetInvalidFileNameChars().Contains(c) || c == ' ' ? '_' : c));

		private static HashSet<string> LoadDone(string path)
		{
			var done = new HashSet<string>();
			if (!File.Exists(path)) return done;
			foreach (var line in File.ReadLines(path))
			{
				try
				{
					using var doc = JsonDocument.Parse(line);
					var r = doc.RootElement;
					var status = r.GetProperty("status").GetString();
					if (status is "ok" or "empty")
						done.Add(Key(r.GetProperty("job").GetString()!, r.GetProperty("from").GetDateTime(), r.GetProperty("to").GetDateTime(), r.GetProperty("loops").GetString() ?? ""));
				}
				catch { /* a torn last line from a killed process is ignored */ }
			}
			return done;
		}

		private static void Append(string path, string job, DateTime from, DateTime to, string loops, string status, long bytes, string? detail)
		{
			var line = JsonSerializer.Serialize(new { at = DateTime.Now, job, from = from.ToString("yyyy-MM-dd"), to = to.ToString("yyyy-MM-dd"), loops, status, bytes, detail });
			File.AppendAllText(path, line + Environment.NewLine);
		}
	}
}
