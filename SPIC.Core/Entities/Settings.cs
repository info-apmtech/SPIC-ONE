using System;
using System.Collections.Generic;
using System.Text;

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
    public class Warehouse
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string WarehouseCode { get; set; }
        public string? GoogleURL { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
    public class RackPoint
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? GoogleURL { get; set; }
        public string? RailwayCode { get; set; }
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
}
