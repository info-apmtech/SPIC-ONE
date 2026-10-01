namespace SPIC.Core.Entities
{
    /// <summary>
    /// Headquarters-wise share of a RegionBudgetAllocation amount for a given FY. One row
    /// per (HeadquarterId, FY) - enforced by a unique index in AppDbContext - so a save
    /// always upserts rather than creating duplicates. HeadquarterId alone is enough for
    /// uniqueness since a Headquarter always belongs to exactly one Region (Headquarter.RegionId).
    /// </summary>
    public class HeadquarterBudgetAllocation
    {
        public int Id { get; set; }
        public int RegionId { get; set; }
        public Region? Region { get; set; }
        public int HeadquarterId { get; set; }
        public Headquarter? Headquarter { get; set; }
        public required string FY { get; set; }
        public decimal Amount { get; set; }

        /// <summary>
        /// Mirrors StateBudgetAllocation/RegionBudgetAllocation.Status's convention. Cascaded
        /// to "Submitted"/"Validated"/"Approved" by the same State-level workflow action that
        /// moves the parent State and Region rows for the same FY - there is no separate HQ workflow.
        /// </summary>
        public string Status { get; set; } = "Draft";

        /// <summary>Set when a Validator moves this allocation from "Submitted" to "Validated".</summary>
        public string? ValidatedBy { get; set; }
        public DateTime? ValidatedDate { get; set; }

        /// <summary>Set when an Approver moves this allocation from "Validated" to "Approved".</summary>
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Append-only audit trail for HeadquarterBudgetAllocation. One row is written per workflow
    /// transition (Submitted/Validated/Approved) as a snapshot of the main row at that
    /// moment - the main HeadquarterBudgetAllocation table remains the only source of current/live data.
    /// </summary>
    public class HeadquarterBudgetAllocationHistory
    {
        public int Id { get; set; }

        public int HeadquarterBudgetAllocationId { get; set; }
        public HeadquarterBudgetAllocation? HeadquarterBudgetAllocation { get; set; }

        public int RegionId { get; set; }
        public int HeadquarterId { get; set; }
        public required string FY { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Draft";

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? ValidatedBy { get; set; }
        public DateTime? ValidatedDate { get; set; }

        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }

        /// <summary>The workflow transition this snapshot records, e.g. "Submitted"/"Validated"/"Approved".</summary>
        public required string Action { get; set; }
        public DateTime ActionDate { get; set; }
    }

    /// <summary>
    /// Level 3 (RM) budget allocation summary - one row per (RegionId, FY). Distinct from
    /// HeadquarterBudgetAllocation (which holds each individual headquarter's own share): this
    /// is the single declared Total Budget / Allocated to Headquarters / Remaining Amount
    /// triple an RM enters for the whole Region+FY. All three fields are independently entered
    /// - Remaining is never derived from Total - Allocated - and are cross-checked server-side
    /// on Submit.
    /// </summary>
    public class HeadquarterBudgetSummary
    {
        public int Id { get; set; }
        public int RegionId { get; set; }
        public Region? Region { get; set; }
        public required string FY { get; set; }

        public decimal TotalBudget { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal RemainingAmount { get; set; }

        /// <summary>Same Draft/Submitted/Validated/Approved convention as HeadquarterBudgetAllocation.Status.</summary>
        public string Status { get; set; } = "Draft";

        public string? ValidatedBy { get; set; }
        public DateTime? ValidatedDate { get; set; }

        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Append-only audit trail for HeadquarterBudgetSummary, same convention as HeadquarterBudgetAllocationHistory.
    /// </summary>
    public class HeadquarterBudgetSummaryHistory
    {
        public int Id { get; set; }

        public int HeadquarterBudgetSummaryId { get; set; }
        public HeadquarterBudgetSummary? HeadquarterBudgetSummary { get; set; }

        public int RegionId { get; set; }
        public required string FY { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Status { get; set; } = "Draft";

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? ValidatedBy { get; set; }
        public DateTime? ValidatedDate { get; set; }

        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }

        public required string Action { get; set; }
        public DateTime ActionDate { get; set; }
    }
}
