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
}
