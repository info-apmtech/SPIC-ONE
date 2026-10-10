using Microsoft.EntityFrameworkCore;
using Npgsql;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace SpicAPI.Services
{
	/// <summary>
	/// Shared Guest House inventory rules used by booking (GuestHouseBookingController) and the
	/// Front Office (GuestHouseFrontOfficeController), so both always agree on which inventory
	/// record (GuestHouseRoom row) a booking holds and which physical room numbers are free.
	///
	/// HOW A BOOKING HOLDS INVENTORY
	/// <list type="bullet">
	/// <item>Single-record booking (every booking made before multi-record booking existed, and
	/// every booking whose quantity fits in one record): holds NumberOfRooms on
	/// GuestHouseBooking.GuestHouseRoomId. Before check-in it has no GuestHouseRoomAllocation
	/// rows; after check-in its rows all point at that same record.</item>
	/// <item>Multi-record booking: created with exactly one GuestHouseRoomAllocation row per
	/// reserved room, each on the record that owns it. At least one row points at a record other
	/// than GuestHouseRoomId, which is how it is recognised. It holds, per record, the number of
	/// its rows on that record. Check-in replaces the rows but keeps the per-record counts.</item>
	/// </list>
	/// A row of a booking that is not CheckedIn is a reservation; a row of a CheckedIn booking is
	/// the final physical room - the booking status tells them apart, as the Front Office grid
	/// already does. Rows are removed at check-out, on payment failure and on cancellation
	/// approval, which is exactly when the booking stops holding inventory.
	/// </summary>
	public static class GuestHouseRoomInventory
	{
		/// <summary>One active booking's hold on inventory.</summary>
		public sealed class RoomHolding
		{
			public int BookingId { get; init; }
			public int PrimaryRoomId { get; init; }
			public int NumberOfRooms { get; init; }
			public GuestHouseBookingStatus BookingStatus { get; init; }
			public DateTime? CheckInDate { get; init; }
			public IReadOnlyList<GuestHouseRoomAllocation> Rows { get; init; } = Array.Empty<GuestHouseRoomAllocation>();

			public bool IsMultiRecord => IsMultiRecordBooking(PrimaryRoomId, Rows);

			/// <summary>Rooms this booking holds on the given inventory record.</summary>
			public int ReservedOn(int roomId) => ReservedOnRecord(PrimaryRoomId, NumberOfRooms, Rows, roomId);

			/// <summary>Inventory records this booking holds rooms on.</summary>
			public IEnumerable<int> RecordIds => IsMultiRecord
				? Rows.Select(r => r.GuestHouseRoomId).Distinct()
				: new[] { PrimaryRoomId };
		}

		public sealed record FreeRoomsResult(List<string> Free, List<string> Occupied);

		/// <summary>
		/// Returns true if the exception or any inner exception is a PostgreSQL serialization
		/// failure (SQLSTATE 40001: could not serialize access due to concurrent read/write dependencies).
		/// </summary>
		public static bool IsSerializationFailure(Exception? ex)
		{
			for (var current = ex; current != null; current = current.InnerException)
			{
				if (current is PostgresException { SqlState: "40001" })
					return true;
			}
			return false;
		}

		public static bool IsMultiRecordBooking(int primaryRoomId, IEnumerable<GuestHouseRoomAllocation>? rows) =>
			rows != null && rows.Any(r => r.GuestHouseRoomId != primaryRoomId);

		public static int ReservedOnRecord(int primaryRoomId, int? numberOfRooms, IEnumerable<GuestHouseRoomAllocation>? rows, int roomId)
		{
			if (IsMultiRecordBooking(primaryRoomId, rows))
				return rows!.Count(r => r.GuestHouseRoomId == roomId);
			return roomId == primaryRoomId ? (numberOfRooms ?? 1) : 0;
		}

		/// <summary>
		/// The same grouping the Rooms page uses (Rooms.razor GetGroupKey): same guest house, same
		/// Master Room Type (or, for legacy rows without one, the same room type name), same rate
		/// and same extra-bed price. Only compatible records may share one booking.
		/// </summary>
		public static bool AreCompatible(GuestHouseRoom a, GuestHouseRoom b)
		{
			if (a.GuestHouseId != b.GuestHouseId) return false;
			if (Math.Round(a.PricePerNight, 2) != Math.Round(b.PricePerNight, 2)) return false;
			if (RoundOrNull(a.ExtraCotPrice) != RoundOrNull(b.ExtraCotPrice)) return false;

			var aMaster = a.RoomTypeId is > 0 ? a.RoomTypeId : null;
			var bMaster = b.RoomTypeId is > 0 ? b.RoomTypeId : null;
			if (aMaster.HasValue || bMaster.HasValue)
				return aMaster == bMaster;

			return string.Equals(LegacyTypeKey(a.RoomType), LegacyTypeKey(b.RoomType), StringComparison.Ordinal);
		}

		private static decimal? RoundOrNull(decimal? value) => value.HasValue ? Math.Round(value.Value, 2) : null;

		private static string LegacyTypeKey(string? roomType) => (roomType ?? "Room").Trim().ToLowerInvariant();

		/// <summary>
		/// Active bookings whose stay overlaps [checkInDate, checkOutDate) and that hold any of the
		/// given records - through GuestHouseRoomId or through allocation rows - each returned ONCE
		/// with all of its allocation rows. Status / PendingPayment hold-window rules are the same
		/// ones availability has always used: Cancelled / Completed never hold; PendingPayment holds
		/// only while CreatedAt &gt;= pendingPaymentHoldCutoff; everything else holds.
		/// </summary>
		public static async Task<List<RoomHolding>> GetHoldingsAsync(
			AppDbContext db,
			IReadOnlyCollection<int> roomIds,
			DateTime checkInDate,
			DateTime checkOutDate,
			DateTime pendingPaymentHoldCutoff,
			int? excludeBookingId = null)
		{
			if (roomIds.Count == 0)
				return new List<RoomHolding>();

			var query = db.GuestHouseBookings
				.AsNoTracking()
				.Where(b => (roomIds.Contains(b.GuestHouseRoomId)
						|| b.RoomAllocations.Any(a => roomIds.Contains(a.GuestHouseRoomId)))
					&& b.BookingStatus != GuestHouseBookingStatus.Cancelled
					&& b.BookingStatus != GuestHouseBookingStatus.Completed
					&& (b.BookingStatus != GuestHouseBookingStatus.PendingPayment || b.CreatedAt >= pendingPaymentHoldCutoff)
					&& b.CheckInDate.HasValue && b.CheckOutDate.HasValue
					&& b.CheckInDate.Value.Date < checkOutDate.Date
					&& b.CheckOutDate.Value.Date > checkInDate.Date);

			if (excludeBookingId.HasValue)
				query = query.Where(b => b.Id != excludeBookingId.Value);

			var bookings = await query
				.Select(b => new { b.Id, b.GuestHouseRoomId, b.NumberOfRooms, b.BookingStatus, b.CheckInDate })
				.ToListAsync();
			if (bookings.Count == 0)
				return new List<RoomHolding>();

			var bookingIds = bookings.Select(b => b.Id).ToList();
			var rows = await db.GuestHouseRoomAllocations
				.AsNoTracking()
				.Where(a => bookingIds.Contains(a.GuestHouseBookingId))
				.ToListAsync();
			var rowsByBooking = rows
				.GroupBy(a => a.GuestHouseBookingId)
				.ToDictionary(g => g.Key, g => (IReadOnlyList<GuestHouseRoomAllocation>)g.ToList());

			return bookings
				.Select(b => new RoomHolding
				{
					BookingId = b.Id,
					PrimaryRoomId = b.GuestHouseRoomId,
					NumberOfRooms = b.NumberOfRooms ?? 1,
					BookingStatus = b.BookingStatus,
					CheckInDate = b.CheckInDate,
					Rows = rowsByBooking.TryGetValue(b.Id, out var r) ? r : Array.Empty<GuestHouseRoomAllocation>()
				})
				.ToList();
		}

		/// <summary>Rooms held per record by the overlapping active bookings (each booking counted once).</summary>
		public static async Task<Dictionary<int, int>> GetCommittedRoomsByRoomAsync(
			AppDbContext db,
			IReadOnlyCollection<int> roomIds,
			DateTime checkInDate,
			DateTime checkOutDate,
			DateTime pendingPaymentHoldCutoff,
			int? excludeBookingId = null)
		{
			var holdings = await GetHoldingsAsync(db, roomIds, checkInDate, checkOutDate, pendingPaymentHoldCutoff, excludeBookingId);

			var committed = new Dictionary<int, int>();
			foreach (var roomId in roomIds.Distinct())
			{
				var total = holdings.Sum(h => h.ReservedOn(roomId));
				if (total > 0)
					committed[roomId] = total;
			}
			return committed;
		}

		// ---- Physical room numbers ----
		//
		// GuestHouseRoom.RoomNumber is the FIRST physical room number of the record and
		// GuestHouseRoom.AvailableQuantity is how many such rooms exist. Physical rooms are
		// derived, never stored or hardcoded: a numeric base (e.g. "111" qty 4) yields 111,
		// 112, 113, 114; a non numeric base (e.g. "D1") yields D1-1, D1-2, ...; a missing
		// base yields "Room 1", "Room 2", ...

		public static string DeriveRoomNumber(string? baseNumber, int offset)
		{
			var baseText = (baseNumber ?? string.Empty).Trim();
			if (int.TryParse(baseText, out var baseValue))
			{
				var raw = baseText.TrimStart('-');
				var width = raw.Length > 1 ? raw.Length : 0;
				var number = (baseValue + offset).ToString(System.Globalization.CultureInfo.InvariantCulture);
				return width > number.Length ? number.PadLeft(width, '0') : number;
			}
			return string.IsNullOrWhiteSpace(baseText) ? $"Room {offset + 1}" : $"{baseText}-{offset + 1}";
		}

		public static List<string> EnumeratePhysicalRoomNumbers(GuestHouseRoom room, int quantity)
		{
			var count = Math.Max(1, quantity);
			var numbers = new List<string>(count);
			for (var i = 0; i < count; i++)
				numbers.Add(DeriveRoomNumber(room.RoomNumber, i));
			return numbers;
		}

		public static bool ContainsNumber(IReadOnlyCollection<string> numbers, string roomNumber) =>
			numbers.Contains(roomNumber, StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// The exact physical room numbers of one record currently free over [checkInDate, checkOutDate).
		///
		/// A room is NOT free when:
		///   1. an active overlapping booking holds that exact number (GuestHouseRoomAllocation), or
		///   2. it falls inside the deterministic lowest-number block still owed by overlapping
		///      bookings that hold rooms of this record without an exact number yet
		///      (rooms held on the record − exact rows on the record).
		/// </summary>
		public static async Task<FreeRoomsResult> GetFreePhysicalRoomsAsync(
			AppDbContext db,
			int roomId,
			DateTime checkInDate,
			DateTime checkOutDate,
			DateTime pendingPaymentHoldCutoff,
			int excludeBookingId = 0)
		{
			var room = await db.GuestHouseRooms
				.AsNoTracking()
				.FirstOrDefaultAsync(r => r.Id == roomId && r.IsActive);
			if (room == null || room.AvailableQuantity <= 0)
				return new FreeRoomsResult(new List<string>(), new List<string>());

			var numbers = EnumeratePhysicalRoomNumbers(room, room.AvailableQuantity);

			var holdings = await GetHoldingsAsync(
				db, new[] { roomId }, checkInDate, checkOutDate, pendingPaymentHoldCutoff,
				excludeBookingId > 0 ? excludeBookingId : null);

			// Exact rows of those bookings on this record.
			var exactRows = holdings
				.SelectMany(h => h.Rows)
				.Where(a => a.GuestHouseRoomId == roomId && ContainsNumber(numbers, a.RoomNumber))
				.ToList();

			// Rooms still owed (held on this record but without an exact number yet).
			var unallocatedCommitted = holdings.Sum(h =>
				Math.Max(0, h.ReservedOn(roomId)
					- h.Rows.Count(a => a.GuestHouseRoomId == roomId && ContainsNumber(numbers, a.RoomNumber))));

			var occupiedExact = exactRows
				.Select(a => a.RoomNumber)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			var remaining = numbers
				.Where(n => !ContainsNumber(occupiedExact, n))
				.ToList();
			var countBlocked = Math.Min(Math.Max(0, unallocatedCommitted), remaining.Count);

			var occupied = occupiedExact
				.Union(remaining.Take(countBlocked), StringComparer.OrdinalIgnoreCase)
				.ToList();
			var free = remaining.Skip(countBlocked).ToList();

			return new FreeRoomsResult(free, occupied);
		}
	}
}
