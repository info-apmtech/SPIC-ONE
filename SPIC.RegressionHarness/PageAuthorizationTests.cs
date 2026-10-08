using SPIC.Core.Entities;

namespace SPIC.RegressionHarness;

/// <summary>
/// Pins the role -&gt; authorization-model mapping and the OpenAccess list.
///
/// These are the decisions the Designation-based access fix rests on: Admin and SuperAdmin keep
/// the page role bypass they have always had, every other role resolves its pages from its
/// Designation (SpecialAdmin and CommonRole with NO open-to-all union), and the OpenAccess list
/// holds only the shell hubs plus the never-configurable documentation / treatment subtree. A
/// regression here silently re-opens whole modules to roles the administrator did not grant them,
/// and no compile error would show it.
/// </summary>
internal static class PageAuthorizationTests
{
	public static void Run()
	{
		Console.WriteLine("PageAuthorization");

		RoleModelSelectsTheRightAuthorizationModel();
		ActionBypassIsAdminOnly();
		AdminAndSuperAdminStillBypass();
		DesignationControlledRolesNeedTheGrant();
		OpenAccessHoldsOnlyHubsAndTheDocumentationSubtree();
		TheNewlyAssignablePagesAreDesignationControlled();
		SpecialAdminSeesOnlyItsDesignation();
	}

	private static void RoleModelSelectsTheRightAuthorizationModel()
	{
		Check.Equal(
			"Admin keeps the page role bypass",
			PageAccessModel.RoleBypass,
			PageAuthorization.ModelFor(AppRole.Admin));
		Check.Equal(
			"SuperAdmin keeps the page role bypass",
			PageAccessModel.RoleBypass,
			PageAuthorization.ModelFor(AppRole.SuperAdmin));

		// The fix: CorporateAdmin is no longer in RoleModels, so it falls through to the default
		// model and reads its pages from its Designation like every other non-admin role.
		Check.Equal(
			"CorporateAdmin is designation-controlled",
			PageAccessModel.DesignationAndOpenPages,
			PageAuthorization.ModelFor(AppRole.CorporateAdmin));
		Check.Equal(
			"Director is designation-controlled",
			PageAccessModel.DesignationAndOpenPages,
			PageAuthorization.ModelFor(AppRole.Director));
		Check.Equal(
			"AVP is designation-controlled",
			PageAccessModel.DesignationAndOpenPages,
			PageAuthorization.ModelFor(AppRole.AVP));
		Check.Equal(
			"SpecialAdmin is designation-only (no open-to-all union)",
			PageAccessModel.DesignationOnly,
			PageAuthorization.ModelFor(AppRole.SpecialAdmin));
		Check.Equal(
			"Dealer is designation-controlled",
			PageAccessModel.DesignationAndOpenPages,
			PageAuthorization.ModelFor(AppRole.Dealer));
		Check.Equal(
			"CommonRole stays designation-only",
			PageAccessModel.DesignationOnly,
			PageAuthorization.ModelFor(AppRole.CommonRole));

		// Strongest form of the same claim: exactly two roles BYPASS. (CommonRole is also in the
		// table, but its entry is DesignationOnly, not a bypass - so counting RoleBypass entries
		// rather than table entries is what makes a future widening of the bypass fail here.)
		var bypassing = PageAuthorization.RoleModels
			.Where(kv => kv.Value == PageAccessModel.RoleBypass)
			.Select(kv => kv.Key)
			.OrderBy(r => r)
			.ToArray();
		Check.Equal(
			"exactly Admin and SuperAdmin hold PageAccessModel.RoleBypass",
			string.Join(",", new[] { AppRole.Admin, AppRole.SuperAdmin }.OrderBy(r => r)),
			string.Join(",", bypassing));
	}

	private static void ActionBypassIsAdminOnly()
	{
		Check.Equal("ActionBypassRoles holds one role", 1, PageAuthorization.ActionBypassRoles.Count);
		Check.That("Admin bypasses Add / Edit / Delete", PageAuthorization.HasActionBypass(AppRole.Admin));
		Check.That(
			"SuperAdmin still does NOT bypass actions (page bypass only)",
			!PageAuthorization.HasActionBypass(AppRole.SuperAdmin));
		Check.That(
			"CorporateAdmin does not bypass actions",
			!PageAuthorization.HasActionBypass(AppRole.CorporateAdmin));
	}

