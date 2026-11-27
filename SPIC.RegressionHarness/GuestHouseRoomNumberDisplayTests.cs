using SPIC.Core.Entities;
using SpicAPI.Controllers;

namespace SPIC.RegressionHarness;

/// <summary>
/// A physical room is assigned only at Front Office check-in. GuestHouseRoom.RoomNumber is just the first
/// number of the room-type record, so it must never be presented as the guest's room before check-in:
///  - Booking Preview shows "Assigned at check-in" and GuestDetails no longer copies the base number;
///  - CustomerRoomNumber (My Bookings / Booking Details) is null before check-in, the allocated room(s)
///    while checked in, and the check-out snapshot once completed; legacy and multi-record stays keep working.
/// </summary>
internal static class GuestHouseRoomNumberDisplayTests
{
	public static void Run()
	{
		Console.WriteLine("GuestHouseRoomNumberDisplay");

		BeforeCheckInShowsNoBaseNumber();
		CheckedInShowsAllocatedRoom();
		CompletedUsesSnapshot();
		LegacyAndMultiRecordKeepWorking();
		BookingPreviewAndGuestDetailsSource();
	}

	private static GuestHouseBooking Booking(GuestHouseBookingStatus status, string baseNumber = "209",
		string? allocatedSnapshot = null, params string[] allocationRows)
	{
		var booking = new GuestHouseBooking
		{
			Id = 1,
			GuestHouseId = 1,
			GuestHouseRoomId = 5,
			BookingStatus = status,
			AllocatedRoomNumber = allocatedSnapshot,
			GuestHouseRoom = new GuestHouseRoom { Id = 5, GuestHouseId = 1, RoomNumber = baseNumber, AvailableQuantity = 4 }
		};
		foreach (var number in allocationRows)
			booking.RoomAllocations.Add(new GuestHouseRoomAllocation { GuestHouseBookingId = 1, GuestHouseId = 1, GuestHouseRoomId = 5, RoomNumber = number });
		return booking;
	}

	private static void BeforeCheckInShowsNoBaseNumber()
	{
		Console.WriteLine(" 1. before check-in the base RoomNumber is never shown");
		foreach (var status in new[]
		{
			GuestHouseBookingStatus.Draft, GuestHouseBookingStatus.PendingPayment,
			GuestHouseBookingStatus.Confirmed, GuestHouseBookingStatus.Cancelled
		})
			Check.That($"{status} booking has no room number", GuestHouseBookingController.CustomerRoomNumber(Booking(status)) == null);
	}

	private static void CheckedInShowsAllocatedRoom()
	{
		Console.WriteLine(" 2. a checked-in booking shows its allocated room");
		var one = GuestHouseBookingController.CustomerRoomNumber(Booking(GuestHouseBookingStatus.CheckedIn, "209", null, "211"));
		Check.That("allocated 211, not base 209", one == "211", one);

		var two = GuestHouseBookingController.CustomerRoomNumber(Booking(GuestHouseBookingStatus.CheckedIn, "209", null, "212", "210"));
		Check.That("several allocated rooms sorted", two == "210, 212", two);
	}

	private static void CompletedUsesSnapshot()
	{
		Console.WriteLine(" 3. a completed booking uses the check-out snapshot");
		var snap = GuestHouseBookingController.CustomerRoomNumber(Booking(GuestHouseBookingStatus.Completed, "209", "211"));
		Check.That("snapshot 211, not base 209", snap == "211", snap);

		var rows = GuestHouseBookingController.CustomerRoomNumber(Booking(GuestHouseBookingStatus.Completed, "209", "211", "211"));
		Check.That("actual allocation still preferred when present", rows == "211", rows);
	}

	private static void LegacyAndMultiRecordKeepWorking()
	{
		Console.WriteLine(" 4. legacy and multi-record stays unchanged");
		var legacyDone = GuestHouseBookingController.CustomerRoomNumber(Booking(GuestHouseBookingStatus.Completed, "209"));
		Check.That("legacy completed stay without snapshot falls back to the record number", legacyDone == "209", legacyDone);

		var multi = Booking(GuestHouseBookingStatus.Confirmed, "209");
		multi.RoomAllocations.Add(new GuestHouseRoomAllocation { GuestHouseBookingId = 1, GuestHouseId = 1, GuestHouseRoomId = 5, RoomNumber = "209" });
		multi.RoomAllocations.Add(new GuestHouseRoomAllocation { GuestHouseBookingId = 1, GuestHouseId = 1, GuestHouseRoomId = 6, RoomNumber = "305" });
		var shown = GuestHouseBookingController.CustomerRoomNumber(multi);
		Check.That("multi-record booking still lists its reserved rooms", shown == "209, 305", shown);
	}

	private static string Page(string file) =>
		File.ReadAllText(RepoFile("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Pages", file));

	private static string RepoFile(params string[] parts)
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SpicOne.sln")))
			dir = dir.Parent;
		return Path.Combine(new[] { dir?.FullName ?? throw new InvalidOperationException("SpicOne.sln not found") }.Concat(parts).ToArray());
	}

	private static void BookingPreviewAndGuestDetailsSource()
	{
		Console.WriteLine(" 5. Booking Preview and Guest Details");
		var preview = Page("BookingPreview.razor");
		Check.That("preview shows 'Assigned at check-in'", preview.Contains("Assigned at check-in"));
		Check.That("preview no longer prints BookingState.RoomNumber", !preview.Contains("@BookingState.RoomNumber"));

		var details = Page("GuestDetails.razor");
		Check.That("GuestDetails does not copy the base number into BookingState",
			!details.Contains("BookingState.RoomNumber = _roomNumber"));
		Check.That("room type is still carried", details.Contains("BookingState.RoomType = _roomType"));
	}
}
