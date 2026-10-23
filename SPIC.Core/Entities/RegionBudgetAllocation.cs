namespace SPIC.Core.Entities
{
    public class RegionBudgetAllocation
    {
        public int Id { get; set; }
        public int StateId { get; set; }
        public State? State { get; set; }
        public int RegionId { get; set; }
        public Region? Region { get; set; }
        public decimal Amount { get; set; }

        public int? RegionBudgetSummaryId { get; set; }
        public RegionBudgetSummary? RegionBudgetSummary { get; set; }

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

    public class RegionBudgetAllocationHistory
    {
        public int Id { get; set; }
        public int RegionBudgetAllocationId { get; set; }
        public RegionBudgetAllocation? RegionBudgetAllocation { get; set; }
        public int StateId { get; set; }
        public int RegionId { get; set; }
        public decimal Amount { get; set; }
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

    public class RegionBudgetSummary
    {
        public int Id { get; set; }
        public int StateId { get; set; }
        public State? State { get; set; }

        // Stores FinancialYears.Id.
        public int FY { get; set; }

        public decimal TotalBudget { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Status { get; set; } = "Draft";
        public string? ValidatedBy { get; set; }
        public DateTime? ValidatedDate { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public ICollection<RegionBudgetAllocation>? RegionBudgetAllocations { get; set; }
    }

    public class RegionBudgetSummaryHistory
    {
        public int Id { get; set; }
        public int RegionBudgetSummaryId { get; set; }
        public RegionBudgetSummary? RegionBudgetSummary { get; set; }
        public int StateId { get; set; }
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