	private static void AdminAndSuperAdminStillBypass()
	{
		var admin = PageAuthorization.GetEffectivePagePermissions(
			AppRole.Admin, (IEnumerable<string>?)null);
		var superAdmin = PageAuthorization.GetEffectivePagePermissions(
			AppRole.SuperAdmin, (IEnumerable<string>?)null);

		Check.That(
			"Admin reaches a page its designation never granted",
			admin.CanReach("SalesAudit"));
		Check.That(
			"SuperAdmin reaches a page its designation never granted",
			superAdmin.CanReach("SalesAudit"));
		Check.That(
			"Admin reaches the pages only role-gated menus used to show",
			admin.CanReach(PagePermission.IfmsAutoImport)
			&& admin.CanReach(PagePermission.FarmDashboard));

		// The one exception that predates this work: SAS Lab / payment keys are designation-only
		// for EVERY role, Admin and SuperAdmin included.
		Check.That(
			"Admin does not reach a lab page without the grant",
			!admin.CanReach(PagePermission.LabDashboard));
		Check.That(
			"SuperAdmin does not reach a lab page without the grant",
			!superAdmin.CanReach(PagePermission.LabDashboard));
	}

	private static void DesignationControlledRolesNeedTheGrant()
	{
		var withoutGrant = PageAuthorization.GetEffectivePagePermissions(
			AppRole.CorporateAdmin, (IEnumerable<string>?)null);
		var withFarmDashboard = PageAuthorization.GetEffectivePagePermissions(
			AppRole.CorporateAdmin, new[] { "FarmDashboard" });
		var withAutoImport = PageAuthorization.GetEffectivePagePermissions(
			AppRole.CorporateAdmin, new[] { "IfmsAutoImport" });

		Check.That(
			"CorporateAdmin with no grant reaches neither new page",
			!withoutGrant.CanReach(PagePermission.FarmDashboard)
			&& !withoutGrant.CanReach(PagePermission.IfmsAutoImport));
		Check.That(
			"the FarmDashboard grant opens Farm Operations only",
			withFarmDashboard.CanReach(PagePermission.FarmDashboard)
			&& !withFarmDashboard.CanReach(PagePermission.IfmsAutoImport));
		Check.That(
			"the IfmsAutoImport grant opens IFMS Auto Import only",
			withAutoImport.CanReach(PagePermission.IfmsAutoImport)
			&& !withAutoImport.CanReach(PagePermission.FarmDashboard));

		// A "Page.Action" token grants reach on its page, exactly as Designation.razor writes it.
		var actionToken = PageAuthorization.GetEffectivePagePermissions(
			AppRole.CorporateAdmin, new[] { "SalesAudit.Edit" });
		Check.That(
			"a Page.Action token still grants reach on its page",
			actionToken.CanReach("SalesAudit"));

		// No designation at all: Welcome only, for a designation-driven role.
		var noDesignation = PageAuthorization.GetEffectivePagePermissions(
			AppRole.CommonRole, (IEnumerable<string>?)null);
		Check.That(
			"a role with no designation reaches Welcome",
			noDesignation.CanReach("Welcome"));
		Check.That(
			"a role with no designation reaches nothing else",
			!noDesignation.CanReach("SalesAudit")
			&& !noDesignation.CanReach("Activities"));

		// Hubs are open to the default model but NOT to DesignationOnly, so CommonRole needs the
		// hub on its designation like any other page.
		var commonWithHub = PageAuthorization.GetEffectivePagePermissions(
			AppRole.CommonRole, new[] { "Activities" });
		Check.That(
			"a granted hub opens for the designation-only role",
			commonWithHub.CanReach("Activities"));
	}

