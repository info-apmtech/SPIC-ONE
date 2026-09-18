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
	///   dotnet SPIC.Ifms.Automation.dll backfill all from=2024-04-01 step=month account=greenstar
	///   dotnet SPIC.Ifms.Automation.dll backfill retail-stocks-greenstar from=2024-04-01 dry=true
	///
	/// Logs in once, then for every period chunk (month by default, or week/day) and every
	/// loop value (state, plant/product) downloads the report into
	/// downloads/backfill/&lt;job&gt;/&lt;from&gt;_&lt;to&gt;/&lt;loop values&gt;/. Nothing is uploaded.
	///
	/// Every result is appended to downloads/backfill/progress.jsonl, and a chunk that already
	/// has a line with status ok or empty is skipped, so the command can be re-run after a
	/// portal timeout, an expired session or a killed process and only does what is left.
	/// A session that dies mid-way is re-established, up to three logins per job.
	///
	/// The date tokens are overridden per chunk: everything a job uses as the start of its
	/// range (fromDate, quarterStart, monthStart, yesterday) becomes the chunk start; everything
	/// it uses as the end (toDate, today, reportDate, monthEnd) becomes the chunk end.
	/// </summary>
	public static class BackfillCommand
	{
		private const int MaxLoginsPerJob = 3;

		public static async Task<int> RunAsync(IServiceProvider services, string[] args)
		{
			var keys = args.Skip(1).Where(a => !a.Contains('=')).ToList();
			var options = args.Skip(1)
				.Where(a => a.Contains('='))
				.Select(a => a.Split('=', 2))
				.ToDictionary(p => p[0], p => p[1], StringComparer.OrdinalIgnoreCase);

			if (keys.Count == 0 || !options.TryGetValue("from", out var fromText))
			{
				Console.WriteLine("usage: backfill <jobKey|all> from=YYYY-MM-DD [to=YYYY-MM-DD] [step=month|week|day] [account=key] [dry=true] [token=value ...]");
				return 1;
			}

			var from = DateTime.ParseExact(fromText, "yyyy-MM-dd", CultureInfo.InvariantCulture);
			var to = options.TryGetValue("to", out var toText)
				? DateTime.ParseExact(toText, "yyyy-MM-dd", CultureInfo.InvariantCulture)
				: DateTime.Today.AddDays(-1);
			var step = options.TryGetValue("step", out var stepText) ? stepText.ToLowerInvariant() : "month";
			var dry = options.TryGetValue("dry", out var dryText) && dryText.Equals("true", StringComparison.OrdinalIgnoreCase);
			var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "from", "to", "step", "account", "dry" };
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
			var jobs = selected
				.Where(j => string.IsNullOrWhiteSpace(j!.AccountKey) || string.Equals(j.AccountKey, account.AccountKey, StringComparison.OrdinalIgnoreCase))
				.OrderBy(j => j!.Order)
				.Select(j => j!)
				.ToList();

			var chunks = Chunks(from, to, step);
			var root = Path.Combine(AppContext.BaseDirectory, "downloads", "backfill");
			Directory.CreateDirectory(root);
			var progressPath = Path.Combine(root, "progress.jsonl");
			var done = LoadDone(progressPath);

			Console.WriteLine($"Backfill {account.CompanyName}: {jobs.Count} job(s), {chunks.Count} {step} chunk(s) from {from:yyyy-MM-dd} to {to:yyyy-MM-dd}.");
			Console.WriteLine($"Files under {root}; progress in {progressPath}. Nothing is uploaded.");
			if (dry)
			{
				foreach (var j in jobs)
					foreach (var (start, end) in chunks)
						Console.WriteLine($"  {j.Key} {start:yyyy-MM-dd}..{end:yyyy-MM-dd} {(done.Contains(Key(j.Key, start, end, "")) ? "(done)" : "")}");
				return 0;
			}

			var portal = scope.ServiceProvider.GetRequiredService<IfmsPortalClient>();
			var summary = new Dictionary<string, (int ok, int empty, int failed, int skipped)>();
			var loggedIn = false;

			try
			{
				foreach (var job in jobs)
				{
					var logins = 0;
					var tally = (ok: 0, empty: 0, failed: 0, skipped: 0);
					Console.WriteLine();
					Console.WriteLine($"=== {job.Key} : {job.Title} ===");

					foreach (var (start, end) in chunks)
					{
						var chunkDir = Path.Combine(root, job.Key, $"{start:yyyy-MM-dd}_{end:yyyy-MM-dd}");
						List<List<(string Name, string Value)>> combinations;
						try
						{
							if (!loggedIn) { logins++; loggedIn = await LoginAsync(portal, account); if (!loggedIn) return 1; }
							var baseTokens = Tokens(account, start, end, pinned);
							combinations = job.ForEach.Count == 0
								? new List<List<(string, string)>> { new() }
								: await ExpandAsync(portal, job, baseTokens, 0, new List<(string, string)>(), pinned, CancellationToken.None);
						}
						catch (Exception ex)
						{
							Console.WriteLine($"  {start:yyyy-MM-dd}..{end:yyyy-MM-dd}  FAILED discovering loop values: {ex.Message.Split(Environment.NewLine)[0]}");
							Append(progressPath, job.Key, start, end, "", "failed", 0, ex.Message);
							tally.failed++;
							continue;
						}

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
									var sessionLost = reason.Contains("authoris", StringComparison.OrdinalIgnoreCase)
										|| reason.Contains("login", StringComparison.OrdinalIgnoreCase)
										|| reason.Contains("session", StringComparison.OrdinalIgnoreCase);
									if (sessionLost && logins < MaxLoginsPerJob && attempt < 3)
									{
										Console.WriteLine($"  {start:yyyy-MM-dd}..{end:yyyy-MM-dd} {loopText,-28} session lost, signing in again");
										loggedIn = false;
										continue;
									}
									if (attempt < 2 && !sessionLost)
									{
										Console.WriteLine($"  {start:yyyy-MM-dd}..{end:yyyy-MM-dd} {loopText,-28} retry after: {reason}");
										await Task.Delay(TimeSpan.FromSeconds(10));
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

		private static async Task<bool> LoginAsync(IfmsPortalClient portal, IfmsAccountCredentials account)
		{
			var login = await portal.LoginAsync(account, runId: 0, CancellationToken.None);
			if (!login.Success)
			{
				Console.WriteLine($"Not signed in: {login.FailureReason}");
				return false;
			}
			Console.WriteLine($"Signed in ({login.CaptchaMethod}, OTP {login.OtpMethod ?? "not requested"}).");
			return true;
		}

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
				values = await portal.DiscoverLoopValuesAsync(job, loop, tokens, ct);
			}
			var result = new List<List<(string, string)>>();
			foreach (var value in values)
			{
				var next = new List<(string, string)>(chosen) { (loop.TokenName, value) };
				result.AddRange(await ExpandAsync(portal, job, tokens, index + 1, next, pinned, ct));
			}
			return result;
		}

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
					_ => new DateTime(cursor.Year, cursor.Month, DateTime.DaysInMonth(cursor.Year, cursor.Month))
				};
				if (end > to.Date) end = to.Date;
				list.Add((cursor, end));
				cursor = end.AddDays(1);
			}
			return list;
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
