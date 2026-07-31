// ============================================================================
//  StockDetailsController
//  Route: api/StockDetails
//  Same thin controller -> interface -> service shape as StockReportController.
//  Adjust the namespace to match your API project.
// ============================================================================

using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using SPIC.Core.DTOs;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class StockDetailsController : ControllerBase
	{
		private readonly IStockDetailsService _service;

		public StockDetailsController(IStockDetailsService service) => _service = service;

		[HttpPost("dashboard")]
		public async Task<ActionResult<StockDetailsDto>> Dashboard([FromBody] StockDetailsFilter filter)
		{
			var data = await _service.GetDashboardAsync(filter ?? new StockDetailsFilter());
			return Ok(data);
		}

		[HttpPost("export/excel")]
		public async Task<IActionResult> ExportExcel([FromBody] StockDetailsFilter filter)
		{
			var f = filter ?? new StockDetailsFilter();
			f.Page = 1;
			f.PageSize = int.MaxValue;

			var data = await _service.GetDashboardAsync(f);
			var l = data.Labels;

			using var wb = new XLWorkbook();
			var ws = wb.Worksheets.Add("Stock Details");

			string[] headers =
			{
				"State",
				$"Opening Stock (as on {l.OpeningAsOn})",
				$"Supplies ({l.SuppliesMonth})",
				"Total Stock",
				$"Sales ({l.SalesBeforeRange})",
				$"Sales ({l.SalesOnDay})",
				"Total Sales",
				$"Closing Stock (as on {l.ClosingAsOn})",
				"Sales %"
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
			foreach (var row in data.Grid.Items)
			{
				WriteRow(ws, r++, row, bold: false);
			}
			WriteRow(ws, r, data.GrandTotal, bold: true);

			ws.Columns().AdjustToContents();

			using var stream = new MemoryStream();
			wb.SaveAs(stream);
			return File(
				stream.ToArray(),
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				$"StockDetails_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
		}

		[HttpPost("export/pdf")]
		public IActionResult ExportPdf([FromBody] StockDetailsFilter filter)
			=> StatusCode(501, "PDF export not implemented yet. Suggest QuestPDF.");

		[HttpPost("send-mail")]
		public IActionResult SendMail([FromBody] StockDetailsFilter filter)
			=> StatusCode(501, "Send mail not implemented yet.");

		private static void WriteRow(IXLWorksheet ws, int r, StockDetailsRowDto row, bool bold)
		{
			ws.Cell(r, 1).Value = row.StateName;
			ws.Cell(r, 2).Value = row.OpeningStock;
			ws.Cell(r, 3).Value = row.Supplies;
			ws.Cell(r, 4).Value = row.TotalStock;
			ws.Cell(r, 5).Value = row.SalesBefore;
			ws.Cell(r, 6).Value = row.SalesOnDay;
			ws.Cell(r, 7).Value = row.TotalSales;
			ws.Cell(r, 8).Value = row.ClosingStock;
			ws.Cell(r, 9).Value = Math.Round(row.SalesPct, 1) / 100.0;
			ws.Cell(r, 9).Style.NumberFormat.Format = "0%";
			if (bold)
				for (int c = 1; c <= 9; c++) ws.Cell(r, c).Style.Font.Bold = true;
		}
	}
}