	private static void OpenAccessHoldsOnlyHubsAndTheDocumentationSubtree()
	{
		string[] hubs = { "Activities", "Farmers", "Alerts" };
		string[] documentation =
		{
			"StartDocumentation", "DemoDetails", "TreatmentDetails",
			"Treatment01", "Treatment02", "TreatmentDemoDetails"
		};

		foreach (var hub in hubs)
			Check.That($"the shell hub {hub} stays open to signed-in users",
				PageAuthorization.OpenAccessRoutes.Contains(hub));

		foreach (var route in documentation)
			Check.That($"the never-configurable route {route} stays open",
				PageAuthorization.OpenAccessRoutes.Contains(route));

		// The three pages that used to be open to everyone and are now Designation-controlled.
		string[] nowDesignationControlled = { "SalesAudit", "ExtensionRequests", "FarmDashboard", "DemoDocumentation" };
		foreach (var route in nowDesignationControlled)
			Check.That($"{route} is no longer an open-to-all route",
				!PageAuthorization.OpenAccessRoutes.Contains(route));

		// THE invariant: a route that has a PagePermission member is configurable on a Designation,
		// so it must not be listed in OpenAccessRoutes - the open grant used to override the
		// administrator's checkbox for every non-CommonRole user, because menus were gated with
		// CanAccess and CanAccess honours the open grant.
		//
		// SANCTIONED EXCEPTION (menu-authorization standard, decisions B + M): the three shell hubs
		// and the six never-configurable Demo Documentation children are deliberately BOTH - the
		// ROUTE stays open to every signed-in user (PageGuard, untouched) while the MENU entry is
		// Designation-controlled through its own PagePermission member via LoginState.CanSeeMenu,
		// which never consults OpenAccessRoutes. That is the standard's "menu != route" split, so
		// these nine keys are the only INTENDED overlap. Three further keys (SalesAudit,
		// ExtensionRequests, FarmDashboard) also overlap for pre-existing reasons and keep
		// failing individually below; they are named separately rather than sanctioned.
		string[] routeOpenMenuControlled =
		{
			"Activities", "Farmers", "Alerts",
			"StartDocumentation", "DemoDetails", "TreatmentDetails",
			"Treatment01", "Treatment02", "TreatmentDemoDetails"
		};

		// Pre-existing overlap, failing before this standard and unchanged by it: closing these
		// three means editing PageAuthorization.OpenAccessRoutes, which decision M puts out of
		// scope. They keep failing individually below so the defect stays visible.
		string[] knownPreExistingOverlap = { "SalesAudit", "ExtensionRequests", "FarmDashboard" };

		var openRoutePermissionKeys = Enum.GetValues<PagePermission>()
			.Select(RoleAccessPermissions.KeyFor)
			.Where(PageAuthorization.OpenAccessRoutes.Contains)
			.ToArray();

		foreach (var key in openRoutePermissionKeys.Where(k => !routeOpenMenuControlled.Contains(k)))
			Check.That($"{key} must not appear in OpenAccessRoutes", false);

		Check.That(
			"the sanctioned exception list is complete (all nine are route-open)",
			routeOpenMenuControlled.All(openRoutePermissionKeys.Contains));

		Check.That(
			"no PagePermission key overlaps OpenAccessRoutes beyond the nine sanctioned keys "
			+ "and the three known pre-existing offenders",
			openRoutePermissionKeys.All(k =>
				routeOpenMenuControlled.Contains(k) || knownPreExistingOverlap.Contains(k)));

		// DemoDocumentation is a normal Designation page now.
		var withoutGrant = PageAuthorization.GetEffectivePagePermissions(
			AppRole.CorporateAdmin, (IEnumerable<string>?)null);
		var withGrant = PageAuthorization.GetEffectivePagePermissions(
			AppRole.CorporateAdmin, new[] { "DemoDocumentation" });
		Check.That(
			"DemoDocumentation needs the grant",
			!withoutGrant.CanReach(PagePermission.DemoDocumentation)
			&& withGrant.CanReach(PagePermission.DemoDocumentation));
	}

	private static void TheNewlyAssignablePagesAreDesignationControlled()
	{
		Check.That(
			"PagePermission.FarmDashboard exists for the Designation grid",
			Enum.IsDefined(PagePermission.FarmDashboard));
		Check.That(
			"PagePermission.IfmsAutoImport exists for the Designation grid",
			Enum.IsDefined(PagePermission.IfmsAutoImport));

		Check.Equal(
			"FarmDashboard's key is its enum name (menu, route and grid agree)",
			"FarmDashboard",
			RoleAccessPermissions.KeyFor(PagePermission.FarmDashboard));
		Check.Equal(
			"IfmsAutoImport's key is its enum name (menu, route and grid agree)",
			"IfmsAutoImport",
			RoleAccessPermissions.KeyFor(PagePermission.IfmsAutoImport));

		// The keys the shell and the route guard resolve are the same ones a designation grants,
		// so a granted menu entry always has a matching route (and vice versa).
		Check.That(
			"a designation granting both tokens opens both pages for a normal role",
			PageAuthorization.GetEffectivePagePermissions(
					AppRole.RM, new[] { "FarmDashboard", "IfmsAutoImport" })
				.CanReach(PagePermission.FarmDashboard)
			&& PageAuthorization.GetEffectivePagePermissions(
					AppRole.RM, new[] { "FarmDashboard", "IfmsAutoImport" })
				.CanReach(PagePermission.IfmsAutoImport));
	}

