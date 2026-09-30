namespace SPIC.Core.Entities
{
    /// <summary>
    /// MD Portal master: one Annual Budgeting record per financial year.
    /// Follows the same shape/convention as Designation (Id, main fields,
    /// IsActive, CreatedBy/At, UpdatedBy/At).
    /// </summary>
    public class AnnualBudgeting
    {
        public int Id { get; set; }
        public required string FY { get; set; }
        public decimal Amount { get; set; }
        public bool IsActive { get; set; } = true;
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
