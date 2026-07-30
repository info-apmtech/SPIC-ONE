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
		public LiquidationCycleController(ILiquidationCycleService svc) => _svc = svc;

		[HttpPost("dashboard")]
		public async Task<ActionResult<LiqCycleDashboardDto>> Dashboard([FromBody] LiqCycleFilter f)
			=> Ok(await _svc.GetDashboardAsync(f ?? new LiqCycleFilter()));

		[HttpPost("export/excel")]
		public async Task<IActionResult> ExportExcel([FromBody] LiqCycleFilter f)
		{
			var rows = await _svc.GetAllRowsAsync(f ?? new LiqCycleFilter());

			using var wb = new XLWorkbook();
			var ws = wb.Worksheets.Add("Liquidation Cycle");

			string[] headers = { "Dealer Name", "Dealer Type", "Product", "Stock (MT)", "Ageing (Days)", "Sales (MT)", "Status" };
			for (var c = 0; c < headers.Length; c++) ws.Cell(1, c + 1).Value = headers[c];
			ws.Row(1).Style.Font.Bold = true;

			var r = 2;
			foreach (var x in rows)
			{
				ws.Cell(r, 1).Value = x.DealerName;
				ws.Cell(r, 2).Value = x.DealerType;
				ws.Cell(r, 3).Value = x.ProductName;
				ws.Cell(r, 4).Value = x.Stock;
				ws.Cell(r, 5).Value = x.AgeingDays;
				ws.Cell(r, 6).Value = x.Sales;
				ws.Cell(r, 7).Value = x.Status;
				r++;
			}
			ws.Columns().AdjustToContents();

			using var ms = new System.IO.MemoryStream();
			wb.SaveAs(ms);
			return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "LiquidationCycle.xlsx");
		}
	}
}