using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SPIC.Ifms.Automation.Options;
using SPIC.Ifms.Automation.Portal.Challenges;

namespace SPIC.Ifms.Automation.Tools
{
	/// <summary>
	/// Measures how well the OCR actually reads the portal's CAPTCHA, before
	/// anybody depends on it at four in the morning.
	///
	/// Run it with:  dotnet run -- test-captcha 25
	///
	/// It pulls that many fresh CAPTCHA images, saves each one next to its
	/// processed version and what the OCR made of it, and writes an index.html you
	/// can open to check them side by side. Reading twenty of them takes a minute
	/// and tells you the real hit rate — which is the number that decides whether
	/// five automatic attempts is generous or not enough.
	///
	/// Tuning, in the order worth trying:
	///   SaturationThreshold  raise if background survives, lower if strokes break
	///   UpscaleFactor        3 to 5
	///   ExpectedLength       set once you know it, to reject obvious misreads
	/// </summary>
	public static class CaptchaCalibrationTool
	{
		/// <summary>
		/// Re-runs the OCR over CAPTCHA images captured by an earlier sample.
		///
		/// Tuning needs to be repeatable, and it should not mean fetching a fresh
		/// batch from the portal every time a threshold moves. Replaying a saved
		/// folder also compares like with like: the same images before and after a
		/// change, so an improvement is an improvement and not a lucky draw.
		/// </summary>
		public static async Task<int> ReplayAsync(
			IServiceProvider services,
			string folder,
			CancellationToken cancellationToken)
		{
			var solver = services.GetServices<ICaptchaSolver>()
				.OfType<OcrCaptchaSolver>()
				.FirstOrDefault();

			if (solver is null)
			{
				Console.WriteLine("The OCR solver is not registered.");
				return 1;
			}

			if (!Directory.Exists(folder))
			{
				Console.WriteLine($"No such folder: {folder}");
				return 1;
			}

			var originals = Directory
				.GetFiles(folder, "*_original.png")
				.OrderBy(f => f)
				.ToList();

			if (originals.Count == 0)
			{
				Console.WriteLine($"No captured CAPTCHA images in {folder}");
				return 1;
			}

			Console.WriteLine($"Replaying {originals.Count} saved images from {folder}");
			Console.WriteLine();

			var results = new List<(int Index, string? Reading, double Confidence)>();

			foreach (var path in originals)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var name = Path.GetFileName(path);
				var index = int.TryParse(name.AsSpan(0, 2), out var parsed) ? parsed : results.Count + 1;

				var png = await File.ReadAllBytesAsync(path, cancellationToken);

				// Rewrite the processed image too, so the index page shows what the
				// current settings actually produce rather than the old ones.
				await File.WriteAllBytesAsync(
					Path.Combine(folder, $"{index:00}_processed.png"),
					solver.RenderPreprocessed(png),
					cancellationToken);

				var answer = await solver.SolveAsync(
					new CaptchaChallenge { ImagePng = png, Attempt = index },
					cancellationToken);

				results.Add((index, answer?.Value, answer?.Confidence ?? 0));

				Console.WriteLine(answer is null
					? $"  {index:00}  (unreadable)"
					: $"  {index:00}  {answer.Value,-10} {answer.Confidence:P0}");
			}

			await WriteIndexAsync(folder, results, cancellationToken);

			var read = results.Count(r => r.Reading is not null);

			Console.WriteLine();
			Console.WriteLine($"{read} of {results.Count} produced a reading.");
			Console.WriteLine($"Open {Path.Combine(folder, "index.html")} and count the correct ones.");

			return 0;
		}

