using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SPIC.Core.Entities;
using SpicAPI.Controllers;

namespace SpicAPI.Services
{
    internal static class GuestHouseInvoicePdfBuilder
    {
        static GuestHouseInvoicePdfBuilder()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static byte[] Build(BillViewDto bill) =>
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                    page.Header().Element(e => ComposeHeader(e, bill));
                    page.Content().Element(e => ComposeContent(e, bill));
                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf();

        private static void ComposeHeader(IContainer header, BillViewDto bill)
        {
            header.BorderBottom(1).BorderColor("#D8D8D8").PaddingBottom(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("SPIC").FontSize(20).Bold().FontColor("#0B4A82");
                    col.Item().Text(bill.GuestHouseName).FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().Text("Guest House Invoice / Bill").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
                row.AutoItem().Column(col =>
                {
                    col.Item().AlignRight().Text(bill.BillNumber).FontSize(13).Bold().FontColor("#0B4A82");
                    col.Item().AlignRight().PaddingTop(2).Text($"Bill Date: {bill.BillDate:dd MMM yyyy hh:mm tt}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().AlignRight().PaddingTop(2).Text($"Booking: {bill.BookingReference}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().AlignRight().PaddingTop(2).Element(e => PaymentChip(e, bill.PaymentStatus));
                });
            });
        }

        private static IContainer PaymentChip(IContainer c, int status)
        {
            var (bg, fg, label) = status switch
            {
                (int)GuestHousePaymentStatus.Paid => ("#DCFCE7", "#166534", "Paid"),
                _ => ("#FEF3C7", "#92400E", "Pending")
            };
            c.Background(bg).PaddingHorizontal(8).PaddingVertical(3)
                .Text(label).FontSize(9).SemiBold().FontColor(fg);
            return c;
        }

        private static void ComposeFooter(IContainer footer)
        {
            footer.PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Text($"Generated on {DateTime.Now:dd MMM yyyy hh:mm tt}")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                row.AutoItem().Text(t =>
                {
                    t.Span("Page ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    t.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1);
                    t.Span(" of ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    t.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        }

        private static void ComposeContent(IContainer content, BillViewDto bill)
        {
            content.Column(col =>
            {
                col.Spacing(12);

                col.Item().Element(e => Section(e, "Billed To"));
                col.Item().Element(e => GuestBlock(e, bill));

                col.Item().PaddingTop(4).Element(e => Section(e, "Stay Details"));
                col.Item().Element(e => StayBlock(e, bill));

                col.Item().PaddingTop(4).Element(e => Section(e, "Charges / Bill Items"));
                col.Item().Element(e => BillItemsTable(e, bill));

                col.Item().PaddingTop(4).Element(e => Section(e, "Amount Summary"));
                col.Item().Element(e => SummaryBlock(e, bill));

                if (!string.IsNullOrWhiteSpace(bill.Remarks))
                {
                    col.Item().PaddingTop(4).Element(e => Section(e, "Remarks"));
                    col.Item().Text(bill.Remarks).FontSize(9).LineHeight(1.4f);
                }
            });
        }

        private static void GuestBlock(IContainer c, BillViewDto bill)
        {
            c.Border(0.5f).BorderColor("#E5E7EB").Padding(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Spacing(3);
                    col.Item().Text(string.IsNullOrWhiteSpace(bill.GuestName) ? "-" : bill.GuestName).FontSize(11).Bold().FontColor("#0B4A82");
                    if (!string.IsNullOrWhiteSpace(bill.Address)) col.Item().Text(bill.Address).FontSize(9);
                    if (!string.IsNullOrWhiteSpace(bill.PhoneNumber)) col.Item().Text($"Phone: {bill.PhoneNumber}").FontSize(9);
                    if (!string.IsNullOrWhiteSpace(bill.Email)) col.Item().Text($"Email: {bill.Email}").FontSize(9);
                });
                row.AutoItem().Column(col =>
                {
                    col.Spacing(3);
                    col.Item().AlignRight().Text(bill.GuestHouseName).FontSize(10).Bold();
                    if (!string.IsNullOrWhiteSpace(bill.RoomNumber)) col.Item().AlignRight().Text($"Room: {bill.RoomNumber}").FontSize(9);
                });
            });
        }

        private static void StayBlock(IContainer c, BillViewDto bill)
        {
            c.Row(row =>
            {
                row.RelativeItem().Element(e => Kv(e, "Room Type", bill.RoomType));
                row.RelativeItem().Element(e => Kv(e, "No. of Rooms", bill.NumberOfRooms?.ToString()));
                row.RelativeItem().Element(e => Kv(e, "Check-In", bill.CheckInAt?.ToString("dd MMM yyyy hh:mm tt")));
                row.RelativeItem().Element(e => Kv(e, "Check-Out", bill.CheckOutAt?.ToString("dd MMM yyyy hh:mm tt")));
                row.RelativeItem().Element(e => Kv(e, "Nights", bill.NumberOfNights?.ToString()));
                row.RelativeItem().Element(e => Kv(e, "Guests", bill.NumberOfPersons?.ToString()));
            });
        }

        private static void Kv(IContainer c, string label, string value)
        {
            c.Column(col =>
            {
                col.Spacing(2);
                col.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                col.Item().Text(string.IsNullOrWhiteSpace(value) ? "-" : value).FontSize(9).SemiBold();
            });
        }

        private static void BillItemsTable(IContainer c, BillViewDto bill)
        {
            c.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.ConstantColumn(50);
                    columns.ConstantColumn(70);
                    columns.ConstantColumn(70);
                    columns.ConstantColumn(60);
                    columns.ConstantColumn(60);
                    columns.ConstantColumn(80);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeadCell).Text("Description").FontColor(Colors.White).FontSize(9).SemiBold();
                    header.Cell().Element(HeadCell).AlignCenter().Text("Qty").FontColor(Colors.White).FontSize(9).SemiBold();
                    header.Cell().Element(HeadCell).AlignRight().Text("Rate").FontColor(Colors.White).FontSize(9).SemiBold();
                    header.Cell().Element(HeadCell).AlignRight().Text("CGST").FontColor(Colors.White).FontSize(9).SemiBold();
                    header.Cell().Element(HeadCell).AlignRight().Text("SGST").FontColor(Colors.White).FontSize(9).SemiBold();
                    header.Cell().Element(HeadCell).AlignRight().Text("Amount").FontColor(Colors.White).FontSize(9).SemiBold();
                    header.Cell().Element(HeadCell).AlignRight().Text("Line Total").FontColor(Colors.White).FontSize(9).SemiBold();
                });

