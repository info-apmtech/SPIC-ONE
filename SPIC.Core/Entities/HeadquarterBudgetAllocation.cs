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
        /// to "Submitted" by the same Submit For Validation action that submits the parent
        /// State and Region rows for the same FY - there is no separate HQ submission workflow.
        /// </summary>
        public string Status { get; set; } = "Draft";

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
