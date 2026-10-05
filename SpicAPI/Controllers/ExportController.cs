using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// Server-side rendering of the generic data-table PDF export. Used to run inside
    /// the client (SpicDataTable.razor) with QuestPDF, but QuestPDF ships native
    /// libraries that Google Play rejects (16 KB page-size rule), so the PDF is built
    /// here and the app only downloads it.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ExportController : ControllerBase
    {
        private const int MaxRows = 20_000;
        private const int MaxColumns = 60;

        public sealed class TablePdfRequest
        {
            public string? Title { get; set; }
            public List<string> Headers { get; set; } = new();
            public List<List<string?>> Rows { get; set; } = new();
        }

        [HttpPost("table-pdf")]
        [Produces("application/pdf")]
        public IActionResult TablePdf([FromBody] TablePdfRequest request)
        {
            if (request.Headers.Count == 0 || request.Headers.Count > MaxColumns)
            {
                return BadRequest(new { message = $"Between 1 and {MaxColumns} columns are required." });
            }

            if (request.Rows.Count > MaxRows)
            {
                return BadRequest(new { message = $"At most {MaxRows} rows can be exported to PDF." });
            }

            var title = string.IsNullOrWhiteSpace(request.Title) ? "Export" : request.Title.Trim();
            var columnCount = request.Headers.Count;

            QuestPDF.Settings.License = LicenseType.Community;

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Text(title).SemiBold().FontSize(16);
                        row.ConstantItem(140).AlignRight().Text(DateTime.Now.ToString("dd MMM yyyy HH:mm")).FontSize(8).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            for (var i = 0; i < columnCount; i++) cols.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            foreach (var h in request.Headers)
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1)
                                    .Padding(4).Text(h ?? string.Empty).Bold();
                            }
                        });

                        foreach (var row in request.Rows)
                        {
                            for (var c = 0; c < columnCount; c++)
                            {
                                var value = c < row.Count ? row[c] : null;
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .PaddingVertical(3).PaddingHorizontal(4).Text(value ?? string.Empty);
                            }
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();

            var fileName = string.Join("_", title.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            return File(pdf, "application/pdf", $"{fileName}_{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}
