using SPIC.Core.Entities;
using SPIC.Core.Help;

namespace SPIC.RegressionHarness;

/// <summary>
/// Review status of a translated section, computed from review records and the CURRENT text:
/// an approval stops counting the moment either the English or the translation changes, and a
/// correction stays pending until the translation file contains it. Nothing is stored as a status,
/// so files and approvals can never disagree.
/// </summary>
internal static class HelpReviewStatusTests
{
	private static readonly HelpSection English = Section("Raise a request", "Tap New request.");
	private static readonly HelpSection Tamil = Section("கோரிக்கை", "New request அழுத்தவும்.");

	public static void Run()
	{
		Console.WriteLine("Help review status");

		ApprovalCountsOnlyForCurrentHashes();
		EnglishChangeResetsApproval();
		CorrectionPendingUntilFileMatches();
		AppliedCorrectionWithoutApprovalIsNotReviewed();
		ReviewMustMatchDisplayedText();
	}

	// Final review (Important 4): a review counts only for the exact text the reviewer saw.
	private static void ReviewMustMatchDisplayedText()
	{
		var shownSource = HelpHash.Of(English.Title, English.Body);
		var shownTranslation = HelpHash.Of(Tamil.Title, Tamil.Body);
		Check.That("same text matches", HelpReviewStatus.SameText(shownSource, shownTranslation, English, Tamil));
		var newer = Tamil with { Body = "புதிய உரை." };
		Check.That("server has newer translation: no match", !HelpReviewStatus.SameText(shownSource, shownTranslation, English, newer));
		Check.That("missing hashes: no match", !HelpReviewStatus.SameText(null, null, English, Tamil));
	}

	private static HelpSection Section(string title, string body) =>
		new("raise", title, new[] { "/PublicityRequest/new" }, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), body, null, HelpConfidence.Ok);

	private static HelpReviewItem Item(HelpReviewKind kind, HelpSection en, HelpSection tr, string? title = null, string? body = null) => new()
	{
		Module = "publicity", SectionId = "raise", Language = "ta", Kind = kind,
		SourceHash = HelpHash.Of(en.Title, en.Body), TranslationHash = HelpHash.Of(tr.Title, tr.Body),
		SuggestedTitle = title, SuggestedBody = body, CreatedAtUtc = DateTime.UtcNow,
	};

	private static void ApprovalCountsOnlyForCurrentHashes()
	{
		Check.Equal("no records = not reviewed", HelpReviewState.NotReviewed, HelpReviewStatus.StateOf(English, Tamil, Array.Empty<HelpReviewItem>()));
		Check.Equal("approval of the current text = approved", HelpReviewState.Approved,
			HelpReviewStatus.StateOf(English, Tamil, new[] { Item(HelpReviewKind.Approval, English, Tamil) }));
		var retranslated = Tamil with { Body = "புதிய மொழிபெயர்ப்பு." };
		Check.Equal("translation changed after approval = not reviewed", HelpReviewState.NotReviewed,
			HelpReviewStatus.StateOf(English, retranslated, new[] { Item(HelpReviewKind.Approval, English, Tamil) }));
	}

	private static void EnglishChangeResetsApproval()
	{
		var newEnglish = English with { Body = "Tap New request, then Save." };
		Check.Equal("English changed after approval = not reviewed", HelpReviewState.NotReviewed,
			HelpReviewStatus.StateOf(newEnglish, Tamil, new[] { Item(HelpReviewKind.Approval, English, Tamil) }));
	}

	private static void CorrectionPendingUntilFileMatches()
	{
		var correction = Item(HelpReviewKind.Correction, English, Tamil, "கோரிக்கை எழுப்புதல்", "New request பொத்தானை அழுத்தவும்.");
		Check.Equal("open correction = needs correction", HelpReviewState.NeedsCorrection,
			HelpReviewStatus.StateOf(English, Tamil, new[] { correction }));
		Check.That("correction not yet applied", !HelpReviewStatus.IsApplied(correction, Tamil));
	}

	private static void AppliedCorrectionWithoutApprovalIsNotReviewed()
	{
		var correction = Item(HelpReviewKind.Correction, English, Tamil, "கோரிக்கை எழுப்புதல்", "New request பொத்தானை அழுத்தவும்.");
		var corrected = Tamil with { Title = correction.SuggestedTitle!, Body = correction.SuggestedBody! };
		Check.That("correction applied once the file has it", HelpReviewStatus.IsApplied(correction, corrected));
		Check.Equal("applied correction still needs a final approval", HelpReviewState.NotReviewed,
			HelpReviewStatus.StateOf(English, corrected, new[] { correction }));
	}
}
