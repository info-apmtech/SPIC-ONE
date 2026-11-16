using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc.Routing;
using SPIC.Core.Entities;
using SPIC.MauiBlazorApp.Shared.Services;
using SpicAPI.Controllers;

namespace SPIC.RegressionHarness;

/// <summary>
/// Guest House permissions are independent: GuestHouse = Guest House Master, GuestHouseBooking = Book a Room
/// (the whole booking flow), MyBookings = the owner's own bookings and cancellation. These checks pin the
/// route-to-permission mapping, the route decision PageGuard makes (ShellNavigation.IsRoutePermitted), the
/// menu visibility, the server-side filter on GuestHouseBookingController and the desktop / mobile menu wiring.
///
/// Not covered here (needs a running API and PostgreSQL): the HTTP pipeline end to end and the backfill SQL
/// of 20261116060000_GuestHouseBookingPermissionBackfill.
/// </summary>
internal static class GuestHousePermissionTests
{
	private static readonly string[] BookingRoutes =
		{ "GuestHouse", "GuestHouseBooking", "Rooms", "RoomDetails", "GuestDetails", "GuestBooking", "Payment", "BookingPreview" };

	private static readonly string[] MyBookingsRoutes =
		{ "MyBookings", "BookingDetails", "CancelBooking", "CancelledBookingDetails", "RefundStatus" };

	public static void Run()
	{
		Console.WriteLine("GuestHousePermissions");

		RoutesResolveToTheirOwnPermission();
		BookingAndMyBookingsAreGrantedIndependently();
		MasterGrantDoesNotOpenBookingAndBookingDoesNotOpenMaster();
		MissingPermissionBlocksTheRoutes();
		DashboardIsNotRequired();
		SmmFollowsTheSameRules();
		AdminAndSuperAdminKeepTheirAccess();
		DealerBookingAccessIsPreserved();
		AdminCancellationRequestsAreUnchanged();
		MenuVisibilityFollowsEachPermission();
		BackendFilterDecision();
		EveryBookingEndpointDeclaresAPermission();
		OwnershipCheckStillGuardsCancellation();
		FrontOfficeAndMasterFiltersAreUntouched();
		DesktopAndMobileMenusMatch();
		CancelBookingMenuEntryStartsFromMyBookings();
	}

	// ---- helpers ------------------------------------------------------------------------------

