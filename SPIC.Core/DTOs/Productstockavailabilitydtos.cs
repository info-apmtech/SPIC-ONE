// ============================================================================
//  SPIC.Core / DTOs / ProductStockAvailabilityDtos.cs
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
		public decimal TotalQuantity { get; set; }
		public decimal TotalSales { get; set; }
		public string HighestStockState { get; set; } = "-";
		public decimal HighestStockQuantity { get; set; }
		public int LowStockAlerts { get; set; }
	}

	public class ProdStockColumnDto
	{
		// Positive = Product.Id, negative = -IfmsProduct.Id.
		public int ProductId { get; set; }
		public int? ApprovedProductId { get; set; }
		public int? IfmsProductId { get; set; }
		public bool IsIfmsProduct => IfmsProductId.HasValue;
		public string ProductName { get; set; } = "";
		public string Group { get; set; } = "Products";
	}

	public class ProdStockStateRowDto
	{
		public int StateId { get; set; }
		public string StateName { get; set; } = "";
		public Dictionary<int, decimal> Quantities { get; set; } = new();
		public Dictionary<int, decimal> SalesQuantities { get; set; } = new();
		public decimal Total { get; set; }
		public decimal TotalSales { get; set; }
	}
}