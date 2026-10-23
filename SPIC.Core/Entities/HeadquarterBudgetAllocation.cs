namespace SPIC.Core.Entities
{
    public class HeadquarterBudgetAllocation
    {
        public int Id { get; set; }
        public int RegionId { get; set; }
        public Region? Region { get; set; }
        public int HeadquarterId { get; set; }
        public Headquarter? Headquarter { get; set; }
        public decimal Amount { get; set; }

        public int? HeadquarterBudgetSummaryId { get; set; }
        public HeadquarterBudgetSummary? HeadquarterBudgetSummary { get; set; }

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

    public class HeadquarterBudgetAllocationHistory
    {
        public int Id { get; set; }
        public int HeadquarterBudgetAllocationId { get; set; }
        public HeadquarterBudgetAllocation? HeadquarterBudgetAllocation { get; set; }
        public int RegionId { get; set; }
        public int HeadquarterId { get; set; }
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

    public class HeadquarterBudgetSummary
    {
        public int Id { get; set; }
        public int RegionId { get; set; }
        public Region? Region { get; set; }

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
        public ICollection<HeadquarterBudgetAllocation>? HeadquarterBudgetAllocations { get; set; }
    }

    public class HeadquarterBudgetSummaryHistory
    {
        public int Id { get; set; }
        public int HeadquarterBudgetSummaryId { get; set; }
        public HeadquarterBudgetSummary? HeadquarterBudgetSummary { get; set; }
        public int RegionId { get; set; }
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
