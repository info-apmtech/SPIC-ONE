using System;
using System.Collections.Generic;
using SPIC.Core.Entities;

namespace SPIC.Core.DTOs
{
	/// <summary>
	/// Server-side Dashboard (/Dashboard) query. Mirrors, one for one, the filter
	/// and sort controls on Shared/Pages/Dashboard.razor so the page can ask the API
	/// for a single page of rows plus a summary instead of downloading every
	/// submitted registration and filtering/counting in the browser.
	///
	/// The location scope (state / region / HQ) is NEVER taken from this query: it is
	/// always derived on the server from the signed-in user's JWT claims, exactly as
	/// Dashboard.razor's ApplyRoleFilter derives it from LoginState.
	/// </summary>
	public class DealerDashboardQuery
	{
		/// <summary>1-based page number.</summary>
		public int Page { get; set; } = 1;

		/// <summary>Rows per page. Matches the page's card batch size (30).</summary>
		public int PageSize { get; set; } = 30;

		/// <summary>Free text matched against Dealer Code and Firm Name (case-insensitive).</summary>
		public string? Search { get; set; }

		/// <summary>"" / null = All, "SPIC", "GreenStar", "Both".</summary>
		public string? Company { get; set; }

		/// <summary>"" / null = all, "new", "existing".</summary>
		public string? RegistrationType { get; set; }

		/// <summary>-1 = All, otherwise the DealerStatus value (0 Active, 1 Inactive, 2 Terminated).</summary>
		public int Status { get; set; } = -1;

		/// <summary>0 = All Regions, otherwise DealerRegistration.Region.</summary>
		public int RegionId { get; set; }

		/// <summary>
		/// The same strings the page's "All Approval Status" dropdown uses:
		/// "All", "Pending Draft", "Pending RM", "Pending SMM", "Pending AVP",
		/// "Approved", "Rejected/Returned".
		/// </summary>
		public string? WorkflowStatus { get; set; } = "All";

		/// <summary>
		/// "newest" (also accepted: "recent" — the page's own value), "oldest", "name".
		/// Every sort is preceded by the "pending for me first" ordering, which depends
		/// on the signed-in user's role.
		/// </summary>
		public string? Sort { get; set; } = "newest";
	}

	/// <summary>
	/// One dealer card. Field-for-field the same projection the existing
	/// GET api/DealerRegistration/submitted endpoint returns, so the Dashboard's
	/// DealerDto deserialises unchanged, plus StepCount (see below).
	/// </summary>
	public class DealerDashboardItemDto
	{
		public int Id { get; set; }
		public bool IsDealer { get; set; }
		public bool InSpic { get; set; }
		public bool InGreenStar { get; set; }
		public bool IsNewDealerRegistration { get; set; }
		public bool HasCompletedExistingMaintenance { get; set; }
		public string? DealerCode { get; set; }
		public string? CreatedBy { get; set; }
		public string? SPICCode { get; set; }
		public string? GreenStarCode { get; set; }
		public string? NCode { get; set; }
		public string? TnCode { get; set; }
		public int StateId { get; set; }
		public int Region { get; set; }
		public int HQ { get; set; }
		public int Status { get; set; }
		public FutureBusinessProposal? InactiveProposal { get; set; }
		public string? FirmName { get; set; }
		public string? BusinessEntityType { get; set; }
		public int? EntityType { get; set; }
		public string? WholeSaleFertilizerLicenseNumber { get; set; }
		public string? RetailFertilizerLicenseNumber { get; set; }
		public string? PinCode { get; set; }
		public double? Latitude { get; set; }
		public double? Longitude { get; set; }
		public bool? RMApproved { get; set; }
		public bool? SMApproved { get; set; }
		public bool? AVPApproved { get; set; }
		public bool? IsSubmittedForReview { get; set; }
		public bool? IsFinalAmountSettled { get; set; }
		public DateTime UpdatedAt { get; set; }

		/// <summary>
		/// Registration step-completion count for THIS row only — the same number
		/// GET api/DealerRegistration/dashboard-completion-counts returns for the
		/// whole table. Shipping it per row lets the Dashboard drop that whole-table
		/// call entirely.
		/// </summary>
		public int StepCount { get; set; }
	}

	/// <summary>One page of dealer cards plus the total row count for the same filter.</summary>
	public class DealerDashboardPageDto
	{
		/// <summary>Total rows matching the filter and the caller's scope (all pages).</summary>
		public int Total { get; set; }

		public int Page { get; set; }
		public int PageSize { get; set; }

		public List<DealerDashboardItemDto> Items { get; set; } = new();
	}

	/// <summary>
	/// Every number the Dashboard's KPI tiles and status counters show, computed over
	/// the SAME filtered, role-scoped set the page endpoint counts.
	///
	/// The definitions deliberately reproduce the page's own client-side counters,
	/// including their small inconsistencies:
	///   • Draft excludes Terminated rows (CountDraft).
	///   • InRM folds in "Rejected by SMM", InSMM folds in "Rejected by AVP".
	///   • Approved / Rejected* are raw approval-flag counts and are NOT gated on
	///     IsSubmittedForReview, unlike the "Approved" / "Rejected/Returned" values
	///     of the WorkflowStatus filter.
	/// </summary>
	public class DealerDashboardSummaryDto
	{
		/// <summary>"Total Registrations" tile — the filtered row count.</summary>
		public int TotalRegistrations { get; set; }

		/// <summary>"Draft" tile: Status != Terminated and approval status is Draft.</summary>
		public int Draft { get; set; }

		/// <summary>"In RM" tile: In RM or Rejected by SMM.</summary>
		public int InRM { get; set; }

		/// <summary>"In SMM" tile: In SMM or Rejected by AVP.</summary>
		public int InSMM { get; set; }

		/// <summary>"In AVP" tile.</summary>
		public int InAVP { get; set; }

		/// <summary>"Approved" tile: AVPApproved == true.</summary>
		public int Approved { get; set; }

		/// <summary>"Rejected / Returned" tile: any of the three approval flags is false.</summary>
		public int Rejected { get; set; }

		public int RejectedByRM { get; set; }
		public int RejectedBySMM { get; set; }
		public int RejectedByAVP { get; set; }

		/// <summary>Status tile: Active.</summary>
		public int StatusActive { get; set; }

		/// <summary>Status tile: Inactive.</summary>
		public int StatusInactive { get; set; }

		/// <summary>Status tile: Terminated.</summary>
		public int StatusTerminated { get; set; }

		/// <summary>DealerStatus value to row count, for any caller that wants the raw map.</summary>
		public Dictionary<int, int> ByStatus { get; set; } = new();
	}
}
