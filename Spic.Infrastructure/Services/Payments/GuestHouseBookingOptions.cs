namespace Spic.Infrastructure.Services.Payments;

/// <summary>
/// Bound from the "GuestHouseBooking" configuration section. Controls how long a
/// PendingPayment booking (created the moment the customer reaches the Payment page,
/// before Razorpay Checkout even opens) continues to hold its room slot in the
/// availability calculation.
///
/// Shared by GuestHouseBookingController (room availability + payment verification)
/// and GuestHouseFrontOfficeController (room occupancy / free-room queries) so both
/// always agree on which PendingPayment bookings still count. See
/// GuestHouseBookingController.GetCommittedRoomsByRoomAsync for how it is applied.
/// </summary>
public class GuestHouseBookingOptions
{
    public const string SectionName = "GuestHouseBooking";

    /// <summary>
    /// How long a PendingPayment booking counts as holding its room, measured from the
    /// booking's own CreatedAt. Confirmed/CheckedIn bookings are never subject to this -
    /// they hold inventory unconditionally, exactly as before.
    /// </summary>
    public int PendingPaymentHoldMinutes { get; set; } = 15;
}
