using System;
using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
	// ============================================================
	//  Ageing Report — shared contract. Same data source as StockReport
	//  (WholesalerStockAsOnToday). Grid reuses your existing PagedResult<T>.
	// ============================================================

	public class AgeingReportFilter
	{
		public List<int> StateIds { get; set; } = new();
		public List<int> RegionIds { get; set; } = new();
		public List<int> HeadQuarterIds { get; set; } = new();
		public List<int> DistrictIds { get; set; } = new();
		public List<int> SubDistrictIds { get; set; } = new();
		public List<int> LyingWithIds { get; set; } = new();      // DealershipNatureId
		public List<int> ProductIds { get; set; } = new();
		public List<string> AgeingRanges { get; set; } = new();   // "0-30","31-60","61-90","91-120","Above 120"

		public string? Search { get; set; }
		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 16;
		public string SortColumn { get; set; } = "ageing";
		public string SortDir { get; set; } = "desc";
	}

	public class AgeingDashboardDto
	{
		public AgeingSummaryDto Summary { get; set; } = new();
		public List<AgeingStateDto> StateWise { get; set; } = new();   // bar chart
		public List<AgeingBucketDto> DayBuckets { get; set; } = new(); // donut + table
		public PagedResult<AgeingRowDto> Grid { get; set; } = new();
	}

	public class AgeingSummaryDto
	{
		public decimal TotalStock { get; set; }
		public decimal TotalStockChangePct { get; set; }

		public double AverageAgeing { get; set; }
		public double AverageAgeingChange { get; set; }

		public decimal Stock30To60 { get; set; }        // ageing > 30 && <= 60
		public decimal Stock30To60ChangePct { get; set; }

		public decimal Stock60Plus { get; set; }        // ageing > 60
		public decimal Stock60PlusChangePct { get; set; }
	}

	// Bar chart: stock per state (Sales left as a placeholder — this table
	// has no sales figure; wire a source later if you want the 2nd series).
	public class AgeingStateDto
	{
		public string StateName { get; set; } = "";
		public decimal Stock { get; set; }
		public decimal Sales { get; set; }   // TODO: no sales column on WholesalerStockAsOnToday
	}

	public class AgeingBucketDto
	{
		public string Label { get; set; } = "";      // "0 - 30 Days", ...
		public string Category { get; set; } = "";    // Fresh / Medium / Slow Moving / Long Aged / Critical
		public decimal Stock { get; set; }
		public double Percentage { get; set; }
		public string Color { get; set; } = "";
	}

	public class AgeingRowDto
	{
		public int? DealerRegistrationId { get; set; }
		public string StateName { get; set; } = "";
		public string DealerName { get; set; } = "";
		public string ProductName { get; set; } = "";
		public decimal Quantity { get; set; }
		public int AgeingDays { get; set; }
		public string Status { get; set; } = "";      // Fresh / Medium / Slow Moving / Dead Stock
		public string? MobileNo { get; set; }

		// Add these missing properties:
		public string? DealerCode { get; set; }
		public string? HeadQuarterName { get; set; }
		public string? DistrictName { get; set; }
		public string? SubDistrictName { get; set; }
		public DateTime? EntryDate { get; set; }
	}
}
