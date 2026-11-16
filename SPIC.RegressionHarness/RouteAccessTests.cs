using SPIC.Core.Entities;
using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.RegressionHarness;

/// <summary>
/// Pins PageGuard's route decision, now owned by <see cref="RouteAccess"/>, so the Help Desk can
/// ask "can this user open that page?" without a second copy of the rules.
///
/// The expected values were derived by reading PageGuard.razor's Check() on 2026-11-16, before the
/// logic moved. If a row fails after a verbatim move, the ROW is wrong: re-derive it from the old
/// code and fix the row, never the moved logic. A regression here silently opens or closes whole
/// modules to roles the administrator did not decide on.
/// </summary>
internal static class RouteAccessTests
{
	private const string Landing = LoginState.DefaultLandingPath;

	public static void Run()
	{
		Console.WriteLine("RouteAccess");

		Expect("NotLoggedInIsAllowed", TestLogin.SignedOut(), "/PublicityRequest", true, null);
		Expect("SuperAdminReachesLab", TestLogin.As(AppRole.SuperAdmin, ""), "/Lab", true, null);
		Expect("AdminBypassesNormalPages", TestLogin.As(AppRole.Admin, "Dashboard.View"), "/PublicityReview", true, null);
		Expect("AdminNeedsDesignationForLab", TestLogin.As(AppRole.Admin, "Dashboard.View"), "/Lab", false, Landing);
		Expect("DealerAlwaysReachesSdwa", TestLogin.As(AppRole.Dealer, ""), "/SDWADashboard", true, null);
		Expect("DealerNeverReachesSchemeApproval", TestLogin.As(AppRole.Dealer, "SchemeApproval.View"), "/SchemeApproval", false, "/SDWADashboard");
		Expect("DirectorReachesSchemeApprovalWithoutDesignation", TestLogin.As(AppRole.Director, ""), "/SchemeApproval", true, null);
		Expect("RmdoReachesPublicityRequest", TestLogin.As(AppRole.RMD, "PublicityRequest.View"), "/PublicityRequest/new", true, null);
		Expect("RmdoCannotReachPublicityReview", TestLogin.As(AppRole.RMD, "PublicityRequest.View"), "/PublicityReview", false, Landing);
		Expect("ShellHubOpenToDefaultRoles", TestLogin.As(AppRole.MDO, "PublicityRequest.View"), "/Community", true, null);
		Expect("ShellHubClosedToDesignationOnlyRoles", TestLogin.As(AppRole.CommonRole, "PublicityRequest.View"), "/Community", false, Landing);
		Expect("NoDesignationGoesToWelcome", TestLogin.As(AppRole.MDO, ""), "/PublicityRequest", false, "/Welcome");
		Expect("LabAnalystReachesTestEntry", TestLogin.As(AppRole.CommonRole, "LabTestEntry.View"), "/Lab/TestEntry/5", true, null);
		Expect("DataExplorerDeniedToOthers", TestLogin.As(AppRole.RMD, "PublicityRequest.View"), "/DataExplorer", false, Landing);
		Expect("AbsoluteUrlAndQueryAreIgnored", TestLogin.As(AppRole.RMD, "PublicityRequest.View"), "https://app.example/PublicityRequest/new?x=1#top", true, null);

		// Help Desk (2026-11-16): open to every signed-in user who has a designation - including
		// designation-only roles such as Lab staff - but a user with NO designation still goes to
		// /Welcome, exactly as for every other feature page.
		Expect("AnyDesignatedUserOpensHelp", TestLogin.As(AppRole.MDO, "PublicityRequest.View"), "/Help", true, null);
		Expect("DesignationOnlyRoleOpensHelp", TestLogin.As(AppRole.CommonRole, "LabTestEntry.View"), "/Help/lab", true, null);
		Expect("NoDesignationUserIsSentToWelcomeFromHelp", TestLogin.As(AppRole.MDO, ""), "/Help", false, "/Welcome");
	}

	private static void Expect(string name, LoginState login, string url, bool allowed, string? redirect)
	{
		var decision = RouteAccess.Decide(login, url, dataExplorerAuthorizedId: null);
		Check.Equal(name, new RouteDecision(allowed, redirect), decision);
	}
}
