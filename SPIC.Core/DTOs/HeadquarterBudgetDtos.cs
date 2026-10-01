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
}
