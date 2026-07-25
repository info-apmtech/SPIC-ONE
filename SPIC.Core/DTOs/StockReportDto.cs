using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
	// ---- Request ----
	public class StockReportFilter
	{
		public List<int> StateIds { get; set; } = new();
		public List<int> RegionIds { get; set; } = new();
		public List<int> HeadQuarterIds { get; set; } = new();
		public List<int> DistrictIds { get; set; } = new();
		public List<int> SubDistrictIds { get; set; } = new();
		public List<int> LyingWithIds { get; set; } = new();
		public List<int> ProductIds { get; set; } = new();
		public List<string> AgeingRanges { get; set; } = new();

		public string? Search { get; set; }
		public string? SortColumn { get; set; }
		public string? SortDir { get; set; } = "asc";
		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 16;
	}

	// ---- Response ----
	public class StockDashboardDto
	{
		public SummaryDto Summary { get; set; } = new();
		public List<StateStockDto> StateWise { get; set; } = new();
		public List<ProductStockDto> ProductWise { get; set; } = new();
		public PagedResult<StockRowDto> Grid { get; set; } = new();
	}

	public class SummaryDto
	{
		public decimal TotalStock { get; set; }
		public decimal TotalStockChangePct { get; set; }
		public int DealerCount { get; set; }
		public int TodayDealerCount { get; set; }
		public int YesterdayDealerCount { get; set; }
		public double AverageAgeing { get; set; }
		public double AverageAgeingChange { get; set; }
		public int HighAgeingCount { get; set; }
		public int HighAgeingChange { get; set; }
	}

	public class StateStockDto
	{
		public string StateName { get; set; } = "";
		public decimal CurrentYear { get; set; }
		public decimal PreviousYear { get; set; }
	}

	public class ProductStockDto
	{
		public string ProductName { get; set; } = "";
		public decimal Quantity { get; set; }
		public double Percentage { get; set; }
		public string Color { get; set; } = "";
	}

	public class StockRowDto
	{
		public int? DealerRegistrationId { get; set; }
		public string StateName { get; set; } = "";
		public string DealerName { get; set; } = "";
		public string ProductName { get; set; } = "";
		public decimal Quantity { get; set; }
		public string LyingWith { get; set; } = "";
		public int AgeingDays { get; set; }
		public string Status { get; set; } = "";
		public string? MobileNo { get; set; }
	}

	public class PagedResult<T>
	{
		public List<T> Items { get; set; } = new();
		public int TotalCount { get; set; }
		public int Page { get; set; }
		public int PageSize { get; set; } = 16;
	}
}

