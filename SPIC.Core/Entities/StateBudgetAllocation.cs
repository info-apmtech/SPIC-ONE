namespace SPIC.Core.Entities
{
    /// <summary>
    /// State-wise share of an AnnualBudgeting amount for a given FY. One row per
    /// (StateId, FY) - enforced by a unique index in AppDbContext - so a save
    /// always upserts rather than creating duplicates.
    /// </summary>
    public class StateBudgetAllocation
    {
        public int Id { get; set; }
        public int StateId { get; set; }
        public State? State { get; set; }
        public required string FY { get; set; }
        public decimal Amount { get; set; }

        /// <summary>
        /// Reuses BudgetProgram.Status's existing plain-string convention (no dedicated
        /// enum exists in this codebase) - "Draft" until Submit For Validation sets every
        /// row for the FY to "Submitted", then "Validated"/"Approved" as the workflow
        /// progresses.
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
    /// Append-only audit trail for StateBudgetAllocation. One row is written per workflow
    /// transition (Submitted/Validated/Approved) as a snapshot of the main row at that
    /// moment - the main StateBudgetAllocation table remains the only source of current/live data.
    /// </summary>
    public class StateBudgetAllocationHistory
    {
        public int Id { get; set; }

        public int StateBudgetAllocationId { get; set; }
        public StateBudgetAllocation? StateBudgetAllocation { get; set; }

        public int StateId { get; set; }
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
    /// Level 1 (Admin) budget allocation summary - one row per FY. Distinct from
    /// StateBudgetAllocation (which holds each individual state's own share): this is the
    /// single declared Total Budget / Allocated to State / Remaining Amount triple an Admin
    /// enters for the whole FY. All three fields are independently entered - Remaining is
    /// never derived from Total - Allocated - and are cross-checked server-side on Submit.
    /// </summary>
    public class StateBudgetSummary
    {
        public int Id { get; set; }
        public required string FY { get; set; }

        public decimal TotalBudget { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal RemainingAmount { get; set; }

        /// <summary>Same Draft/Submitted/Validated/Approved convention as StateBudgetAllocation.Status.</summary>
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
    /// Append-only audit trail for StateBudgetSummary, same convention as StateBudgetAllocationHistory.
    /// </summary>
    public class StateBudgetSummaryHistory
    {
        public int Id { get; set; }

        public int StateBudgetSummaryId { get; set; }
        public StateBudgetSummary? StateBudgetSummary { get; set; }

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
