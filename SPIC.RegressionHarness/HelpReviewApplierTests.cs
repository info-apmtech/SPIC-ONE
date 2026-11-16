using SPIC.Core.DTOs;
using SPIC.Core.Help;

namespace SPIC.RegressionHarness;

/// <summary>
/// help-tools apply-reviews: reviewer corrections downloaded from /Help/Review are written into
/// the translation file. Only corrections made against the CURRENT English apply (an older one
/// could re-introduce a removed step), the newest wins, and the English headers are kept.
/// </summary>
internal static class HelpReviewApplierTests
{
	public static void Run()
	{
		Console.WriteLine("Help review applier");

		AppliesCorrectionAndKeepsHeaders();
		SkipsCorrectionForStaleEnglish();
		NewestCorrectionWins();
	}

	private static (HelpGuide En, HelpGuide Ta) Guides()
	{
		var en = HelpGuideParser.Parse("publicity", "en", HelpLibraryTests.EnglishThree);
		var ta = HelpGuideParser.Parse("publicity", "ta", HelpGuideWriter.Write(en, "ta", new Dictionary<string, (string Title, string Body, HelpConfidence Confidence)>
		{
			["_title"] = ("விளம்பரம்", string.Empty, HelpConfidence.Ok),
			["one"] = ("ஒன்று", "பழைய உரை.", HelpConfidence.Low),
			["two"] = ("இரண்டு", "இரண்டாவது.", HelpConfidence.Ok),
		}));
		return (en, ta);
	}

	private static HelpCorrectionExportDto Correction(HelpGuide en, string id, string title, string body, DateTime at, string? sourceHash = null)
	{
		var s = en.Sections.Single(x => x.Id == id);
		return new HelpCorrectionExportDto("publicity", id, "ta", sourceHash ?? HelpHash.Of(s.Title, s.Body), title, body, "reviewer", at);
	}

	private static void AppliesCorrectionAndKeepsHeaders()
	{
		var (en, ta) = Guides();
		var (text, skipped) = HelpReviewApplier.Apply(en, ta, new[] { Correction(en, "one", "ஒன்று (சரி)", "திருத்திய உரை.", DateTime.UtcNow) });
		var back = HelpGuideParser.Parse("publicity", "ta", text);
		var one = back.Sections.Single(s => s.Id == "one");
		Check.Equal("nothing skipped", 0, skipped.Count);
		Check.Equal("corrected body written", "திருத்திய உரை.", one.Body);
		Check.Equal("corrected title written", "ஒன்று (சரி)", one.Title);
		Check.Equal("corrected section marked ok", HelpConfidence.Ok, one.Confidence);
		Check.Equal("routes still from English", "/PublicityRequest", string.Join(",", one.Routes));
		Check.Equal("other translations kept", "இரண்டாவது.", back.Sections.Single(s => s.Id == "two").Body);
		Check.Equal("translated guide title kept", "விளம்பரம்", back.Title);
	}

	private static void SkipsCorrectionForStaleEnglish()
	{
		var (en, ta) = Guides();
		var (text, skipped) = HelpReviewApplier.Apply(en, ta, new[] { Correction(en, "one", "x", "பழைய திருத்தம்.", DateTime.UtcNow, sourceHash: "0000000000000000") });
		Check.Equal("stale correction skipped", 1, skipped.Count);
		Check.Equal("file text unchanged for that section", "பழைய உரை.", HelpGuideParser.Parse("publicity", "ta", text).Sections.Single(s => s.Id == "one").Body);
	}

	private static void NewestCorrectionWins()
	{
		var (en, ta) = Guides();
		var now = DateTime.UtcNow;
		var (text, _) = HelpReviewApplier.Apply(en, ta, new[]
		{
			Correction(en, "one", "a", "புதியது.", now),
			Correction(en, "one", "b", "பழையது.", now.AddHours(-2)),
		});
		Check.Equal("newest correction applied", "புதியது.", HelpGuideParser.Parse("publicity", "ta", text).Sections.Single(s => s.Id == "one").Body);
	}
}
