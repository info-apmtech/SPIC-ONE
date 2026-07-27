// ============================================================================
//  PendingAckController
//  Thin controller — mirrors StockReportController. All work lives in
//  IPendingAckService. Route [controller] => "PendingAck", so the endpoints are
//  api/PendingAck/dashboard, /export/excel, /export/pdf, /send-mail — matching
//  the CompanySales.razor calls.
//
//  Adjust the namespace to match your API project (SpicAPI.Controllers here).
//
//  Register the service in Program.cs (next to your StockReport registration):
//      builder.Services.AddScoped<IPendingAckService, PendingAckService>();
// ============================================================================

using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using SPIC.Core.DTOs;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class PendingAckController : ControllerBase
	{
		private readonly IPendingAckService _service;

		public PendingAckController(IPendingAckService service) => _service = service;

		// ---- The one the view needs on load / Apply Filters / tab switch ----
		[HttpPost("dashboard")]
		public async Task<ActionResult<PendingAckDashboardDto>> Dashboard([FromBody] PendingAckFilter filter)
		{
			var data = await _service.GetDashboardAsync(filter ?? new PendingAckFilter());
			return Ok(data);
		}

		// ---- Filter dropdowns that need custom queries ----
		[HttpGet("dealer-types")]
		public async Task<ActionResult<List<PendingAckDealerTypeDto>>> DealerTypes()
			=> Ok(await _service.GetDealerTypesAsync());

		[HttpGet("dealers")]
		public async Task<ActionResult<List<PendingAckDealerDto>>> Dealers()
			=> Ok(await _service.GetDealersAsync());

		// ---- Excel export of the filtered rows (ClosedXML) ----
		[HttpPost("export/excel")]
		public async Task<IActionResult> ExportExcel([FromBody] PendingAckFilter filter)
		{
			var rows = await _service.GetAllRowsAsync(filter ?? new PendingAckFilter());

			using var wb = new XLWorkbook();
			var ws = wb.Worksheets.Add("Pending Acknowledgement");

			string[] headers =
			{
				"Invoice No.", "Invoice Date", "Agency Name", "Source", "Dealer Type",
				"State", "District", "Quantity (MT)", "Age Status",
				"Pending Ack Age (Days)", "Workflow Status", "Entry Date"
			};
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
				ws.Cell(r, 1).Value  = row.InvoiceNo;
				ws.Cell(r, 2).Value  = row.InvoiceDate?.ToString("dd-MM-yyyy");
				ws.Cell(r, 3).Value  = row.AgencyName;
				ws.Cell(r, 4).Value  = row.Source;
				ws.Cell(r, 5).Value  = row.DealerType;
				ws.Cell(r, 6).Value  = row.StateName;
				ws.Cell(r, 7).Value  = row.District;
				ws.Cell(r, 8).Value  = row.QuantityMT;
				ws.Cell(r, 9).Value  = row.AgeStatus;
				ws.Cell(r, 10).Value = row.AgeStatus == "Completed" ? "--" : row.PendingAckAgeDays.ToString();
				ws.Cell(r, 11).Value = row.WorkflowStatus;
				ws.Cell(r, 12).Value = row.EntryDate?.ToString("dd-MM-yyyy");
				r++;
			}
			ws.Columns().AdjustToContents();

			using var stream = new MemoryStream();
			wb.SaveAs(stream);
			return File(
				stream.ToArray(),
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				$"PendingAcknowledgement_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
		}

		// ---- Optional stubs: wire when you get to PDF / mail ----
		[HttpPost("export/pdf")]
		public IActionResult ExportPdf([FromBody] PendingAckFilter filter)
			=> StatusCode(501, "PDF export not implemented yet. Suggest QuestPDF.");

		[HttpPost("send-mail")]
		public IActionResult SendMail([FromBody] PendingAckFilter filter)
			=> StatusCode(501, "Send mail not implemented yet.");
	}
}