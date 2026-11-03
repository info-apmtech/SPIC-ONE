using System;

namespace SPIC.Core.Entities
{
    public enum FarmProductGroup
    {
        LiveStock,
        Orchard
    }

    public enum FarmRequestType
    {
        NewProduct,
        RateChange
    }

    public enum FarmRequestStatus
    {
        Pending,
        Approved,
        Rejected
    }

    /// <summary>
    /// The livestock/orchard product catalog used by Farm Operations (Farm Admin / Farm Office).
    /// Deliberately separate from Product/ProductGroup in Settings.cs, which is the unrelated
    /// fertilizer/dealer product catalog - do not merge these.
    /// </summary>
    public class FarmProduct
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public FarmProductGroup Group { get; set; }
        public required string Uom { get; set; }
        public decimal CurrentRate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }

    /// <summary>
    /// A Farm Office request to either add a new FarmProduct or change an existing one's rate.
    /// Approving a RateChange updates FarmProduct.CurrentRate immediately (no scheduled/future-dated
    /// activation). Approving a NewProduct creates the FarmProduct row and links FarmProductId back
    /// onto this request for audit/rate-history purposes.
    /// </summary>
    public class FarmProductRateRequest
    {
        public int Id { get; set; }
        public FarmRequestType RequestType { get; set; }

        // Set for RateChange; set only AFTER approval for NewProduct (null while pending).
        public int? FarmProductId { get; set; }
        public FarmProduct? FarmProduct { get; set; }

        // NewProduct only - the proposed catalog entry.
        public string? ProposedProductName { get; set; }
        public FarmProductGroup? ProposedGroup { get; set; }
        public required string ProposedUom { get; set; }

        // RateChange only - snapshot of the rate at request time, for display/audit.
        public decimal? CurrentRateAtRequestTime { get; set; }

        public decimal RequestedRate { get; set; }
        public required string Reason { get; set; }

        public FarmRequestStatus Status { get; set; } = FarmRequestStatus.Pending;
        public required string RequestedBy { get; set; }
        public DateTime RequestedAt { get; set; }

        // Admin can adjust these before approving - defaults to the requested values if left as-is.
        public decimal? ApprovedPrice { get; set; }
        public string? ApprovedUom { get; set; }
        public string? AdminRemarks { get; set; }

        public string? RejectionReason { get; set; }
        public string? DecidedBy { get; set; }
        public DateTime? DecidedAt { get; set; }
    }
}
