using System.Globalization;
using System.Text.Json;
using Microsoft.Playwright;

namespace SPIC.HelpTools;

/// <summary>
/// help-tools shoot: signs in to the LOCAL web app as local test users and photographs each guide
/// screen at desktop (1280 wide) and phone (390 wide) size. When the section has a guided tour, the
/// tour targets' positions on the picture become its numbered hotspots (in %), so the markers and
/// the tour always point at the same things. The guide's screenshot / hotspots keys are written in
/// guide.en.md. Uses the installed Microsoft Edge, so no browser download is needed.
/// </summary>
internal static class Shooter
{
	private sealed record Shot(string Module, string Section, string User, string Url, string? Wait);
	private sealed record ShotsFile(string BaseUrl, string Password, List<Shot> Shots);

	private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

	public static async Task<int> RunAsync(string? module, string? section, bool desktop, bool phone)
	{
		var file = JsonSerializer.Deserialize<ShotsFile>(File.ReadAllText(Path.Combine(Repo.Root, "tools", "help-demo", "shots.json")), Json)
			?? throw new InvalidOperationException("tools/help-demo/shots.json is empty.");
		// Each module may also list its shots in tools/help-demo/shots/<module>.json: { "shots": [ ... ] }.
		var extraDir = Path.Combine(Repo.Root, "tools", "help-demo", "shots");
		if (Directory.Exists(extraDir))
			foreach (var extra in Directory.EnumerateFiles(extraDir, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
				file.Shots.AddRange(JsonSerializer.Deserialize<ShotsFile>(File.ReadAllText(extra), Json)?.Shots ?? new List<Shot>());
		if (!Uri.TryCreate(file.BaseUrl, UriKind.Absolute, out var baseUri) || !baseUri.IsLoopback)
		{
			Console.Error.WriteLine($"Refusing to shoot {file.BaseUrl}: screenshots come from the LOCAL web app only (spicone-web-local on localhost).");
			return 2;
		}

		var shots = file.Shots
			.Where(s => module is null || s.Module.Equals(module, StringComparison.OrdinalIgnoreCase))
			.Where(s => section is null || s.Section.Equals(section, StringComparison.OrdinalIgnoreCase))
			.ToList();
		if (shots.Count == 0) { Console.Error.WriteLine("Nothing to shoot: check --module / --section."); return 2; }

		var library = Repo.LoadLibrary();
		using var playwright = await Playwright.CreateAsync();
		await using var browser = await playwright.Chromium.LaunchAsync(new() { Channel = "msedge", Headless = true });

		var failures = 0;
		foreach (var group in shots.GroupBy(s => s.User))
		{
			foreach (var (kind, viewport, mobile) in Sizes(desktop, phone))
			{
				await using var context = await browser.NewContextAsync(new()
				{
					ViewportSize = viewport,
					IsMobile = mobile,
					HasTouch = mobile,
					DeviceScaleFactor = 1,
					Locale = "en-IN",
					ReducedMotion = ReducedMotion.Reduce,
				});
				var page = await context.NewPageAsync();
				if (!await SignInAsync(page, file.BaseUrl, group.Key, file.Password))
				{
					Console.WriteLine($"  {group.Key}: sign-in failed, skipped");
					failures += group.Count();
					continue;
				}

				foreach (var shot in group)
				{
					var guide = library.English(shot.Module);
					var target = guide?.Sections.FirstOrDefault(s => s.Id.Equals(shot.Section, StringComparison.OrdinalIgnoreCase));
					if (target is null) { Console.WriteLine($"  {shot.Module}/{shot.Section}: no such section, skipped"); failures++; continue; }
					try
					{
						var (fileName, spots) = await ShootAsync(page, file.BaseUrl, shot, kind, viewport, target);
						Repo.SetScreenKeys(shot.Module, shot.Section, kind, fileName, spots);
						Console.WriteLine($"  {shot.Module}/{shot.Section} ({kind}): {fileName}{(spots is null ? "" : $", {spots.Count} hotspots")}");
					}
					catch (Exception ex)
					{
						Console.WriteLine($"  {shot.Module}/{shot.Section} ({kind}): failed - {ex.Message.Split('\n')[0]}");
						failures++;
					}
				}
			}
		}

		Console.WriteLine();
		Console.WriteLine("Next: review the pictures and the hotspots in the guide page, run 'help-tools check', commit.");
		return failures == 0 ? 0 : 1;
	}

	private static IEnumerable<(string Kind, ViewportSize Viewport, bool Mobile)> Sizes(bool desktop, bool phone)
	{
		if (desktop) yield return ("desktop", new ViewportSize { Width = 1280, Height = 900 }, false);
		if (phone) yield return ("phone", new ViewportSize { Width = 390, Height = 844 }, true);
	}

	private static async Task<bool> SignInAsync(IPage page, string baseUrl, string user, string password)
	{
		await page.GotoAsync(baseUrl.TrimEnd('/') + "/login", new() { WaitUntil = WaitUntilState.NetworkIdle });
		await page.FillAsync("input[placeholder='User Name']", user);
		await page.FillAsync("input[type=password]", password);
		await page.ClickAsync("button[type=submit]");
		try
		{
			await page.WaitForURLAsync(u => !u.Contains("/login", StringComparison.OrdinalIgnoreCase) && new Uri(u).AbsolutePath != "/", new() { Timeout = 20000 });
			return true;
		}
		catch (TimeoutException)
		{
			return false;
		}
	}

	private static async Task<(string File, List<string>? Spots)> ShootAsync(IPage page, string baseUrl, Shot shot, string kind, ViewportSize viewport, SPIC.Core.Help.HelpSection section)
	{
		await page.GotoAsync(baseUrl.TrimEnd('/') + shot.Url, new() { WaitUntil = WaitUntilState.NetworkIdle });
		if (!string.IsNullOrWhiteSpace(shot.Wait))
			await page.WaitForSelectorAsync(shot.Wait, new() { Timeout = 20000, State = WaitForSelectorState.Visible });
		await page.WaitForTimeoutAsync(1200); // skeletons and charts settle
		await page.EvaluateAsync("() => { const m = document.querySelector('.main-content'); (m || document.scrollingElement).scrollTo(0, 0); window.scrollTo(0, 0); }");

		// The page body only (no app menu or top bar), from its top to the bottom of the viewport.
		var box = await page.Locator(".main-content").First.BoundingBoxAsync() ?? new LocatorBoundingBoxResult { X = 0, Y = 0, Width = viewport.Width, Height = viewport.Height };
		// Tall enough to include the tour targets further down (tables), but not the whole page.
		var maxHeight = kind == "phone" ? 1500 : 1400;
		var clip = new Clip
		{
			X = (float)box.X,
			Y = (float)box.Y,
			Width = (float)Math.Min(box.Width, viewport.Width - box.X),
			Height = (float)Math.Min(Math.Max(box.Height, 200), maxHeight - box.Y),
		};
		await page.SetViewportSizeAsync(viewport.Width, (int)(clip.Y + clip.Height));
		await page.WaitForTimeoutAsync(400);

		var fileName = kind == "phone" ? $"{shot.Section}.phone.jpg" : $"{shot.Section}.png";
		var dir = Path.Combine(Repo.ImagesDir, shot.Module, "images");
		Directory.CreateDirectory(dir);
		await page.ScreenshotAsync(new()
		{
			Path = Path.Combine(dir, fileName),
			Clip = clip,
			Type = kind == "phone" ? ScreenshotType.Jpeg : ScreenshotType.Png,
			Quality = kind == "phone" ? 78 : null,
		});

		List<string>? spots = null;
		var tour = section.Screen?.Tour ?? Array.Empty<SPIC.Core.Help.HelpTourStep>();
		if (tour.Count > 0)
		{
			spots = new List<string>();
			foreach (var step in tour)
			{
				var r = await page.Locator(step.Target).First.BoundingBoxAsync();
				// Missing, hidden at this size, or outside the picture: no marker rather than a wrong one.
				var inside = r is not null && r.Width > 0 && r.Height > 0
					&& r.Y < clip.Y + clip.Height - 4 && r.Y + r.Height > clip.Y + 4
					&& r.X < clip.X + clip.Width - 4 && r.X + r.Width > clip.X + 4;
				if (!inside)
				{
					Console.WriteLine($"    warning: {shot.Section} ({kind}) step {step.N}: {step.Target} is not in the picture, no marker");
					spots.Add($"{step.N} @ -");
					continue;
				}
				var x = Clamp((r!.X - clip.X) / clip.Width * 100);
				var y = Clamp((r.Y - clip.Y) / clip.Height * 100);
				var w = Clamp(r.Width / clip.Width * 100, 100 - x);
				var h = Clamp(r.Height / clip.Height * 100, 100 - y);
				spots.Add(string.Create(CultureInfo.InvariantCulture, $"{step.N} @ {Round(x)},{Round(y)},{Round(w)},{Round(h)}"));
			}
		}
		await page.SetViewportSizeAsync(viewport.Width, viewport.Height);
		return (fileName, spots);
	}

	private static double Clamp(double v, double max = 100) => Math.Max(0, Math.Min(max, v));
	private static double Round(double v) => Math.Round(v, 1);
}
