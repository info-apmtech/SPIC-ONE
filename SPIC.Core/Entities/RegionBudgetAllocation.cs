namespace SPIC.Core.Entities
{
    /// <summary>
    /// Region-wise share of a StateBudgetAllocation amount for a given FY. One row per
    /// (RegionId, FY) - enforced by a unique index in AppDbContext - so a save always
    /// upserts rather than creating duplicates. RegionId alone is enough for uniqueness
    /// since a Region always belongs to exactly one State (Region.StateId).
    /// </summary>
    public class RegionBudgetAllocation
    {
        public int Id { get; set; }
        public int StateId { get; set; }
        public State? State { get; set; }
        public int RegionId { get; set; }
        public Region? Region { get; set; }
        public required string FY { get; set; }
        public decimal Amount { get; set; }

        /// <summary>
        /// FK to the Level 2 (SM) RegionBudgetSummary (for this row's StateId+FY) this row
        /// belongs to. The authoritative Summary-to-Detail link - FY itself remains on both
        /// tables for display/filtering only. Nullable because older rows saved before this
        /// column existed may not yet be backfilled (see v16AddBudgetSummaryIdForeignKey migration).
        /// </summary>
        public int? RegionBudgetSummaryId { get; set; }
        public RegionBudgetSummary? RegionBudgetSummary { get; set; }

        /// <summary>
        /// Mirrors StateBudgetAllocation.Status's convention. Cascaded to "Submitted"/
        /// "Validated"/"Approved" by the same State-level workflow action that moves the
        /// parent State row for the same FY - there is no separate Region workflow.
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
    /// Append-only audit trail for RegionBudgetAllocation. One row is written per workflow
    /// transition (Submitted/Validated/Approved) as a snapshot of the main row at that
    /// moment - the main RegionBudgetAllocation table remains the only source of current/live data.
    /// </summary>
    public class RegionBudgetAllocationHistory
    {
        public int Id { get; set; }

        public int RegionBudgetAllocationId { get; set; }
        public RegionBudgetAllocation? RegionBudgetAllocation { get; set; }

        public int StateId { get; set; }
        public int RegionId { get; set; }
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
    /// Level 2 (SM) budget allocation summary - one row per (StateId, FY). Distinct from
    /// RegionBudgetAllocation (which holds each individual region's own share): this is the
    /// single declared Total Budget / Allocated to Region / Remaining Amount triple an SM
    /// enters for the whole State+FY. All three fields are independently entered - Remaining
    /// is never derived from Total - Allocated - and are cross-checked server-side on Submit.
    /// </summary>
    public class RegionBudgetSummary
    {
        public int Id { get; set; }
        public int StateId { get; set; }
        public State? State { get; set; }
        public required string FY { get; set; }

        public decimal TotalBudget { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal RemainingAmount { get; set; }

        /// <summary>Same Draft/Submitted/Validated/Approved convention as RegionBudgetAllocation.Status.</summary>
        public string Status { get; set; } = "Draft";

        public string? ValidatedBy { get; set; }
        public DateTime? ValidatedDate { get; set; }

        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>Inverse of RegionBudgetAllocation.RegionBudgetSummary - every region's detail row for this State+FY.</summary>
        public ICollection<RegionBudgetAllocation>? RegionBudgetAllocations { get; set; }
    }

    /// <summary>
    /// Append-only audit trail for RegionBudgetSummary, same convention as RegionBudgetAllocationHistory.
    /// </summary>
    public class RegionBudgetSummaryHistory
    {
        public int Id { get; set; }

        public int RegionBudgetSummaryId { get; set; }
        public RegionBudgetSummary? RegionBudgetSummary { get; set; }

        public int StateId { get; set; }
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
