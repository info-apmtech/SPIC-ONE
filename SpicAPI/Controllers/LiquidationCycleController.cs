using System.Collections.Generic;
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
	public class LiquidationCycleController : ControllerBase
	{
		private readonly ILiquidationCycleService _svc;

		public LiquidationCycleController(ILiquidationCycleService svc)
		{
			_svc = svc;
		}

		[HttpGet("products")]
		public async Task<ActionResult<List<LiqCycleProductDto>>> Products(
			CancellationToken cancellationToken)
		{
			return Ok(await _svc.GetProductsAsync(cancellationToken));
		}

		[HttpPost("dashboard")]
		public async Task<ActionResult<LiqCycleDashboardDto>> Dashboard(
			[FromBody] LiqCycleFilter? filter,
			CancellationToken cancellationToken)
		{
			var result = await _svc.GetDashboardAsync(
				filter ?? new LiqCycleFilter(),
				cancellationToken);

			return Ok(result);
		}

		[HttpPost("export/excel")]
		public async Task<IActionResult> ExportExcel(
			[FromBody] LiqCycleFilter? filter,
			CancellationToken cancellationToken)
		{
			var rows = await _svc.GetAllRowsAsync(
				filter ?? new LiqCycleFilter(),
				cancellationToken);

			using var workbook = new XLWorkbook();
			var worksheet = workbook.Worksheets.Add("Liquidation Cycle");

			string[] headers =
			{
				"Dealer Name",
				"Dealer Type",
				"Product",
				"Stock (MT)",
				"Ageing (Days)",
				"Sales (MT)",
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
				worksheet.Cell(rowNumber, 1).Value = row.DealerName;
				worksheet.Cell(rowNumber, 2).Value = row.DealerType;
				worksheet.Cell(rowNumber, 3).Value = row.ProductName;
				worksheet.Cell(rowNumber, 4).Value = row.Stock;
				worksheet.Cell(rowNumber, 5).Value = row.AgeingDays;
				worksheet.Cell(rowNumber, 6).Value = row.Sales;
				worksheet.Cell(rowNumber, 7).Value = row.Status;
				rowNumber++;
			}

			worksheet.Column(1).Width = 30;
			worksheet.Column(2).Width = 18;
			worksheet.Column(3).Width = 24;
			worksheet.Column(4).Width = 16;
			worksheet.Column(5).Width = 16;
			worksheet.Column(6).Width = 16;
			worksheet.Column(7).Width = 16;
			worksheet.SheetView.FreezeRows(1);
			worksheet.RangeUsed()?.SetAutoFilter();

			using var stream = new System.IO.MemoryStream();
			workbook.SaveAs(stream);

			return File(
				stream.ToArray(),
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				"LiquidationCycle.xlsx");
		}
	}
}