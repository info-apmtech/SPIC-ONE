using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPIC.Core.Entities
{
    public class WarehouseDistrictWiseDetailsGlobalStock
    {
        [Key]
        public int Id { get; set; }

        public int? StateId { get; set; }

        public int? DistrictId { get; set; }

        public int? WarehouseId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningStockAtLocation { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningStockGIT { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ImportsProduction { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Receipt { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Dispatches { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Sales { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalesReturn { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal StockAdjustment { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ClosingGIT { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ClosingStock { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class WholesalerStockAsOnToday
    {
        [Key]
        public int Id { get; set; }

        public int? StateId { get; set; }

        public int? DistrictId { get; set; }

        public int? DealerRegistrationId { get; set; }
        public int? IfmsDealerId { get; set; }

        public string? AgencyName { get; set; }
        public int? DealerTypeId { get; set; }
        public string? DealershipNature { get; set; }
        public int? CompetitorId { get; set; }
        public int? PlantId { get; set; }

        public int? ProductId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Stock { get; set; }

        public DateTime StockDate { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class StateWiseGlobalStockReconciliation
    {
        [Key]
        public int Id { get; set; }

        public int? StateId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningStock { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningGIT { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProductionImports { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Receipt { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Dispatches { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Sales { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalesReturn { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal StockAdjustment { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ClosingGIT { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ClosingStock { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class SalesWholesaler
    {
        [Key]
        public int Id { get; set; }

        public string? TransactionId { get; set; }
        public string? InvoiceNo { get; set; }
        public DateTime? InvoiceDate { get; set; }

        public string? Marketer { get; set; }
        public string? Manufacturer { get; set; }
        public int? PlantId { get; set; }

        public string? WholesalerId { get; set; }
        public string? WholesalerAgencyName { get; set; }
        public string? WholesalerNature { get; set; }

        public int? StateId { get; set; }

        public int? SellerDistrictId { get; set; }

        public int? BuyerDistrictId { get; set; }

        public string? DealerId { get; set; }
        public int? DealerTypeId { get; set; }

        public int? DealerRegistrationId { get; set; }
        public int? IfmsDealerId { get; set; }

        public string? AgencyName { get; set; }
        public string? DealerNature { get; set; }
        public string? MobileNo { get; set; }

        public int? ProductId { get; set; }

        public int? UnitId { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal QuantityMT { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal ReceivedQuantityMT { get; set; }

        public int? StatusId { get; set; }
        public string? TxnType { get; set; }

        public DateTime? EntryDate { get; set; }
        public DateTime? LockDate { get; set; }

        public string? AckThrough { get; set; }
        public string? TxnRemark { get; set; }

        public string? SubsidyMonth1 { get; set; }
        public string? SubsidyYear1 { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal? Month1Qty { get; set; }

        public string? SubsidyMonth2 { get; set; }
        public string? SubsidyYear2 { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal? Month2Qty { get; set; }

        public string? ChallanNo { get; set; }
        public string? LorryNo { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? LorryCapacity { get; set; }
        public string? DispatchNo { get; set; }

        public DateTime? RetailerReceiptDate { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class SalesAndReceipt
    {
        [Key]
        public int Id { get; set; }

        public string? Company { get; set; }
        public int? PlantId { get; set; }

        public int? ProductId { get; set; }

        public int? StateId { get; set; }

        public int? DistrictId { get; set; }

        public string? DealerId { get; set; }
        public string? DealerNature { get; set; }
        public string? AgencyName { get; set; }

        public int? DealerRegistrationId { get; set; }
        public int? IfmsDealerId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CompWsSale { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CompWsSaleRcpt { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ReceivedFromWs { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ReceivedFromWsAck { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal WsRtSale { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal WsRtSaleRcpt { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal WsWsSale { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal WsWsSaleRcpt { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSalesByWs { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal StockTransferWsToRetailer { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal StockTransferWsToRetailerAck { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceWithWs { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAckToWs { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class SalesCompanySale
    {
        [Key]
        public int Id { get; set; }

        public string? TransactionId { get; set; }
        public string? InvoiceNo { get; set; }
        public DateTime? InvoiceDate { get; set; }

        public string? Marketer { get; set; }
        public string? Manufacturer { get; set; }
        public int? PlantId { get; set; }

        public string? DealerId { get; set; }
        public string? DealerName { get; set; }
        public int? DealerTypeId { get; set; }
        public string? DealershipNature { get; set; }
        public string? MobileNo { get; set; }

        public int? DealerRegistrationId { get; set; }
        public int? IfmsDealerId { get; set; }

        public int? StateId { get; set; }

        public int? DistrictId { get; set; }

        public int? ProductId { get; set; }

        public int? UnitId { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal QuantityMT { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal ReceivedQuantity { get; set; }

        public int? StatusId { get; set; }
        public DateTime? EntryDate { get; set; }
        public DateTime? LockDate { get; set; }
        public string? AckThrough { get; set; }
        public string? TxnRemark { get; set; }

        public string? SubsidyMonth1 { get; set; }
        public string? SubsidyYear1 { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal? Month1Qty { get; set; }

        public string? SubsidyMonth2 { get; set; }
        public string? SubsidyYear2 { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal? Month2Qty { get; set; }

        public string? ChallanNo { get; set; }
        public string? DdNo { get; set; }
        public string? LorryNo { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? LorryCapacity { get; set; }

        public DateTime? RetailerReceiptDate { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class DptReport
    {
        [Key]
        public int Id { get; set; }

        public int? StateId { get; set; }

        public int? DistrictId { get; set; }

        public int? SubDistrictId { get; set; }

        public string? RetailerId { get; set; }
        public string? RetailerName { get; set; }

        public int? DealerRegistrationId { get; set; }
        public int? IfmsDealerId { get; set; }

        public string? MobileNo { get; set; }
        public string? DealershipNature { get; set; }

        public string? Company { get; set; }
        public int? PlantId { get; set; }

        public int? ProductId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ReceivedQuantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SoldQuantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Availability { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ClosingBalance { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }
}
