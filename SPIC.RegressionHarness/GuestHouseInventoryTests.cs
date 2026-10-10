using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using SpicAPI.Services;

namespace SPIC.RegressionHarness;

public static class GuestHouseInventoryTests
{
    private static AppDbContext CreateMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    public static void Run()
    {
        Console.WriteLine("GuestHouseInventoryTests");
        Console.WriteLine("------------------------");

        TestSingleRecordBooking();
        TestMultiRecordBooking();
        TestInsufficientAvailability();
        TestOverlappingDates();
        TestPaymentSuccessAndFailure();
        TestExpiredPaymentHolds();
        TestCancellationApproval();
        TestCheckInValidation();
        TestCheckOutSnapshots();
        TestConcurrentBookingAndSerialization();
    }

    private static void TestSingleRecordBooking()
    {
        using var db = CreateMemoryDb();
        var checkIn = new DateTime(2026, 11, 1);
        var checkOut = new DateTime(2026, 11, 3);
        var cutoff = DateTime.Now.AddMinutes(-30);

        var room = new GuestHouseRoom
        {
            Id = 1,
            GuestHouseId = 10,
            RoomType = "AC Deluxe",
            RoomNumber = "101",
            PricePerNight = 2000,
            AvailableQuantity = 5,
            IsActive = true
        };
        db.GuestHouseRooms.Add(room);

        // A single-record booking of 2 rooms
        var booking = new GuestHouseBooking
        {
            Id = 101,
            GuestHouseId = 10,
            GuestHouseRoomId = 1,
            NumberOfRooms = 2,
            BookingStatus = GuestHouseBookingStatus.Confirmed,
            PaymentStatus = GuestHousePaymentStatus.Paid,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            CreatedAt = DateTime.Now
        };
        db.GuestHouseBookings.Add(booking);
        db.SaveChanges();

        // 1. IsMultiRecord is false when there are no allocation rows
        Check.That("single-record booking is not multi-record",
            !GuestHouseRoomInventory.IsMultiRecordBooking(booking.GuestHouseRoomId, booking.RoomAllocations));

        // 2. ReservedOnRecord returns NumberOfRooms on the primary room
        Check.Equal("single-record holds full quantity on primary record", 2,
            GuestHouseRoomInventory.ReservedOnRecord(booking.GuestHouseRoomId, booking.NumberOfRooms, booking.RoomAllocations, 1));

        // 3. ReservedOnRecord returns 0 on any other record
        Check.Equal("single-record holds 0 on other record", 0,
            GuestHouseRoomInventory.ReservedOnRecord(booking.GuestHouseRoomId, booking.NumberOfRooms, booking.RoomAllocations, 2));

        // 4. Committed count from db is exactly 2
        var committed = GuestHouseRoomInventory.GetCommittedRoomsByRoomAsync(db, new[] { 1 }, checkIn, checkOut, cutoff).Result;
        Check.Equal("committed count is 2 for single-record booking", 2, committed.GetValueOrDefault(1));

        // 5. Free physical rooms excludes the deterministic block (first 2 rooms 101 and 102 blocked, 103..105 free)
        var freeResult = GuestHouseRoomInventory.GetFreePhysicalRoomsAsync(db, 1, checkIn, checkOut, cutoff).Result;
        Check.Equal("free physical count is 3 out of 5", 3, freeResult.Free.Count);
        Check.That("101 is occupied", freeResult.Occupied.Contains("101"));
        Check.That("102 is occupied", freeResult.Occupied.Contains("102"));
        Check.That("103 is free", freeResult.Free.Contains("103"));
    }

