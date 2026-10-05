namespace SPIC.Core.Entities;

// ------------------------------------------------------------------------------------------------
// JMDO Task Allocation: a field officer's daily "Request" submission from the Tasks Allocation
// wizard (dealer tasks, SPCM/Soil targets, MD programs, PoS liquidation targets), awaiting a
// single MDO/RM approval step. Shape mirrors SampleCollection/SampleItem (SampleCollection.cs) -
// header row + FK'd child tables, never JSON/CSV columns for row-like children.
// ------------------------------------------------------------------------------------------------

/// <summary>Single-step JMDO -> MDO/RM approval, same 3-value shape as WelfareApprovalStatus/SamplePaymentStatus.</summary>
public enum JmdoTaskAllocationStatus { Pending = 0, Approved = 1, Rejected = 2 }

public class JmdoTaskAllocation
{
    public int Id { get; set; }

    /// <summary>Calendar date this allocation is for (server "today" at creation) - one submission per day.</summary>
    public DateTime AllocationDate { get; set; } = DateTime.Today;

    public string SubmittedByUserId { get; set; } = string.Empty;
    public string? SubmittedByRole { get; set; }

    // From the submitter's JWT claims at creation time, for location-scoped review queues later.
    public int? HeadquarterId { get; set; }
    public int? RegionId { get; set; }
    public int? StateId { get; set; }

    public int? SpcmTarget { get; set; }
    public int? SoilSampleTarget { get; set; }
    public int? UreaTarget { get; set; }
    public int? DapTarget { get; set; }
    public int? NpsTarget { get; set; }
    public int? OthersTarget { get; set; }

    public JmdoTaskAllocationStatus Status { get; set; } = JmdoTaskAllocationStatus.Pending;

    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewRemarks { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime SubmittedAt { get; set; } = DateTime.Now;

    public ICollection<JmdoTaskAllocationDealer> Dealers { get; set; } = new List<JmdoTaskAllocationDealer>();
    public ICollection<JmdoTaskAllocationProgram> Programs { get; set; } = new List<JmdoTaskAllocationProgram>();
}

/// <summary>One picked dealer task. Name/Code are a snapshot at submission time, independent of the
/// live SubDealerRegistration row, so edits to the dealer master don't change what was submitted.</summary>
public class JmdoTaskAllocationDealer
{
    public int Id { get; set; }
    public int AllocationId { get; set; }
    public JmdoTaskAllocation? Allocation { get; set; }

    public int SubDealerId { get; set; }
    public string DealerName { get; set; } = string.Empty;
    public string DealerCode { get; set; } = string.Empty;
}

/// <summary>One picked MD/training program. Name/Budget are a snapshot at submission time, same
/// reasoning as JmdoTaskAllocationDealer.</summary>
public class JmdoTaskAllocationProgram
{
    public int Id { get; set; }
    public int AllocationId { get; set; }
    public JmdoTaskAllocation? Allocation { get; set; }

    public int Csr1Id { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public decimal Budget { get; set; }
}
