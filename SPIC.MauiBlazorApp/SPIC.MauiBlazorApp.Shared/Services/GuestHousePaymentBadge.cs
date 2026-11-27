namespace SPIC.MauiBlazorApp.Shared.Services
{
    /// <summary>
    /// What the My Bookings / Booking Details / Front Office payment badge shows. Display only: the
    /// stored PaymentStatus (and the API field) never changes - Check-Out, billing and invoices depend
    /// on an employee's Complimentary booking being stored as Paid.
    ///
    /// An Employee Stay is identified ONLY by its payment method (the API names Complimentary
    /// "Employee Stay"), never by booking or payment status, so a Dealer's Pay After Stay booking
    /// (Pay After Stay, Pending) still reads "Pending".
    ///
    /// An Employee Stay reads "Booked" through Confirmed, CheckedIn and Completed (Check-Out is not a
    /// payment), and "Paid" only once its bill has been generated - GuestHouseBill exists for the booking
    /// (the same fact Booking Details reports as HasInvoice).
    /// </summary>
    public static class GuestHousePaymentBadge
    {
        public const string EmployeeStayMethod = "Employee Stay";
        public const string Booked = "Booked";

        public static string Text(string? paymentMethod, string? bookingStatus, string paymentStatus, bool billGenerated)
        {
            if (!billGenerated
                && string.Equals(paymentMethod?.Trim(), EmployeeStayMethod, StringComparison.OrdinalIgnoreCase)
                && IsBookedStage(bookingStatus))
                return Booked;

            return paymentStatus;
        }

        // Confirmed, CheckedIn (the API sends "CheckedIn") and Completed. Cancelled and every other
        // status keep the existing payment-status text.
        private static bool IsBookedStage(string? bookingStatus)
        {
            var status = (bookingStatus ?? string.Empty).Replace(" ", "");
            return string.Equals(status, "Confirmed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "CheckedIn", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase);
        }
    }
}
