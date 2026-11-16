using SPIC.Core.Entities;
using SPIC.Core.Help;
using SPIC.HelpTools;

// help-tools: Help Desk developer tools. See tools/help-tools/README.md.
var command = args.FirstOrDefault()?.ToLowerInvariant();
return command switch
{
	"check" => Check(),
	_ => Usage(),
};

static int Usage()
{
	Console.WriteLine("""
		help-tools <command>

		  check          List screens without a guide, stale or missing translations,
		                 unknown permissions and missing screenshots. Exit 1 when anything is found.
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
