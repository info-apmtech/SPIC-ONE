namespace SPIC.Core.Entities
{
    public class StateBudgetAllocation
    {
        public int Id { get; set; }
        public int StateId { get; set; }
        public State? State { get; set; }
        public decimal Amount { get; set; }

        public int? StateBudgetSummaryId { get; set; }
        public StateBudgetSummary? StateBudgetSummary { get; set; }

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

    public class StateBudgetAllocationHistory
    {
        public int Id { get; set; }
        public int StateBudgetAllocationId { get; set; }
        public StateBudgetAllocation? StateBudgetAllocation { get; set; }
        public int StateId { get; set; }
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

    public class StateBudgetSummary
    {
        public int Id { get; set; }

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
        public ICollection<StateBudgetAllocation>? StateBudgetAllocations { get; set; }
    }

    public class StateBudgetSummaryHistory
    {
        public int Id { get; set; }
        public int StateBudgetSummaryId { get; set; }
        public StateBudgetSummary? StateBudgetSummary { get; set; }
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