                foreach (var line in bill.LineItems)
                {
                    table.Cell().Element(BodyCell).Text(line.Description ?? "-");
                    table.Cell().Element(BodyCell).AlignCenter().Text(line.Quantity.ToString("0.##"));
                    table.Cell().Element(BodyCell).AlignRight().Text($"₹{line.Rate:0.00}");
                    table.Cell().Element(BodyCell).AlignRight().Text($"{line.CgstPercent:0.##}%");
                    table.Cell().Element(BodyCell).AlignRight().Text($"{line.SgstPercent:0.##}%");
                    table.Cell().Element(BodyCell).AlignRight().Text($"₹{line.Amount:0.00}");
                    table.Cell().Element(BodyCell).AlignRight().Text($"₹{line.LineTotal:0.00}");
                }
            });
        }

        private static void SummaryBlock(IContainer c, BillViewDto bill)
        {
            c.Row(row =>
            {
                row.RelativeItem();
                row.ConstantItem(260).Column(col =>
                {
                    col.Spacing(4);

                    col.Item().Element(e => TotalsRow(e, "Subtotal", bill.Subtotal));
                    col.Item().Element(e => TotalsRow(e, "CGST", bill.CgstAmount));
                    col.Item().Element(e => TotalsRow(e, "SGST", bill.SgstAmount));
                    col.Item().Element(e => TotalsRow(e, "Grand Total", bill.TotalAmount, true));
                    if (bill.Discount != 0)
                        col.Item().Element(e => TotalsRow(e, "Discount", -bill.Discount));
                    if (bill.AdvancePayment != 0)
                        col.Item().Element(e => TotalsRow(e, "Advance Payment", -bill.AdvancePayment));
                    if (bill.RoundOff != 0)
                        col.Item().Element(e => TotalsRow(e, "Round Off", bill.RoundOff));
                    col.Item().Element(e => TotalsRow(e, "Balance Payable", bill.BalanceAmount, true, "#0B4A82"));
                });
            });
        }

        private static IContainer TotalsRow(IContainer c, string label, decimal amount, bool bold = false, string color = null)
        {
            c.BorderBottom(0.5f).BorderColor("#E5E7EB")
                .Row(row =>
                {
                    row.RelativeItem().Text(label).FontSize(9).SemiBold();
                    row.AutoItem().AlignRight().Text($"₹{amount:0.00}").FontSize(9)
                        .SemiBold()
                        .FontColor(color ?? (bold ? "#0B4A82" : Colors.Grey.Darken3));
                });
            return c;
        }

        private static IContainer Section(IContainer c, string title)
        {
            c.PaddingTop(4).PaddingBottom(5).BorderBottom(1).BorderColor("#D8D8D8")
                .Text(title).FontSize(10).Bold().FontColor("#0B4A82");
            return c;
        }

        private static IContainer HeadCell(IContainer c) =>
            c.Background("#0B4A82").PaddingVertical(4).PaddingHorizontal(5);

        private static IContainer BodyCell(IContainer c) =>
            c.BorderBottom(0.5f).BorderColor("#E5E7EB").PaddingVertical(3).PaddingHorizontal(5);
    }
}
