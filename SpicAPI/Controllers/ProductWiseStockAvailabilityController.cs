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
	public class ProductStockAvailabilityController : ControllerBase
	{
		private readonly IProductStockAvailabilityService _service;

		public ProductStockAvailabilityController(IProductStockAvailabilityService service)
		{
			_service = service;
		}

		[HttpPost("dashboard")]
		public async Task<ActionResult<ProductStockAvailabilityDto>> Dashboard(
			[FromBody] ProductStockAvailabilityFilter? filter,
			CancellationToken cancellationToken)
		{
			var data = await _service.GetDashboardAsync(
				filter ?? new ProductStockAvailabilityFilter(),
				cancellationToken);

			return Ok(data);
		}

		[HttpPost("export/excel")]
		public async Task<IActionResult> ExportExcel(
			[FromBody] ProductStockAvailabilityFilter? filter,
			CancellationToken cancellationToken)
		{
			var exportFilter = CloneForExport(
				filter ?? new ProductStockAvailabilityFilter());

			var data = await _service.GetDashboardAsync(exportFilter, cancellationToken);

			using var workbook = new XLWorkbook();
			var worksheet = workbook.Worksheets.Add("Product-wise Stock");

			var productCount = data.Columns.Count;
			var nextColumn = 2;

			worksheet.Cell(1, 1).Value = "State";
			worksheet.Range(1, 1, 2, 1).Merge();

			var stockStartColumn = nextColumn;
			if (productCount > 0)
			{
				var stockEndColumn = stockStartColumn + productCount - 1;
				worksheet.Cell(1, stockStartColumn).Value = "Current Stock (MT)";
				worksheet.Range(1, stockStartColumn, 1, stockEndColumn).Merge();

				for (var index = 0; index < productCount; index++)
				{
					worksheet.Cell(2, stockStartColumn + index).Value =
						data.Columns[index].ProductName;
				}

				nextColumn = stockEndColumn + 1;
			}

			var salesStartColumn = nextColumn;
			if (productCount > 0)
			{
				var salesEndColumn = salesStartColumn + productCount - 1;
				worksheet.Cell(1, salesStartColumn).Value = "Sales (MT)";
				worksheet.Range(1, salesStartColumn, 1, salesEndColumn).Merge();

				for (var index = 0; index < productCount; index++)
				{
					worksheet.Cell(2, salesStartColumn + index).Value =
						data.Columns[index].ProductName;
				}

				nextColumn = salesEndColumn + 1;
			}

			var totalStockColumn = nextColumn++;
			var totalSalesColumn = nextColumn++;
			var lastColumn = totalSalesColumn;

			worksheet.Cell(1, totalStockColumn).Value = "Total Stock (MT)";
			worksheet.Range(1, totalStockColumn, 2, totalStockColumn).Merge();

			worksheet.Cell(1, totalSalesColumn).Value = "Total Sales (MT)";
			worksheet.Range(1, totalSalesColumn, 2, totalSalesColumn).Merge();

			var header = worksheet.Range(1, 1, 2, lastColumn);
			header.Style.Font.Bold = true;
			header.Style.Fill.BackgroundColor = XLColor.FromHtml("#0f172a");
			header.Style.Font.FontColor = XLColor.White;
			header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
			header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

			var rowIndex = 3;

			foreach (var row in data.Grid.Items)
			{
				worksheet.Cell(rowIndex, 1).Value = row.StateName;

				for (var index = 0; index < productCount; index++)
				{
					var productId = data.Columns[index].ProductId;

					worksheet.Cell(rowIndex, stockStartColumn + index).Value =
						row.Quantities.TryGetValue(productId, out var stock) ? stock : 0m;

					worksheet.Cell(rowIndex, salesStartColumn + index).Value =
						row.SalesQuantities.TryGetValue(productId, out var sales) ? sales : 0m;
				}

				worksheet.Cell(rowIndex, totalStockColumn).Value = row.Total;
				worksheet.Cell(rowIndex, totalSalesColumn).Value = row.TotalSales;
				rowIndex++;
			}

			worksheet.Cell(rowIndex, 1).Value = "Grand Total";

			for (var index = 0; index < productCount; index++)
			{
				var productId = data.Columns[index].ProductId;

				worksheet.Cell(rowIndex, stockStartColumn + index).Value =
					data.GrandTotal.Quantities.TryGetValue(productId, out var stock) ? stock : 0m;

				worksheet.Cell(rowIndex, salesStartColumn + index).Value =
					data.GrandTotal.SalesQuantities.TryGetValue(productId, out var sales) ? sales : 0m;
			}

			worksheet.Cell(rowIndex, totalStockColumn).Value = data.GrandTotal.Total;
			worksheet.Cell(rowIndex, totalSalesColumn).Value = data.GrandTotal.TotalSales;

			var grandTotalRange = worksheet.Range(rowIndex, 1, rowIndex, lastColumn);
			grandTotalRange.Style.Font.Bold = true;
			grandTotalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#e2e8f0");

			if (rowIndex >= 3 && lastColumn >= 2)
			{
				worksheet.Range(3, 2, rowIndex, lastColumn)
					.Style.NumberFormat.Format = "#,##0.###";
			}

			worksheet.SheetView.FreezeRows(2);
			worksheet.SheetView.FreezeColumns(1);
			worksheet.Column(1).Width = 28;

			for (var excelColumn = 2; excelColumn <= lastColumn; excelColumn++)
			{
				worksheet.Column(excelColumn).Width = 16;
			}

			worksheet.RangeUsed()?.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
			worksheet.RangeUsed()?.Style.Border.SetInsideBorder(XLBorderStyleValues.Hair);

			using var stream = new MemoryStream();
			workbook.SaveAs(stream);

			return File(
				stream.ToArray(),
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				$"ProductWiseStock_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
		}

		[HttpPost("export/pdf")]
		public IActionResult ExportPdf([FromBody] ProductStockAvailabilityFilter? filter)
		{
			return StatusCode(501, "PDF export is not implemented yet.");
		}

		[HttpPost("send-mail")]
		public IActionResult SendMail([FromBody] ProductStockAvailabilityFilter? filter)
		{
			return StatusCode(501, "Send mail is not implemented yet.");
		}

		private static ProductStockAvailabilityFilter CloneForExport(
			ProductStockAvailabilityFilter source)
		{
			return new ProductStockAvailabilityFilter
			{
				DateFrom = source.DateFrom,
				DateTo = source.DateTo,
				StateIds = source.StateIds is null ? new() : new(source.StateIds),
				RegionIds = source.RegionIds is null ? new() : new(source.RegionIds),
				HeadQuarterIds = source.HeadQuarterIds is null ? new() : new(source.HeadQuarterIds),
				Search = source.Search,
				SortColumn = source.SortColumn,
				SortDir = source.SortDir,
				Page = 1,
				PageSize = int.MaxValue
			};
		}
	}
}