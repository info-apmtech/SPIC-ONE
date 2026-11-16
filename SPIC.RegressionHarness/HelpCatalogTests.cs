using Microsoft.Extensions.Configuration;
using SPIC.Core.Entities;
using SPIC.Core.Help;
using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.RegressionHarness;

/// <summary>
/// Which guide sections a signed-in user sees: a section shows when the user can open one of its
/// routes (RouteAccess), narrowed by action rights ("Page.Action"), role and designation name.
/// A wrong answer here shows people guides for screens they cannot use - the spec's anti-goal.
/// </summary>
internal static class HelpCatalogTests
{
	private const string Publicity = """
		---
		module: publicity
		title: Publicity
		order: 40
		---

		::: section raise-request
		title: Raise a publicity request
		routes: /PublicityRequest/new
		for: PublicityRequest.Entry

		Tap New request.
		:::

		::: section review
		title: Review requests
		routes: /PublicityReview

		Open a request and review it.
		:::

		::: section dispatch
		title: Dispatch material
		routes: /PublicityReview/{id}/dispatch
		designations: ADMIN-MD CS, DGM-MD CS

		Courier docket number goes here.
		:::
		""";

	public static void Run()
	{
		Console.WriteLine("Help catalog");

		RmdoSeesRaiseNotReview();
		ViewOnlyRmdoDoesNotSeeRaise();
		ReviewerWithoutMdCsDoesNotSeeDispatch();
		MdCsAdminSeesDispatch();
		QuestionButtonFindsSectionForEditUrl();
		QuestionButtonFallsBackToModuleThenNull();
		SearchOnlyReturnsVisibleSections();
		NonEnglishIsMachineTranslatedUntilApproved();
	}

	private static HelpLibrary Library(params (string, string)[] extra)
	{
		var files = new List<(string, string)> { ("help/publicity/guide.en.md", Publicity) };
		files.AddRange(extra);
		return HelpLibraryTests.Library(files.ToArray());
	}

	private static HelpCatalog Catalog(LoginState login, HelpLibrary? lib = null) =>
		new(lib ?? Library(), login, new ConfigurationBuilder().Build());

	private static string Ids(HelpCatalog catalog) =>
		string.Join(",", catalog.VisibleModules("en").SelectMany(m => m.Sections).Select(s => s.Section.Id));

	private static void RmdoSeesRaiseNotReview() =>
		Check.Equal("RMDO with Entry sees only raise-request", "raise-request",
			Ids(Catalog(TestLogin.As(AppRole.RMD, "PublicityRequest.View,PublicityRequest.Entry"))));

	private static void ViewOnlyRmdoDoesNotSeeRaise()
	{
		var catalog = Catalog(TestLogin.As(AppRole.RMD, "PublicityRequest.View"));
		Check.Equal("view-only RMDO sees no publicity module", 0, catalog.VisibleModules("en").Count);
	}

	private static void ReviewerWithoutMdCsDoesNotSeeDispatch() =>
		Check.Equal("RM reviewer sees review, not dispatch", "review",
			Ids(Catalog(TestLogin.As(AppRole.RM, "PublicityReview.View", "RM"))));

	private static void MdCsAdminSeesDispatch() =>
		Check.Equal("ADMIN-MD CS sees review and dispatch", "review,dispatch",
			Ids(Catalog(TestLogin.As(AppRole.MDAdmin, "PublicityReview.View", "ADMIN-MD CS"))));

	private static void QuestionButtonFindsSectionForEditUrl()
	{
		var catalog = Catalog(TestLogin.As(AppRole.MDAdmin, "PublicityReview.View", "admin-md cs"));
		Check.Equal("? on a dispatch URL opens dispatch", "dispatch",
			catalog.ForPath("https://app/PublicityReview/77/dispatch?x=1", "en")?.Section.Id);
	}

	private static void QuestionButtonFallsBackToModuleThenNull()
	{
		var catalog = Catalog(TestLogin.As(AppRole.MDAdmin, "PublicityReview.View", "ADMIN-MD CS"));
		Check.Equal("? on an unguided page of a guided module opens that module", "review",
			catalog.ForPath("/PublicityReview/consolidation", "en")?.Section.Id);
		Check.That("? on an unrelated page finds nothing", catalog.ForPath("/StockReport", "en") is null);
	}

	private static void SearchOnlyReturnsVisibleSections()
	{
		var rm = Catalog(TestLogin.As(AppRole.RM, "PublicityReview.View", "RM"));
		Check.Equal("RM search for a dispatch-only word finds nothing", 0, rm.Search("docket", "en").Count);
		var md = Catalog(TestLogin.As(AppRole.MDAdmin, "PublicityReview.View", "ADMIN-MD CS"));
		Check.Equal("MD CS search finds the dispatch section", "dispatch", md.Search("DOCKET", "en").Single().Section.Section.Id);
	}

	private static void NonEnglishIsMachineTranslatedUntilApproved()
	{
		var en = HelpGuideParser.Parse("publicity", "en", Publicity);
		var review = en.Sections.Single(s => s.Id == "review");
		var tamil = $"""
			::: section review
			title: மதிப்பாய்வு
			source-hash: {HelpHash.Of(review.Title, review.Body)}
			confidence: ok

			கோரிக்கையைத் திறக்கவும்.
			:::
			""";
		var catalog = Catalog(TestLogin.As(AppRole.RM, "PublicityReview.View", "RM"),
			Library(("help/publicity/guide.ta.md", tamil)));

		var section = catalog.VisibleModules("ta").Single().Sections.Single();
		Check.That("Tamil section is machine translated by default", section.MachineTranslated && !section.IsFallbackEnglish);

		catalog.ApprovedKeys = _ => new HashSet<string> { "publicity/review" };
		var approved = catalog.VisibleModules("ta").Single().Sections.Single();
		Check.That("approved Tamil section is not labelled", !approved.MachineTranslated);

		var english = catalog.VisibleModules("en").Single().Sections.Single();
		Check.That("English is never labelled", !english.MachineTranslated && !english.IsFallbackEnglish);
	}
}
