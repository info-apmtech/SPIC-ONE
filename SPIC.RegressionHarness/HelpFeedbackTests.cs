using SPIC.Core.DTOs;
using SPIC.Core.Help;

namespace SPIC.RegressionHarness;

/// <summary>"Was this guide helpful?" votes: only real guides, screens and languages, short comments.</summary>
internal static class HelpFeedbackTests
{
	public static void Run()
	{
		Console.WriteLine("Help feedback rules");

		var lib = new HelpLibrary(new[]
		{
			("help.config.json", """{ "defaultLanguage": "en", "languages": [ { "code": "en", "name": "English", "native": "English", "voice": "x" }, { "code": "ta", "name": "Tamil", "native": "தமிழ்", "voice": "y" } ] }"""),
			("publicity/guide.en.md", HelpParserTests.SampleGuide),
		});

		Check.Equal("module-level vote is valid", null, HelpFeedbackRules.Validate(new HelpFeedbackDto("publicity", null, "ta", true, null), lib));
		Check.Equal("screen vote is valid", null, HelpFeedbackRules.Validate(new HelpFeedbackDto("publicity", "raise-request", "en", false, "Too long"), lib));
		Check.That("unknown module rejected", HelpFeedbackRules.Validate(new HelpFeedbackDto("ghost", null, "en", true, null), lib) is not null);
		Check.That("unknown section rejected", HelpFeedbackRules.Validate(new HelpFeedbackDto("publicity", "ghost", "en", true, null), lib) is not null);
		Check.That("unknown language rejected", HelpFeedbackRules.Validate(new HelpFeedbackDto("publicity", null, "fr", true, null), lib) is not null);
		Check.That("long comment rejected", HelpFeedbackRules.Validate(new HelpFeedbackDto("publicity", null, "en", true, new string('a', 501)), lib) is not null);
		Check.Equal("500-character comment accepted", null, HelpFeedbackRules.Validate(new HelpFeedbackDto("publicity", null, "en", true, new string('a', 500)), lib));
	}
}
