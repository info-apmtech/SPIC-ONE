// ============================================================================
//  SpicAPI / Controllers / ProductStockAvailabilityController.cs
// ============================================================================

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

		public ProductStockAvailabilityController(
			IProductStockAvailabilityService service)
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

			var data = await _service.GetDashboardAsync(
				exportFilter,
				cancellationToken);

			using var workbook = new XLWorkbook();
			var worksheet = workbook.Worksheets.Add("Product-wise Stock");

			var lastColumn = data.Columns.Count + 2;

			worksheet.Cell(1, 1).Value = "State";

			var columnIndex = 2;
			foreach (var column in data.Columns)
			{
				worksheet.Cell(1, columnIndex++).Value = column.ProductName;
			}

			worksheet.Cell(1, lastColumn).Value = "Total (MT)";

			var header = worksheet.Range(1, 1, 1, lastColumn);
			header.Style.Font.Bold = true;
			header.Style.Fill.BackgroundColor = XLColor.FromHtml("#0f172a");
			header.Style.Font.FontColor = XLColor.White;
			header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
			header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

			var rowIndex = 2;

			foreach (var row in data.Grid.Items)
			{
				worksheet.Cell(rowIndex, 1).Value = row.StateName;

				columnIndex = 2;
				foreach (var column in data.Columns)
				{
					worksheet.Cell(rowIndex, columnIndex++).Value =
						row.Quantities.TryGetValue(column.ProductId, out var quantity)
							? quantity
							: 0m;
				}

				worksheet.Cell(rowIndex, lastColumn).Value = row.Total;
				rowIndex++;
			}

			worksheet.Cell(rowIndex, 1).Value = "Grand Total";

			columnIndex = 2;
			foreach (var column in data.Columns)
			{
				worksheet.Cell(rowIndex, columnIndex++).Value =
					data.GrandTotal.Quantities.TryGetValue(
						column.ProductId,
						out var quantity)
						? quantity
						: 0m;
			}

			worksheet.Cell(rowIndex, lastColumn).Value = data.GrandTotal.Total;

			var grandTotalRange = worksheet.Range(rowIndex, 1, rowIndex, lastColumn);
			grandTotalRange.Style.Font.Bold = true;
			grandTotalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#e2e8f0");

			if (rowIndex >= 2 && lastColumn >= 2)
			{
				worksheet.Range(2, 2, rowIndex, lastColumn)
					.Style.NumberFormat.Format = "#,##0.###";
			}

			worksheet.SheetView.FreezeRows(1);
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
		public IActionResult ExportPdf(
			[FromBody] ProductStockAvailabilityFilter? filter)
		{
			return StatusCode(501, "PDF export is not implemented yet.");
		}

		[HttpPost("send-mail")]
		public IActionResult SendMail(
			[FromBody] ProductStockAvailabilityFilter? filter)
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
				StateIds = source.StateIds is null
					? new()
					: new(source.StateIds),
				RegionIds = source.RegionIds is null
					? new()
					: new(source.RegionIds),
				HeadQuarterIds = source.HeadQuarterIds is null
					? new()
					: new(source.HeadQuarterIds),
				Search = source.Search,
				SortColumn = source.SortColumn,
				SortDir = source.SortDir,
				Page = 1,
				PageSize = int.MaxValue
			};
		}
	}
}