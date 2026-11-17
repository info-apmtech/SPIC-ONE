using SPIC.Core.Help;

namespace SPIC.RegressionHarness;

/// <summary>
/// Help centre home data: "Your path" per role (paths.json), the support box settings
/// (help.config.json) and What's new (from each guide's updated/note).
/// </summary>
internal static class HelpPathsTests
{
	public static void Run()
	{
		Console.WriteLine("Help paths, support and what's new");

		LoadsPaths();
		SupportDefaultsEmpty();
		SupportReadsNumbers();
		WhatsNewOrdersNewestFirstAndSkipsUndated();
	}

	private const string Paths = """
		{
		  "paths": [
		    { "id": "rmdo", "roles": [], "designations": ["RMDO"],
		      "title": { "en": "Your path as an RMDO", "ta": "RMDO ஆக உங்கள் பாதை" },
		      "steps": [
		        { "module": "publicity", "section": "raise-request", "minutes": 5, "tour": true },
		        { "module": "publicity", "section": "acknowledge", "minutes": 3 }
		      ] },
		    { "id": "farmer", "roles": ["Farmer"], "designations": [],
		      "title": { "en": "Your path as a farmer" },
		      "steps": [ { "module": "sas-sample-collection", "section": "my-samples", "minutes": 2 } ] }
		  ]
		}
		""";

	private static void LoadsPaths()
	{
		var lib = new HelpLibrary(new[] { ("paths.json", Paths) });
		Check.Equal("two paths", "rmdo,farmer", string.Join(",", lib.Paths.Select(p => p.Id)));
		Check.Equal("designation", "RMDO", lib.Paths[0].Designations.Single());
		Check.Equal("tamil title", "RMDO ஆக உங்கள் பாதை", lib.Paths[0].Title["ta"]);
		Check.Equal("step section", "acknowledge", lib.Paths[0].Steps[1].Section);
		Check.That("tour flag", lib.Paths[0].Steps[0].Tour && !lib.Paths[0].Steps[1].Tour);
		Check.Equal("minutes", 5, lib.Paths[0].Steps[0].Minutes);
		Check.Equal("role list", "Farmer", lib.Paths[1].Roles.Single());
		Check.Equal("no paths file means no paths", 0, new HelpLibrary(Array.Empty<(string, string)>()).Paths.Count);
	}

	private static void SupportDefaultsEmpty()
	{
		var lib = new HelpLibrary(new[] { ("help.config.json", """{ "defaultLanguage": "en", "languages": [] }""") });
		Check.That("support missing is not shown", lib.Support is null || !lib.Support.HasContact);
	}

	private static void SupportReadsNumbers()
	{
		var lib = new HelpLibrary(new[] { ("help.config.json", """
			{ "defaultLanguage": "en", "languages": [],
			  "support": { "phone": "+91 00000 00000", "whatsApp": "", "hours": { "en": "Mon–Sat, 9 to 6" } } }
			""") });
		Check.That("support with a phone is shown", lib.Support is { HasContact: true });
		Check.Equal("hours", "Mon–Sat, 9 to 6", lib.Support!.Hours!["en"]);
		Check.Equal("blank WhatsApp", "", lib.Support.WhatsApp);
	}

	private static HelpGuide Guide(string module, string front) =>
		HelpGuideParser.Parse(module, "en", $"---\nmodule: {module}\ntitle: {module} guide\n{front}\n---\n");

	private static void WhatsNewOrdersNewestFirstAndSkipsUndated()
	{
		var items = HelpWhatsNew.From(new[]
		{
			Guide("a", "updated: 2026-11-02\nnote: Lab reports in Marathi"),
			Guide("b", "note: no date"),
			Guide("c", "updated: 2026-11-16\nnote: Dealer guide added"),
			Guide("d", "updated: 2026-11-10"),
		}, 2);
		Check.Equal("newest two, undated skipped", "c,d", string.Join(",", items.Select(i => i.Module)));
		Check.Equal("note carried", "Dealer guide added", items[0].Note);
		Check.Equal("title carried", "c guide", items[0].Title);
	}
}
