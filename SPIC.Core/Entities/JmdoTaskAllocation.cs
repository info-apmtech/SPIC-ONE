namespace SPIC.Core.Entities;

// ------------------------------------------------------------------------------------------------
// JMDO Task Allocation: a field officer's daily "Request" submission from the Tasks Allocation
// wizard (dealer tasks, SPCM/Soil targets, MD programs, PoS liquidation targets), awaiting a
// single MDO/RM approval step. Shape mirrors SampleCollection/SampleItem (SampleCollection.cs) -
// header row + FK'd child tables, never JSON/CSV columns for row-like children.
// ------------------------------------------------------------------------------------------------

/// <summary>Single-step JMDO -> MDO/RM approval, same 3-value shape as WelfareApprovalStatus/SamplePaymentStatus.</summary>
public enum JmdoTaskAllocationStatus { Pending = 0, Approved = 1, Rejected = 2 }

/// <summary>A JMDO's field-work progress on one task (a dealer visit, an MD program, or one of the
/// Spcm/Soil/PosLiquidation targets) - distinct from JmdoTaskAllocationStatus, which is the MDO's
/// approval status of the whole week's submission, not progress on any individual task within it.</summary>
public enum AllocTaskStatus { YetToStart = 0, InProgress = 1, Completed = 2 }

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

    // Spcm/Soil/PosLiquidation are one-per-week-allocation (unlike dealers/programs, which get
    // their own child rows), so their field-work progress lives directly on the header.
    public AllocTaskStatus SpcmStatus { get; set; } = AllocTaskStatus.YetToStart;
    public AllocTaskStatus SoilSampleStatus { get; set; } = AllocTaskStatus.YetToStart;
    public AllocTaskStatus PosLiquidationStatus { get; set; } = AllocTaskStatus.YetToStart;

    // Stamped when the matching Status above is set to InProgress, so "Time on task" can be
    // computed as DateTime.Now - StartedAt on every page load instead of resetting to 0.
    public DateTime? SpcmStartedAt { get; set; }
    public DateTime? SoilSampleStartedAt { get; set; }
    public DateTime? PosLiquidationStartedAt { get; set; }

    public JmdoTaskAllocationStatus Status { get; set; } = JmdoTaskAllocationStatus.Pending;

    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewRemarks { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime SubmittedAt { get; set; } = DateTime.Now;

    public ICollection<JmdoTaskAllocationDealer> Dealers { get; set; } = new List<JmdoTaskAllocationDealer>();
    public ICollection<JmdoTaskAllocationProgram> Programs { get; set; } = new List<JmdoTaskAllocationProgram>();
    public ICollection<JmdoTaskCloseout> Closeouts { get; set; } = new List<JmdoTaskCloseout>();
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

    /// <summary>Weekday this dealer visit is planned for (Sun-Sat tabs in the wizard). Nullable so
    /// rows created before this field existed don't need a backfilled value.</summary>
    public DayOfWeek? PlannedDay { get; set; }

    /// <summary>Exact calendar date the MDO pins this task to during review/approval - the JMDO only
    /// picks a weekday (PlannedDay) at submission time; this is set later and separately.</summary>
    public DateTime? PlannedDate { get; set; }

    /// <summary>The JMDO's field-work progress on this specific dealer visit (Start/Stop on TaskDetail).</summary>
    public AllocTaskStatus Status { get; set; } = AllocTaskStatus.YetToStart;

    /// <summary>Stamped when Status is set to InProgress - lets "Time on task" resume from the
    /// real elapsed time instead of 0 on every page load.</summary>
    public DateTime? StartedAt { get; set; }
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

    /// <summary>The JMDO's field-work progress on this specific MD program (Start/Stop on TaskDetail).</summary>
    public AllocTaskStatus Status { get; set; } = AllocTaskStatus.YetToStart;

    /// <summary>Stamped when Status is set to InProgress - lets "Time on task" resume from the
    /// real elapsed time instead of 0 on every page load.</summary>
    public DateTime? StartedAt { get; set; }
}

/// <summary>Which kind of task a JmdoTaskCloseout record closes out - mirrors TaskDetail.razor's
/// own TaskKind, since each kind's close-out form captures different extra fields.</summary>
public enum JmdoCloseoutKind { Dealer = 0, Spcm = 1, MdProgram = 2, PosLiquidation = 3 }

/// <summary>One "Save Record" submission from TaskDetail's close-out form. A task can accumulate
/// several of these over time (the UI supports adding more than one visit record per task), so this
/// is its own child table under JmdoTaskAllocation rather than a single set of columns.</summary>
public class JmdoTaskCloseout
{
    public int Id { get; set; }
    public int AllocationId { get; set; }
    public JmdoTaskAllocation? Allocation { get; set; }

    public JmdoCloseoutKind Kind { get; set; }

    /// <summary>Which JmdoTaskAllocationDealer this closes out, for Kind == Dealer.</summary>
    public int? SubDealerId { get; set; }

    /// <summary>Which JmdoTaskAllocationProgram this closes out, for Kind == MdProgram.</summary>
    public int? Csr1Id { get; set; }

    public string Outcome { get; set; } = string.Empty;
    public string? Remarks { get; set; }

    /// <summary>Kind == Spcm only.</summary>
    public int? AchievedTarget { get; set; }

    /// <summary>Kind == PosLiquidation only.</summary>
    public int? Urea { get; set; }
    public int? Nps { get; set; }
    public int? Dap { get; set; }
    public int? Other { get; set; }

    /// <summary>Web-relative path under wwwroot (e.g. "/uploads/jmdo-closeouts/{userId}/{guid}.pdf"),
    /// same convention as SubDealerFileController - null when no file was attached.</summary>
    public string? FilePath { get; set; }
    public string? FileName { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<JmdoTaskCloseoutProduct> Products { get; set; } = new List<JmdoTaskCloseoutProduct>();
}

/// <summary>One product line added via TaskDetail's "Add another Product" popup - only populated
/// when the parent JmdoTaskCloseout.Kind == Dealer. Name/Group/Stock are a free-text snapshot at
/// save time, same reasoning as JmdoTaskAllocationDealer's DealerName/DealerCode.</summary>
public class JmdoTaskCloseoutProduct
{
    public int Id { get; set; }
    public int CloseoutId { get; set; }
    public JmdoTaskCloseout? Closeout { get; set; }

    public string ProductGroupName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string StockText { get; set; } = string.Empty;
}
