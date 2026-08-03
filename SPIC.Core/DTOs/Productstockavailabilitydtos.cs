// ============================================================================
//  SPIC.Core / DTOs / ProductStockAvailabilityDtos.cs
//  DTOs for the Product-wise Stock Availability report.
//
//  Shape: State x Product pivot.
//    - Columns    : products present in the selected stock snapshot.
//    - Grid       : one row per State, quantities keyed by ProductId.
//    - GrandTotal : totals across every filtered state, independent of paging.
//
//  Reuses PagedResult<T> from StockReportDtos.cs.
// ============================================================================

using System;
using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
	public class ProductStockAvailabilityFilter
	{
		public DateTime? DateFrom { get; set; }
		public DateTime? DateTo { get; set; }

		// The Razor cascade resolves Region/HQ selections into StateIds.
		public List<int> StateIds { get; set; } = new();
		public List<int> RegionIds { get; set; } = new();
		public List<int> HeadQuarterIds { get; set; } = new();

		public string? Search { get; set; }
		public string? SortColumn { get; set; }
		public string? SortDir { get; set; } = "asc";

		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 16;
	}

	public class ProductStockAvailabilityDto
	{
		public ProdStockSummaryDto Summary { get; set; } = new();
		public List<ProdStockColumnDto> Columns { get; set; } = new();
		public ProdStockStateRowDto GrandTotal { get; set; } = new();
		public PagedResult<ProdStockStateRowDto> Grid { get; set; } = new();
	}

	public class ProdStockSummaryDto
	{
		public int TotalStates { get; set; }
		public int TotalProducts { get; set; }
		public decimal TotalQuantity { get; set; }
		public string HighestStockState { get; set; } = "-";
		public decimal HighestStockQuantity { get; set; }
		public int LowStockAlerts { get; set; }
	}

	public class ProdStockColumnDto
	{
		public int ProductId { get; set; }
		public string ProductName { get; set; } = "";
		public string Group { get; set; } = "Products";
	}

	public class ProdStockStateRowDto
	{
		public int StateId { get; set; }
		public string StateName { get; set; } = "";
		public Dictionary<int, decimal> Quantities { get; set; } = new();
		public decimal Total { get; set; }
	}
}