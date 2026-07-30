// ============================================================================
//  SPIC.Core / DTOs / AckCycleDtos.cs
//  DTOs for the Acknowledgement Cycle Report (invoice -> retailer-receipt cycle).
//  Mirrors the PendingAck DTO conventions and reuses the shared PagedResult<T>.
// ============================================================================
using System;
using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
	/// <summary>
	/// Single request object for the dashboard POST. Carries every filter plus
	/// the grid paging/sort/search state so one round trip drives the whole page.
	/// </summary>
	public class AckCycleFilter
	{
		// ---- Filters (apply to KPIs, Top-5 lists AND grid) ----
		public DateTime? DateFrom { get; set; }
		public DateTime? DateTo { get; set; }

		public List<int> StateIds { get; set; } = new();
		public List<int> DistrictIds { get; set; } = new();
		public List<int> ProductIds { get; set; } = new();
		public List<int> StatusIds { get; set; } = new();

		// Keyed dealer scheme, same as everywhere else: "R{id}" = DealerRegistration, "I{id}" = IfmsDealer.
		public List<string> DealerKeys { get; set; } = new();

		// ---- Grid-only knobs (do NOT affect the KPI cards / charts) ----
		// "All" | "Company Sales" | "Wholesaler Sales"
		public string? Source { get; set; }
		// Multi-select cycle buckets: any of "Fast" | "Normal" | "Delayed" | "Critical". Empty = all.
		public List<string> Buckets { get; set; } = new();

		public string? Search { get; set; }
		// Top-5 chart grouping dimension: "State" | "Product" | "Dealer".
		public string GroupBy { get; set; } = "State";
		public string? SortColumn { get; set; }   // dealer|product|invoiceno|invoicedate|receiptdate|cycledays|status
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

	/// <summary>The five KPI cards.</summary>
	public class AckCycleSummaryDto
	{
		public int Total { get; set; }      // total ACKNOWLEDGED transactions in scope
		public int Fast { get; set; }       // cycle 0-2 days
		public int Normal { get; set; }     // cycle 3-5 days
		public int Delayed { get; set; }    // cycle 6-10 days
		public int Critical { get; set; }   // cycle > 10 days
		public double AverageCycleDays { get; set; }

		// Convenience shares (0-100), useful for the card sub-text.
		public double FastPct => Total == 0 ? 0 : Math.Round(Fast * 100.0 / Total, 1);
		public double NormalPct => Total == 0 ? 0 : Math.Round(Normal * 100.0 / Total, 1);
		public double DelayedPct => Total == 0 ? 0 : Math.Round(Delayed * 100.0 / Total, 1);
		public double CriticalPct => Total == 0 ? 0 : Math.Round(Critical * 100.0 / Total, 1);
	}

	/// <summary>One row of the Top-5 state lists (with the tooltip breakdown).</summary>
	public class AckCycleStateStatDto
	{
		public string StateName { get; set; } = "";
		public int Total { get; set; }
		public int Fast { get; set; }
		public int Normal { get; set; }
		public int Delayed { get; set; }
		public int Critical { get; set; }
		public double Rate { get; set; }    // 0-100. Fast-rate for the fast list, delay-rate for the delayed list.
	}

	public class AckCycleRowDto
	{
		public int SNo { get; set; }
		public int Id { get; set; }
		public string Source { get; set; } = "";      // "Company Sales" | "Wholesaler Sales"
		public string TransactionId { get; set; } = "";
		public string DealerName { get; set; } = "";
		public string DealerCode { get; set; } = "";
		public string ProductName { get; set; } = "";
		public string InvoiceNo { get; set; } = "";
		public DateTime? InvoiceDate { get; set; }
		public DateTime? EntryDate { get; set; }
		public DateTime? ReceiptDate { get; set; }
		public int CycleDays { get; set; }
		public string Bucket { get; set; } = "";       // Fast | Normal | Delayed | Critical
		public string StateName { get; set; } = "";
		public string District { get; set; } = "";
		public string WorkflowStatus { get; set; } = "";
		public decimal QuantityMT { get; set; }
		public decimal ReceivedQuantity { get; set; }
		public string? DdNo { get; set; }
		public string? MobileNo { get; set; }
	}

	/// <summary>Generic {Id,Name} shape for the filter dropdowns (dealer Id is the keyed string).</summary>
	public class AckLookupItemDto
	{
		public string Id { get; set; } = "";
		public string Name { get; set; } = "";
	}
}