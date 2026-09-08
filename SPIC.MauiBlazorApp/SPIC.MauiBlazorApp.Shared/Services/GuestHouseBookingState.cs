using Microsoft.AspNetCore.Components;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    /// <summary>
    /// Scoped in-memory state that carries the ongoing Guest House booking between the
    /// GuestDetails -> Preview -> Payment -> Submit steps of the flow.
    /// </summary>
    public class GuestHouseBookingState
    {
        private bool _initialized;

        public int GuestHouseId { get; set; }
        public int RoomTypeId { get; set; }
        public string? GuestHouseName { get; set; }
        public string? RoomType { get; set; }
        public string? RoomNumber { get; set; }
        public DateTime? CheckInDate { get; set; }
        public TimeSpan? CheckInTime { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public int NumberOfNights { get; set; }
        public int NumberOfRooms { get; set; } = 1;
        public int ExtraBeds { get; set; }
        public decimal RoomPricePerNight { get; set; }
        public decimal ExtraCotPrice { get; set; }
        public decimal RoomCost { get; set; }
        public decimal ExtraBedCost { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }

        // Guest form
        public string? EmployeeOrDealerCode { get; set; }
        public string? GuestName { get; set; }
        public string? CompanyName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? AadhaarOrPassport { get; set; }
        public string? Nationality { get; set; }
        public int? NumberOfPersons { get; set; }
        public int? NumberOfAdults { get; set; }
        public int? NumberOfChildren { get; set; }
        public string? Address { get; set; }

        public bool IsInitialized => _initialized;

        public void Clear() => _initialized = false;
    }
}