	private static LoginState Login(AppRole role, string? roleAccess)
	{
		var state = new LoginState();
		var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"role\":\"{role}\",\"sub\":\"u1\"}}"))
			.TrimEnd('=').Replace('+', '-').Replace('/', '_');
		state.Token = $"e30.{payload}.sig";
		state.SetAllowedPages(roleAccess);
		return state;
	}

	private static bool CanOpen(LoginState state, string segment) =>
		ShellNavigation.IsRoutePermitted(state, ShellNavigation.ResolvePermissionKey(segment), segment);

	private static bool CanOpenAll(LoginState state, IEnumerable<string> segments) =>
		segments.All(s => CanOpen(state, s));

	private static bool CanOpenAny(LoginState state, IEnumerable<string> segments) =>
		segments.Any(s => CanOpen(state, s));

	private static string RepoFile(params string[] parts)
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SpicOne.sln")))
			dir = dir.Parent;
		return Path.Combine(new[] { dir?.FullName ?? throw new InvalidOperationException("SpicOne.sln not found") }.Concat(parts).ToArray());
	}

	// ---- route to permission --------------------------------------------------------------------

	private static void RoutesResolveToTheirOwnPermission()
	{
		foreach (var route in BookingRoutes)
			Check.Equal($"/{route} resolves to GuestHouseBooking", nameof(PagePermission.GuestHouseBooking), ShellNavigation.ResolvePermissionKey(route));

		foreach (var route in MyBookingsRoutes)
			Check.Equal($"/{route} resolves to MyBookings", nameof(PagePermission.MyBookings), ShellNavigation.ResolvePermissionKey(route));

		Check.Equal("/GuestHouseMaster still resolves to GuestHouse", nameof(PagePermission.GuestHouse), ShellNavigation.ResolvePermissionKey("GuestHouseMaster"));
		Check.Equal("/GuestHouseCancellations still resolves to GuestHouseCancellations", nameof(PagePermission.GuestHouseCancellations), ShellNavigation.ResolvePermissionKey("GuestHouseCancellations"));
	}

	// ---- RM / designation rules -----------------------------------------------------------------

	private static void BookingAndMyBookingsAreGrantedIndependently()
	{
		var rm = Login(AppRole.RM, "GuestHouseBooking.View,MyBookings.View");
		Check.That("RM with GuestHouseBooking + MyBookings opens every booking route", CanOpenAll(rm, BookingRoutes));
		Check.That("RM with GuestHouseBooking + MyBookings opens every My Bookings route", CanOpenAll(rm, MyBookingsRoutes));

		var bookingOnly = Login(AppRole.RM, "GuestHouseBooking.View");
		Check.That("RM with only GuestHouseBooking opens booking routes", CanOpenAll(bookingOnly, BookingRoutes));
		Check.That("RM with only GuestHouseBooking cannot open My Bookings routes", !CanOpenAny(bookingOnly, MyBookingsRoutes));

		var myBookingsOnly = Login(AppRole.RM, "MyBookings.View");
		Check.That("RM with only MyBookings opens My Bookings routes", CanOpenAll(myBookingsOnly, MyBookingsRoutes));
		Check.That("RM with only MyBookings cannot open booking routes", !CanOpenAny(myBookingsOnly, BookingRoutes));
	}

	private static void MasterGrantDoesNotOpenBookingAndBookingDoesNotOpenMaster()
	{
		var rm = Login(AppRole.RM, "GuestHouseBooking.View,MyBookings.View");
		Check.That("RM with booking permission cannot open Guest House Master", !CanOpen(rm, "GuestHouseMaster"));
		Check.That("RM with booking permission does not see Guest House Master in the menu", !rm.CanSeeMenu(PagePermission.GuestHouse));

		var masterOnly = Login(AppRole.RM, "GuestHouse.View");
		Check.That("RM with only the Master permission opens Guest House Master", CanOpen(masterOnly, "GuestHouseMaster"));
		Check.That("RM with only the Master permission cannot open /GuestHouse (booking)", !CanOpen(masterOnly, "GuestHouse"));
		Check.That("RM with only the Master permission cannot open any booking route", !CanOpenAny(masterOnly, BookingRoutes));
		Check.That("RM with only the Master permission cannot open My Bookings routes", !CanOpenAny(masterOnly, MyBookingsRoutes));
	}

	private static void MissingPermissionBlocksTheRoutes()
	{
		var rm = Login(AppRole.RM, "SDWADashboard.View");
		Check.That("RM without booking permission cannot open booking routes (direct URL)", !CanOpenAny(rm, BookingRoutes));
		Check.That("RM without MyBookings permission cannot open My Bookings / cancellation routes", !CanOpenAny(rm, MyBookingsRoutes));

		// The legacy fallback ("the raw segment is a key") must not reopen them either.
		var stray = Login(AppRole.RM, "Rooms.View,Payment.View,BookingDetails.View,CancelBooking.View");
		Check.That("Per-step legacy tokens alone do not open the booking routes", !CanOpenAny(stray, BookingRoutes));
		Check.That("Per-step legacy tokens alone do not open the My Bookings routes", !CanOpenAny(stray, MyBookingsRoutes));
	}

	private static void DashboardIsNotRequired()
	{
		var rm = Login(AppRole.RM, "GuestHouseBooking.View,MyBookings.View");
		Check.That("RM without Dashboard cannot open Dashboard", !CanOpen(rm, "Dashboard"));
		Check.That("RM without Dashboard cannot see the Dashboard menu", !rm.CanSeeMenu(PagePermission.Dashboard));
		Check.That("RM without Dashboard can open the assigned booking routes", CanOpenAll(rm, BookingRoutes));
		Check.That("RM without Dashboard can open the assigned My Bookings routes", CanOpenAll(rm, MyBookingsRoutes));
		Check.Equal("Landing page is unchanged for a designated user", LoginState.DefaultLandingPath, rm.LandingPage);

		var withDashboard = Login(AppRole.RM, "Dashboard.View");
		Check.That("Dashboard permission alone does not open booking routes", !CanOpenAny(withDashboard, BookingRoutes));
	}

	private static void SmmFollowsTheSameRules()
	{
		var smm = Login(AppRole.SMM, "GuestHouseBooking.View,MyBookings.View");
		Check.That("SMM with both permissions opens booking routes", CanOpenAll(smm, BookingRoutes));
		Check.That("SMM with both permissions opens My Bookings routes", CanOpenAll(smm, MyBookingsRoutes));
		Check.That("SMM with both permissions cannot open Guest House Master", !CanOpen(smm, "GuestHouseMaster"));

		var none = Login(AppRole.SMM, "SDWADashboard.View");
		Check.That("SMM without the permissions cannot open booking or My Bookings routes",
			!CanOpenAny(none, BookingRoutes) && !CanOpenAny(none, MyBookingsRoutes));
		Check.That("SMM without the permissions sees no Guest House menu entries",
			!none.CanSeeMenu(PagePermission.GuestHouseBooking) && !none.CanSeeMenu(PagePermission.MyBookings) && !none.CanSeeMenu(PagePermission.GuestHouse));
	}

	private static void AdminAndSuperAdminKeepTheirAccess()
	{
		foreach (var role in new[] { AppRole.Admin, AppRole.SuperAdmin })
		{
			var admin = Login(role, null);
			Check.That($"{role} reaches booking and My Bookings routes", CanOpenAll(admin, BookingRoutes) && CanOpenAll(admin, MyBookingsRoutes));
			Check.That($"{role} reaches Guest House Master", CanOpen(admin, "GuestHouseMaster"));
			Check.That($"{role} sees all three Guest House menu entries",
				admin.CanSeeMenu(PagePermission.GuestHouseBooking) && admin.CanSeeMenu(PagePermission.MyBookings) && admin.CanSeeMenu(PagePermission.GuestHouse));
		}
	}

	private static void DealerBookingAccessIsPreserved()
	{
		// After the backfill a Dealer designation that held GuestHouse also holds GuestHouseBooking.
		var dealer = Login(AppRole.Dealer, "SDWADashboard.View,GuestHouse.View,GuestHouseBooking.View,MyBookings.View");
		Check.That("Dealer with the backfilled GuestHouseBooking token still books rooms", CanOpenAll(dealer, BookingRoutes));
		Check.That("Dealer keeps My Bookings", CanOpenAll(dealer, MyBookingsRoutes));
		Check.That("Dealer keeps Guest House Master only through its existing GuestHouse token", CanOpen(dealer, "GuestHouseMaster"));

		// The Dealer route rules in PageGuard are untouched: the SDWA dashboard pages still bypass the grid.
		var shellDealer = ShellNavigation.Find("GuestHouse");
		Check.That("Dealer priority tab 'GuestHouse' is visible with GuestHouseBooking",
			shellDealer != null && ShellNavigation.IsAccessible(dealer, shellDealer));
	}

	private static void AdminCancellationRequestsAreUnchanged()
	{
		var bookingUser = Login(AppRole.RM, "GuestHouseBooking.View,MyBookings.View");
		Check.That("Booking permissions do not open Cancellation Requests", !CanOpen(bookingUser, "GuestHouseCancellations"));
		Check.That("Booking permissions do not show Cancellation Requests", !bookingUser.CanSeeMenu(PagePermission.GuestHouseCancellations));

		var cancellation = Login(AppRole.RM, "GuestHouseCancellations.View");
		Check.That("GuestHouseCancellations still opens the Cancellation Requests route", CanOpen(cancellation, "GuestHouseCancellations"));
		Check.That("GuestHouseCancellations does not open booking or My Bookings routes",
			!CanOpenAny(cancellation, BookingRoutes) && !CanOpenAny(cancellation, MyBookingsRoutes));

		var ghAdmin = Login(AppRole.RM, "GHAdmin.View");
		Check.That("GHAdmin designation still sees Master and Cancellation Requests",
			ghAdmin.HasGuestHouseAdminPermission);
	}

	private static void MenuVisibilityFollowsEachPermission()
	{
		var bookingTab = ShellNavigation.Find("GuestHouse");
		var myBookingsTab = ShellNavigation.Find("MyBookings");
		var masterTab = ShellNavigation.Find("GuestHouseMaster");
		Check.That("Shell tabs for the three entries exist", bookingTab != null && myBookingsTab != null && masterTab != null);
		if (bookingTab == null || myBookingsTab == null || masterTab == null) return;

		Check.Equal("Book a Room tab permission key", nameof(PagePermission.GuestHouseBooking), bookingTab.PermissionKey);
		Check.Equal("My Bookings tab permission key", nameof(PagePermission.MyBookings), myBookingsTab.PermissionKey);
		Check.Equal("Guest House Master tab permission key", nameof(PagePermission.GuestHouse), masterTab.PermissionKey);

		var rm = Login(AppRole.RM, "GuestHouseBooking.View,MyBookings.View");
		Check.That("RM sees Book a Room", ShellNavigation.IsAccessible(rm, bookingTab));
		Check.That("RM sees My Bookings", ShellNavigation.IsAccessible(rm, myBookingsTab));
		Check.That("RM does not see Guest House Master", !ShellNavigation.IsAccessible(rm, masterTab));

		var myBookingsOnly = Login(AppRole.RM, "MyBookings.View");
		Check.That("MyBookings only: no Book a Room", !ShellNavigation.IsAccessible(myBookingsOnly, bookingTab));
		Check.That("MyBookings only: My Bookings shown", ShellNavigation.IsAccessible(myBookingsOnly, myBookingsTab));
	}

	// ---- backend ----------------------------------------------------------------------------------

	private static void BackendFilterDecision()
	{
		const string bookingOnly = "GuestHouseBooking.View";
		const string myBookingsOnly = "MyBookings.View";
		const string masterOnly = "GuestHouse.View";

		Check.That("API: GuestHouseBooking grants booking endpoints",
			GuestHouseBookingAccessAttribute.IsGranted(false, bookingOnly, PagePermission.GuestHouseBooking));
		Check.That("API: missing GuestHouseBooking rejects booking endpoints",
			!GuestHouseBookingAccessAttribute.IsGranted(false, myBookingsOnly, PagePermission.GuestHouseBooking));
		Check.That("API: the Master permission does not grant booking endpoints",
			!GuestHouseBookingAccessAttribute.IsGranted(false, masterOnly, PagePermission.GuestHouseBooking));
		Check.That("API: a designation-less user is rejected",
			!GuestHouseBookingAccessAttribute.IsGranted(false, null, PagePermission.GuestHouseBooking)
			&& !GuestHouseBookingAccessAttribute.IsGranted(false, "", PagePermission.MyBookings));
		Check.That("API: own-cancellation requires MyBookings",
			GuestHouseBookingAccessAttribute.IsGranted(false, myBookingsOnly, PagePermission.MyBookings)
			&& !GuestHouseBookingAccessAttribute.IsGranted(false, bookingOnly, PagePermission.MyBookings));
		Check.That("API: Admin / SuperAdmin bypass is preserved",
			GuestHouseBookingAccessAttribute.IsGranted(true, null, PagePermission.GuestHouseBooking)
			&& GuestHouseBookingAccessAttribute.IsGranted(true, null, PagePermission.MyBookings));
		Check.That("API: a Dealer designation holding the backfilled token is granted",
			GuestHouseBookingAccessAttribute.IsGranted(false, "SDWADashboard,GuestHouse,GuestHouseBooking", PagePermission.GuestHouseBooking));
	}

	private static MethodInfo[] Actions() =>
		typeof(GuestHouseBookingController)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
			.Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any())
			.ToArray();

	private static PagePermission? Declared(MethodInfo m) =>
		m.GetCustomAttribute<GuestHouseBookingAccessAttribute>()?.Permission;

	private static void EveryBookingEndpointDeclaresAPermission()
	{
		// Image and document streaming keep their own rules: the image endpoint serves the catalogue photos
		// that Guest House Master and Front Office also show; documents are owner / Front Office scoped inside.
		var exempt = new HashSet<string> { "ViewImage", "ViewDocument", "DownloadDocument" };
		var actions = Actions();
		Check.That("Booking controller actions were discovered", actions.Length >= 15);

		var undeclared = actions
			.Where(a => Declared(a) is null && !exempt.Contains(a.Name))
			.Select(a => a.Name)
			.ToList();
		Check.That("Every booking endpoint declares a page permission (or is a documented exemption)",
			undeclared.Count == 0, string.Join(", ", undeclared));

		var booking = new[] { "GetHouses", "GetAvailability", "GetRoom", "CreateBooking", "UploadTempDocument", "CreatePaymentOrder", "VerifyPayment", "MarkPaymentFailed" };
		var myBookings = new[] { "GetMyBookings", "GetBookingDetails", "DownloadInvoice", "GetCancellationPreview", "RequestCancellation", "GetRefundStatus" };

		foreach (var name in booking.Concat(myBookings))
			Check.That($"Endpoint {name} exists", actions.Any(a => a.Name == name));

		foreach (var a in actions.Where(a => booking.Contains(a.Name)))
			Check.Equal($"{a.Name} requires GuestHouseBooking", (PagePermission?)PagePermission.GuestHouseBooking, Declared(a));
		foreach (var a in actions.Where(a => myBookings.Contains(a.Name)))
			Check.Equal($"{a.Name} requires MyBookings", (PagePermission?)PagePermission.MyBookings, Declared(a));

		foreach (var a in actions.Where(a => a.Name.Contains("Payment", StringComparison.OrdinalIgnoreCase)
			|| a.GetCustomAttributes<HttpMethodAttribute>().Any(h => h.Template?.Contains("payment") == true)))
			Check.Equal($"Payment endpoint {a.Name} requires GuestHouseBooking", (PagePermission?)PagePermission.GuestHouseBooking, Declared(a));
	}

	private static void OwnershipCheckStillGuardsCancellation()
	{
		var source = File.ReadAllText(RepoFile("SpicAPI", "Controllers", "GuestHouseBookingController.cs"));
		var start = source.IndexOf("public async Task<IActionResult> RequestCancellation(", StringComparison.Ordinal);
		var next = source.IndexOf("[HttpGet(", start, StringComparison.Ordinal);
		var body = start >= 0 && next > start ? source[start..next] : string.Empty;
		Check.That("RequestCancellation still rejects a booking the caller does not own (IsBookingOwner)",
			body.Contains("!IsBookingOwner(booking.CreatedBy)") && body.Contains("NotFound"));
	}

	private static void FrontOfficeAndMasterFiltersAreUntouched()
	{
		var frontOffice = typeof(GuestHouseFrontOfficeController).GetCustomAttributes(true)
			.Any(a => a.GetType().Name == "GuestHouseFrontOfficeAccessAttribute");
		Check.That("GuestHouseFrontOfficeController keeps its access filter", frontOffice);
		Check.That("Front Office controller is not using the booking filter",
			!typeof(GuestHouseFrontOfficeController).GetMethods().Any(m => m.GetCustomAttribute<GuestHouseBookingAccessAttribute>() != null));
		Check.That("GuestHouseMasterController is not using the booking filter",
			!typeof(GuestHouseMasterController).GetMethods().Any(m => m.GetCustomAttribute<GuestHouseBookingAccessAttribute>() != null));
	}

	// ---- menus ------------------------------------------------------------------------------------

	private static void DesktopAndMobileMenusMatch()
	{
		var desktop = File.ReadAllText(RepoFile("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Layout", "NavMenu.razor"));
		var mobile = File.ReadAllText(RepoFile("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Components", "Shell", "MobileSidebar.razor"));

		foreach (var (name, text) in new[] { ("NavMenu", desktop), ("MobileSidebar", mobile) })
		{
			Check.That($"{name}: Book a Room entry is gated by GuestHouseBooking only",
				HasEntry(text, "PagePermission.GuestHouseBooking", "/GuestHouse\""));
			Check.That($"{name}: My Bookings entry is gated by MyBookings only",
				HasEntry(text, "PagePermission.MyBookings", "/MyBookings\""));
			Check.That($"{name}: Master entry is still gated by GuestHouse (not booking)",
				text.Contains("CanSeeMenu(PagePermission.GuestHouse) || ") && text.Contains("href=\"/GuestHouseMaster\""));
			Check.That($"{name}: SDWA group opens for booking-only and My Bookings-only users",
				text.Contains("CanSeeMenu(PagePermission.GuestHouseBooking)") && text.Contains("CanSeeMenu(PagePermission.MyBookings)"));
			Check.That($"{name}: Cancellation Requests entry is unchanged",
				text.Contains("CanSeeMenu(PagePermission.GuestHouseCancellations)") && text.Contains("href=\"/GuestHouseCancellations\""));
			Check.That($"{name}: active-section list includes the new routes",
				text.Contains("uri == \"guesthouse\"") && text.Contains("uri == \"mybookings\""));
		}

		Check.That("NavMenu: menu search knows Book a Room and My Bookings",
			desktop.Contains("SearchState.Matches(\"Book a Room\")") && desktop.Contains("SearchState.Matches(\"My Bookings\")"));
		Check.That("NavMenu: search highlight classes use the new labels",
			desktop.Contains("SubmenuLinkClass(\"Book a Room\")") && desktop.Contains("SubmenuLinkClass(\"My Bookings\")"));
	}

	// "Cancel Booking" in the menus: /CancelBooking needs ?bookingId=, so the entry opens My Bookings, where the
	// user picks their own booking. It lives in the same MyBookings-gated block as "My Bookings".
	private static void CancelBookingMenuEntryStartsFromMyBookings()
	{
		var shared = new[] { "SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared" };
		var desktop = File.ReadAllText(RepoFile(shared.Concat(new[] { "Layout", "NavMenu.razor" }).ToArray()));
		var mobile = File.ReadAllText(RepoFile(shared.Concat(new[] { "Components", "Shell", "MobileSidebar.razor" }).ToArray()));

		foreach (var (name, text) in new[] { ("NavMenu", desktop), ("MobileSidebar", mobile) })
		{
			var start = text.IndexOf("CanSeeMenu(PagePermission.MyBookings))", StringComparison.Ordinal);
			var next = start < 0 ? -1 : text.IndexOf("@if (", start, StringComparison.Ordinal);
			var block = start >= 0 && next > start ? text[start..next] : string.Empty;

			Check.That($"{name}: My Bookings and Cancel Booking share the MyBookings-only block",
				block.Contains("My Bookings") && block.Contains("Cancel Booking") && !block.Contains("||"));
			Check.That($"{name}: Cancel Booking opens /MyBookings, never /CancelBooking without a booking",
				block.Contains("href=\"/MyBookings\"") && !text.Contains("href=\"/CancelBooking\""));
			Check.That($"{name}: /CancelBooking keeps the SDWA section open",
				text.Contains("uri == \"cancelbooking\"") && text.Contains("uri.StartsWith(\"cancelbooking?\")"));
		}

		Check.That("NavMenu: menu search knows Cancel Booking",
			desktop.Contains("SearchState.Matches(\"Cancel Booking\")") && desktop.Contains("SubmenuLinkClass(\"Cancel Booking\")"));

		var cancelPage = File.ReadAllText(RepoFile(shared.Concat(new[] { "Pages", "CancelBooking.razor" }).ToArray()));
		var myBookingsPage = File.ReadAllText(RepoFile(shared.Concat(new[] { "Pages", "MyBookings.razor" }).ToArray()));
		Check.That("CancelBooking still needs a bookingId (so a bare menu link would be invalid)",
			cancelPage.Contains("Name = \"bookingId\"") && cancelPage.Contains("No booking selected."));
		Check.That("My Bookings opens the existing cancel flow for the selected booking",
			myBookingsPage.Contains("/CancelBooking?bookingId={bookingId}"));

		// Permission behaviour for both entries and the direct route.
		var with = Login(AppRole.RM, "MyBookings.View");
		var without = Login(AppRole.RM, "GuestHouseBooking.View,GuestHouse.View");
		Check.That("With MyBookings: both entries and /CancelBooking are available",
			with.CanSeeMenu(PagePermission.MyBookings) && CanOpen(with, "MyBookings") && CanOpen(with, "CancelBooking"));
		Check.That("Without MyBookings: neither entry is shown and both routes are blocked",
			!without.CanSeeMenu(PagePermission.MyBookings) && !CanOpen(without, "MyBookings") && !CanOpen(without, "CancelBooking"));
		Check.That("Booking and Master permissions stay independent of the new entries",
			Login(AppRole.RM, "MyBookings.View").CanSeeMenu(PagePermission.GuestHouseBooking) == false
			&& Login(AppRole.RM, "MyBookings.View").CanSeeMenu(PagePermission.GuestHouse) == false);
	}

	// An @if (... <permission> ...) block followed, within a few lines, by the NavLink to <href>.
	private static bool HasEntry(string razor, string permissionExpr, string href)
	{
		var idx = 0;
		while ((idx = razor.IndexOf("@if (", idx, StringComparison.Ordinal)) >= 0)
		{
			var lineEnd = razor.IndexOf('\n', idx);
			var condition = razor[idx..lineEnd];
			var window = razor[idx..Math.Min(razor.Length, idx + 600)];
			if (condition.Contains(permissionExpr) && !condition.Contains("||") && window.Contains("href=\"" + href.TrimStart()))
				return true;
			idx = lineEnd;
		}
		return false;
	}
}
