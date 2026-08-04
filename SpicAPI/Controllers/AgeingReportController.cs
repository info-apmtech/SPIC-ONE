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
	public class AgeingReportController : ControllerBase
	{
		private readonly IAgeingReportService _service;

		public AgeingReportController(IAgeingReportService service)
		{
			_service = service;
		}

		[HttpPost("dashboard")]
		public async Task<ActionResult<AgeingDashboardDto>> Dashboard(
			[FromBody] AgeingReportFilter? filter,
			CancellationToken cancellationToken)
		{
			var data = await _service.GetDashboardAsync(
				filter ?? new AgeingReportFilter(),
				cancellationToken);

			return Ok(data);
		}

		[HttpPost("export/excel")]
		public async Task<IActionResult> ExportExcel(
			[FromBody] AgeingReportFilter? filter,
			CancellationToken cancellationToken)
		{
			var rows = await _service.GetAllRowsAsync(
				filter ?? new AgeingReportFilter(),
				cancellationToken);

			using var workbook = new XLWorkbook();
			var worksheet = workbook.Worksheets.Add("Ageing Report");

			string[] headers =
			{
				"State",
				"District",
				"Sub-District",
				"Head Quarters",
				"Dealer ID",
				"Dealer Name",
				"Mobile No.",
				"Product",
				"Quantity (MT)",
				"ACK / Entry Date",
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
				worksheet.Cell(rowNumber, 2).Value = row.DistrictName;
				worksheet.Cell(rowNumber, 3).Value = row.SubDistrictName;
				worksheet.Cell(rowNumber, 4).Value = row.HeadQuarterName;
				worksheet.Cell(rowNumber, 5).Value = row.DealerCode;
				worksheet.Cell(rowNumber, 6).Value = row.DealerName;
				worksheet.Cell(rowNumber, 7).Value = row.MobileNo;
				worksheet.Cell(rowNumber, 8).Value = row.ProductName;
				worksheet.Cell(rowNumber, 9).Value = row.Quantity;
				worksheet.Cell(rowNumber, 10).Value =
					row.EntryDate?.ToString("dd-MM-yyyy") ?? string.Empty;
				worksheet.Cell(rowNumber, 11).Value = row.AgeingDays;
				worksheet.Cell(rowNumber, 12).Value = row.Status;
				rowNumber++;
			}

			worksheet.SheetView.FreezeRows(1);
			worksheet.RangeUsed()?.SetAutoFilter();
			worksheet.Columns().AdjustToContents();

			using var stream = new MemoryStream();
			workbook.SaveAs(stream);

			return File(
				stream.ToArray(),
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				$"AgeingReport_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
		}

		[HttpPost("export/pdf")]
		public IActionResult ExportPdf([FromBody] AgeingReportFilter? filter)
		{
			return StatusCode(501, "PDF export not implemented yet.");
		}

		[HttpPost("send-mail")]
		public IActionResult SendMail([FromBody] AgeingReportFilter? filter)
		{
			return StatusCode(501, "Send mail not implemented yet.");
		}
	}
}