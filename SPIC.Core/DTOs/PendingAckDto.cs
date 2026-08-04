using System;
using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
	public static class AgeStatus
	{
		public const string Completed = "Completed";
		public const string Latest = "Latest";
		public const string Critical = "Critical";
		public const string Overdue = "Overdue";
		public const string ConsentOfBuyer = "Consent of Buyer";
	}

	public class PendingAckFilter
	{
		public DateTime? DateFrom { get; set; }
		public DateTime? DateTo { get; set; }

		public List<int> StateIds { get; set; } = new();
		public List<int> DistrictIds { get; set; } = new();
		public List<int> DealerTypeIds { get; set; } = new();
		public List<int> ProductIds { get; set; } = new();
		public List<string> DealerKeys { get; set; } = new();

		public string? Source { get; set; }
		public List<string> AgeStatuses { get; set; } = new();

		public string? Search { get; set; }
		public string? SortColumn { get; set; }
		public string? SortDir { get; set; } = "desc";
		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 16;
	}

	public class PendingAckDashboardDto
	{
		public PendingAckCategorySummaryDto Summary { get; set; } = new();
		public PendingAckCategorySummaryDto Overall { get; set; } = new();
		public PendingAckCategorySummaryDto CompanySales { get; set; } = new();
		public PendingAckCategorySummaryDto WholesalerSales { get; set; } = new();

		// Kept for API compatibility. The UI displays this as "Retailer Sales".
		public PendingAckCategorySummaryDto DptSales { get; set; } = new();

		public List<PendingAckStateWiseDto> StateWise { get; set; } = new();
		public PagedResult<PendingAckRowDto> Grid { get; set; } = new();
	}

	public class PendingAckCategorySummaryDto
	{
		public int TotalCount { get; set; }
		public decimal TotalQuantity { get; set; }

		public int CompletedCount { get; set; }
		public decimal CompletedQuantity { get; set; }

		public int LatestCount { get; set; }
		public decimal LatestQuantity { get; set; }

		public int CriticalCount { get; set; }
		public decimal CriticalQuantity { get; set; }

		public int OverdueCount { get; set; }
		public decimal OverdueQuantity { get; set; }

		public int ConsentBuyerCount { get; set; }
		public decimal ConsentBuyerQuantity { get; set; }

		public int Total => TotalCount;
		public int Completed => CompletedCount;
		public int Latest => LatestCount;
		public int Critical => CriticalCount;
		public int Overdue => OverdueCount;
		public int ConsentBuyer => ConsentBuyerCount;

		public int CompanyTotal { get; set; }
		public int CompanyCompleted { get; set; }
		public int CompanyLatest { get; set; }
		public int CompanyCritical { get; set; }
		public int CompanyOverdue { get; set; }
		public int CompanyConsentBuyer { get; set; }

		public int WholesalerTotal { get; set; }
		public int WholesalerCompleted { get; set; }
		public int WholesalerLatest { get; set; }
		public int WholesalerCritical { get; set; }
		public int WholesalerOverdue { get; set; }
		public int WholesalerConsentBuyer { get; set; }

		// Kept for API compatibility. These values represent Retailer Sales.
		public int DptTotal { get; set; }
		public int DptCompleted { get; set; }
		public int DptLatest { get; set; }
		public int DptCritical { get; set; }
		public int DptOverdue { get; set; }
		public int DptConsentBuyer { get; set; }
	}

	public class PendingAckStateWiseDto
	{
		public int StateId { get; set; }
		public string StateName { get; set; } = string.Empty;

		public int LatestCount { get; set; }
		public decimal LatestQuantity { get; set; }

		public int CriticalCount { get; set; }
		public decimal CriticalQuantity { get; set; }

		public int OverdueCount { get; set; }
		public decimal OverdueQuantity { get; set; }

		public int ConsentBuyerCount { get; set; }
		public decimal ConsentBuyerQuantity { get; set; }

		public int CompletedCount { get; set; }
		public decimal CompletedQuantity { get; set; }

		public int TotalPendingCount =>
			LatestCount + CriticalCount + OverdueCount + ConsentBuyerCount;

		public decimal TotalPendingQuantity =>
			LatestQuantity + CriticalQuantity + OverdueQuantity + ConsentBuyerQuantity;

		public int Latest => LatestCount;
		public int Critical => CriticalCount;
		public int Overdue => OverdueCount;
		public int ConsentBuyer => ConsentBuyerCount;
		public int Completed => CompletedCount;
		public int Total =>
			LatestCount + CriticalCount + OverdueCount + ConsentBuyerCount + CompletedCount;
		public int TotalPending =>
			LatestCount + CriticalCount + OverdueCount + ConsentBuyerCount;
	}

	public class PendingAckRowDto
	{
		public int SNo { get; set; }
		public int SalesId { get; set; }
		public string Source { get; set; } = string.Empty;

		public string? TransactionId { get; set; }
		public string? InvoiceNo { get; set; }
		public DateTime? InvoiceDate { get; set; }
		public DateTime? EntryDate { get; set; }

		public string? AgencyName { get; set; }
		public string? DealerCode { get; set; }
		public string? DealerType { get; set; }
		public string? MobileNo { get; set; }

		public string? StateName { get; set; }
		public string? District { get; set; }
		public string? ProductName { get; set; }

		public int? StateId { get; set; }
		public int? DistrictId { get; set; }
		public int? ProductId { get; set; }

		public decimal QuantityMT { get; set; }
		public decimal ReceivedQuantity { get; set; }
		public string? DdNo { get; set; }
		public string? DispatchNo { get; set; }

		public int PendingAckAgeDays { get; set; }
		public string AgeStatus { get; set; } = string.Empty;

		// Exact Status master value uploaded from Excel for company/wholesaler rows.
		// Retailer Sales (DPT) has no StatusId, so it displays "Reported".
		public string WorkflowStatus { get; set; } = "New";
		public string BuyerConsentStatus { get; set; } = "Not Required";
	}

	public class PendingAckDealerTypeDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}

	public class PendingAckDealerDto
	{
		public string Key { get; set; } = string.Empty;

		public string Id
		{
			get => Key;
			set => Key = value;
		}

		public string Name { get; set; } = string.Empty;
	}
}