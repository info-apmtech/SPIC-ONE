using System;
using System.Collections.Generic;
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
	public class PendingAckController : ControllerBase
	{
		private readonly IPendingAckService _service;

		public PendingAckController(IPendingAckService service)
		{
			_service = service;
		}

		[HttpPost("dashboard")]
		public async Task<ActionResult<PendingAckDashboardDto>> Dashboard(
			[FromBody] PendingAckFilter? filter,
			CancellationToken cancellationToken)
		{
			var data = await _service.GetDashboardAsync(
				filter ?? new PendingAckFilter(),
				cancellationToken);

			return Ok(data);
		}

		[HttpGet("dealer-types")]
		public async Task<ActionResult<List<PendingAckDealerTypeDto>>> DealerTypes(
			CancellationToken cancellationToken)
		{
			return Ok(await _service.GetDealerTypesAsync(cancellationToken));
		}

		[HttpGet("dealers")]
		public async Task<ActionResult<List<PendingAckDealerDto>>> Dealers(
			CancellationToken cancellationToken)
		{
			return Ok(await _service.GetDealersAsync(cancellationToken));
		}

		[HttpPost("export/excel")]
		public async Task<IActionResult> ExportExcel(
			[FromBody] PendingAckFilter? filter,
			CancellationToken cancellationToken)
		{
			var rows = await _service.GetAllRowsAsync(
				filter ?? new PendingAckFilter(),
				cancellationToken);

			using var workbook = new XLWorkbook();
			var worksheet = workbook.Worksheets.Add("Pending Acknowledgement");

			string[] headers =
			{
				"Invoice / Reference No.",
				"Invoice / Report Date",
				"Agency Name",
				"Source",
				"Dealer Type",
				"State",
				"District",
				"Quantity (MT)",
				"Age Status",
				"Pending Ack Age (Days)",
				"Workflow Status",
				"Entry Date"
			};

			for (var columnIndex = 0; columnIndex < headers.Length; columnIndex++)
			{
				var cell = worksheet.Cell(1, columnIndex + 1);
				cell.Value = headers[columnIndex];
				cell.Style.Font.Bold = true;
				cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0f172a");
				cell.Style.Font.FontColor = XLColor.White;
			}

			var rowIndex = 2;
			foreach (var row in rows)
			{
				worksheet.Cell(rowIndex, 1).Value =
					string.IsNullOrWhiteSpace(row.InvoiceNo)
						? row.Source == "Retailer Sales" ? "Retailer Report" : string.Empty
						: row.InvoiceNo;
				worksheet.Cell(rowIndex, 2).Value = row.InvoiceDate?.ToString("dd-MM-yyyy");
				worksheet.Cell(rowIndex, 3).Value = row.AgencyName;
				worksheet.Cell(rowIndex, 4).Value = row.Source;
				worksheet.Cell(rowIndex, 5).Value = row.DealerType;
				worksheet.Cell(rowIndex, 6).Value = row.StateName;
				worksheet.Cell(rowIndex, 7).Value = row.District;
				worksheet.Cell(rowIndex, 8).Value = row.QuantityMT;
				worksheet.Cell(rowIndex, 9).Value = row.AgeStatus;
				worksheet.Cell(rowIndex, 10).Value =
					row.AgeStatus == AgeStatus.Completed
						? "--"
						: row.PendingAckAgeDays.ToString();
				worksheet.Cell(rowIndex, 11).Value = row.WorkflowStatus;
				worksheet.Cell(rowIndex, 12).Value = row.EntryDate?.ToString("dd-MM-yyyy");
				rowIndex++;
			}

			worksheet.SheetView.FreezeRows(1);
			worksheet.RangeUsed()?.SetAutoFilter();
			worksheet.Columns().AdjustToContents();

			using var stream = new MemoryStream();
			workbook.SaveAs(stream);

			return File(
				stream.ToArray(),
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				$"PendingAcknowledgement_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
		}

		[HttpPost("export/pdf")]
		public IActionResult ExportPdf([FromBody] PendingAckFilter filter)
		{
			return StatusCode(501, "PDF export not implemented yet. Suggest QuestPDF.");
		}

		[HttpPost("send-mail")]
		public IActionResult SendMail([FromBody] PendingAckFilter filter)
		{
			return StatusCode(501, "Send mail not implemented yet.");
		}
	}
}