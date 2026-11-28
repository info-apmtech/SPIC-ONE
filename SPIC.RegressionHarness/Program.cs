namespace SPIC.RegressionHarness;

internal static class Program
{
	private static int Main()
	{
		Console.WriteLine("SPIC regression harness");
		Console.WriteLine("========================");
		Console.WriteLine();

		// Every importer routes its master matching through these two types, so they
		// are the highest-leverage checks in the repository: a regression here silently
		// reappears in all eleven bulk upload flows at once.
		MasterNormalizerTests.Run();
		Console.WriteLine();
		MissingMasterCollectorTests.Run();
		Console.WriteLine();

		// Role -> authorization model, the bypass sets and the OpenAccess list: pure
		// data, so the Designation-based access rules can be asserted without a server.
		PageAuthorizationTests.Run();
		Console.WriteLine();

		// Data Explorer access rule shared by the UI and the API.
		DataExplorerAuthorizationTests.Run();
		Console.WriteLine();

		// Guest House single and multi-record inventory availability, holding windows,
		// serialization detection, cancellations, check-in, check-out and allocations.
		GuestHouseInventoryTests.Run();
		Console.WriteLine();

		// Guest House Master / Book a Room / My Bookings: independent permissions, route mapping,
		// menu visibility and the server-side filter on the booking API.
		GuestHousePermissionTests.Run();
		Console.WriteLine();

		// Receptionist state scope on the booking API (houses / availability / rooms / book).
		GuestHouseReceptionistScopeTests.Run();
		Console.WriteLine();

		// Room number is shown only once assigned at check-in (Booking Preview, My Bookings, Booking Details).
		GuestHouseRoomNumberDisplayTests.Run();
		Console.WriteLine();

		// Employee Stay shows "Booked"; Dealer Paid / Pending / Failed / Refunded unchanged (display only).
		GuestHousePaymentBadgeTests.Run();
		Console.WriteLine();

		// My Bookings: Cancel Booking hidden once CheckedIn / Completed / Cancelled.
		GuestHouseMyBookingsCancelButtonTests.Run();
		Console.WriteLine();

		// One booking = one room = one bill: real controller actions on an InMemory database
		// with a stubbed Razorpay service (no real payment, no real database).
		GuestHouseSingleRoomBookingTests.Run();
		Console.WriteLine();

		// Late check-out grace period + extra-day charge, payment gate, invoice line, overdue-room protection.
		GuestHouseLateCheckOutTests.Run();
		Console.WriteLine();

		// Front Office Booking Details: current + future Confirmed/CheckedIn bookings, one row per booking.
		GuestHouseFrontOfficeBookingListTests.Run();
		Console.WriteLine();

		// GPS Settings entity, DTOs, controller authorization, and role access.
		GpsSettingsTests.Run();
		Console.WriteLine();
		MultiRegionHqTests.Run();
		SessionClockTests.Run();
		Console.WriteLine();

		// PageGuard's route decision (RouteAccess), shared with the Help Desk.
		RouteAccessTests.Run();
		Console.WriteLine();

		// Help Desk: guide format, library, visibility, coverage, translation, voice, review.
		HelpParserTests.Run();
		HelpLibraryTests.Run();
		HelpCatalogTests.Run();
		HelpCoverageTests.Run();
		HelpTranslationTests.Run();
		HelpSpeechTests.Run();
		HelpReviewStatusTests.Run();
		HelpReviewApplierTests.Run();
		HelpImportTests.Run();
		HelpParserV2Tests.Run();
		HelpBlocksTests.Run();
		HelpV2TranslationTests.Run();
		HelpMarkdownTests.Run();
		HelpPathsTests.Run();
		HelpCoverageV2Tests.Run();
		HelpFeedbackTests.Run();
		HelpCatalogV2Tests.Run();
		HelpReviewFixTests.Run();
		Console.WriteLine();

		// Farmer portal (docs/farmer-portal.md): OTP sign-in rules and the farmer profile.
		FarmerPortalAuthTests.Run();
		FarmerPortalRouteTests.Run();
		FarmerPortalUiTests.Run();
		FarmerPortalConnectTests.Run();
		FarmerPortalTrendsTests.Run();
		FarmerPortalFertilizerTests.Run();
		FarmerPortalAssistantTests.Run();
		FarmerPortalLeafCheckTests.Run();
		FarmerPortalYieldTests.Run();
		FarmerPortalHardeningTests.Run();
		FarmerPortalRecheckTests.Run();

		return Check.Report("Total");
	}
}
