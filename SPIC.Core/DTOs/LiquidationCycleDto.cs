using System;
using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
	public class LiqCycleFilter
	{
		public DateTime? DateFrom { get; set; }
		public DateTime? DateTo { get; set; }
		public List<int> StateIds { get; set; } = new();
		public List<int> DistrictIds { get; set; } = new();
		public List<int> ProductIds { get; set; } = new();
		public List<int> StatusIds { get; set; } = new();
		public List<string> DealerKeys { get; set; } = new();

		public string? Source { get; set; } // "Company Sales" | "Wholesaler Sales"
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
		public PagedResult<LiqCycleRowDto> Grid { get; set; } = new();
	}

	public class LiqCycleSummaryDto
	{
		public decimal TotalStock { get; set; }
		public decimal Liquidated { get; set; }
		public decimal BalanceStock => TotalStock - Liquidated;

		// Mock trend percentages for UI
		public double StockTrendPct { get; set; } = 12.0;
		public double LiquidatedTrendPct { get; set; } = 7.2;
		public double BalanceTrendPct { get; set; } = 6.1;
	}

	public class LiqCycleStatDto
	{
		public string DealerName { get; set; } = "";
		public decimal TotalStock { get; set; }
		public decimal FastLiquidated { get; set; }
		public decimal SlowLiquidated { get; set; }
		public double Rate { get; set; } // 0-100%
	}

	public class LiqCycleRowDto
	{
		public int Id { get; set; }
		public string Source { get; set; } = "";
		public string DealerName { get; set; } = "";
		public string DealerCode { get; set; } = "";
		public string DealerType { get; set; } = "";
		public string ProductName { get; set; } = "";
		public string StateName { get; set; } = "";
		public string District { get; set; } = "";
		public string MobileNo { get; set; } = "";

		public decimal Stock { get; set; }
		public decimal Sales { get; set; }
		public int AgeingDays { get; set; }

		public string Bucket { get; set; } = ""; // Fast, Normal, Slow, Critical
		public string Status { get; set; } = ""; // Active, Monitoring, Critical
	}
}