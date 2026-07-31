// ============================================================================
//  SPIC.Core / DTOs / StockDetailsDtos.cs
//  DTOs for the Stock Details report - a state-wise stock+sales ledger:
//    Opening Stock | Supplies | Total Stock | Sales(before) | Sales(as-on day)
//    | Total Sales | Closing Stock | Sales %
//
//  Time anchor: a selected Month/Year, read "as on" a through-date within it.
//  Reuses the shared PagedResult<T>.
// ============================================================================
using System;
using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
	public class StockDetailsFilter
	{
		public DateTime? DateFrom { get; set; }
		public DateTime? DateTo { get; set; }
		// Optional: financial-year ids selected in the UI. The view resolves them to
		// DateFrom/DateTo before posting, so the service only needs the dates.
		public List<int> FinancialYearIds { get; set; } = new();

		public List<int> StateIds { get; set; } = new();

		public string? Search { get; set; }        // matches State name
		public string? SortColumn { get; set; }     // state | totalstock | totalsales | closing | salespct
		public string? SortDir { get; set; } = "asc";

		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 16;
	}

	public class StockDetailsDto
	{
		public StockDetailsSummaryDto Summary { get; set; } = new();
		public StockDetailsLabelsDto Labels { get; set; } = new();     // dynamic column date captions
		public StockDetailsRowDto GrandTotal { get; set; } = new();    // across ALL filtered states
		public PagedResult<StockDetailsRowDto> Grid { get; set; } = new();
	}

	/// <summary>The four KPI cards.</summary>
	public class StockDetailsSummaryDto
	{
		public decimal TotalStock { get; set; }
		public decimal TotalSales { get; set; }
		public decimal ClosingStock { get; set; }
		public double SalesPct { get; set; }   // 0-100
	}

	/// <summary>Human captions for the dated columns (e.g. "1 Apr", "April", "1-6 Apr", "7 Apr").</summary>
	public class StockDetailsLabelsDto
	{
		public string OpeningAsOn { get; set; } = "";       // month start, e.g. "1 Apr"
		public string SuppliesMonth { get; set; } = "";     // month name, e.g. "April"
		public string SalesBeforeRange { get; set; } = "";  // e.g. "1-6 Apr"
		public string SalesOnDay { get; set; } = "";        // e.g. "7 Apr"
		public string ClosingAsOn { get; set; } = "";       // e.g. "7 Apr"
	}

	public class StockDetailsRowDto
	{
		public int StateId { get; set; }
		public string StateName { get; set; } = "";

		public decimal OpeningStock { get; set; }
		public decimal Supplies { get; set; }
		public decimal TotalStock { get; set; }     // Opening + Supplies

		public decimal SalesBefore { get; set; }     // [monthStart, asOn)
		public decimal SalesOnDay { get; set; }       // the as-on day
		public decimal TotalSales { get; set; }       // SalesBefore + SalesOnDay

		public decimal ClosingStock { get; set; }     // TotalStock - TotalSales
		public double SalesPct { get; set; }          // TotalSales / TotalStock * 100
	}
}