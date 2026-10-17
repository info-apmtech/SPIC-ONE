using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Change this namespace to the namespace used by your existing SPIC.Core.Entities.
namespace SPIC.Core.Entities;

// One CSR-2 event report for an approved CSR-1. If your business permits
// multiple events/reports per CSR-1, remove the UNIQUE index shown in DbContextMappings.txt.
public class CSR2
{
    [Key] public int Id { get; set; }
    [Required] public int CSR1Id { get; set; }
    public CSR1? CSR1 { get; set; }

    [MaxLength(30)] public string Status { get; set; } = "Draft";
    public DateTime? ActualEventDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int? EventCoordinatorEmployeeId { get; set; }
    [MaxLength(500)] public string? ActualLocation { get; set; }
    public int? ExpectedParticipantsCount { get; set; }
    public bool IsOrganized { get; set; } = true;
    [MaxLength(2000)] public string? Remarks { get; set; }

    [Column(TypeName = "decimal(18,2)")] public decimal? ApprovedBudgetSnapshot { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(100)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    [MaxLength(100)] public string? UpdatedBy { get; set; }
    public DateTime? SubmittedAt { get; set; }
    [MaxLength(100)] public string? SubmittedBy { get; set; }

    public ICollection<CSR2Participant> Participants { get; set; } = new List<CSR2Participant>();
    public ICollection<CSR2Employee> Employees { get; set; } = new List<CSR2Employee>();
    public ICollection<CSR2Activity> Activities { get; set; } = new List<CSR2Activity>();
    public ICollection<CSR2Media> Media { get; set; } = new List<CSR2Media>();
    public ICollection<CSR2Expense> Expenses { get; set; } = new List<CSR2Expense>();
}

public class CSR2Participant
{
    [Key] public int Id { get; set; }
    public int CSR2Id { get; set; }
    public CSR2? CSR2 { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [MaxLength(20)] public string? PhoneNumber { get; set; }
    [Required, MaxLength(50)] public string Role { get; set; } = ""; // Farmer/Dealer/Distributor/Other
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CSR2Employee
{
    [Key] public int Id { get; set; }
    public int CSR2Id { get; set; }
    public CSR2? CSR2 { get; set; }
    [Required] public int EmployeeInformationId { get; set; }
    // Name / phone / role snapshots preserve what was displayed when the report was made.
    [Required, MaxLength(150)] public string EmployeeName { get; set; } = "";
    [MaxLength(20)] public string? PhoneNumber { get; set; }
    [Required, MaxLength(80)] public string RoleName { get; set; } = "";
}

public class CSR2Activity
{
    [Key] public int Id { get; set; }
    public int CSR2Id { get; set; }
    public CSR2? CSR2 { get; set; }
    [Required, MaxLength(200)] public string ActivityName { get; set; } = "";
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int? ParticipantsCount { get; set; }
    [Required, MaxLength(3000)] public string Description { get; set; } = "";
    [MaxLength(2000)] public string? Remarks { get; set; }
    public int SortOrder { get; set; }
}

// File bytes belong in private file/object storage, not in this table.
// Use authenticated upload/download APIs; do not expose physical disk paths.
public class CSR2Media
{
    [Key] public int Id { get; set; }
    public int CSR2Id { get; set; }
    public CSR2? CSR2 { get; set; }
    [Required, MaxLength(10)] public string MediaType { get; set; } = "Photo"; // Photo or Video
    [Required, MaxLength(260)] public string OriginalFileName { get; set; } = "";
    [Required, MaxLength(150)] public string ContentType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    [Required, MaxLength(600)] public string StorageKey { get; set; } = "";
    [MaxLength(500)] public string? Caption { get; set; }
    public bool UseInDigitalLibrary { get; set; } = true;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(100)] public string? UploadedBy { get; set; }
}

public class CSR2Expense
{
    [Key] public int Id { get; set; }
    public int CSR2Id { get; set; }
    public CSR2? CSR2 { get; set; }
    [Required, MaxLength(60)] public string CategoryKey { get; set; } = "";
    [Required, MaxLength(150)] public string CategoryName { get; set; } = "";
    [Column(TypeName = "decimal(18,2)")] public decimal PlannedAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal? ActualAmount { get; set; }
    public DateTime? ExpenseDate { get; set; }
    public ICollection<CSR2ExpenseProof> Proofs { get; set; } = new List<CSR2ExpenseProof>();
}

public class CSR2ExpenseProof
{
    [Key] public int Id { get; set; }
    public int CSR2ExpenseId { get; set; }
    public CSR2Expense? CSR2Expense { get; set; }
    [Required, MaxLength(260)] public string OriginalFileName { get; set; } = "";
    [Required, MaxLength(150)] public string ContentType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    [Required, MaxLength(600)] public string StorageKey { get; set; } = "";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
