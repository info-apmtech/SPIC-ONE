using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SPIC.Core.Entities;
using SpicAPI.Controllers;

namespace SpicAPI.Services
{
    // Renders the Guest House Tax Invoice PDF. Visually modelled on the association's
    // existing printed Tax Invoice letterhead (logo header band, bordered bill-info grid,
    // line-item table, totals block, GST breakup table, footer declaration and signatures).
    // Every guest/stay/financial value comes from the BillViewDto (built from the saved
    // GuestHouseBill + line items) passed in - only the letterhead itself (association
    // name, address, phone, GSTIN, logo, footer boilerplate) is fixed, exactly as on a
    // printed letterhead.
    internal static class GuestHouseInvoicePdfBuilder
    {
        private const string BrandNavy = "#1E3255";
        private const string InkColor = "#111111";
        private const string BorderColor = "#333333";
        private const string HeaderBandColor = "#F0F0F0";
        private const string HeadCellColor = "#EFEFEF";
        private const string AccentOrange = "#D7604A";

        private static readonly byte[] LogoBytes = LoadLogo();

        static GuestHouseInvoicePdfBuilder()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        private static byte[] LoadLogo()
        {
            var assembly = typeof(GuestHouseInvoicePdfBuilder).Assembly;
            using var stream = assembly.GetManifestResourceStream("SpicAPI.Assets.GuestHouseInvoiceLogo.png");
            if (stream == null)
                return Array.Empty<byte>();

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        public static byte[] Build(BillViewDto bill) =>
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(InkColor));

                    page.Background().Row(row =>
                    {
                        row.ConstantItem(5).Background(AccentOrange);
                        row.RelativeItem();
                    });

