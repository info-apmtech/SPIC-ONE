// ============================================================================
//  ProductStockAvailabilityController
//  Route: api/ProductStockAvailability
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
	public class ProductStockAvailabilityController : ControllerBase
	{
		private readonly IProductStockAvailabilityService _service;

		public ProductStockAvailabilityController(IProductStockAvailabilityService service) => _service = service;

		// ---- The one the view binds to ----
		[HttpPost("dashboard")]
		public async Task<ActionResult<ProductStockAvailabilityDto>> Dashboard(
			[FromBody] ProductStockAvailabilityFilter filter)
		{
			var data = await _service.GetDashboardAsync(filter ?? new ProductStockAvailabilityFilter());
			return Ok(data);
		}

		// ---- Excel export of the full (unpaged) pivot, columns built dynamically ----
		[HttpPost("export/excel")]
		public async Task<IActionResult> ExportExcel([FromBody] ProductStockAvailabilityFilter filter)
		{
			var f = filter ?? new ProductStockAvailabilityFilter();
			f.Page = 1;
			f.PageSize = int.MaxValue;   // pull every state row

			var data = await _service.GetDashboardAsync(f);

			using var wb = new XLWorkbook();
			var ws = wb.Worksheets.Add("Product-wise Stock");

			// Header: State | <each product> | Total (MT)
			int col = 1;
			WriteHeader(ws, 1, col++, "State");
			foreach (var c in data.Columns)
				WriteHeader(ws, 1, col++, c.ProductName);
			WriteHeader(ws, 1, col, "Total (MT)");

			// State rows
			int r = 2;
			foreach (var row in data.Grid.Items)
			{
				int cc = 1;
				ws.Cell(r, cc++).Value = row.StateName;
				foreach (var c in data.Columns)
					ws.Cell(r, cc++).Value = row.Quantities.TryGetValue(c.ProductId, out var v) ? v : 0m;
				ws.Cell(r, cc).Value = row.Total;
				r++;
			}

			// Grand total row
			{
				int cc = 1;
				var gc = ws.Cell(r, cc++);
				gc.Value = "Grand Total";
				gc.Style.Font.Bold = true;
				foreach (var c in data.Columns)
				{
					var cell = ws.Cell(r, cc++);
					cell.Value = data.GrandTotal.Quantities.TryGetValue(c.ProductId, out var v) ? v : 0m;
					cell.Style.Font.Bold = true;
				}
				var tc = ws.Cell(r, cc);
				tc.Value = data.GrandTotal.Total;
				tc.Style.Font.Bold = true;
			}

			ws.Columns().AdjustToContents();

			using var stream = new MemoryStream();
			wb.SaveAs(stream);
			return File(
				stream.ToArray(),
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				$"ProductWiseStock_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
		}

		[HttpPost("export/pdf")]
		public IActionResult ExportPdf([FromBody] ProductStockAvailabilityFilter filter)
			=> StatusCode(501, "PDF export not implemented yet. Suggest QuestPDF.");

		[HttpPost("send-mail")]
		public IActionResult SendMail([FromBody] ProductStockAvailabilityFilter filter)
			=> StatusCode(501, "Send mail not implemented yet.");

		private static void WriteHeader(IXLWorksheet ws, int row, int colIdx, string text)
		{
			var cell = ws.Cell(row, colIdx);
			cell.Value = text;
			cell.Style.Font.Bold = true;
			cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0f172a");
			cell.Style.Font.FontColor = XLColor.White;
		}
	}
}