    private static void TestMultiRecordBooking()
    {
        using var db = CreateMemoryDb();
        var checkIn = new DateTime(2026, 11, 1);
        var checkOut = new DateTime(2026, 11, 3);
        var cutoff = DateTime.Now.AddMinutes(-30);

        var roomA = new GuestHouseRoom
        {
            Id = 1,
            GuestHouseId = 10,
            RoomTypeId = 5,
            RoomType = "Deluxe",
            RoomNumber = "101",
            PricePerNight = 2500,
            ExtraCotPrice = 500,
            AvailableQuantity = 1,
            IsActive = true
        };
        var roomB = new GuestHouseRoom
        {
            Id = 2,
            GuestHouseId = 10,
            RoomTypeId = 5,
            RoomType = "Deluxe",
            RoomNumber = "102",
            PricePerNight = 2500,
            ExtraCotPrice = 500,
            AvailableQuantity = 1,
            IsActive = true
        };
        var roomCIncompatiblePrice = new GuestHouseRoom
        {
            Id = 3,
            GuestHouseId = 10,
            RoomTypeId = 5,
            RoomType = "Deluxe",
            RoomNumber = "103",
            PricePerNight = 3000,
            ExtraCotPrice = 500,
            AvailableQuantity = 1,
            IsActive = true
        };
        db.GuestHouseRooms.AddRange(roomA, roomB, roomCIncompatiblePrice);

        // Check compatibility
        Check.That("compatible sibling records match", GuestHouseRoomInventory.AreCompatible(roomA, roomB));
        Check.That("incompatible price records do not match", !GuestHouseRoomInventory.AreCompatible(roomA, roomCIncompatiblePrice));

        // Multi-record booking across Room A and Room B
        var multiBooking = new GuestHouseBooking
        {
            Id = 201,
            GuestHouseId = 10,
            GuestHouseRoomId = 1, // primary is room A
            NumberOfRooms = 2,
            BookingStatus = GuestHouseBookingStatus.Confirmed,
            PaymentStatus = GuestHousePaymentStatus.Paid,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            CreatedAt = DateTime.Now
        };
        multiBooking.RoomAllocations.Add(new GuestHouseRoomAllocation
        {
            GuestHouseBookingId = 201,
            GuestHouseId = 10,
            GuestHouseRoomId = 1,
            RoomNumber = "101",
            CheckInDate = checkIn,
            CheckOutDate = checkOut
        });
        multiBooking.RoomAllocations.Add(new GuestHouseRoomAllocation
        {
            GuestHouseBookingId = 201,
            GuestHouseId = 10,
            GuestHouseRoomId = 2,
            RoomNumber = "102",
            CheckInDate = checkIn,
            CheckOutDate = checkOut
        });
        db.GuestHouseBookings.Add(multiBooking);
        db.SaveChanges();

        // 1. IsMultiRecordBooking is true
        Check.That("multi-record booking recognized by rows pointing to different records",
            GuestHouseRoomInventory.IsMultiRecordBooking(multiBooking.GuestHouseRoomId, multiBooking.RoomAllocations));

        // 2. Each record holds exactly 1 room
        Check.Equal("multi-record holds 1 on room A", 1,
            GuestHouseRoomInventory.ReservedOnRecord(multiBooking.GuestHouseRoomId, multiBooking.NumberOfRooms, multiBooking.RoomAllocations, 1));
        Check.Equal("multi-record holds 1 on room B", 1,
            GuestHouseRoomInventory.ReservedOnRecord(multiBooking.GuestHouseRoomId, multiBooking.NumberOfRooms, multiBooking.RoomAllocations, 2));

        // 3. Committed rooms correctly distributes 1 to Room A and 1 to Room B
        var committed = GuestHouseRoomInventory.GetCommittedRoomsByRoomAsync(db, new[] { 1, 2 }, checkIn, checkOut, cutoff).Result;
        Check.Equal("Room A committed count is 1", 1, committed.GetValueOrDefault(1));
        Check.Equal("Room B committed count is 1", 1, committed.GetValueOrDefault(2));

        // 4. Physical rooms are both occupied
        var freeA = GuestHouseRoomInventory.GetFreePhysicalRoomsAsync(db, 1, checkIn, checkOut, cutoff).Result;
        var freeB = GuestHouseRoomInventory.GetFreePhysicalRoomsAsync(db, 2, checkIn, checkOut, cutoff).Result;
        Check.Equal("Room A has 0 free", 0, freeA.Free.Count);
        Check.Equal("Room B has 0 free", 0, freeB.Free.Count);
        Check.That("101 is occupied on A", freeA.Occupied.Contains("101"));
        Check.That("102 is occupied on B", freeB.Occupied.Contains("102"));
    }

