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
	/// Used by both PVT WH and C&F WH in Logistics Master.
	/// WarehouseCategory distinguishes the two views without creating another table.
	/// Legacy rows with null/empty WarehouseCategory are treated as PVT WH by the UI.
	/// </summary>
	public class Warehouse
	{
		public int Id { get; set; }
		public required string Name { get; set; }

		// Existing field. The current UI labels this as "SAP Code".
		public string WarehouseCode { get; set; } = string.Empty;

		// "PVT" or "C&F". Nullable for safe legacy-row compatibility.
		public string? WarehouseCategory { get; set; }

		// Company selection shown in Basic Information.
		public bool? InSpic { get; set; }
		public bool? InGreenStar { get; set; }

		// Basic Information location selection.
		public int? BasicStateId { get; set; }
		public int? RegionId { get; set; }
		public int? HeadquarterId { get; set; }

		// Dropdown values: Dealer / Others.
		public LogisticsOperatedBy? OperatedBy { get; set; }

		// Dropdown values: Field WH / EPT WH.
		// C&F WH intentionally stores null because WH Type is hidden for that tab.
		public LogisticsWarehouseType? WarehouseType { get; set; }

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

		// PVT WH = Contact No; C&F WH = C&F Operator Contact No.
		public string? ContactNumber { get; set; }

		// PVT WH reservation quantities (number fields, stored as decimal).
		// Separate values are maintained for SPIC and GFL.
		public decimal? SpicApprovedReservationQuantityMT { get; set; }
		public decimal? SpicAdditionalReservationQuantityMT { get; set; }
		public decimal? GflApprovedReservationQuantityMT { get; set; }
		public decimal? GflAdditionalReservationQuantityMT { get; set; }

		// C&F WH reservation quantities. These are GFL-only as per the current requirement.
		public decimal? GflReservationQuantityMT { get; set; }
		public decimal? GflAdditionalReservationQuantityLitres { get; set; }

		// PVT WH required documents. C&F WH leaves these null.
		public string? GstDocumentPath { get; set; }
		public string? InsuranceDocumentPath { get; set; }
		public string? FertilizerLicenseDocumentPath { get; set; }

		// Optional multiple documents for both PVT WH and C&F WH.
		// Stored as a JSON string containing an array of relative file paths.
		public string? OtherDocumentPathsJson { get; set; }

		public bool IsActive { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
		public required string UpdatedBy { get; set; }
	}

	/// <summary>
	/// Used by the Rake Point tab.
	/// Existing RailwayCode is preserved. SAPCode is used by the current UI.
	/// The page writes the same SAP code to both properties for backward compatibility.
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

		// Dropdown values: Dealer / Others.
		public LogisticsOperatedBy? OperatedBy { get; set; }

		// Dropdown values: Field WH / EPT WH.
		public LogisticsWarehouseType? WarehouseType { get; set; }

		public double? Latitude { get; set; }
		public double? Longitude { get; set; }

		// DoorNo and Street intentionally remain absent for Rake Point.
		public string? SubVillage { get; set; }
		public string? PinCode { get; set; }
		public string? Village { get; set; }
		public string? Block { get; set; }
		public string? Taluk { get; set; }
		public string? ContactNumber { get; set; }

		// Rake Point supports optional multiple documents only.
		// Stored as a JSON string containing an array of relative file paths.
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
