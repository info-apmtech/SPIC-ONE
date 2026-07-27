// ============================================================================
//  StockReportController
//  The dashboard endpoint is the one your StockReport.razor calls on load and
//  on "Apply Filters". The export/mail endpoints are optional extras for the
//  Excel / PDF / Send Mail buttons - safe to ignore until you need them.
//
//  Adjust the namespace to match your API project.
// ============================================================================

using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using SPIC.Core.DTOs;         // <-- match your DTO namespace
using SPIC.Core.Interfaces;   // <-- match your interface namespace

namespace SpicAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class StockReportController : ControllerBase
	{
		private readonly IStockReportService _service;

		public StockReportController(IStockReportService service) => _service = service;

		// ---- The one the view needs to bind data ----
		[HttpPost("dashboard")]
		public async Task<ActionResult<StockDashboardDto>> Dashboard([FromBody] StockReportFilter filter)
		{
			var data = await _service.GetDashboardAsync(filter ?? new StockReportFilter());
			return Ok(data);
		}

		// ---- Optional: Excel export of the filtered rows (uses ClosedXML) ----
		[HttpPost("export/excel")]
		public async Task<IActionResult> ExportExcel([FromBody] StockReportFilter filter)
		{
			var rows = await _service.GetAllRowsAsync(filter ?? new StockReportFilter());

			using var wb = new XLWorkbook();
			var ws = wb.Worksheets.Add("Stock Report");

			string[] headers = { "State", "Dealer", "Product", "Quantity (MT)", "Lying With", "Ageing Days", "Status" };
			for (int c = 0; c < headers.Length; c++)
			{
				var cell = ws.Cell(1, c + 1);
				cell.Value = headers[c];
				cell.Style.Font.Bold = true;
				cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0f172a");
				cell.Style.Font.FontColor = XLColor.White;
			}

			int r = 2;
			foreach (var row in rows)
			{
				ws.Cell(r, 1).Value = row.StateName;
				ws.Cell(r, 2).Value = row.DealerName;
				ws.Cell(r, 3).Value = row.ProductName;
				ws.Cell(r, 4).Value = row.Quantity;
				ws.Cell(r, 5).Value = row.LyingWith;
				ws.Cell(r, 6).Value = row.AgeingDays;
				ws.Cell(r, 7).Value = row.Status;
				r++;
			}

			ws.Columns().AdjustToContents();

			using var stream = new MemoryStream();
			wb.SaveAs(stream);
			return File(
				stream.ToArray(),
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				$"StockReport_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
		}

		// ---- Optional stubs: wire these when you get to PDF / mail ----
		[HttpPost("export/pdf")]
		public IActionResult ExportPdf([FromBody] StockReportFilter filter)
			=> StatusCode(501, "PDF export not implemented yet. Suggest QuestPDF.");

		[HttpPost("send-mail")]
		public IActionResult SendMail([FromBody] StockReportFilter filter)
			=> StatusCode(501, "Send mail not implemented yet.");
	}
}
