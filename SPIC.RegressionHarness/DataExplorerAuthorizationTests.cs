using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.RegressionHarness;

/// <summary>
/// Pins the Data Explorer access rule (DataExplorerAuthorization.IsAuthorized).
///
/// This is the single authority shared by the UI (NavMenu, MobileSidebar, ShellNavigation,
/// PageGuard, DataExplorer.razor) and the API (DataExplorerController). A regression here
/// either hides the page from the authorized user or opens it to Admin / every CorporateAdmin,
/// and no compile error would show it.
/// </summary>
internal static class DataExplorerAuthorizationTests
{
	private const string Authorized = "62d84251-b25b-4986-bf7e-e9402bccb2bb";

	public static void Run()
	{
		Console.WriteLine("DataExplorerAuthorization");

		SuperAdminIsAlwaysAllowed();
		AdminIsAlwaysDenied();
		ConfiguredCorporateAdminIsAllowed();
		OtherCorporateAdminsAreDenied();
		MissingConfigurationOrUserIdDenies();
		ComparisonIsCaseInsensitive();
		ConfiguredIdCanBeAList();
		OtherRolesAreDeniedEvenWhenConfigured();
	}

	private static void SuperAdminIsAlwaysAllowed()
	{
		Check.That("SuperAdmin allowed with no config and no user id",
			DataExplorerAuthorization.IsAuthorized(AppRole.SuperAdmin, null, null));
		Check.That("SuperAdmin allowed with a designation present",
			DataExplorerAuthorization.IsAuthorized(AppRole.SuperAdmin, "someone-else", "another-id"));
	}

	private static void AdminIsAlwaysDenied()
	{
		Check.That("Admin denied even when it matches the configured id",
			!DataExplorerAuthorization.IsAuthorized(AppRole.Admin, Authorized, Authorized));
		Check.That("Admin denied with no config",
			!DataExplorerAuthorization.IsAuthorized(AppRole.Admin, null, null));
	}

	private static void ConfiguredCorporateAdminIsAllowed()
	{
		Check.That("configured CorporateAdmin allowed",
			DataExplorerAuthorization.IsAuthorized(AppRole.CorporateAdmin, Authorized, Authorized));
	}

	private static void OtherCorporateAdminsAreDenied()
	{
		Check.That("unconfigured CorporateAdmin denied",
			!DataExplorerAuthorization.IsAuthorized(AppRole.CorporateAdmin, "some-other-user", Authorized));
	}

	private static void MissingConfigurationOrUserIdDenies()
	{
		Check.That("CorporateAdmin denied when config is null",
			!DataExplorerAuthorization.IsAuthorized(AppRole.CorporateAdmin, Authorized, null));
		Check.That("CorporateAdmin denied when config is empty",
			!DataExplorerAuthorization.IsAuthorized(AppRole.CorporateAdmin, Authorized, "  "));
		Check.That("CorporateAdmin denied when user id is empty",
			!DataExplorerAuthorization.IsAuthorized(AppRole.CorporateAdmin, "  ", Authorized));
		Check.That("CorporateAdmin denied when user id is null",
			!DataExplorerAuthorization.IsAuthorized(AppRole.CorporateAdmin, null, Authorized));
	}

	private static void ComparisonIsCaseInsensitive()
	{
		Check.That("id comparison is case-insensitive",
			DataExplorerAuthorization.IsAuthorized(AppRole.CorporateAdmin, Authorized.ToUpperInvariant(), Authorized.ToLowerInvariant()));
	}

	private static void ConfiguredIdCanBeAList()
	{
		Check.That("configured id matches inside a comma separated list",
			DataExplorerAuthorization.IsAuthorized(AppRole.CorporateAdmin, Authorized, $"first-id, {Authorized}, last-id"));
		Check.That("configured id matches inside a pipe separated list",
			DataExplorerAuthorization.IsAuthorized(AppRole.CorporateAdmin, Authorized, $"first-id|{Authorized}"));
	}

	private static void OtherRolesAreDeniedEvenWhenConfigured()
	{
		Check.That("Director denied even when it matches the configured id",
			!DataExplorerAuthorization.IsAuthorized(AppRole.Director, Authorized, Authorized));
		Check.That("Dealer denied even when it matches the configured id",
			!DataExplorerAuthorization.IsAuthorized(AppRole.Dealer, Authorized, Authorized));
	}
}
