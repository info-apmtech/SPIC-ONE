namespace SPIC.Core.DTOs
{
    /// <summary>One region's row in the State Budget Management page's Region Allocation table.</summary>
    public class RegionBudgetDto
    {
        public int RegionId { get; set; }
        public string RegionName { get; set; } = string.Empty;
        public decimal BudgetAmount { get; set; }
    }

    /// <summary>One region's allocation as submitted from the Region Allocation save action.</summary>
    public class RegionBudgetAllocationRequest
    {
        public int RegionId { get; set; }
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Full replace of every region's allocation for one State+FY. The client always sends
    /// every region's current amount (not just the changed row), same convention as
    /// SaveStateBudgetRequest, so an edited row's own previous value is never double-counted.
    /// </summary>
    public class SaveRegionBudgetRequest
    {
        public int StateId { get; set; }
        public string FY { get; set; } = string.Empty;
        public List<RegionBudgetAllocationRequest> Allocations { get; set; } = new();
    }

    /// <summary>
    /// Level 2 (SM) summary for one State+FY: the Total Budget / Allocated to Region /
    /// Remaining Amount triple from RegionBudgetSummary. All three are plain persisted values -
    /// never recomputed from each other - returned as 0/"Draft" when nothing has been saved yet.
    /// </summary>
    public class RegionBudgetSummaryDto
    {
        public int StateId { get; set; }
        public string FY { get; set; } = string.Empty;
        public decimal TotalBudget { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Status { get; set; } = "Draft";
    }

    /// <summary>
    /// Save (Draft) request for the Level 2 summary. All three values are whatever the SM
    /// typed - no server-side recomputation of RemainingAmount from TotalBudget - AllocatedAmount.
    /// </summary>
    public class SaveRegionBudgetSummaryRequest
    {
        public int StateId { get; set; }
        public string FY { get; set; } = string.Empty;
        public decimal TotalBudget { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
    }

    /// <summary>
    /// Submit request for the Level 2 summary: re-validates TotalBudget = AllocatedAmount +
    /// RemainingAmount, and that TotalBudget does not exceed the applicable State's own
    /// allocated amount (StateBudgetAllocation.Amount for the same State+FY).
    /// </summary>
    public class SubmitRegionBudgetSummaryRequest
    {
        public int StateId { get; set; }
        public string FY { get; set; } = string.Empty;
    }
}