		public static async Task<int> RunAsync(
			IServiceProvider services,
			int sampleSize,
			CancellationToken cancellationToken)
		{
			var options = services.GetRequiredService<IOptions<IfmsOptions>>().Value;
			var solver = services.GetServices<ICaptchaSolver>()
				.OfType<OcrCaptchaSolver>()
				.FirstOrDefault();

			if (solver is null)
			{
				Console.WriteLine("The OCR solver is not registered.");
				return 1;
			}

			var url = $"{options.BaseUrl.TrimEnd('/')}/mFMS/captcha.jsp";

			var folder = Path.Combine(
				AppContext.BaseDirectory,
				"captcha-calibration",
				DateTime.Now.ToString("yyyy-MM-dd_HHmmss"));

			Directory.CreateDirectory(folder);

			var http = services.GetRequiredService<IHttpClientFactory>().CreateClient("captcha-calibration");
			http.Timeout = TimeSpan.FromSeconds(30);
			http.DefaultRequestHeaders.UserAgent.ParseAdd(options.Browser.UserAgent);

			Console.WriteLine($"Sampling {sampleSize} CAPTCHA images from {url}");
			Console.WriteLine($"Writing to {folder}");
			Console.WriteLine();

			var results = new List<(int Index, string? Reading, double Confidence)>();

			for (var i = 1; i <= sampleSize; i++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				try
				{
					var png = await http.GetByteArrayAsync(url, cancellationToken);
					await File.WriteAllBytesAsync(
						Path.Combine(folder, $"{i:00}_original.png"), png, cancellationToken);

					await File.WriteAllBytesAsync(
						Path.Combine(folder, $"{i:00}_processed.png"),
						solver.RenderPreprocessed(png),
						cancellationToken);

					var answer = await solver.SolveAsync(
						new CaptchaChallenge { ImagePng = png, Attempt = i },
						cancellationToken);

					results.Add((i, answer?.Value, answer?.Confidence ?? 0));

					Console.WriteLine(answer is null
						? $"  {i:00}  (unreadable)"
						: $"  {i:00}  {answer.Value,-10} {answer.Confidence:P0}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"  {i:00}  failed: {ex.Message}");
					results.Add((i, null, 0));
				}

				// Space the requests out; there is no reason to hammer the portal.
				await Task.Delay(TimeSpan.FromMilliseconds(700), cancellationToken);
			}

			await WriteIndexAsync(folder, results, cancellationToken);

			var read = results.Count(r => r.Reading is not null);

			Console.WriteLine();
			Console.WriteLine($"{read} of {results.Count} produced a reading.");
			Console.WriteLine("Open index.html in that folder and count how many are actually correct.");
			Console.WriteLine();
			Console.WriteLine("If the true rate is p, five attempts succeed with probability 1-(1-p)^5:");
			Console.WriteLine("   p=0.20 -> 67%    p=0.40 -> 92%    p=0.60 -> 99%");
			Console.WriteLine("Below about 0.35 the phone will be asked most mornings; raise the");
			Console.WriteLine("attempt count or retune the preprocessing before relying on it.");

			return 0;
		}

		private static async Task WriteIndexAsync(
			string folder,
			List<(int Index, string? Reading, double Confidence)> results,
			CancellationToken cancellationToken)
		{
			var sb = new StringBuilder();

			sb.AppendLine("<!doctype html><meta charset='utf-8'>");
			sb.AppendLine("<title>CAPTCHA calibration</title>");
			sb.AppendLine("<style>body{font-family:system-ui;margin:24px;background:#f8fafc}" +
						  "table{border-collapse:collapse;background:#fff}" +
						  "td,th{border:1px solid #e2e8f0;padding:10px 14px;text-align:left}" +
						  "img{height:50px;image-rendering:pixelated}" +
						  "code{font-size:18px;letter-spacing:2px}</style>");
			sb.AppendLine("<h1>CAPTCHA calibration</h1>");
			sb.AppendLine("<p>Compare each image with what the OCR read. Count the matches — " +
						  "that fraction is the real per-attempt hit rate.</p>");
			sb.AppendLine("<table><tr><th>#</th><th>Original</th><th>What the OCR sees</th>" +
						  "<th>OCR read</th><th>Confidence</th></tr>");

			foreach (var (index, reading, confidence) in results)
			{
				sb.AppendLine(
					$"<tr><td>{index:00}</td>" +
					$"<td><img src='{index:00}_original.png'></td>" +
					$"<td><img src='{index:00}_processed.png'></td>" +
					$"<td><code>{reading ?? "&mdash;"}</code></td>" +
					$"<td>{confidence:P0}</td></tr>");
			}

			sb.AppendLine("</table>");

			await File.WriteAllTextAsync(
				Path.Combine(folder, "index.html"), sb.ToString(), cancellationToken);
		}
	}
}
