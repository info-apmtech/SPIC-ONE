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
        /// Mirrors StateBudgetAllocation.Status's convention. Cascaded to "Submitted" by
        /// the same Submit For Validation action that submits the parent State row for
        /// the same FY - there is no separate Region submission workflow.
        /// </summary>
        public string Status { get; set; } = "Draft";

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
