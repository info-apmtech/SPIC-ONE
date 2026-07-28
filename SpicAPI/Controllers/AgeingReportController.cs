// ============================================================================
//  AgeingReportController — thin, mirrors StockReportController.
//  Route [controller] => "AgeingReport": api/AgeingReport/dashboard, /export/excel, ...
//
//  Register in Program.cs:
//      builder.Services.AddScoped<IAgeingReportService, AgeingReportService>();
// ============================================================================

using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using SPIC.Core.DTOs;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AgeingReportController : ControllerBase
	{
		private readonly IAgeingReportService _service;
		public AgeingReportController(IAgeingReportService service) => _service = service;

		[HttpPost("dashboard")]
		public async Task<ActionResult<AgeingDashboardDto>> Dashboard([FromBody] AgeingReportFilter filter)
			=> Ok(await _service.GetDashboardAsync(filter ?? new AgeingReportFilter()));

		[HttpPost("export/excel")]
		public async Task<IActionResult> ExportExcel([FromBody] AgeingReportFilter filter)
		{
			var rows = await _service.GetAllRowsAsync(filter ?? new AgeingReportFilter());

			using var wb = new XLWorkbook();
			var ws = wb.Worksheets.Add("Ageing Report");

			string[] headers = { "State", "Dealer", "Product", "Quantity (MT)", "Ageing Days", "Status" };
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
				ws.Cell(r, 5).Value = row.AgeingDays;
				ws.Cell(r, 6).Value = row.Status;
				r++;
			}
			ws.Columns().AdjustToContents();

			using var stream = new MemoryStream();
			wb.SaveAs(stream);
			return File(stream.ToArray(),
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				$"AgeingReport_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
		}

		[HttpPost("export/pdf")]
		public IActionResult ExportPdf([FromBody] AgeingReportFilter filter)
			=> StatusCode(501, "PDF export not implemented yet.");

		[HttpPost("send-mail")]
		public IActionResult SendMail([FromBody] AgeingReportFilter filter)
			=> StatusCode(501, "Send mail not implemented yet.");
	}
}