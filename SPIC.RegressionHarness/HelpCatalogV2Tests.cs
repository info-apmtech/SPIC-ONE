using SPIC.Core.Entities;
using SPIC.Core.Help;
using SPIC.MauiBlazorApp.Shared.Services;
using Microsoft.Extensions.Configuration;

namespace SPIC.RegressionHarness;

/// <summary>
/// Help centre v2 catalog: module blocks (flow, roles, FAQ…) follow the module's screens — shown
/// only when the user can open at least one screen — and the home page's path, popular questions,
/// What's new and "You" markers are limited to what the user can see.
/// </summary>
internal static class HelpCatalogV2Tests
{
	public static void Run()
	{
		Console.WriteLine("Help catalog v2");

		BlocksHiddenWhenNoScreenVisible();
		BlocksVisibleWithOneScreen();
		PathMatchesDesignationAndDropsHiddenSteps();
		MyRoleCardAndLaneByDesignation();
		WhatsNewOnlyVisibleModules();
		SearchFindsFaqText();
	}

	private const string Publicity = """
		---
		module: publicity
		title: Publicity
		order: 40
		updated: 2026-11-14
		note: Transfer added
		---

		::: section overview
		title: What this module does
		kind: overview

		Ask for publicity material for your HQ.

		- start | icon: bi-pencil-square
		  title: You start it
		  text: Raise a request.
		:::

		::: section flow
		title: How a request moves
		kind: flow

		- rmdo | type: lane | designations: RMDO
		  title: RMDO
		- approver | type: lane | designations: RM, SMD
		  title: Approvers
		- raise | lane: rmdo | col: 1
		  title: Raise
		:::

		::: section roles
		title: Who does what
		kind: roles

		- rmdo | designations: RMDO
		  title: RMDO
		  - Raise requests
		- rm | roles: RM
		  title: RM
		  - Approve
		:::

		::: section faq
		title: Problems
		kind: faq

		- no-pencil | popular: true
		  q: Why is there no pencil icon?
		  a: Only an approver can return the request.
		- no-rows
		  q: I see no rows
		  a: Clear the filters.
		:::

		::: section raise-request
		title: Raise a publicity request
		routes: /PublicityRequest/new
		for: PublicityRequest.Entry

		1. Tap New request.
		:::

		::: section review
		title: Review requests
		routes: /PublicityReview

		1. Open a request.
		:::
		""";

	private const string Lab = """
		---
		module: lab
		title: Lab
		updated: 2026-11-16
		note: Lab added
		---

		::: section faq
		title: Problems
		kind: faq

		- secret | popular: true
		  q: Lab secret question
		  a: Lab answer.
		:::

		::: section consignments
		title: Consignments
		routes: /Lab/Consignments
		for: LabConsignment.View

		1. Open one.
		:::
		""";

	private const string Paths = """
		{ "paths": [
		  { "id": "rmdo", "designations": ["RMDO"], "title": { "en": "Your path as an RMDO", "ta": "RMDO பாதை" },
		    "steps": [
		      { "module": "publicity", "section": "raise-request", "minutes": 5, "tour": true },
		      { "module": "publicity", "section": "review", "minutes": 2 },
		      { "module": "lab", "section": "consignments", "minutes": 2 } ] } ] }
		""";

	private static HelpCatalog Catalog(LoginState login) =>
		new(HelpLibraryTests.Library(("help/publicity/guide.en.md", Publicity), ("help/lab/guide.en.md", Lab), ("help/paths.json", Paths)),
			login, new ConfigurationBuilder().Build());

	private static LoginState Rmdo() => TestLogin.As(AppRole.RMD, "PublicityRequest.View,PublicityRequest.Entry", "RMDO");

	private static void BlocksHiddenWhenNoScreenVisible()
	{
		var nobody = Catalog(TestLogin.As(AppRole.RMD, "", "RMDO"));
		Check.Equal("no visible screen: no module", 0, nobody.VisibleModules("en").Count);
		Check.Equal("no visible screen: no popular FAQ", 0, nobody.PopularFaq("en").Count);
		Check.Equal("no visible screen: no FAQ search hit", 0, nobody.Search("pencil", "en").Count);
		Check.Equal("no visible screen: no path", null, nobody.PathFor("en"));

		var rmdo = Catalog(Rmdo());
		Check.That("lab FAQ hidden from RMDO", !rmdo.PopularFaq("en").Any(f => f.Module == "lab"));
	}

	private static void BlocksVisibleWithOneScreen()
	{
		var m = Catalog(Rmdo()).Module("publicity", "en")!;
		Check.Equal("one screen", "raise-request", string.Join(",", m.Screens.Select(s => s.Section.Id)));
		Check.That("flow block visible", m.Block("flow") is not null);
		Check.That("faq block visible", m.Block("faq") is not null);
		Check.Equal("lead from overview", "Ask for publicity material for your HQ.", m.Lead);
		Check.Equal("guide meta carried", "Transfer added", m.Guide.Note);
		Check.Equal("popular FAQ", "no-pencil", string.Join(",", Catalog(Rmdo()).PopularFaq("en").Select(f => f.Item.Id)));
	}

	private static void PathMatchesDesignationAndDropsHiddenSteps()
	{
		var path = Catalog(Rmdo()).PathFor("ta");
		Check.That("RMDO has a path", path is not null);
		Check.Equal("title in Tamil", "RMDO பாதை", path!.Title);
		Check.Equal("hidden steps dropped", "raise-request", string.Join(",", path.Steps.Select(s => s.Section.Section.Id)));
		Check.That("tour flag kept", path.Steps[0].Step.Tour);

		var rm = Catalog(TestLogin.As(AppRole.RM, "PublicityReview.View", "RM"));
		Check.Equal("no path for a designation without one", null, rm.PathFor("en"));
	}

	private static void MyRoleCardAndLaneByDesignation()
	{
		var rmdo = Catalog(Rmdo());
		var m = rmdo.Module("publicity", "en")!;
		Check.Equal("RMDO card", "rmdo", rmdo.MyRoleCard(m));
		Check.Equal("RMDO lane", "rmdo", rmdo.MyLane(m));

		var rm = Catalog(TestLogin.As(AppRole.RM, "PublicityReview.View", "SMD"));
		var mr = rm.Module("publicity", "en")!;
		Check.Equal("role match when no designation card", "rm", rm.MyRoleCard(mr));
		Check.Equal("lane by designation", "approver", rm.MyLane(mr));
	}

	private static void WhatsNewOnlyVisibleModules()
	{
		Check.Equal("RMDO sees publicity news only", "publicity", string.Join(",", Catalog(Rmdo()).WhatsNew("en").Select(w => w.Module)));
	}

	private static void SearchFindsFaqText()
	{
		var hits = Catalog(Rmdo()).Search("approver can return", "en");
		Check.That("FAQ answer found", hits.Any(h => h.Section.Section.Id == "faq"));
	}
}
