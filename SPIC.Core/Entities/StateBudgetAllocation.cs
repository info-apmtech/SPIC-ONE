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
        /// row for the FY to "Submitted".
        /// </summary>
        public string Status { get; set; } = "Draft";

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
