using System;
using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
	public class AckCycleFilter
	{
		public DateTime? DateFrom { get; set; }
		public DateTime? DateTo { get; set; }

		public List<int> StateIds { get; set; } = new();
		public List<int> DistrictIds { get; set; } = new();
		public List<int> ProductIds { get; set; } = new();
		public List<int> StatusIds { get; set; } = new();
		public List<string> DealerKeys { get; set; } = new();

		// Grid-only filters. KPI cards and Top-5 lists intentionally ignore these.
		public string? Source { get; set; }
		public List<string> Buckets { get; set; } = new();

		public string? Search { get; set; }
		public string GroupBy { get; set; } = "State";
		public string? SortColumn { get; set; }
		public bool SortDesc { get; set; } = true;

		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 16;
	}

	public class AckCycleDashboardDto
	{
		public AckCycleSummaryDto Summary { get; set; } = new();
		public List<AckCycleStateStatDto> TopFastStates { get; set; } = new();
		public List<AckCycleStateStatDto> TopDelayedStates { get; set; } = new();
		public PagedResult<AckCycleRowDto> Grid { get; set; } = new();
	}

	public class AckCycleSummaryDto
	{
		public int Total { get; set; }
		public int Fast { get; set; }
		public int Normal { get; set; }
		public int Delayed { get; set; }
		public int Critical { get; set; }
		public double AverageCycleDays { get; set; }

		public double FastPct => Total == 0 ? 0 : Math.Round(Fast * 100.0 / Total, 1);
		public double NormalPct => Total == 0 ? 0 : Math.Round(Normal * 100.0 / Total, 1);
		public double DelayedPct => Total == 0 ? 0 : Math.Round(Delayed * 100.0 / Total, 1);
		public double CriticalPct => Total == 0 ? 0 : Math.Round(Critical * 100.0 / Total, 1);
	}

	public class AckCycleStateStatDto
	{
		public string StateName { get; set; } = "";

		private string? _label;
		public string Label
		{
			get => string.IsNullOrEmpty(_label) ? StateName : _label;
			set => _label = value;
		}

		public int Total { get; set; }
		public int Fast { get; set; }
		public int Normal { get; set; }
		public int Delayed { get; set; }
		public int Critical { get; set; }
		public double Rate { get; set; }
	}

	public class AckCycleRowDto
	{
		public int SNo { get; set; }
		public int Id { get; set; }
		public string Source { get; set; } = "";
		public string TransactionId { get; set; } = "";
		public string DealerName { get; set; } = "";
		public string DealerCode { get; set; } = "";
		public string ProductName { get; set; } = "";
		public string InvoiceNo { get; set; } = "";
		public DateTime? InvoiceDate { get; set; }
		public DateTime? EntryDate { get; set; }
		public DateTime? ReceiptDate { get; set; }
		public int CycleDays { get; set; }
		public string Bucket { get; set; } = "";
		public string StateName { get; set; } = "";
		public string District { get; set; } = "";
		public string WorkflowStatus { get; set; } = "";
		public decimal QuantityMT { get; set; }
		public decimal ReceivedQuantity { get; set; }
		public string? DdNo { get; set; }
		public string? MobileNo { get; set; }
	}

	public class AckLookupItemDto
	{
		public string Id { get; set; } = "";
		public string Name { get; set; } = "";
	}
}