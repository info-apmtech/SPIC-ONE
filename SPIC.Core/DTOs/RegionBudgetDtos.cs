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
}
