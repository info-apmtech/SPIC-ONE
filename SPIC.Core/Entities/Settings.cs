using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace SPIC.Core.Entities
{
    public class Crop
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
    public class Competitor
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
    public class Sector
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
    public class Unit
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
    public class Category
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int UnitId { get; set; }
        public Unit? Unit { get; set; }
        public bool IsSpecialityProduct { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
    public class Product
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public decimal? RPU { get; set; }
        public int? ProductGroupId { get; set; }
        public ProductGroup? ProductGroup { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
    public class ProductGroup
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public enum LogisticsOperatedBy
	{
		Dealer = 1,
		Others = 2
	}

	[JsonConverter(typeof(JsonStringEnumConverter))]
	public enum LogisticsWarehouseType
	{
		FieldWH = 1,
		RakepointWH = 2
	}

	/// <summary>
	/// Category stored on the single Warehouse table.
	/// PVT WH, C&F WH and Both all use the same Warehouse entity/API.
	/// </summary>
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public enum LogisticsWarehouseCategory
	{
		[JsonStringEnumMemberName("PVT")]
		PvtWH = 1,

		[JsonStringEnumMemberName("C&F")]
		CandFWH = 2,

		[JsonStringEnumMemberName("Both")]
		Both = 3
	}

	/// <summary>
	/// Single Warehouse master used by PVT WH, C&F WH and Both.
	/// Category-specific columns stay nullable so one table can safely store
	/// all three warehouse categories without affecting existing flows.
	/// </summary>
	public enum LogisticsType
	{
		Warehouse = 0,
		RakePoint = 1
	}

	/// <summary>
	/// Common approval-history table for Warehouse and Rake Point.
	/// Warehouse/RackPoint current approval fields continue to hold the latest workflow state.
	/// This table stores every Approve / Send Back action and its remarks.
	/// </summary>
	public class LogisticsApprovalHistory
	{
		public int Id { get; set; }	
		public int LogisticsSourceId { get; set; }

		public LogisticsType LogisticsType { get; set; }
		public string ApprovedBy { get; set; } = string.Empty;

		public string Role { get; set; } = string.Empty;

		public DateTime ApprovedAt { get; set; }

		public string Remarks { get; set; } = string.Empty;

		public bool IsApproved { get; set; }
	}


	public class Warehouse
	{
		public int Id { get; set; }
		public required string Name { get; set; }

		// Current Logistics UI label: SAP Code.
		public string WarehouseCode { get; set; } = string.Empty;

		// Single-table category discriminator.
		public LogisticsWarehouseCategory WarehouseCategory { get; set; }
			= LogisticsWarehouseCategory.PvtWH;

		// Company selection.
		public bool? InSpic { get; set; }
		public bool? InGreenStar { get; set; }

		// Basic Information cascading location.
		public int? BasicStateId { get; set; }
		public int? RegionId { get; set; }
		public int? HeadquarterId { get; set; }

		// Shared dropdown.
		public LogisticsOperatedBy? OperatedBy { get; set; }

		// WH Type is used by exact PVT WH and Both. C&F saves null.
		public LogisticsWarehouseType? WarehouseType { get; set; }

		// Primary Location.
		public string? GoogleURL { get; set; }
		public double? Latitude { get; set; }
		public double? Longitude { get; set; }
		public string? DoorNo { get; set; }
		public string? Street { get; set; }
		public string? SubVillage { get; set; }
		public string? PinCode { get; set; }
		public string? Village { get; set; }
		public string? Block { get; set; }
		public string? Taluk { get; set; }
        public int? StateId { get; set; }
        public int? DistrictId { get; set; }
        public string? ContactNumber { get; set; }
		public string? AdditionalPhoneNo { get; set; }

		// PVT WH content. Also used when category = Both.
		public decimal? SpicApprovedReservationQuantityMT { get; set; }
		public decimal? SpicAdditionalReservationQuantityMT { get; set; }
		public decimal? GflApprovedReservationQuantityMT { get; set; }
		public decimal? GflAdditionalReservationQuantityMT { get; set; }

		// C&F WH content. Also used when category = Both.
		public decimal? GflReservationQuantityMT { get; set; }
		public decimal? GflAdditionalReservationQuantityLitres { get; set; }

		// Legacy document-path columns.
		// Exact PVT WH upload is currently inactive;
		// Both can retain the previous document flow.
		public string? GstDocumentPath { get; set; }
		public string? InsuranceDocumentPath { get; set; }
		public string? FertilizerLicenseDocumentPath { get; set; }

		// Optional for all Warehouse categories.
		public string? OtherDocumentPathsJson { get; set; }

		// ---------------------------------------------------------
		// CURRENT Logistics workflow state.
		// Keep these fields.
		//
		// LogisticsApprovalHistory stores the complete audit trail.
		// ---------------------------------------------------------

		// MO/MDO/JMDO creation starts RM -> SMM -> AVP approval.
		public string? CreatedBy { get; set; }
		public string? CreatedByName { get; set; }
		public bool IsSubmittedForReview { get; set; } = false;

		public bool? RMApproved { get; set; }
		public bool? SMApproved { get; set; }
		public bool? AVPApproved { get; set; }

		public string? RMApprovedBy { get; set; }
		public DateTime? RMApprovedAt { get; set; }

		public string? SMApprovedBy { get; set; }
		public DateTime? SMApprovedAt { get; set; }

		public string? AVPApprovedBy { get; set; }
		public DateTime? AVPApprovedAt { get; set; }

		// Latest/current workflow remark.
		// Complete remarks history is stored in LogisticsApprovalHistory.
		public string? ApprovalRemarks { get; set; }

		public bool IsActive { get; set; }

		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }

		public required string UpdatedBy { get; set; }
	}


	/// <summary>
	/// Existing Rake Point entity retained unchanged in behavior.
	/// </summary>
	public class RackPoint
	{
		public int Id { get; set; }
		public required string Name { get; set; }

		public string? GoogleURL { get; set; }
		public string? RailwayCode { get; set; }
		public string? SAPCode { get; set; }

		public bool? InSpic { get; set; }
		public bool? InGreenStar { get; set; }

		public int? BasicStateId { get; set; }
		public int? RegionId { get; set; }
		public int? HeadquarterId { get; set; }

		public LogisticsOperatedBy? OperatedBy { get; set; }

		public double? Latitude { get; set; }
		public double? Longitude { get; set; }
		public string? SubVillage { get; set; }
		public string? PinCode { get; set; }
		public string? Village { get; set; }
		public string? Block { get; set; }
		public string? Taluk { get; set; }
		public string? ContactNumber { get; set; }
		public string? AdditionalContactNumber { get; set; }

		public string? OtherDocumentPathsJson { get; set; }

        public int? StateId { get; set; }
        public State? State { get; set; }

		public int? DistrictId { get; set; }
		public District? District { get; set; }

		// ---------------------------------------------------------
		// CURRENT Logistics workflow state.
		// Keep these fields.
		//
		// LogisticsApprovalHistory stores the complete audit trail.
		// ---------------------------------------------------------

		// MO/MDO/JMDO creation starts RM -> SMM -> AVP approval.
		public string? CreatedBy { get; set; }
		public string? CreatedByName { get; set; }
		public bool IsSubmittedForReview { get; set; } = false;

		public bool? RMApproved { get; set; }
		public bool? SMApproved { get; set; }
		public bool? AVPApproved { get; set; }

		public string? RMApprovedBy { get; set; }
		public DateTime? RMApprovedAt { get; set; }

		public string? SMApprovedBy { get; set; }
		public DateTime? SMApprovedAt { get; set; }

		public string? AVPApprovedBy { get; set; }
		public DateTime? AVPApprovedAt { get; set; }

		// Latest/current workflow remark.
		// Complete remarks history is stored in LogisticsApprovalHistory.
		public string? ApprovalRemarks { get; set; }

		public bool IsActive { get; set; }

		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }

		public required string UpdatedBy { get; set; }
	}

	public class Port
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? GoogleURL { get; set; }
        public int StateId { get; set; } = 0;
        public State? State { get; set; }
        public int DistrictId { get; set; } = 0;
        public District? District { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
    public class Bank
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string IFSCPrefix { get; set; }
        public string? Icon { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
    public class FinancialYear
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
    public class Relationship
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
    public class Plant
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
    public class DealerType
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
    public class Status
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
    public class IfmsDealer
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? DealerTypeId { get; set; }
        public int? DealershipNatureId { get; set; }
        public int? StateId { get; set; }
        public int? DistrictId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
		public string? MobileNo { get; set; }
	}
    public class DealershipNature
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
    public class Company
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }

    public class TxnType
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
    public class AckThrough
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }

    public class LyingWithMaster
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
	public class IfmsProduct
	{
		public int Id { get; set; }
		public string? Name { get; set; }
		public int? CategoryId { get; set; }
		public bool IsActive { get; set; } = true;
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
		public string? UpdatedBy { get; set; }
	}
	public class PVTMaster
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public string Code { get; set; }

		[Required]
		public string Name { get; set; }

		[Required]
		public bool IsActive { get; set; } = true;

		[Required]
		public DateTime CreatedAt { get; set; }

		[Required]
		public DateTime UpdatedAt { get; set; }

		[StringLength(100)]
		public string CreatedBy { get; set; }

		[StringLength(100)]
		public string UpdatedBy { get; set; }
	}

	/// <summary>
	/// Rake Point Master loaded via the PVT Master page's file upload.
	/// Mirrors the PVTMaster structure. Table created manually - no EF migration.
	/// </summary>
	public class RakePointMaster
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public string RakePointCode { get; set; }

		[Required]
		public string Name { get; set; }

		[Required]
		public bool IsActive { get; set; } = true;

		[Required]
		public DateTime CreatedAt { get; set; }

		[Required]
		public DateTime UpdatedAt { get; set; }

		[StringLength(100)]
		public string CreatedBy { get; set; }

		[StringLength(100)]
		public string UpdatedBy { get; set; }
	}
}
