namespace SPIC.Core.DTOs
{
    /// <summary>One headquarter's row in the State Budget Management page's Headquarters Allocation table.</summary>
    public class HeadquarterBudgetDto
    {
        public int HeadquarterId { get; set; }
        public string HeadquarterName { get; set; } = string.Empty;
        public decimal BudgetAmount { get; set; }
    }

    /// <summary>One headquarter's allocation as submitted from the Headquarters Allocation save action.</summary>
    public class HeadquarterBudgetAllocationRequest
    {
        public int HeadquarterId { get; set; }
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Full replace of every headquarter's allocation for one Region+FY. The client always
    /// sends every headquarter's current amount (not just the changed row), same convention
    /// as SaveRegionBudgetRequest, so an edited row's own previous value is never double-counted.
    /// </summary>
    public class SaveHeadquarterBudgetRequest
    {
        public int RegionId { get; set; }
        public string FY { get; set; } = string.Empty;
        public List<HeadquarterBudgetAllocationRequest> Allocations { get; set; } = new();
    }

    /// <summary>
    /// Level 3 (RM) summary for one Region+FY: the Total Budget / Allocated to Headquarters /
    /// Remaining Amount triple from HeadquarterBudgetSummary. All three are plain persisted
    /// values - never recomputed from each other - returned as 0/"Draft" when nothing has been
    /// saved yet.
    /// </summary>
    public class HeadquarterBudgetSummaryDto
    {
        public int RegionId { get; set; }
        public string FY { get; set; } = string.Empty;
        public decimal TotalBudget { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Status { get; set; } = "Draft";
    }

    /// <summary>
    /// Save (Draft) request for the Level 3 summary. All three values are whatever the RM
    /// typed - no server-side recomputation of RemainingAmount from TotalBudget - AllocatedAmount.
    /// </summary>
    public class SaveHeadquarterBudgetSummaryRequest
    {
        public int RegionId { get; set; }
        public string FY { get; set; } = string.Empty;
        public decimal TotalBudget { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
    }

    /// <summary>
    /// Submit request for the Level 3 summary: re-validates TotalBudget = AllocatedAmount +
    /// RemainingAmount, and that TotalBudget does not exceed the applicable Region's own
    /// allocated amount (RegionBudgetAllocation.Amount for the same Region+FY).
    /// </summary>
    public class SubmitHeadquarterBudgetSummaryRequest
    {
        public int RegionId { get; set; }
        public string FY { get; set; } = string.Empty;
    }
}
