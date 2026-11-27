using System.Text;
using SPIC.Core.Entities;
using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.RegressionHarness;

/// <summary>
/// Farmer portal Task 1: who may open /farmer/*, where a farmer lands, and the farmer shell tabs.
/// The portal is ROLE-gated (AppRole.Farmer, plus Admin / SuperAdmin for support) so self signed-up
/// farmers with no designation can use it; staff are sent back to their own home.
/// </summary>
internal static class FarmerPortalRouteTests
{
	private const string StaffLanding = LoginState.DefaultLandingPath;
	private const string FarmerHome = LoginState.FarmerLandingPath;

	public static void Run()
	{
		Console.WriteLine("Farmer portal: routes, landing page and shell tabs");

		var farmer = TestLogin.As(AppRole.Farmer, "");
		Check.Equal("landing: a farmer lands on /farmer", FarmerHome, farmer.LandingPage);
		Check.Equal("landing: staff landing is unchanged", StaffLanding, TestLogin.As(AppRole.MDO, "Dashboard.View").LandingPage);

		Expect("farmer opens /farmer", farmer, "/farmer", true, null);
		Expect("farmer opens onboarding", farmer, "/farmer/onboarding", true, null);
		Expect("farmer opens profile", farmer, "/farmer/profile?x=1", true, null);
		Expect("farmer opens own SAS samples", farmer, "/Sas/MySamples", true, null);
		Expect("farmer opens own SAS reports", farmer, "/Sas/MyReports", true, null);
		Expect("farmer without designation never sees the staff Welcome", farmer, "/Welcome", false, FarmerHome);
		Expect("farmer without designation never sees the staff dashboard", farmer, "/Dashboard", false, FarmerHome);
		Expect("farmer without designation never sees DefaultWelcome", farmer, "/DefaultWelcome", false, FarmerHome);
		Expect("farmer without designation cannot open SAS payments", farmer, "/Sas/Payments", false, FarmerHome);
		Expect("farmer may still reach the login page", farmer, "/login", true, null);

		var sasFarmer = TestLogin.As(AppRole.Farmer, "SampleCollection.View");
		Expect("farmer with the SAS designation keeps SAS payments", sasFarmer, "/Sas/Payments", true, null);
		Expect("farmer with a designation is sent to the farmer home, not the dashboard", sasFarmer, "/Dashboard", false, FarmerHome);

		Expect("admin may open the farmer portal for support", TestLogin.As(AppRole.Admin, ""), "/farmer", true, null);
		Expect("super admin may open the farmer portal", TestLogin.As(AppRole.SuperAdmin, ""), "/farmer/profile", true, null);
		Expect("MDO is sent back to its home from /farmer", TestLogin.As(AppRole.MDO, "Dashboard.View"), "/farmer", false, StaffLanding);
		Expect("corporate admin is sent back from /farmer/profile", TestLogin.As(AppRole.CorporateAdmin, "Dashboard.View"), "/farmer/profile", false, StaffLanding);
		Expect("staff without designation is sent to Welcome from /farmer", TestLogin.As(AppRole.MDO, ""), "/farmer", false, "/Welcome");
		Expect("dealer is sent back from /farmer", TestLogin.As(AppRole.Dealer, ""), "/farmer", false, "/Welcome");
		Expect("signed in staff may still open /farmer/login", TestLogin.As(AppRole.MDO, "Dashboard.View"), "/farmer/login", true, null);
		Expect("signed out user may open /farmer/login", TestLogin.SignedOut(), "/farmer/login", true, null);
		Expect("staff routes are unchanged for staff", TestLogin.As(AppRole.RMD, "PublicityRequest.View"), "/PublicityRequest/new", true, null);

		// Shell: Home | (My Samples | My Reports when linked) | Profile; staff never get farmer tabs.
		Check.Equal("tabs: farmer without SAS link", "farmer,FarmerProfile", Keys(ShellNavigation.GetPhoneTabs(farmer)));
		Check.Equal("tabs: farmer tablet rail is the farmer list only", "farmer,FarmerProfile", Keys(ShellNavigation.GetRailItems(farmer)));
		Check.Equal("tabs: farmer More sheet offers no staff pages", "", Keys(ShellNavigation.MoreItems(farmer)));
		var linked = TestLogin.As(AppRole.Farmer, "");
		linked.SetFarmerSasLinked(true);
		Check.Equal("tabs: farmer with a linked SAS record", "farmer,SasMySamples,SasMyReports,FarmerProfile", Keys(ShellNavigation.GetPhoneTabs(linked)));
		Check.That("tabs: linked farmer's More sheet has no Payments without the designation",
			ShellNavigation.MoreItems(linked).All(t => t.Key != "SasMyPayments"));
		Check.That("tabs: MDO gets no farmer tabs",
			ShellNavigation.GetRailItems(TestLogin.As(AppRole.MDO, "Dashboard.View")).All(t => t.Key is not ("farmer" or "FarmerProfile")));
		Check.That("tabs: Admin gets no farmer tabs",
			ShellNavigation.GetRailItems(TestLogin.As(AppRole.Admin, "")).All(t => t.Key is not ("farmer" or "FarmerProfile")));
		Check.Equal("tabs: /farmer/profile highlights Profile", "FarmerProfile", ShellNavigation.ActiveKey("https://app.example/farmer/profile", "https://app.example/"));
		Check.Equal("tabs: /farmer/onboarding highlights Home", "farmer", ShellNavigation.ActiveKey("/farmer/onboarding"));
		Check.Equal("tabs: /farmer highlights Home", "farmer", ShellNavigation.ActiveKey("/farmer"));

		// The SAS link flag belongs to one signed-in user only.
		var state = new LoginState();
		state.Token = Token(AppRole.Farmer, "user-a");
		state.SetFarmerSasLinked(true);
		state.Token = Token(AppRole.Farmer, "user-a");
		Check.That("sas link: kept when the same user's token is refreshed", state.FarmerSasLinked);
		state.Token = Token(AppRole.Farmer, "user-b");
		Check.That("sas link: cleared when another user signs in", !state.FarmerSasLinked);
		state.SetFarmerSasLinked(true);
		state.Token = null;
		Check.That("sas link: cleared on sign out", !state.FarmerSasLinked);
	}

	private static void Expect(string name, LoginState login, string url, bool allowed, string? redirect)
	{
		var decision = RouteAccess.Decide(login, url, dataExplorerAuthorizedId: null);
		Check.Equal("route: " + name, new RouteDecision(allowed, redirect), decision);
	}

	private static string Keys(IEnumerable<ShellTab> tabs) => string.Join(",", tabs.Select(t => t.Key));

	private static string Token(AppRole role, string userId)
	{
		var json = $"{{\"role\":\"{role}\",\"nameid\":\"{userId}\"}}";
		var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
		return $"x.{payload}.x";
	}
}