    private static void TestInsufficientAvailability()
    {
        using var db = CreateMemoryDb();
        var checkIn = new DateTime(2026, 11, 1);
        var checkOut = new DateTime(2026, 11, 3);
        var cutoff = DateTime.Now.AddMinutes(-30);

        var room = new GuestHouseRoom
        {
            Id = 1,
            GuestHouseId = 10,
            RoomType = "Standard",
            RoomNumber = "201",
            PricePerNight = 1000,
            AvailableQuantity = 1,
            IsActive = true
        };
        db.GuestHouseRooms.Add(room);

        // Existing booking takes 1 room
        db.GuestHouseBookings.Add(new GuestHouseBooking
        {
            Id = 301,
            GuestHouseId = 10,
            GuestHouseRoomId = 1,
            NumberOfRooms = 1,
            BookingStatus = GuestHouseBookingStatus.Confirmed,
            PaymentStatus = GuestHousePaymentStatus.Paid,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            CreatedAt = DateTime.Now
        });
        db.SaveChanges();

        var committed = GuestHouseRoomInventory.GetCommittedRoomsByRoomAsync(db, new[] { 1 }, checkIn, checkOut, cutoff).Result;
        var remainingCapacity = Math.Max(0, room.AvailableQuantity - committed.GetValueOrDefault(1));
        Check.Equal("remaining capacity is 0 when all rooms are committed", 0, remainingCapacity);
    }

    private static void TestOverlappingDates()
    {
        using var db = CreateMemoryDb();
        var cutoff = DateTime.Now.AddMinutes(-30);

        // Stay 1: Nov 1 to Nov 5
        var stay1In = new DateTime(2026, 11, 1);
        var stay1Out = new DateTime(2026, 11, 5);

        db.GuestHouseRooms.Add(new GuestHouseRoom
        {
            Id = 1,
            GuestHouseId = 10,
            RoomType = "Standard",
            RoomNumber = "301",
            PricePerNight = 1000,
            AvailableQuantity = 1,
            IsActive = true
        });

        db.GuestHouseBookings.Add(new GuestHouseBooking
        {
            Id = 401,
            GuestHouseId = 10,
            GuestHouseRoomId = 1,
            NumberOfRooms = 1,
            BookingStatus = GuestHouseBookingStatus.Confirmed,
            CheckInDate = stay1In,
            CheckOutDate = stay1Out,
            CreatedAt = DateTime.Now
        });
        db.SaveChanges();

        // Check 1: Mid-stay overlap (Nov 2 to Nov 4) -> should be committed
        var midCommitted = GuestHouseRoomInventory.GetCommittedRoomsByRoomAsync(
            db, new[] { 1 }, new DateTime(2026, 11, 2), new DateTime(2026, 11, 4), cutoff).Result;
        Check.Equal("mid-stay overlaps and commits room", 1, midCommitted.GetValueOrDefault(1));

        // Check 2: Adjacent stay starting on checkout day (Nov 5 to Nov 8) -> does NOT overlap!
        var adjacentCommitted = GuestHouseRoomInventory.GetCommittedRoomsByRoomAsync(
            db, new[] { 1 }, new DateTime(2026, 11, 5), new DateTime(2026, 11, 8), cutoff).Result;
        Check.Equal("checkout day checkin does not overlap", 0, adjacentCommitted.GetValueOrDefault(1));

        // Check 3: Adjacent stay checking out on checkin day (Oct 28 to Nov 1) -> does NOT overlap!
        var priorAdjacentCommitted = GuestHouseRoomInventory.GetCommittedRoomsByRoomAsync(
            db, new[] { 1 }, new DateTime(2026, 10, 28), new DateTime(2026, 11, 1), cutoff).Result;
        Check.Equal("checkin day checkout does not overlap", 0, priorAdjacentCommitted.GetValueOrDefault(1));
    }