                    page.Header().Element(ComposeHeader);
                    page.Content().PaddingHorizontal(26).PaddingTop(10).PaddingBottom(6).Element(e => ComposeContent(e, bill));
                    page.Footer().PaddingHorizontal(26).PaddingBottom(8).Element(ComposePageFooter);
                });
            }).GeneratePdf();

        private static void ComposeHeader(IContainer header)
        {
            header.Background(HeaderBandColor).Padding(12).Row(row =>
            {
                row.ConstantItem(60).Height(60).Image(LogoBytes).FitArea();
                row.RelativeItem().PaddingLeft(10).Column(col =>
                {
                    col.Item().AlignCenter().Text("SPIC DEALERS' WELFARE ASSOCIATION").FontSize(17).Bold().FontColor(BrandNavy);
                    col.Item().AlignCenter().PaddingTop(3).Text("# 100, BAZULLAH ROAD, T.NAGAR, CHENNAI - 600017").FontSize(9.5f).FontColor(BrandNavy);
                    col.Item().AlignCenter().Text("Phone: 044-2814 1474 / 2814 2398 / 4860 3839").FontSize(9.5f).FontColor(BrandNavy);
                    col.Item().AlignCenter().Text("GSTIN : 33AAMCS2846K1Z0").FontSize(9.5f).FontColor(BrandNavy);
                });
            });
        }

        private static void ComposePageFooter(IContainer footer)
        {
            footer.Row(row =>
            {
                row.RelativeItem();
                row.AutoItem().Text(t =>
                {
                    t.Span("Page ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    t.CurrentPageNumber().FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    t.Span(" of ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    t.TotalPages().FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                });
            });
        }

        private static void ComposeContent(IContainer content, BillViewDto bill)
        {
            content.Column(col =>
            {
                col.Item().PaddingBottom(6).AlignCenter().Text("TAX INVOICE").FontSize(14).Bold().FontColor(InkColor);

                col.Item().Element(e => BillInfoTable(e, bill));

                col.Item().PaddingTop(8).Element(e => LineItemsTable(e, bill));

                col.Item().PaddingTop(8).Element(e => TotalsTable(e, bill));

                col.Item().PaddingTop(8).Element(e => GstBreakupTable(e, bill));

                col.Item().PaddingTop(16).Element(FooterDeclaration);
            });
        }

        // ---- Bill / guest information grid ----

        private static void BillInfoTable(IContainer c, BillViewDto bill)
        {
            c.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(95);
                    columns.RelativeColumn(1.3f);
                    columns.ConstantColumn(120);
                    columns.RelativeColumn(1.3f);
                });

                table.Cell().Element(InfoLabelCell).Text("Bill No");
                table.Cell().Element(InfoValueCell).Text(string.IsNullOrWhiteSpace(bill.BillNumber) ? "-" : bill.BillNumber);
                table.Cell().Element(InfoLabelCell).Text("Booking ID");
                table.Cell().Element(InfoValueCell).Text(bill.BookingId.ToString());

                table.Cell().Element(InfoLabelCell).Text("Guest Name");
                table.Cell().Element(InfoValueCell).Text(string.IsNullOrWhiteSpace(bill.GuestName) ? "-" : bill.GuestName);
                table.Cell().Element(InfoLabelCell).Text("Room No");
                table.Cell().Element(InfoValueCell).Text(string.IsNullOrWhiteSpace(bill.RoomNumber) ? "-" : bill.RoomNumber);

                table.Cell().Element(InfoLabelCell).Text("Company Name");
                table.Cell().ColumnSpan(3).Element(InfoValueCell).Text(string.IsNullOrWhiteSpace(bill.CompanyName) ? "-" : bill.CompanyName);

                table.Cell().Element(InfoLabelCell).Text("Address");
                table.Cell().ColumnSpan(3).Element(InfoValueCell).Text(string.IsNullOrWhiteSpace(bill.Address) ? "-" : bill.Address);

                table.Cell().Element(InfoLabelCell).Text("GST No");
                table.Cell().Element(InfoValueCell).Text(string.IsNullOrWhiteSpace(bill.GstNumber) ? "-" : bill.GstNumber);
                table.Cell().Element(InfoLabelCell).Text("PAX (No of Persons)");
                table.Cell().Element(InfoValueCell).Text(bill.NumberOfPersons?.ToString() ?? "-");

                table.Cell().Element(InfoLabelCell).Text("Check In");
                table.Cell().Element(InfoValueCell).Text(bill.CheckInAt?.ToString("dd-MM-yy hh:mm tt") ?? "-");
                table.Cell().Element(InfoLabelCell).Text("Check Out");
                table.Cell().Element(InfoValueCell).Text(bill.CheckOutAt?.ToString("dd-MM-yy hh:mm tt") ?? "-");
            });
        }

        // ---- Line items ----

        private static void LineItemsTable(IContainer c, BillViewDto bill)
        {
            c.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3f);
                    columns.ConstantColumn(58);
                    columns.ConstantColumn(68);
                    columns.ConstantColumn(72);
                    columns.ConstantColumn(58);
                    columns.ConstantColumn(72);
                });

                table.Header(header =>
                {
                    header.Cell().Element(LineHeadCell).AlignCenter().Text("Description\n(Room/ Food/ Other)").Bold().FontSize(8.5f);
                    header.Cell().Element(LineHeadCell).AlignCenter().Text("SAC\nCode").Bold().FontSize(8.5f);
                    header.Cell().Element(LineHeadCell).AlignCenter().Text("Days (Nos.)/\nVoucher").Bold().FontSize(8.5f);
                    header.Cell().Element(LineHeadCell).AlignCenter().Text("Charges/day\n(in Rs.)").Bold().FontSize(8.5f);
                    header.Cell().Element(LineHeadCell).AlignCenter().Text("Tax\n(in Rs.)").Bold().FontSize(8.5f);
                    header.Cell().Element(LineHeadCell).AlignCenter().Text("Total\n(in Rs.)").Bold().FontSize(8.5f);
                });

                foreach (var line in bill.LineItems)
                {
                    var sac = ResolveSacCode(line.Description);
                    var tax = line.CgstAmount + line.SgstAmount;

                    table.Cell().Element(LineBodyCell).Text(string.IsNullOrWhiteSpace(line.Description) ? "-" : line.Description);
                    table.Cell().Element(LineBodyCell).AlignCenter().Text(sac);
                    table.Cell().Element(LineBodyCell).AlignCenter().Text(line.Quantity.ToString("0.##"));
                    table.Cell().Element(LineBodyCell).AlignRight().Text(line.Rate.ToString("0.00"));
                    table.Cell().Element(LineBodyCell).AlignRight().Text(tax.ToString("0.00"));
                    table.Cell().Element(LineBodyCell).AlignRight().Text(line.LineTotal.ToString("0.00"));
                }
            });
        }

        // ---- Totals / payment ----

        private static void TotalsTable(IContainer c, BillViewDto bill)
        {
            c.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3f);
                    columns.RelativeColumn(1.4f);
                });

                table.Cell().Element(InfoLabelCell).Text("Total Amount (in Rs.)").Bold();
                table.Cell().Element(InfoValueCell).AlignRight().Text(bill.TotalAmount.ToString("0.00")).Bold();

                if (bill.Discount != 0)
                {
                    table.Cell().Element(InfoLabelCell).Text("Discount (in Rs.)");
                    table.Cell().Element(InfoValueCell).AlignRight().Text(bill.Discount.ToString("0.00"));
                }

                if (bill.AdvancePayment != 0)
                {
                    table.Cell().Element(InfoLabelCell).Text("Advance Payment (in Rs.)");
                    table.Cell().Element(InfoValueCell).AlignRight().Text(bill.AdvancePayment.ToString("0.00"));
                }

                if (bill.RoundOff != 0)
                {
                    table.Cell().Element(InfoLabelCell).Text("Round Off (in Rs.)");
                    table.Cell().Element(InfoValueCell).AlignRight().Text(bill.RoundOff.ToString("0.00"));
                }

                table.Cell().Element(InfoLabelCell).Text("Balance Amount (in Rs.)").Bold();
                table.Cell().Element(InfoValueCell).AlignRight().Text(bill.BalanceAmount.ToString("0.00")).Bold().FontColor(BrandNavy);

                table.Cell().Element(InfoLabelCell).Text("Payment Method");
                table.Cell().Element(InfoValueCell).AlignRight().Text(PaymentMethodLabel(bill.PaymentMethod));

                table.Cell().Element(InfoLabelCell).Text("Remarks");
                table.Cell().Element(InfoValueCell).AlignRight().Text(string.IsNullOrWhiteSpace(bill.Remarks) ? "-" : bill.Remarks);
            });
        }

        private static string PaymentMethodLabel(int method) => (GuestHousePaymentMethod)method switch
        {
            GuestHousePaymentMethod.PayAfterStay => "Credit",
            GuestHousePaymentMethod.Razorpay => "Razorpay",
            GuestHousePaymentMethod.UPI => "UPI",
            GuestHousePaymentMethod.Card => "Card",
            GuestHousePaymentMethod.NetBanking => "Net Banking",
            _ => method.ToString()
        };

        // ---- GST breakup ----
        // Grouped by SAC code + rate combination (not one row per line item), matching the
        // reference invoice's SAC-wise summary. Must reconcile exactly with bill.CgstAmount
        // / bill.SgstAmount - these are the same persisted per-line GST amounts from
        // Generate Bill, only re-grouped for display, never recalculated.

        private static void GstBreakupTable(IContainer c, BillViewDto bill)
        {
            var groups = bill.LineItems
                .Select(li => new
                {
                    Sac = ResolveSacCode(li.Description),
                    li.Amount,
                    li.CgstPercent,
                    li.CgstAmount,
                    li.SgstPercent,
                    li.SgstAmount
                })
                .GroupBy(x => new { x.Sac, x.CgstPercent, x.SgstPercent })
                .Select(g => new
                {
                    g.Key.Sac,
                    g.Key.CgstPercent,
                    g.Key.SgstPercent,
                    TaxableValue = g.Sum(x => x.Amount),
                    CgstAmount = g.Sum(x => x.CgstAmount),
                    SgstAmount = g.Sum(x => x.SgstAmount)
                })
                .OrderBy(x => x.Sac)
                .ThenBy(x => x.CgstPercent)
                .ToList();

            c.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(70);
                    columns.RelativeColumn(1.1f);
                    columns.ConstantColumn(52);
                    columns.RelativeColumn(0.9f);
                    columns.ConstantColumn(52);
                    columns.RelativeColumn(0.9f);
                    columns.RelativeColumn(0.9f);
                });

                table.Header(header =>
                {
                    header.Cell().RowSpan(2).Element(LineHeadCell).AlignCenter().Text("SAC\nCode").Bold().FontSize(8.5f);
                    header.Cell().RowSpan(2).Element(LineHeadCell).AlignCenter().Text("Taxable\nValue").Bold().FontSize(8.5f);
                    header.Cell().ColumnSpan(2).Element(LineHeadCell).AlignCenter().Text("CGST").Bold().FontSize(8.5f);
                    header.Cell().ColumnSpan(2).Element(LineHeadCell).AlignCenter().Text("SGST").Bold().FontSize(8.5f);
                    header.Cell().RowSpan(2).Element(LineHeadCell).AlignCenter().Text("Total\nTax").Bold().FontSize(8.5f);

                    header.Cell().Element(LineHeadCell).AlignCenter().Text("Rate (%)").FontSize(8);
                    header.Cell().Element(LineHeadCell).AlignCenter().Text("Amount").FontSize(8);
                    header.Cell().Element(LineHeadCell).AlignCenter().Text("Rate (%)").FontSize(8);
                    header.Cell().Element(LineHeadCell).AlignCenter().Text("Amount").FontSize(8);
                });

                foreach (var g in groups)
                {
                    table.Cell().Element(LineBodyCell).AlignCenter().Text(g.Sac);
                    table.Cell().Element(LineBodyCell).AlignRight().Text(g.TaxableValue.ToString("0.00"));
                    table.Cell().Element(LineBodyCell).AlignCenter().Text(g.CgstPercent.ToString("0.##"));
                    table.Cell().Element(LineBodyCell).AlignRight().Text(g.CgstAmount.ToString("0.00"));
                    table.Cell().Element(LineBodyCell).AlignCenter().Text(g.SgstPercent.ToString("0.##"));
                    table.Cell().Element(LineBodyCell).AlignRight().Text(g.SgstAmount.ToString("0.00"));
                    table.Cell().Element(LineBodyCell).AlignRight().Text((g.CgstAmount + g.SgstAmount).ToString("0.00"));
                }

                table.Cell().ColumnSpan(6).Element(LineHeadCell).AlignRight().Text("Total").Bold().FontSize(9);
                table.Cell().Element(LineHeadCell).AlignRight().Text((bill.CgstAmount + bill.SgstAmount).ToString("0.00")).Bold().FontSize(9);
            });
        }

        // SAC codes are derived from the line description rather than stored, since no SAC
        // field exists on GuestHouseBillLineItem. Standard GST SAC codes for a guest house:
        // 996311 (accommodation - room / additional bed), 996332 (food / catering),
        // 999721 (laundry). Anything else falls back to "Other services".
        private static string ResolveSacCode(string? description)
        {
            var d = (description ?? string.Empty).ToLowerInvariant();
            if (d.Contains("food")) return "996332";
            if (d.Contains("laundry")) return "999721";
            return "996311";
        }

        // ---- Footer: key-return notice, guest declaration, signatures, thank-you ----

        private static void FooterDeclaration(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().AlignCenter().Text("*Please Return Your Key on Departure*").Bold().FontSize(10.5f).FontColor(InkColor);

                col.Item().PaddingTop(8).AlignCenter().Text("I Agree that I am responsible for the full payment of this bill in the event").FontSize(9.5f).FontColor(BrandNavy);
                col.Item().AlignCenter().Text("it is not paid by the Company, Organisation or Person indicated.").FontSize(9.5f).FontColor(BrandNavy);

                col.Item().PaddingTop(22).Row(row =>
                {
                    row.RelativeItem().Text("AUTHORISED SIGNATURE").Bold().FontSize(10).FontColor(InkColor);
                    row.RelativeItem().AlignRight().Text("GUEST SIGNATURE").Bold().FontSize(10).FontColor(InkColor);
                });

                col.Item().PaddingTop(8).AlignCenter().Text("***THANK YOU VISIT AGAIN***").Bold().FontSize(10.5f).FontColor(InkColor);
            });
        }

        // ---- Shared cell styles ----

        private static IContainer InfoLabelCell(IContainer c) =>
            c.Border(0.75f).BorderColor(BorderColor).Background("#FAFAFA").Padding(5);

        private static IContainer InfoValueCell(IContainer c) =>
            c.Border(0.75f).BorderColor(BorderColor).Padding(5);

        private static IContainer LineHeadCell(IContainer c) =>
            c.Border(0.75f).BorderColor(BorderColor).Background(HeadCellColor).Padding(4);

        private static IContainer LineBodyCell(IContainer c) =>
            c.Border(0.75f).BorderColor(BorderColor).Padding(4);
    }
}
