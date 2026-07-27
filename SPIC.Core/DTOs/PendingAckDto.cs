using System;
using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
	// ============================================================
	//  Pending Acknowledgement report — shared contract.
	//  Mirrors the StockReport DTOs. The grid reuses your EXISTING
	//  PagedResult<T> (same one StockReport returns) — do NOT redefine it here.
	// ============================================================

	public class PendingAckFilter
	{
		// Multi-select filters (Select2)
		public List<int> StateIds { get; set; } = new();
		public List<int> DistrictIds { get; set; } = new();
		public List<int> DealerTypeIds { get; set; } = new();
		public List<int> ProductIds { get; set; } = new();
		// Dealer / agency comes from TWO tables, so each value is a keyed string:
		//   "R{id}" = DealerRegistrations.Id,  "I{id}" = IfmsDealers.Id
		public List<string> DealerKeys { get; set; } = new();
		public List<string> AgeStatuses { get; set; } = new();     // Fresh/Pending/Critical/Overdue/Completed

		// Tab: "All" | "Company Sales" | "Wholesaler Sales"
		public string Source { get; set; } = "All";

		// Date range on InvoiceDate
		public DateTime? DateFrom { get; set; }
		public DateTime? DateTo { get; set; }

		// Grid
		public string? Search { get; set; }
		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 16;
		public string SortColumn { get; set; } = "age";
		public string SortDir { get; set; } = "desc";
	}

	public class PendingAckDashboardDto
	{
		public PendingAckSummaryDto Summary { get; set; } = new();
		public List<PendingAckStateDto> StateWise { get; set; } = new();
		public PagedResult<PendingAckRowDto> Grid { get; set; } = new();   // your existing PagedResult<T>
	}

	// KPI cards. "Overall" respects every filter except the Source tab
	// and the Age-status filter, so the three cards stay stable.
	public class PendingAckSummaryDto
	{
		public int Completed { get; set; }
		public int Critical { get; set; }
		public int Overdue { get; set; }
		public int ConsentBuyer { get; set; }

		public int CompanyTotal { get; set; }
		public int CompanyCompleted { get; set; }
		public int CompanyCritical { get; set; }
		public int CompanyOverdue { get; set; }

		public int WholesalerTotal { get; set; }
		public int WholesalerCompleted { get; set; }
		public int WholesalerCritical { get; set; }
		public int WholesalerOverdue { get; set; }
	}

	public class PendingAckStateDto
	{
		public string StateName { get; set; } = "";
		public int Completed { get; set; }
		public int Overdue { get; set; }
		public int Critical { get; set; }
		public int Total => Completed + Overdue + Critical;
	}

	public class PendingAckRowDto
	{
		public int Id { get; set; }
		public string TransactionId { get; set; } = "";
		public string InvoiceNo { get; set; } = "";
		public DateTime? InvoiceDate { get; set; }
		public string AgencyName { get; set; } = "";
		public string DealerCode { get; set; } = "";     // dealer id shown in the drawer
		public string Source { get; set; } = "";        // "Company Sales" / "Wholesaler Sales"
		public string DealerType { get; set; } = "";     // "Retailer" / "Wholesaler"
		public string StateName { get; set; } = "";
		public string District { get; set; } = "";
		public string ProductName { get; set; } = "";
		public decimal QuantityMT { get; set; }
		public decimal ReceivedQuantity { get; set; }
		public string AgeStatus { get; set; } = "";      // Fresh/Pending/Critical/Overdue/Completed
		public int PendingAckAgeDays { get; set; }
		public string WorkflowStatus { get; set; } = ""; // New / Consent Buyer / Acknowledged
		public DateTime? EntryDate { get; set; }
		public string? DdNo { get; set; }
		public string? DispatchNo { get; set; }
		public string? MobileNo { get; set; }
	}

	// Filter-option payloads for the two custom lookup endpoints.
	public class PendingAckDealerTypeDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = "";
	}

	public class PendingAckDealerDto
	{
		public string Id { get; set; } = "";   // keyed: "R{id}" (registration) or "I{id}" (ifms)
		public string Name { get; set; } = "";
	}

	// NOTE: PagedResult<T> already exists in SPIC.Core.DTOs (StockReport uses it).
	// Nothing to add here for it.
}