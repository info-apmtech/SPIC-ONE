// ============================================================================
//  SPIC.Core / DTOs / AgeingReportDto.cs
//  Single source of truth for all Ageing Report DTOs.
// ============================================================================

using System;
using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
	public class AgeingReportFilter
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
		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 16;
		public string SortColumn { get; set; } = "ageing";
		public string SortDir { get; set; } = "desc";
	}

	public class AgeingDashboardDto
	{
		public AgeingSummaryDto Summary { get; set; } = new();
		public List<AgeingStateDto> StateWise { get; set; } = new();
		public List<AgeingStateBucketDto> StateBuckets { get; set; } = new();
		public List<AgeingBucketDto> DayBuckets { get; set; } = new();
		public PagedResult<AgeingRowDto> Grid { get; set; } = new();
	}

	public class AgeingSummaryDto
	{
		public decimal TotalStock { get; set; }
		public decimal TotalStockChangePct { get; set; }
		public double AverageAgeing { get; set; }
		public double AverageAgeingChange { get; set; }
		public decimal Stock30To60 { get; set; }
		public decimal Stock30To60ChangePct { get; set; }
		public decimal Stock60Plus { get; set; }
		public decimal Stock60PlusChangePct { get; set; }
	}

	public class AgeingStateDto
	{
		public string StateName { get; set; } = string.Empty;
		public decimal Stock { get; set; }
		public decimal Sales { get; set; }
	}

	public class AgeingStateBucketDto
	{
		public string StateName { get; set; } = string.Empty;
		public decimal Fresh { get; set; }
		public decimal Medium { get; set; }
		public decimal SlowMoving { get; set; }
		public decimal LongAged { get; set; }
		public decimal Critical { get; set; }
		public decimal Total => Fresh + Medium + SlowMoving + LongAged + Critical;
	}

	public class AgeingBucketDto
	{
		public string Label { get; set; } = string.Empty;
		public string Category { get; set; } = string.Empty;
		public decimal Stock { get; set; }
		public double Percentage { get; set; }
		public string Color { get; set; } = string.Empty;
	}

	public class AgeingRowDto
	{
		public int? DealerRegistrationId { get; set; }
		public string StateName { get; set; } = string.Empty;
		public string DealerName { get; set; } = string.Empty;
		public string ProductName { get; set; } = string.Empty;
		public decimal Quantity { get; set; }
		public int AgeingDays { get; set; }
		public string Status { get; set; } = string.Empty;
		public string? MobileNo { get; set; }
		public string? DealerCode { get; set; }
		public string? HeadQuarterName { get; set; }
		public string? DistrictName { get; set; }
		public string? SubDistrictName { get; set; }
		public DateTime? EntryDate { get; set; }
	}
}