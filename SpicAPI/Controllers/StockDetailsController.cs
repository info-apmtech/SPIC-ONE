using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

		public StockDetailsController(IStockDetailsService service)
		{
			_service = service;
		}

		[HttpPost("dashboard")]
		public async Task<ActionResult<StockDetailsDto>> Dashboard(
			[FromBody] StockDetailsFilter? filter,
			CancellationToken cancellationToken)
		{
			var data = await _service.GetDashboardAsync(
				filter ?? new StockDetailsFilter(),
				cancellationToken);

			return Ok(data);
		}

		[HttpPost("export/excel")]
		public async Task<IActionResult> ExportExcel(
			[FromBody] StockDetailsFilter? filter,
			CancellationToken cancellationToken)
		{
			var exportFilter = filter ?? new StockDetailsFilter();
			exportFilter.Page = 1;
			exportFilter.PageSize = int.MaxValue;

			var data = await _service.GetDashboardAsync(
				exportFilter,
				cancellationToken);

			using var workbook = new XLWorkbook();
			var worksheet = workbook.Worksheets.Add("Stock Details");
			var labels = data.Labels;

			var headers = new[]
			{
				"State",
				$"Opening Stock (as on {labels.OpeningAsOn})",
				$"Supplies ({labels.SuppliesMonth})",
				"Total Stock",
				$"Sales ({labels.SalesBeforeRange})",
				$"Sales ({labels.SalesOnDay})",
				"Total Sales",
				$"Closing Stock (as on {labels.ClosingAsOn})",
				"Sales %"
			};

			for (var column = 0; column < headers.Length; column++)
			{
				WriteHeader(worksheet, 1, column + 1, headers[column]);
			}

			var rowIndex = 2;

			foreach (var row in data.Grid.Items)
			{
				WriteRow(worksheet, rowIndex++, row, bold: false);
			}

			WriteRow(worksheet, rowIndex, data.GrandTotal, bold: true);

			worksheet.SheetView.FreezeRows(1);
			worksheet.RangeUsed().SetAutoFilter();

			// Fixed widths are much faster than AdjustToContents for large exports.
			worksheet.Column(1).Width = 24;
			worksheet.Columns(2, 8).Width = 19;
			worksheet.Column(9).Width = 13;

			worksheet.Columns(2, 8).Style.NumberFormat.Format = "#,##0.###";
			worksheet.Column(9).Style.NumberFormat.Format = "0.0%";

			using var stream = new MemoryStream();
			workbook.SaveAs(stream);

			return File(
				stream.ToArray(),
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				$"StockDetails_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
		}

		[HttpPost("export/pdf")]
		public IActionResult ExportPdf(
			[FromBody] StockDetailsFilter? filter)
		{
			return StatusCode(
				501,
				"PDF export not implemented yet. Suggest QuestPDF.");
		}

		[HttpPost("send-mail")]
		public IActionResult SendMail(
			[FromBody] StockDetailsFilter? filter)
		{
			return StatusCode(
				501,
				"Send mail not implemented yet.");
		}

		private static void WriteHeader(
			IXLWorksheet worksheet,
			int row,
			int column,
			string text)
		{
			var cell = worksheet.Cell(row, column);
			cell.Value = text;
			cell.Style.Font.Bold = true;
			cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0f172a");
			cell.Style.Font.FontColor = XLColor.White;
			cell.Style.Alignment.WrapText = true;
			cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
		}

		private static void WriteRow(
			IXLWorksheet worksheet,
			int rowIndex,
			StockDetailsRowDto row,
			bool bold)
		{
			worksheet.Cell(rowIndex, 1).Value = row.StateName;
			worksheet.Cell(rowIndex, 2).Value = row.OpeningStock;
			worksheet.Cell(rowIndex, 3).Value = row.Supplies;
			worksheet.Cell(rowIndex, 4).Value = row.TotalStock;
			worksheet.Cell(rowIndex, 5).Value = row.SalesBefore;
			worksheet.Cell(rowIndex, 6).Value = row.SalesOnDay;
			worksheet.Cell(rowIndex, 7).Value = row.TotalSales;
			worksheet.Cell(rowIndex, 8).Value = row.ClosingStock;
			worksheet.Cell(rowIndex, 9).Value = row.SalesPct / 100d;

			if (bold)
			{
				worksheet.Range(rowIndex, 1, rowIndex, 9)
					.Style.Font.Bold = true;
			}
		}
	}
}