using System;
using System.IO;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using SPIC.Core.DTOs;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class StockReportController : ControllerBase
	{
		private readonly IStockReportService _service;

		public StockReportController(IStockReportService service)
		{
			_service = service;
		}

		/// <summary>
		/// Returns the existing Product master and the new IfmsProduct master in
		/// one dropdown-safe list. P:id and I:id prevent ID collisions.
		/// </summary>
		[HttpGet("products")]
		public async Task<ActionResult> Products()
		{
			var products = await _service.GetProductOptionsAsync();
			return Ok(products);
		}

		[HttpPost("dashboard")]
		public async Task<ActionResult<StockDashboardDto>> Dashboard(
			[FromBody] StockReportFilter? filter)
		{
			var data = await _service.GetDashboardAsync(
				filter ?? new StockReportFilter());

			return Ok(data);
		}

		[HttpPost("export/excel")]
		public async Task<IActionResult> ExportExcel(
			[FromBody] StockReportFilter? filter)
		{
			// GetAllRowsAsync still uses the same latest-snapshot flow. It now also
			// resolves rows whose product is stored in IfmsProducts.
			var rows = await _service.GetAllRowsAsync(
				filter ?? new StockReportFilter());

			using var workbook = new XLWorkbook();
			var worksheet = workbook.Worksheets.Add("Stock Report");

			string[] headers =
			{
				"State",
				"Dealer",
				"Product",
				"Quantity (MT)",
				"Lying With",
				"Ageing Days",
				"Status"
			};

			for (var column = 0; column < headers.Length; column++)
			{
				var cell = worksheet.Cell(1, column + 1);
				cell.Value = headers[column];
				cell.Style.Font.Bold = true;
				cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0f172a");
				cell.Style.Font.FontColor = XLColor.White;
			}

			var rowNumber = 2;

			foreach (var row in rows)
			{
				worksheet.Cell(rowNumber, 1).Value = row.StateName;
				worksheet.Cell(rowNumber, 2).Value = row.DealerName;
				worksheet.Cell(rowNumber, 3).Value = row.ProductName;
				worksheet.Cell(rowNumber, 4).Value = row.Quantity;
				worksheet.Cell(rowNumber, 5).Value = row.LyingWith;

				if (row.HasAckAgeing)
				{
					worksheet.Cell(rowNumber, 6).Value = row.AgeingDays;
				}
				else
				{
					worksheet.Cell(rowNumber, 6).Value = "N/A";
				}

				worksheet.Cell(rowNumber, 7).Value = FormatAgeingStatus(row.Status);
				rowNumber++;
			}

			worksheet.SheetView.FreezeRows(1);
			worksheet.Columns().AdjustToContents();

			using var stream = new MemoryStream();
			workbook.SaveAs(stream);

			return File(
				stream.ToArray(),
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				$"StockReport_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
		}

		private static string FormatAgeingStatus(string? status)
		{
			return status?.Trim() switch
			{
				"Fresh" => "Fresh (0-30)",
				"Medium" => "Medium (30-90)",
				"Slow Moving" => "Slow Moving (90-180)",
				"Long Aged" => "Long Aged (180-365)",
				"Critical" => "Critical (365+)",
				_ => status ?? string.Empty
			};
		}

		[HttpPost("export/pdf")]
		public IActionResult ExportPdf(
			[FromBody] StockReportFilter? filter)
		{
			return StatusCode(
				501,
				"PDF export not implemented yet. Suggest QuestPDF.");
		}

		[HttpPost("send-mail")]
		public IActionResult SendMail(
			[FromBody] StockReportFilter? filter)
		{
			return StatusCode(
				501,
				"Send mail not implemented yet.");
		}
	}
}