    private static void TestPaymentSuccessAndFailure()
    {
        using var db = CreateMemoryDb();
        var checkIn = new DateTime(2026, 11, 1);
        var checkOut = new DateTime(2026, 11, 3);
        var cutoff = DateTime.Now.AddMinutes(-30);

        var room = new GuestHouseRoom
        {
            Id = 1,
            GuestHouseId = 10,
            RoomType = "Standard",
            RoomNumber = "401",
            PricePerNight = 1000,
            AvailableQuantity = 2,
            IsActive = true
        };
        db.GuestHouseRooms.Add(room);

        // Booking 1: Failed payment -> moves to Cancelled, allocations deleted
        var failedBooking = new GuestHouseBooking
        {
            Id = 501,
            GuestHouseId = 10,
            GuestHouseRoomId = 1,
            NumberOfRooms = 1,
            BookingStatus = GuestHouseBookingStatus.Cancelled,
            PaymentStatus = GuestHousePaymentStatus.Failed,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            CreatedAt = DateTime.Now
        };
        db.GuestHouseBookings.Add(failedBooking);
        db.SaveChanges();

        var committedAfterFail = GuestHouseRoomInventory.GetCommittedRoomsByRoomAsync(db, new[] { 1 }, checkIn, checkOut, cutoff).Result;
        Check.Equal("failed payment booking does not hold room", 0, committedAfterFail.GetValueOrDefault(1));

        // Booking 2: Confirmed paid booking -> holds room
        var paidBooking = new GuestHouseBooking
        {
            Id = 502,
            GuestHouseId = 10,
            GuestHouseRoomId = 1,
            NumberOfRooms = 1,
            BookingStatus = GuestHouseBookingStatus.Confirmed,
            PaymentStatus = GuestHousePaymentStatus.Paid,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            CreatedAt = DateTime.Now
        };
        db.GuestHouseBookings.Add(paidBooking);
        db.SaveChanges();

        var committedAfterPaid = GuestHouseRoomInventory.GetCommittedRoomsByRoomAsync(db, new[] { 1 }, checkIn, checkOut, cutoff).Result;
        Check.Equal("paid booking holds room", 1, committedAfterPaid.GetValueOrDefault(1));
    }

    private static void TestExpiredPaymentHolds()
    {
        using var db = CreateMemoryDb();
        var checkIn = new DateTime(2026, 11, 1);
        var checkOut = new DateTime(2026, 11, 3);
        var cutoff = DateTime.Now.AddMinutes(-15); // 15 min hold window

        var room = new GuestHouseRoom
        {
            Id = 1,
            GuestHouseId = 10,
            RoomType = "Standard",
            RoomNumber = "501",
            PricePerNight = 1000,
            AvailableQuantity = 1,
            IsActive = true
        };
        db.GuestHouseRooms.Add(room);

        // Booking A: Created 30 minutes ago (expired PendingPayment)
        var expiredBooking = new GuestHouseBooking
        {
            Id = 601,
            GuestHouseId = 10,
            GuestHouseRoomId = 1,
            NumberOfRooms = 1,
            BookingStatus = GuestHouseBookingStatus.PendingPayment,
            PaymentStatus = GuestHousePaymentStatus.Pending,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            CreatedAt = DateTime.Now.AddMinutes(-30)
        };
        db.GuestHouseBookings.Add(expiredBooking);
        db.SaveChanges();

        // Expired booking should NOT hold inventory
        var committedExpired = GuestHouseRoomInventory.GetCommittedRoomsByRoomAsync(db, new[] { 1 }, checkIn, checkOut, cutoff).Result;
        Check.Equal("expired PendingPayment does not hold room", 0, committedExpired.GetValueOrDefault(1));

        // Booking B: Created 5 minutes ago (active PendingPayment within cutoff)
        var activeHoldBooking = new GuestHouseBooking
        {
            Id = 602,
            GuestHouseId = 10,
            GuestHouseRoomId = 1,
            NumberOfRooms = 1,
            BookingStatus = GuestHouseBookingStatus.PendingPayment,
            PaymentStatus = GuestHousePaymentStatus.Pending,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            CreatedAt = DateTime.Now.AddMinutes(-5)
        };
        db.GuestHouseBookings.Add(activeHoldBooking);
        db.SaveChanges();

        // Active hold booking DOES hold inventory
        var committedActive = GuestHouseRoomInventory.GetCommittedRoomsByRoomAsync(db, new[] { 1 }, checkIn, checkOut, cutoff).Result;
        Check.Equal("active PendingPayment hold holds room", 1, committedActive.GetValueOrDefault(1));
    }

