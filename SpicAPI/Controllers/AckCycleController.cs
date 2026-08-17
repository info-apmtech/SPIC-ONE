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
	public class AckCycleController : ControllerBase
	{
		private readonly IAckCycleService _service;

		public AckCycleController(IAckCycleService service)
		{
			_service = service;
		}

		[HttpPost("dashboard")]
		public async Task<ActionResult<AckCycleDashboardDto>> Dashboard(
			[FromBody] AckCycleFilter? filter,
			CancellationToken cancellationToken)
		{
			var data = await _service.GetDashboardAsync(
				filter ?? new AckCycleFilter(),
				cancellationToken);

			return Ok(data);
		}

		[HttpGet("states")]
		public async Task<ActionResult<List<AckLookupItemDto>>> States(
			CancellationToken cancellationToken)
			=> Ok(await _service.GetStatesAsync(cancellationToken));

		[HttpPost("districts")]
		public async Task<ActionResult<List<AckLookupItemDto>>> Districts(
			[FromBody] List<int>? stateIds,
			CancellationToken cancellationToken)
			=> Ok(await _service.GetDistrictsAsync(
				stateIds ?? new List<int>(),
				cancellationToken));

		[HttpGet("products")]
		public async Task<ActionResult<List<AckLookupItemDto>>> Products(
			CancellationToken cancellationToken)
			=> Ok(await _service.GetProductsAsync(cancellationToken));

		[HttpGet("statuses")]
		public async Task<ActionResult<List<AckLookupItemDto>>> Statuses(
			CancellationToken cancellationToken)
			=> Ok(await _service.GetStatusesAsync(cancellationToken));

		[HttpGet("dealers")]
		public async Task<ActionResult<List<AckLookupItemDto>>> Dealers(
			CancellationToken cancellationToken)
			=> Ok(await _service.GetDealersAsync(cancellationToken));

		[HttpPost("export/excel")]
		public async Task<IActionResult> ExportExcel(
			[FromBody] AckCycleFilter? filter,
			CancellationToken cancellationToken)
		{
			var rows = await _service.GetAllRowsAsync(
				filter ?? new AckCycleFilter(),
				cancellationToken);

			using var workbook = new XLWorkbook();
			var worksheet = workbook.Worksheets.Add("Acknowledgement Cycle");

			string[] headers =
			{
				"S.No", "Source", "Dealer Name", "Product", "Invoice No",
				"Invoice Date", "Receipt Date", "Ack Cycle (Days)",
				"Cycle Status", "Workflow Status", "State", "District", "Qty (MT)"
			};

			for (var column = 0; column < headers.Length; column++)
			{
				worksheet.Cell(1, column + 1).Value = headers[column];
			}

			var header = worksheet.Range(1, 1, 1, headers.Length);
			header.Style.Font.Bold = true;
			header.Style.Fill.BackgroundColor = XLColor.FromHtml("#0f172a");
			header.Style.Font.FontColor = XLColor.White;

			var rowNumber = 2;
			foreach (var row in rows)
			{
				worksheet.Cell(rowNumber, 1).Value = row.SNo;
				worksheet.Cell(rowNumber, 2).Value = row.Source;
				worksheet.Cell(rowNumber, 3).Value = row.DealerName;
				worksheet.Cell(rowNumber, 4).Value = row.ProductName;
				worksheet.Cell(rowNumber, 5).Value = row.InvoiceNo;

				if (row.InvoiceDate.HasValue)
				{
					worksheet.Cell(rowNumber, 6).Value = row.InvoiceDate.Value;
					worksheet.Cell(rowNumber, 6).Style.DateFormat.Format = "dd-MM-yyyy";
				}

				if (row.ReceiptDate.HasValue)
				{
					worksheet.Cell(rowNumber, 7).Value = row.ReceiptDate.Value;
					worksheet.Cell(rowNumber, 7).Style.DateFormat.Format = "dd-MM-yyyy";
				}

				worksheet.Cell(rowNumber, 8).Value = row.CycleDays;
				worksheet.Cell(rowNumber, 9).Value = row.Bucket;
				worksheet.Cell(rowNumber, 10).Value = row.WorkflowStatus;
				worksheet.Cell(rowNumber, 11).Value = row.StateName;
				worksheet.Cell(rowNumber, 12).Value = row.District;
				worksheet.Cell(rowNumber, 13).Value = row.QuantityMT;
				rowNumber++;
			}

			worksheet.Column(1).Width = 8;
			worksheet.Column(2).Width = 20;
			worksheet.Column(3).Width = 30;
			worksheet.Column(4).Width = 24;
			worksheet.Column(5).Width = 20;
			worksheet.Column(6).Width = 14;
			worksheet.Column(7).Width = 14;
			worksheet.Column(8).Width = 18;
			worksheet.Column(9).Width = 16;
			worksheet.Column(10).Width = 18;
			worksheet.Column(11).Width = 20;
			worksheet.Column(12).Width = 20;
			worksheet.Column(13).Width = 14;
			worksheet.SheetView.FreezeRows(1);

			using var stream = new MemoryStream();
			workbook.SaveAs(stream);

			return File(
				stream.ToArray(),
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				$"AcknowledgementCycle_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
		}

		// Existing flow retained. These endpoints remain placeholders until the
		// project adds real PDF generation and mail delivery.
		[HttpPost("export/pdf")]
		public IActionResult ExportPdf([FromBody] AckCycleFilter? filter)
			=> NoContent();

		[HttpPost("send-mail")]
		public IActionResult SendMail([FromBody] AckCycleFilter? filter)
			=> NoContent();
	}
}