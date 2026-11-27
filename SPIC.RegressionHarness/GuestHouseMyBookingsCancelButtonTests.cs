using System.Reflection;
using SPIC.Core.Entities;
using SpicAPI.Controllers;

namespace SPIC.RegressionHarness;

/// <summary>
/// My Bookings offers "Cancel Booking" only while a stay can still be cancelled: not once CheckedIn,
/// Completed or Cancelled (Cancelled shows Refund Status instead). Frontend visibility only - the server
/// refuses cancellation of a checked-in stay regardless.
///
/// The decision is inline in MyBookings.razor, so this pins the markup condition and runs the page's own
/// status helpers against the exact status strings the API sends.
/// </summary>
internal static class GuestHouseMyBookingsCancelButtonTests
{
	public static void Run()
	{
		Console.WriteLine("GuestHouseMyBookingsCancelButton");

		var source = File.ReadAllText(RepoFile("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Pages", "MyBookings.razor"));
		Check.That("Cancel Booking is hidden for Completed and CheckedIn",
			source.Contains("else if (!IsCompleted(booking.BookingStatus) && !IsCheckedIn(booking.BookingStatus))"));
		Check.That("Cancelled still shows Refund Status instead of Cancel Booking",
			source.Contains("if (IsCancelled(booking.BookingStatus))") && source.Contains("ViewRefundStatus(booking.BookingId)"));

		var page = typeof(SPIC.MauiBlazorApp.Shared.Pages.MyBookings);
		bool Helper(string name, string status) =>
			(bool)page.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { status })!;
		var apiName = typeof(GuestHouseBookingController).GetMethod("BookingStatusName", BindingFlags.NonPublic | BindingFlags.Static)!;
		string Api(GuestHouseBookingStatus s) => (string)apiName.Invoke(null, new object[] { s })!;

		// Offered = not Completed, not CheckedIn, not Cancelled (the markup's three branches).
		bool CancelOffered(GuestHouseBookingStatus s)
		{
			var shown = Api(s);
			return !Helper("IsCancelled", shown) && !Helper("IsCompleted", shown) && !Helper("IsCheckedIn", shown);
		}

		Check.That("Confirmed: Cancel Booking offered", CancelOffered(GuestHouseBookingStatus.Confirmed));
		Check.That("PendingPayment: Cancel Booking offered (unchanged)", CancelOffered(GuestHouseBookingStatus.PendingPayment));
		Check.That("Draft: Cancel Booking offered (unchanged)", CancelOffered(GuestHouseBookingStatus.Draft));
		Check.That("CheckedIn: Cancel Booking hidden", !CancelOffered(GuestHouseBookingStatus.CheckedIn));
		Check.That("Completed: Cancel Booking hidden", !CancelOffered(GuestHouseBookingStatus.Completed));
		Check.That("Cancelled: Cancel Booking hidden", !CancelOffered(GuestHouseBookingStatus.Cancelled));
	}

	private static string RepoFile(params string[] parts)
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SpicOne.sln")))
			dir = dir.Parent;
		return Path.Combine(new[] { dir?.FullName ?? throw new InvalidOperationException("SpicOne.sln not found") }.Concat(parts).ToArray());
	}
}