    private static void TestCancellationApproval()
    {
        using var db = CreateMemoryDb();
        var checkIn = new DateTime(2026, 11, 1);
        var checkOut = new DateTime(2026, 11, 3);
        var cutoff = DateTime.Now.AddMinutes(-30);

        var room = new GuestHouseRoom
        {
            Id = 1,
            GuestHouseId = 10,
            RoomType = "Standard",
            RoomNumber = "601",
            PricePerNight = 1000,
            AvailableQuantity = 1,
            IsActive = true
        };
        db.GuestHouseRooms.Add(room);

        var booking = new GuestHouseBooking
        {
            Id = 701,
            GuestHouseId = 10,
            GuestHouseRoomId = 1,
            NumberOfRooms = 1,
            BookingStatus = GuestHouseBookingStatus.Confirmed,
            PaymentStatus = GuestHousePaymentStatus.Paid,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            CreatedAt = DateTime.Now
        };
        booking.RoomAllocations.Add(new GuestHouseRoomAllocation
        {
            GuestHouseBookingId = 701,
            GuestHouseId = 10,
            GuestHouseRoomId = 1,
            RoomNumber = "601",
            CheckInDate = checkIn,
            CheckOutDate = checkOut
        });
        db.GuestHouseBookings.Add(booking);
        db.SaveChanges();

        // Holds 1
        var committedBefore = GuestHouseRoomInventory.GetCommittedRoomsByRoomAsync(db, new[] { 1 }, checkIn, checkOut, cutoff).Result;
        Check.Equal("room held before cancellation", 1, committedBefore.GetValueOrDefault(1));

        // Admin approves cancellation: status becomes Cancelled, allocation rows removed
        booking.BookingStatus = GuestHouseBookingStatus.Cancelled;
        db.GuestHouseRoomAllocations.RemoveRange(booking.RoomAllocations);
        db.SaveChanges();

        var committedAfter = GuestHouseRoomInventory.GetCommittedRoomsByRoomAsync(db, new[] { 1 }, checkIn, checkOut, cutoff).Result;
        Check.Equal("room released immediately after cancellation approval", 0, committedAfter.GetValueOrDefault(1));
    }

    private static void TestCheckInValidation()
    {
        using var db = CreateMemoryDb();
        var checkIn = new DateTime(2026, 11, 1);
        var checkOut = new DateTime(2026, 11, 3);
        var cutoff = DateTime.Now.AddMinutes(-30);

        var roomA = new GuestHouseRoom
        {
            Id = 1,
            GuestHouseId = 10,
            RoomType = "Deluxe",
            RoomNumber = "701",
            AvailableQuantity = 2,
            IsActive = true
        };
        var roomB = new GuestHouseRoom
        {
            Id = 2,
            GuestHouseId = 10,
            RoomType = "Deluxe",
            RoomNumber = "702",
            AvailableQuantity = 1,
            IsActive = true
        };
        db.GuestHouseRooms.AddRange(roomA, roomB);

        // Derived physical room numbers
        var numbersA = GuestHouseRoomInventory.EnumeratePhysicalRoomNumbers(roomA, roomA.AvailableQuantity);
        Check.Equal("Room A derives 2 physical numbers", 2, numbersA.Count);
        Check.That("Room A has 701", numbersA.Contains("701"));
        Check.That("Room A has 702", numbersA.Contains("702"));

        var numbersB = GuestHouseRoomInventory.EnumeratePhysicalRoomNumbers(roomB, roomB.AvailableQuantity);
        Check.Equal("Room B derives 1 physical number", 1, numbersB.Count);
        Check.That("Room B has 702", numbersB.Contains("702"));

        // Duplicate room number detection: 702 exists in both Room A and Room B
        bool collides = numbersA.Any(na => numbersB.Contains(na));
        Check.That("number collision between records is correctly detected", collides);
    }

