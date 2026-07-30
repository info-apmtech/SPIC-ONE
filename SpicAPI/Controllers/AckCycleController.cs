// ============================================================================
//  SpicAPI / Controllers / AckCycleController.cs
//  Thin controller -> IAckCycleService. One dashboard POST + master-data GETs
//  + an Excel export (ClosedXML). PDF / Send-Mail are stubs so they don't 404.
// ============================================================================
using System.Collections.Generic;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using SPIC.Core.DTOs;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]   // -> api/AckCycle
	public class AckCycleController : ControllerBase
	{
		private readonly IAckCycleService _svc;
		public AckCycleController(IAckCycleService svc) => _svc = svc;

		[HttpPost("dashboard")]
		public async Task<ActionResult<AckCycleDashboardDto>> Dashboard([FromBody] AckCycleFilter f)
			=> Ok(await _svc.GetDashboardAsync(f ?? new AckCycleFilter()));

		[HttpGet("states")]
		public async Task<ActionResult<List<AckLookupItemDto>>> States()
			=> Ok(await _svc.GetStatesAsync());

		// POST so the (possibly multi-value) state selection rides in the body.
		[HttpPost("districts")]
		public async Task<ActionResult<List<AckLookupItemDto>>> Districts([FromBody] List<int> stateIds)
			=> Ok(await _svc.GetDistrictsAsync(stateIds ?? new List<int>()));

		[HttpGet("products")]
		public async Task<ActionResult<List<AckLookupItemDto>>> Products()
			=> Ok(await _svc.GetProductsAsync());

		[HttpGet("statuses")]
		public async Task<ActionResult<List<AckLookupItemDto>>> Statuses()
			=> Ok(await _svc.GetStatusesAsync());

		[HttpGet("dealers")]
		public async Task<ActionResult<List<AckLookupItemDto>>> Dealers()
			=> Ok(await _svc.GetDealersAsync());

		[HttpPost("export/excel")]
		public async Task<IActionResult> ExportExcel([FromBody] AckCycleFilter f)
		{
			var rows = await _svc.GetAllRowsAsync(f ?? new AckCycleFilter());

			using var wb = new XLWorkbook();
			var ws = wb.Worksheets.Add("Acknowledgement Cycle");

			string[] headers =
			{
				"S.No", "Source", "Dealer Name", "Product", "Invoice No",
				"Invoice Date", "Receipt Date", "Ack Cycle (Days)", "Status",
				"State", "District", "Qty (MT)"
			};
			for (var c = 0; c < headers.Length; c++)
				ws.Cell(1, c + 1).Value = headers[c];
			ws.Row(1).Style.Font.Bold = true;

			var r = 2;
			foreach (var x in rows)
			{
				ws.Cell(r, 1).Value = x.SNo;
				ws.Cell(r, 2).Value = x.Source;
				ws.Cell(r, 3).Value = x.DealerName;
				ws.Cell(r, 4).Value = x.ProductName;
				ws.Cell(r, 5).Value = x.InvoiceNo;
				ws.Cell(r, 6).Value = x.InvoiceDate?.ToString("dd-MM-yyyy") ?? "";
				ws.Cell(r, 7).Value = x.ReceiptDate?.ToString("dd-MM-yyyy") ?? "";
				ws.Cell(r, 8).Value = x.CycleDays;
				ws.Cell(r, 9).Value = x.Bucket;
				ws.Cell(r, 10).Value = x.StateName;
				ws.Cell(r, 11).Value = x.District;
				ws.Cell(r, 12).Value = x.QuantityMT;
				r++;
			}

			ws.Columns().AdjustToContents();

			using var ms = new System.IO.MemoryStream();
			wb.SaveAs(ms);
			return File(ms.ToArray(),
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				"AcknowledgementCycle.xlsx");
		}

		// TODO: implement if/when needed. Kept as stubs so the UI buttons don't 404.
		[HttpPost("export/pdf")]
		public IActionResult ExportPdf([FromBody] AckCycleFilter f) => NoContent();

		[HttpPost("send-mail")]
		public IActionResult SendMail([FromBody] AckCycleFilter f) => NoContent();
	}
}