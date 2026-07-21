using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPIC.Core.Entities
{

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
        public int? DealershipNatureId { get; set; }
        public int? CompanyId { get; set; }
        public int? PlantId { get; set; }

        public int? ProductId { get; set; }

        public decimal Stock { get; set; }

        public DateTime StockDate { get; set; }

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

        public int? MarketerId { get; set; }
        public int? ManufacturerId { get; set; }
        public int? PlantId { get; set; }

        public int? WholesalerId { get; set; }
        public int? IfmsWholesalerId { get; set; }
        public string? WholesalerAgencyName { get; set; }
        public int? WholesalerNatureId { get; set; }

        public int? StateId { get; set; }

        public int? SellerDistrictId { get; set; }

        public int? BuyerDistrictId { get; set; }

        public int? DealerId { get; set; }
        public int? DealerTypeId { get; set; }

        public int? IfmsDealerId { get; set; }

        public string? AgencyName { get; set; }
        public int? DealerNatureId { get; set; }
        public string? MobileNo { get; set; }

        public int? ProductId { get; set; }

        public int? UnitId { get; set; }

        public decimal Quantity { get; set; }

        public decimal QuantityMT { get; set; }

        public decimal ReceivedQuantityMT { get; set; }

        public int? StatusId { get; set; }
        public int? TxnTypeId { get; set; }

        public DateTime? EntryDate { get; set; }
        public DateTime? LockDate { get; set; }

        public int? AckThroughId { get; set; }
        public string? TxnRemark { get; set; }

        public string? SubsidyMonth1 { get; set; }
        public string? SubsidyYear1 { get; set; }
        public decimal? Month1Qty { get; set; }

        public string? SubsidyMonth2 { get; set; }
        public string? SubsidyYear2 { get; set; }
        public decimal? Month2Qty { get; set; }

        public string? ChallanNo { get; set; }
        public string? LorryNo { get; set; }
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

        public int? CompanyId { get; set; }
        public int? PlantId { get; set; }

        public int? ProductId { get; set; }

        public int? StateId { get; set; }

        public int? DistrictId { get; set; }

        public int? DealershipNatureId { get; set; }
        public string? AgencyName { get; set; }

        public int? DealerRegistrationId { get; set; }
        public int? IfmsDealerId { get; set; }

        public decimal OpeningBalance { get; set; }

        public decimal CompWsSale { get; set; }

        public decimal CompWsSaleRcpt { get; set; }

        public decimal ReceivedFromWs { get; set; }

        public decimal ReceivedFromWsAck { get; set; }

        public decimal WsRtSale { get; set; }

        public decimal WsRtSaleRcpt { get; set; }

        public decimal WsWsSale { get; set; }

        public decimal WsWsSaleRcpt { get; set; }

        public decimal TotalSalesByWs { get; set; }

        public decimal StockTransferWsToRetailer { get; set; }

        public decimal StockTransferWsToRetailerAck { get; set; }

        public decimal BalanceWithWs { get; set; }

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

        public int? MarketerId { get; set; }
        public int? ManufacturerId { get; set; }
        public int? PlantId { get; set; }

        public string? DealerName { get; set; }
        public int? DealerTypeId { get; set; }
        public int? DealershipNatureId { get; set; }
        public string? MobileNo { get; set; }

        public int? DealerRegistrationId { get; set; }
        public int? IfmsDealerId { get; set; }

        public int? StateId { get; set; }

        public int? DistrictId { get; set; }

        public int? ProductId { get; set; }

        public int? UnitId { get; set; }

        public decimal Quantity { get; set; }

        public decimal QuantityMT { get; set; }

        public decimal ReceivedQuantity { get; set; }

        public int? StatusId { get; set; }
        public DateTime? EntryDate { get; set; }
        public DateTime? LockDate { get; set; }
        public int? AckThroughId { get; set; }
        public string? TxnRemark { get; set; }

        public string? SubsidyMonth1 { get; set; }
        public string? SubsidyYear1 { get; set; }
        public decimal? Month1Qty { get; set; }

        public string? SubsidyMonth2 { get; set; }
        public string? SubsidyYear2 { get; set; }
        public decimal? Month2Qty { get; set; }

        public string? ChallanNo { get; set; }
        public string? DdNo { get; set; }
        public string? LorryNo { get; set; }

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

        public string? RetailerName { get; set; }

        public int? DealerRegistrationId { get; set; }
        public int? IfmsDealerId { get; set; }

        public string? MobileNo { get; set; }
        public int? DealershipNatureId { get; set; }

        public int? CompanyId { get; set; }
        public int? PlantId { get; set; }

        public int? ProductId { get; set; }

        public decimal OpeningBalance { get; set; }

        public decimal ReceivedQuantity { get; set; }

        public decimal SoldQuantity { get; set; }

        public decimal Availability { get; set; }

        public decimal ClosingBalance { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }
    public class StateGlobalStockReconciliation
    {
        [Key]
        public int Id { get; set; }

        public int? PlantId { get; set; }
        public int? ProductId { get; set; }

        public int? StateId { get; set; }

        public decimal OpeningStock { get; set; }
        public decimal OpeningGIT { get; set; }
        public decimal ProductionImports { get; set; }
        public decimal Receipt { get; set; }
        public decimal Dispatches { get; set; }
        public decimal Sales { get; set; }
        public decimal SalesReturn { get; set; }
        public decimal StockAdjustment { get; set; }
        public decimal ClosingGIT { get; set; }
        public decimal ClosingStock { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class WarehouseDistrictGlobalStockReconciliation
    {
        [Key]
        public int Id { get; set; }

        public int? PlantId { get; set; }
        public int? ProductId { get; set; }

        public int? StateId { get; set; }
        public int? DistrictId { get; set; }
        public int? WarehouseId { get; set; }

        public decimal OpeningStockAtLocation { get; set; }
        public decimal OpeningStockGIT { get; set; }
        public decimal ProductionImports { get; set; }
        public decimal Receipt { get; set; }
        public decimal Dispatches { get; set; }
        public decimal Sales { get; set; }
        public decimal SalesReturn { get; set; }
        public decimal StockAdjustment { get; set; }
        public decimal ClosingGIT { get; set; }
        public decimal ClosingStock { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }
}
