using SPIC.Core.Entities;
using SPIC.Core.Help;
using SPIC.HelpTools;

// help-tools: Help Desk developer tools. See tools/help-tools/README.md.
var command = args.FirstOrDefault()?.ToLowerInvariant();
return command switch
{
	"check" => Check(),
	"translate" => await Translate(args),
	_ => Usage(),
};

static string? Option(string[] args, string name)
{
	var i = Array.FindIndex(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
	return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static async Task<int> Translate(string[] args)
{
	if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
	{
		Console.Error.WriteLine("Set ANTHROPIC_API_KEY first (never commit it).");
		return 2;
	}

	var model = Environment.GetEnvironmentVariable("HELP_TOOLS_MODEL") is { Length: > 0 } m ? m : "claude-opus-5-5";
	var onlyModule = Option(args, "--module");
	var onlyLang = Option(args, "--lang");
	var library = Repo.LoadLibrary();
	var translator = new Translator(model);
	var failures = new List<string>();

	var languages = library.Config.Languages
		.Where(l => !l.Code.Equals("en", StringComparison.OrdinalIgnoreCase))
		.Where(l => onlyLang is null || l.Code.Equals(onlyLang, StringComparison.OrdinalIgnoreCase))
		.ToList();
	var modules = library.Modules.Where(x => onlyModule is null || x.Equals(onlyModule, StringComparison.OrdinalIgnoreCase)).ToList();
	if (modules.Count == 0 || languages.Count == 0)
	{
		Console.Error.WriteLine("Nothing to translate: check --module / --lang.");
		return 2;
	}

	Console.WriteLine($"Model: {model}");
	foreach (var module in modules)
	{
		var en = library.English(module)!;
		foreach (var lang in languages)
		{
			var existing = library.Raw(module, lang.Code);
			var todo = HelpTranslationPlanner.SectionsToTranslate(en, existing);
			Console.WriteLine($"{module} / {lang.Code}: {todo.Count} of {en.Sections.Count} sections to translate");
			if (todo.Count == 0 && existing is not null) continue;

			var result = new Dictionary<string, (string Title, string Body, HelpConfidence Confidence)>(StringComparer.OrdinalIgnoreCase);
			foreach (var t in existing?.Sections ?? Array.Empty<HelpSection>())
				if (!todo.Contains(t.Id, StringComparer.OrdinalIgnoreCase))
					result[t.Id] = (t.Title, t.Body, t.Confidence);

			// Guide title: translated once, kept afterwards.
			if (existing is not null && existing.Title != module)
				result[HelpGuideWriter.GuideTitleKey] = (existing.Title, string.Empty, HelpConfidence.Ok);
			else if (await translator.TranslateAsync(lang, library.GlossaryFor(lang.Code), en.Title, string.Empty) is { } gt)
				result[HelpGuideWriter.GuideTitleKey] = gt;

			int ok = 0, low = 0;
			foreach (var id in todo)
			{
				var s = en.Sections.First(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
				Console.WriteLine($"    {id}");
				var translated = await translator.TranslateAsync(lang, library.GlossaryFor(lang.Code), s.Title, s.Body);
				if (translated is null) { failures.Add($"{module}/{lang.Code}/{id}"); continue; }
				result[id] = translated.Value;
				if (translated.Value.Confidence == HelpConfidence.Low) low++; else ok++;
			}

			File.WriteAllText(Repo.GuidePath(module, lang.Code), HelpGuideWriter.Write(en, lang.Code, result), new System.Text.UTF8Encoding(false));
			Console.WriteLine($"  wrote guide.{lang.Code}.md: {ok} ok, {low} low confidence, {todo.Count - ok - low} failed");
		}
	}

	if (failures.Count > 0)
	{
		Console.WriteLine();
		Console.WriteLine("Left in English (re-run to retry):");
		foreach (var f in failures) Console.WriteLine($"  {f}");
	}
	return failures.Count == 0 ? 0 : 1;
}

static int Usage()
{
	Console.WriteLine("""
		help-tools <command>

		  check          List screens without a guide, stale or missing translations,
		                 unknown permissions and missing screenshots. Exit 1 when anything is found.
		  translate [--module m] [--lang ta|te|mr|hi]
		                 Translate new or changed English sections with Claude and check each one
		                 by back-translation. Needs ANTHROPIC_API_KEY; model from HELP_TOOLS_MODEL
		                 (default claude-opus-5-5).
		""");
	return 2;
}

static int Check()
{
	var library = Repo.LoadLibrary();
	var routes = Repo.PageRoutes();
	var ignorePath = Path.Combine(Repo.ToolDir, "coverage-ignore.txt");
	var ignored = File.Exists(ignorePath)
		? File.ReadAllLines(ignorePath).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith('#')).ToList()
		: new List<string>();

	var issues = HelpCoverage.Check(
		library,
		routes,
		ignored,
		relative => File.Exists(Path.Combine(Repo.ImagesDir, relative.Replace('/', Path.DirectorySeparatorChar))),
		Enum.GetNames<PagePermission>());

	foreach (var group in issues.GroupBy(i => i.Module).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
	{
		Console.WriteLine($"[{group.Key}]");
		foreach (var issue in group.OrderBy(i => i.Kind).ThenBy(i => i.Detail, StringComparer.OrdinalIgnoreCase))
			Console.WriteLine($"  {issue.Kind,-20} {issue.Detail}");
	}

	var uncovered = issues.Count(i => i.Kind == "uncovered-route");
	var counted = routes.Count(r => !ignored.Any(x => HelpRoutes.Shape(x) == HelpRoutes.Shape(r)));
	Console.WriteLine();
	Console.WriteLine($"Guides: {library.Modules.Count} modules. Screens: {uncovered} uncovered of {counted}. Issues: {issues.Count}.");
	return issues.Count == 0 ? 0 : 1;
}
