using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPIC.Core.Entities;

public class CSR2
{
    [Key] public int Id { get; set; }
    [Required] public int CSR1Id { get; set; }
    public CSR1? CSR1 { get; set; }
    public CSR2Status Status { get; set; } = CSR2Status.Draft;
    public DateTime? ActualEventDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? EventCoordinatorEmployeeId { get; set; }
    [MaxLength(500)] public string? ActualLocation { get; set; }
    public int? ExpectedParticipantsCount { get; set; }
    [MaxLength(2000)] public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(100)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    [MaxLength(100)] public string? UpdatedBy { get; set; }
    public DateTime? SubmittedAt { get; set; }
    [MaxLength(100)] public string? SubmittedBy { get; set; }
    public ICollection<CSR2Participant> Participants { get; set; } = new List<CSR2Participant>();
    public ICollection<CSR2Employee> Employees { get; set; } = new List<CSR2Employee>();
    public ICollection<CSR2Media> Media { get; set; } = new List<CSR2Media>();
    public ICollection<CSR2Expense> Expenses { get; set; } = new List<CSR2Expense>();
    public ICollection<CSR2FocusProduct> FocusProducts { get; set; } = new List<CSR2FocusProduct>();
}

public class CSR2FocusProduct
{
    [Key] public int Id { get; set; }
    public int CSR2Id { get; set; }
    public CSR2? CSR2 { get; set; }
    public int GroupNumber { get; set; }
    public int CropId { get; set; }
    public int ProductId { get; set; }
}

public class CSR2Participant
{
    [Key] public int Id { get; set; }
    public int CSR2Id { get; set; }
    public CSR2? CSR2 { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [MaxLength(20)] public string? PhoneNumber { get; set; }
    [Required, MaxLength(50)] public string Role { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CSR2Employee
{
    [Key] public int Id { get; set; }
    public int CSR2Id { get; set; }
    public CSR2? CSR2 { get; set; }

    // Changed from int to string as requested.
    [Required, MaxLength(100)]
    public string EmployeeInformationId { get; set; } = "";

    [Required, MaxLength(150)] public string EmployeeName { get; set; } = "";
    [MaxLength(20)] public string? PhoneNumber { get; set; }
    [Required, MaxLength(80)] public string RoleName { get; set; } = "";
}

public class CSR2Media
{
    [Key] public int Id { get; set; }
    public int CSR2Id { get; set; }
    public CSR2? CSR2 { get; set; }

    // Photo, Video or Document.
    [Required, MaxLength(20)] public string MediaType { get; set; } = "Photo";
    [Required, MaxLength(260)] public string OriginalFileName { get; set; } = "";
    [Required, MaxLength(150)] public string ContentType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    [Required, MaxLength(600)] public string StorageKey { get; set; } = "";
    [MaxLength(500)] public string? Caption { get; set; }
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
    [Column(TypeName = "decimal(18,2)")] public decimal? ActualAmount { get; set; }
    [MaxLength(100)] public string? BillNo { get; set; }
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

public enum CSR2Status
{
    Draft = 0,
    Submitted = 1
}