    private static void TestCheckOutSnapshots()
    {
        using var db = CreateMemoryDb();
        var checkIn = new DateTime(2026, 11, 1);
        var checkOut = new DateTime(2026, 11, 3);
        var cutoff = DateTime.Now.AddMinutes(-30);

        var room = new GuestHouseRoom
        {
            Id = 1,
            GuestHouseId = 10,
            RoomType = "Deluxe",
            RoomNumber = "801",
            AvailableQuantity = 2,
            IsActive = true
        };
        db.GuestHouseRooms.Add(room);

        var booking = new GuestHouseBooking
        {
            Id = 801,
            GuestHouseId = 10,
            GuestHouseRoomId = 1,
            NumberOfRooms = 2,
            BookingStatus = GuestHouseBookingStatus.CheckedIn,
            PaymentStatus = GuestHousePaymentStatus.Paid,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            CreatedAt = DateTime.Now
        };
        var alloc1 = new GuestHouseRoomAllocation
        {
            GuestHouseBookingId = 801,
            GuestHouseId = 10,
            GuestHouseRoomId = 1,
            RoomNumber = "801",
            CheckInDate = checkIn,
            CheckOutDate = checkOut
        };
        var alloc2 = new GuestHouseRoomAllocation
        {
            GuestHouseBookingId = 801,
            GuestHouseId = 10,
            GuestHouseRoomId = 1,
            RoomNumber = "802",
            CheckInDate = checkIn,
            CheckOutDate = checkOut
        };
        booking.RoomAllocations.Add(alloc1);
        booking.RoomAllocations.Add(alloc2);
        db.GuestHouseBookings.Add(booking);
        db.SaveChanges();

        // While checked-in, room numbers are allocated
        var allocatedNumbers = booking.RoomAllocations.Select(a => a.RoomNumber).OrderBy(n => n).ToList();
        var snapshot = string.Join(", ", allocatedNumbers);
        Check.Equal("snapshot contains assigned physical room numbers", "801, 802", snapshot);

        // At Check-Out: snapshot onto AllocatedRoomNumber, remove allocations, set Completed
        booking.AllocatedRoomNumber = snapshot;
        booking.BookingStatus = GuestHouseBookingStatus.Completed;
        db.GuestHouseRoomAllocations.RemoveRange(booking.RoomAllocations);
        db.SaveChanges();

        Check.Equal("AllocatedRoomNumber preserved after allocation deletion", "801, 802", booking.AllocatedRoomNumber);

        // Completed stay does NOT hold inventory
        var committedAfterCheckout = GuestHouseRoomInventory.GetCommittedRoomsByRoomAsync(db, new[] { 1 }, checkIn, checkOut, cutoff).Result;
        Check.Equal("completed booking releases inventory hold", 0, committedAfterCheckout.GetValueOrDefault(1));
    }

    private static void TestConcurrentBookingAndSerialization()
    {
        // 1. Serialization failure detector
        var directPostgres40001 = new Npgsql.PostgresException("could not serialize access", "ERROR", "ERROR", "40001");
        Check.That("IsSerializationFailure detects direct 40001",
            GuestHouseRoomInventory.IsSerializationFailure(directPostgres40001));

        var wrappedException = new DbUpdateException("update failed", directPostgres40001);
        Check.That("IsSerializationFailure detects wrapped DbUpdateException 40001",
            GuestHouseRoomInventory.IsSerializationFailure(wrappedException));

        var nestedException = new Exception("outer", new InvalidOperationException("mid", wrappedException));
        Check.That("IsSerializationFailure detects deeply nested 40001",
            GuestHouseRoomInventory.IsSerializationFailure(nestedException));

        var otherPostgres = new Npgsql.PostgresException("syntax error", "ERROR", "ERROR", "42601");
        Check.That("IsSerializationFailure rejects other SQLSTATE",
            !GuestHouseRoomInventory.IsSerializationFailure(otherPostgres));

        // 2. Concurrency simulation on availability
        using var db = CreateMemoryDb();
        var checkIn = new DateTime(2026, 11, 1);
        var checkOut = new DateTime(2026, 11, 3);
        var cutoff = DateTime.Now.AddMinutes(-30);

        var room = new GuestHouseRoom
        {
            Id = 1,
            GuestHouseId = 10,
            RoomType = "Standard",
            RoomNumber = "901",
            PricePerNight = 1000,
            AvailableQuantity = 1,
            IsActive = true
        };
        db.GuestHouseRooms.Add(room);
        db.SaveChanges();

        // User A commits first
        db.GuestHouseBookings.Add(new GuestHouseBooking
        {
            Id = 901,
            GuestHouseId = 10,
            GuestHouseRoomId = 1,
            NumberOfRooms = 1,
            BookingStatus = GuestHouseBookingStatus.Confirmed,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            CreatedAt = DateTime.Now
        });
        db.SaveChanges();

        // User B re-checks availability in serializable transaction
        var committed = GuestHouseRoomInventory.GetCommittedRoomsByRoomAsync(db, new[] { 1 }, checkIn, checkOut, cutoff).Result;
        var availableForUserB = Math.Max(0, room.AvailableQuantity - committed.GetValueOrDefault(1));
        Check.Equal("User B detects 0 available after User A booked", 0, availableForUserB);
    }
}
