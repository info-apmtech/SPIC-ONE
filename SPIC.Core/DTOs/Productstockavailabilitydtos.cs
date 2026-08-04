// ============================================================================
//  SPIC.Core / DTOs / ProductStockAvailabilityDtos.cs
//
//  State x Product pivot for current stock and sales.
//
//  Current stock sources:
//    * WholesalerStockAsOnToday.Stock
//    * DptReport.ClosingBalance
//    * WarehouseDistrictGlobalStockReconciliation.ClosingStock
//
//  Sales sources:
//    * SalesWholesaler.QuantityMT
//    * SalesCompanySale.QuantityMT
//    * DptReport.SoldQuantity from the latest DPT snapshot
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

		// Backward-compatible name: this is the current stock total.
		public decimal TotalQuantity { get; set; }

		public decimal TotalSales { get; set; }
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

		// Current stock values by ProductId.
		public Dictionary<int, decimal> Quantities { get; set; } = new();

		// Sales values by ProductId.
		public Dictionary<int, decimal> SalesQuantities { get; set; } = new();

		// Current stock total for the state.
		public decimal Total { get; set; }

		// Sales total for the state.
		public decimal TotalSales { get; set; }
	}
}