	/// <summary>
	/// SpecialAdmin is designation-driven and nothing else: no Admin / SuperAdmin page bypass, no
	/// open-to-all union, so Digital Library / Community / Profile / the shell hubs need the grant.
	/// Pinned here because the leak came from a role model, not from a page, and no compile error
	/// would show it coming back.
	/// </summary>
	private static void SpecialAdminSeesOnlyItsDesignation()
	{
		Check.That(
			"SpecialAdmin does not bypass pages",
			!PageAuthorization.RoleModels.TryGetValue(AppRole.SpecialAdmin, out var specialModel)
			|| specialModel != PageAccessModel.RoleBypass);
		Check.That("SpecialAdmin does not bypass actions", !PageAuthorization.HasActionBypass(AppRole.SpecialAdmin));

		var logisticsMasterOnly = PageAuthorization.GetEffectivePagePermissions(
			AppRole.SpecialAdmin, new[] { "LogisticsMaster" });
		Check.That(
			"LogisticsMaster designation opens LogisticsMaster only",
			logisticsMasterOnly.CanReach(PagePermission.LogisticsMaster));
		Check.That(
			"a LogisticsMaster-only SpecialAdmin gets no Digital Library",
			!logisticsMasterOnly.CanReach(PagePermission.DigitalLibrary));
		Check.That(
			"a LogisticsMaster-only SpecialAdmin gets no Community",
			!logisticsMasterOnly.CanReach(PagePermission.Community));
		Check.That(
			"a LogisticsMaster-only SpecialAdmin gets no Profile",
			!logisticsMasterOnly.CanReach(PagePermission.Profile));
		Check.That(
			"a LogisticsMaster-only SpecialAdmin gets no shell hub",
			!logisticsMasterOnly.CanReach("Activities"));

		var withLibrary = PageAuthorization.GetEffectivePagePermissions(
			AppRole.SpecialAdmin, new[] { "LogisticsMaster", "DigitalLibrary" });
		Check.That(
			"granting DigitalLibrary opens it alongside LogisticsMaster",
			withLibrary.CanReach(PagePermission.DigitalLibrary)
			&& withLibrary.CanReach(PagePermission.LogisticsMaster));
		Check.That(
			"the extra grant still does not open Community",
			!withLibrary.CanReach(PagePermission.Community));

		// Direct-URL behaviour follows the same table: without the grant the route is denied, so
		// PageGuard bounces it to LandingPage.
		Check.That(
			"an ungranted route is denied for the designation-only SpecialAdmin",
			!logisticsMasterOnly.CanReach("StockDetails"));

		// No designation at all: Welcome only, so LandingPage sends the user to /Welcome.
		var noDesignation = PageAuthorization.GetEffectivePagePermissions(
			AppRole.SpecialAdmin, (IEnumerable<string>?)null);
		Check.That("a SpecialAdmin with no designation reaches Welcome", noDesignation.CanReach("Welcome"));
		Check.That(
			"a SpecialAdmin with no designation reaches nothing else",
			!noDesignation.CanReach(PagePermission.LogisticsMaster)
			&& !noDesignation.CanReach(PagePermission.DigitalLibrary));

		// Admin and SuperAdmin are untouched by the SpecialAdmin change.
		var admin = PageAuthorization.GetEffectivePagePermissions(
			AppRole.Admin, (IEnumerable<string>?)null);
		var superAdmin = PageAuthorization.GetEffectivePagePermissions(
			AppRole.SuperAdmin, (IEnumerable<string>?)null);
		Check.That(
			"Admin still reaches Digital Library and Community without a grant",
			admin.CanReach(PagePermission.DigitalLibrary) && admin.CanReach(PagePermission.Community));
		Check.That(
			"SuperAdmin still reaches Digital Library and Community without a grant",
			superAdmin.CanReach(PagePermission.DigitalLibrary)
			&& superAdmin.CanReach(PagePermission.Community));
	}
}
