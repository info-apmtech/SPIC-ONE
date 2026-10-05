using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SpicAPI.Controllers;
using SPIC.Core.Entities;

namespace SpicAPI.Services
{
    // Shared, single source of truth for turning a generated GuestHouseBill into the
    // BillViewDto used by the Tax Invoice PDF (and the View Bill page). Both the Front
    // Office / Admin download and the customer-side Download Invoice go through this same
    // mapping and the same GuestHouseInvoicePdfBuilder, so the PDF is always byte-identical.
    //
    // Values are only ever read from the persisted bill (no GST / billing recalculation).
    internal static class GuestHouseBillPdfHelper
    {
        public static BillViewDto ToBillViewDto(GuestHouseBill bill, string? companyName = null, string? gstNumber = null)
        {
            var lines = bill.LineItems
                .OrderBy(i => i.Id)
                .Select(i => new BillViewLineItemDto
                {
                    Description = i.Description,
                    Quantity = i.Quantity,
                    Rate = i.Rate,
                    Amount = i.Amount,
                    CgstPercent = i.CgstPercent,
                    SgstPercent = i.SgstPercent,
                    CgstAmount = i.CgstAmount,
                    SgstAmount = i.SgstAmount,
                    LineTotal = i.LineTotal
                }).ToList();

            return new BillViewDto
            {
                BillId = bill.Id,
                BillNumber = bill.BillNumber ?? $"BILL-{bill.Id:000000}",
                BookingReference = bill.BookingReference ?? $"BK{bill.GuestHouseBookingId}",
                BookingId = bill.GuestHouseBookingId,
                BillDate = bill.BillDate,
                GuestName = bill.GuestName,
                CompanyName = companyName,
                GstNumber = gstNumber,
                Address = bill.Address,
                Email = bill.Email,
                PhoneNumber = bill.PhoneNumber,
                GuestHouseName = bill.GuestHouseName,
                RoomNumber = bill.RoomNumber,
                RoomType = bill.RoomType,
                CheckInAt = bill.CheckInAt,
                CheckOutAt = bill.CheckOutAt,
                NumberOfNights = bill.NumberOfNights,
                NumberOfRooms = bill.NumberOfRooms,
                NumberOfPersons = bill.NumberOfPersons,
                ExtraBeds = bill.ExtraBeds,
                LineItems = lines,
                Subtotal = bill.Subtotal,
                CgstAmount = bill.CgstAmount,
                SgstAmount = bill.SgstAmount,
                TotalAmount = bill.TotalAmount,
                Discount = bill.Discount,
                AdvancePayment = bill.AdvancePayment,
                RoundOff = bill.RoundOff,
                BalanceAmount = bill.BalanceAmount,
                Remarks = bill.Remarks,
                PaymentStatus = (int)bill.PaymentStatus,
                PaymentMethod = (int)bill.PaymentMethod
            };
        }

        // The guest's Company Name (captured on the booking's guest record) and GST No
        // (looked up from the Dealer master by the Employee/Dealer Code entered for the
        // stay, when it resolves to a registered dealer) shown on the Tax Invoice. Neither
        // is persisted on GuestHouseBill, so it is resolved live from the still-intact
        // booking/guest/dealer data every time the bill is viewed or printed.
        public static async Task<(string? CompanyName, string? GstNumber)> ResolveGuestCompanyAndGstAsync(
            AppDbContext db, int guestHouseBookingId)
        {
            var guest = await db.GuestHouseBookingGuests
                .AsNoTracking()
                .Where(g => g.GuestHouseBookingId == guestHouseBookingId)
                .FirstOrDefaultAsync();
            if (guest == null)
                return (null, null);

            string? gstNumber = null;
            if (!string.IsNullOrWhiteSpace(guest.EmployeeOrDealerCode))
            {
                gstNumber = await db.DealerRegistrations
                    .AsNoTracking()
                    .Where(d => d.DealerCode == guest.EmployeeOrDealerCode)
                    .Select(d => d.GSTNumber)
                    .FirstOrDefaultAsync();
            }

            return (guest.CompanyName, gstNumber);
        }
    }
}
