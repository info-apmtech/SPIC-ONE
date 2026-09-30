namespace SPIC.Core.DTOs
{
    // StateBudgetDto (one state's row in the State Budget Management table) already
    // exists in StateBudgetDto.cs - reused as-is, not redefined here.

    /// <summary>One state's allocation as submitted from the State Budget Management save action.</summary>
    public class StateBudgetAllocationRequest
    {
        public int StateId { get; set; }
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Full replace of every state's allocation for one FY. The client always sends every
    /// state's current amount (not just the changed row), so the sum submitted here IS the
    /// new total allocated for the FY - there is no partial/incremental save to reconcile.
    /// </summary>
    public class SaveStateBudgetRequest
    {
        public string FY { get; set; } = string.Empty;
        public List<StateBudgetAllocationRequest> Allocations { get; set; } = new();
    }

    /// <summary>Submit For Validation request: marks every saved allocation for one FY as Submitted.</summary>
    public class SubmitStateBudgetRequest
    {
        public string FY { get; set; } = string.Empty;
    }

    /// <summary>Current submission status for one FY's state allocations ("Draft" if none saved yet).</summary>
    public class StateBudgetStatusDto
    {
        public string Status { get; set; } = "Draft";
    }
}
