using System.Text.RegularExpressions;
using SPIC.Core.Help;

namespace SPIC.HelpTools;

/// <summary>Paths in the repository and loading of the guide SOURCE files (not the embedded copy),
/// so the tools see edits that have not been built yet.</summary>
internal static class Repo
{
	private static readonly Regex PageDirective = new(@"^\s*@page\s+""([^""]+)""", RegexOptions.Compiled | RegexOptions.Multiline);

	public static string Root { get; } = FindRoot();

	public static string ContentDir => Path.Combine(Root, "SPIC.Core", "HelpContent");
	public static string SharedDir => Path.Combine(Root, "SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared");
	public static string PagesDir => Path.Combine(SharedDir, "Pages");
	public static string ImagesDir => Path.Combine(SharedDir, "wwwroot", "help");
	public static string ToolDir => Path.Combine(Root, "tools", "help-tools");

	public static string GuidePath(string module, string lang) => Path.Combine(ContentDir, module, $"guide.{lang}.md");

	public static HelpLibrary LoadLibrary()
	{
		var files = Directory.EnumerateFiles(ContentDir, "*.*", SearchOption.AllDirectories)
			.Where(f => f.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
			.Select(f => ("help/" + Path.GetRelativePath(ContentDir, f).Replace('\\', '/'), File.ReadAllText(f)));
		return new HelpLibrary(files);
	}

	public static IReadOnlyList<string> PageRoutes() =>
		Directory.EnumerateFiles(PagesDir, "*.razor", SearchOption.AllDirectories)
			.SelectMany(f => PageDirective.Matches(File.ReadAllText(f)).Select(m => m.Groups[1].Value))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

	private static readonly Regex DataHelpAttribute = new(@"data-help=""([^""]+)""", RegexOptions.Compiled);

	/// <summary>Every data-help="…" id in the shared Razor library (tour targets).</summary>
	public static IReadOnlySet<string> DataHelpIds() =>
		Directory.EnumerateFiles(SharedDir, "*.razor", SearchOption.AllDirectories)
			.SelectMany(f => DataHelpAttribute.Matches(File.ReadAllText(f)).Select(m => m.Groups[1].Value))
			.ToHashSet(StringComparer.Ordinal);

	/// <summary>
	/// Sets a screen section's picture keys in guide.en.md: "screenshot"/"hotspots" for desktop,
	/// "screenshot-phone"/"hotspots-phone" for phone. Other header lines stay as they are.
	/// </summary>
	public static void SetScreenKeys(string module, string sectionId, string kind, string fileName, IReadOnlyList<string>? spots)
	{
		var path = GuidePath(module, "en");
		var text = File.ReadAllText(path).Replace("\r\n", "\n");
		var marker = "::: section " + sectionId + "\n";
		var start = text.IndexOf(marker, StringComparison.Ordinal);
		if (start < 0) throw new InvalidOperationException($"{module}: section {sectionId} not found");
		var headerStart = start + marker.Length;
		var headerEnd = text.IndexOf("\n\n", headerStart, StringComparison.Ordinal);
		var header = text[headerStart..headerEnd].Split('\n').ToList();

		var imageKey = kind == "phone" ? "screenshot-phone" : "screenshot";
		var spotsKey = kind == "phone" ? "hotspots-phone" : "hotspots";
		void Set(string key, string? value)
		{
			var i = header.FindIndex(l => l.StartsWith(key + ":", StringComparison.Ordinal));
			if (value is null) { if (i >= 0) header.RemoveAt(i); return; }
			var line = $"{key}: {value}";
			if (i >= 0) header[i] = line; else header.Add(line);
		}
		Set(imageKey, fileName);
		Set(spotsKey, spots is null ? null : string.Join(" | ", spots));

		text = text[..headerStart] + string.Join("\n", header) + text[headerEnd..];
		File.WriteAllText(path, text, new System.Text.UTF8Encoding(false));
	}

	private static string FindRoot()
	{
		var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
		while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SpicOne.sln")))
			dir = dir.Parent;
		return dir?.FullName ?? throw new InvalidOperationException("Run help-tools from inside the SPIC-ONE repository.");
	}
}
