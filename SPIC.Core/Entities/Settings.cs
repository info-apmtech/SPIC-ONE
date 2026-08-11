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
		EPTWH = 2
	}

	/// <summary>
	/// PVT Warehouse master. This is a dedicated table/entity and is no longer
	/// mixed with C&F Warehouse rows.
	/// </summary>
	public class Warehouse//PVT Warehouse master
	{
		public int Id { get; set; }
		public required string Name { get; set; }

		// Current Logistics Master UI label: SAP Code.
		public string WarehouseCode { get; set; } = string.Empty;

		// Company selection.
		public bool? InSpic { get; set; }
		public bool? InGreenStar { get; set; }

		// Basic Information cascading location.
		public int? BasicStateId { get; set; }
		public int? RegionId { get; set; }
		public int? HeadquarterId { get; set; }

		// Dropdowns.
		public LogisticsOperatedBy? OperatedBy { get; set; }
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
		public int StateId { get; set; } = 0;
		public int DistrictId { get; set; } = 0;
		public string? ContactNumber { get; set; }

		// PVT WH reservation quantities - separate for SPIC and GFL.
		public decimal? SpicApprovedReservationQuantityMT { get; set; }
		public decimal? SpicAdditionalReservationQuantityMT { get; set; }
		public decimal? GflApprovedReservationQuantityMT { get; set; }
		public decimal? GflAdditionalReservationQuantityMT { get; set; }

		// PVT WH required highlighted documents.
		public string? GstDocumentPath { get; set; }
		public string? InsuranceDocumentPath { get; set; }
		public string? FertilizerLicenseDocumentPath { get; set; }

		// Optional multiple documents stored as JSON array of relative paths.
		public string? OtherDocumentPathsJson { get; set; }

		public bool IsActive { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
		public required string UpdatedBy { get; set; }
	}

	/// <summary>
	/// C&F Warehouse master. Dedicated table/entity.
	/// WH Type is intentionally not present because the C&F UI hides it.
	/// </summary>
	public class CandFWarehouse
	{
		public int Id { get; set; }
		public required string Name { get; set; }

		// Current Logistics Master UI label: SAP Code.
		public string WarehouseCode { get; set; } = string.Empty;

		// Company selection.
		public bool? InSpic { get; set; }
		public bool? InGreenStar { get; set; }

		// Basic Information cascading location.
		public int? BasicStateId { get; set; }
		public int? RegionId { get; set; }
		public int? HeadquarterId { get; set; }

		// C&F still uses Operated By. WH Type is not applicable.
		public LogisticsOperatedBy? OperatedBy { get; set; }

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
		public int StateId { get; set; } = 0;
		public int DistrictId { get; set; } = 0;

		// UI label: C&F Operator Contact No.
		public string? ContactNumber { get; set; }

		// GFL-only C&F reservation quantities.
		public decimal? GflReservationQuantityMT { get; set; }
		public decimal? GflAdditionalReservationQuantityLitres { get; set; }

		// C&F supports optional other documents only.
		public string? OtherDocumentPathsJson { get; set; }

		public bool IsActive { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
		public required string UpdatedBy { get; set; }
	}

	/// <summary>
	/// Existing Rake Point table/entity retained. DoorNo and Street are intentionally
	/// absent because the current Logistics Master hides those fields for Rake Point.
	/// </summary>
	public class RackPoint
	{
		public int Id { get; set; }
		public required string Name { get; set; }

		public string? GoogleURL { get; set; }

		// Existing field preserved for backward compatibility.
		public string? RailwayCode { get; set; }

		// Current UI SAP Code field. The page writes the same value to RailwayCode.
		public string? SAPCode { get; set; }

		public bool? InSpic { get; set; }
		public bool? InGreenStar { get; set; }

		public int? BasicStateId { get; set; }
		public int? RegionId { get; set; }
		public int? HeadquarterId { get; set; }

		public LogisticsOperatedBy? OperatedBy { get; set; }
		public LogisticsWarehouseType? WarehouseType { get; set; }

		public double? Latitude { get; set; }
		public double? Longitude { get; set; }
		public string? SubVillage { get; set; }
		public string? PinCode { get; set; }
		public string? Village { get; set; }
		public string? Block { get; set; }
		public string? Taluk { get; set; }
		public string? ContactNumber { get; set; }

		// Optional multiple documents only.
		public string? OtherDocumentPathsJson { get; set; }

		public int StateId { get; set; } = 0;
		public State? State { get; set; }
		public int DistrictId { get; set; } = 0;
		public District? District { get; set; }

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
}
