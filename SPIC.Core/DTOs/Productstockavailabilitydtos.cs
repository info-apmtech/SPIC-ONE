// ============================================================================
//  SPIC.Core / DTOs / ProductStockAvailabilityDtos.cs
//  DTOs for the Product-wise Stock Availability report.
//
//  Shape: a State x Product PIVOT.
//    - Columns  = the products present in scope (each carries a Group band).
//    - Grid     = one row per State (Quantities keyed by ProductId).
//    - GrandTotal = the totals row (sum across ALL filtered states, not just the page).
//
//  Reuses the shared PagedResult<T> from StockReportDtos.cs.
// ============================================================================
using System;
using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
	/// <summary>One POST drives the whole page: filters + grid paging/sort/search.</summary>
	public class ProductStockAvailabilityFilter
	{
		public DateTime? DateFrom { get; set; }
		public DateTime? DateTo { get; set; }

		// Region/HeadQuarter are resolved down to StateIds by the view's cascade
		// (these stock tables only carry StateId), so the service filters on StateIds.
		public List<int> StateIds { get; set; } = new();
		public List<int> RegionIds { get; set; } = new();
		public List<int> HeadQuarterIds { get; set; } = new();

		public string? Search { get; set; }        // matches State name
		public string? SortColumn { get; set; }     // "state" | "total"
		public string? SortDir { get; set; } = "asc";

		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 16;
	}

	public class ProductStockAvailabilityDto
	{
		public ProdStockSummaryDto Summary { get; set; } = new();
		public List<ProdStockColumnDto> Columns { get; set; } = new();   // ordered by Group, then ProductName
		public ProdStockStateRowDto GrandTotal { get; set; } = new();    // totals across ALL filtered states
		public PagedResult<ProdStockStateRowDto> Grid { get; set; } = new();
	}

	/// <summary>The five KPI cards.</summary>
	public class ProdStockSummaryDto
	{
		public int TotalStates { get; set; }
		public int TotalProducts { get; set; }
		public decimal TotalQuantity { get; set; }
		public string HighestStockState { get; set; } = "-";
		public decimal HighestStockQuantity { get; set; }
		public int LowStockAlerts { get; set; }   // states whose total is below the low-stock threshold
	}

	/// <summary>A product column in the pivot. Group drives the top header band.</summary>
	public class ProdStockColumnDto
	{
		public int ProductId { get; set; }
		public string ProductName { get; set; } = "";
		// Category band, e.g. "Normal Products" / "Imported Products" / "Others".
		// Defaults to "Products" until a category source exists on the Product entity.
		public string Group { get; set; } = "Products";
	}

	/// <summary>One state's row of the pivot. Quantities is ProductId -> MT.</summary>
	public class ProdStockStateRowDto
	{
		public int StateId { get; set; }
		public string StateName { get; set; } = "";
		public Dictionary<int, decimal> Quantities { get; set; } = new();
		public decimal Total { get; set; }
	}
}