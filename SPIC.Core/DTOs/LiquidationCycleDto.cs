using System;
using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
	public class LiqCycleFilter
	{
		public DateTime? DateFrom { get; set; }
		public DateTime? DateTo { get; set; }

		public List<int> StateIds { get; set; } = new();

		// Additive geography filters. Existing State/District clients remain compatible.
		// Registered dealers are matched through DealerRegistration.Region / HQ.
		// Warehouse rows are matched through Warehouse.RegionId / HeadquarterId.
		public List<int> RegionIds { get; set; } = new();
		public List<int> HeadQuarterIds { get; set; } = new();

		public List<int> DistrictIds { get; set; } = new();

		// DPT/Retailer rows have SubDistrictId. Other sources continue to use the
		// effective parent DistrictIds so the previous flow remains compatible.
		public List<int> SubDistrictIds { get; set; } = new();

		// Backward-compatible approved Product IDs used by existing clients.
		public List<int> ProductIds { get; set; } = new();

		// Combined product keys used by the updated dropdown:
		// P:10 = Product.Id 10, I:10 = IfmsProduct.Id 10.
		public List<string> ProductKeys { get; set; } = new();

		public List<int> StatusIds { get; set; } = new();
		public List<string> DealerKeys { get; set; } = new();

		public string? Source { get; set; }
		public string? Search { get; set; }
		public string? SortColumn { get; set; }
		public bool SortDesc { get; set; } = true;

		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 16;
	}

	public class LiqCycleDashboardDto
	{
		public LiqCycleSummaryDto Summary { get; set; } = new();
		public List<LiqCycleStatDto> TopFastDealers { get; set; } = new();
		public List<LiqCycleStatDto> TopSlowDealers { get; set; } = new();

		// Existing State-wise chart contract.
		public List<LiqCycleStatDto> TopFastStates { get; set; } = new();
		public List<LiqCycleStatDto> TopSlowStates { get; set; } = new();

		public PagedResult<LiqCycleRowDto> Grid { get; set; } = new();
	}

	public class LiqCycleSummaryDto
	{
		public decimal TotalStock { get; set; }
		public decimal Liquidated { get; set; }
		public decimal BalanceStock => TotalStock - Liquidated;

		// Kept for API compatibility. The service does not invent trend percentages.
		public double StockTrendPct { get; set; }
		public double LiquidatedTrendPct { get; set; }
		public double BalanceTrendPct { get; set; }
	}

	public class LiqCycleStatDto
	{
		// Retained name for API compatibility. State-wise statistics also use this
		// property as their display label in the existing chart contract.
		public string DealerName { get; set; } = "";
		public decimal TotalStock { get; set; }
		public decimal FastLiquidated { get; set; }
		public decimal SlowLiquidated { get; set; }
		public double Rate { get; set; }
	}

	public class LiqCycleRowDto
	{
		public int Id { get; set; }
		public string Source { get; set; } = "";
		public string DealerName { get; set; } = "";
		public string DealerCode { get; set; } = "";
		public string DealerType { get; set; } = "";

		public int? ProductId { get; set; }
		public int? IfmsProductId { get; set; }

		public string ProductKey => ProductId.HasValue
			? $"P:{ProductId.Value}"
			: IfmsProductId.HasValue
				? $"I:{IfmsProductId.Value}"
				: string.Empty;

		public string ProductName { get; set; } = "";
		public string StateName { get; set; } = "";
		public string District { get; set; } = "";
		public string MobileNo { get; set; } = "";

		public decimal Stock { get; set; }
		public decimal Sales { get; set; }
		public int AgeingDays { get; set; }

		public string Bucket { get; set; } = "";
		public string Status { get; set; } = "";
	}

	public class LiqCycleProductDto
	{
		public string Key { get; set; } = string.Empty;

		// Alias retained for controls or clients that expect an Id property.
		public string Id
		{
			get => Key;
			set => Key = value;
		}

		public string Name { get; set; } = string.Empty;
		public string Source { get; set; } = string.Empty;
	}
}
