using System;
using System.Collections.Generic;

namespace SPIC.Core.DTOs;

public class CSR2SaveDto
{
    public int? Id { get; set; }
    public int CSR1Id { get; set; }
    public DateTime? ActualEventDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int? EventCoordinatorEmployeeId { get; set; }
    public string? ActualLocation { get; set; }
    public int? ExpectedParticipantsCount { get; set; }
    public bool IsOrganized { get; set; } = true;
    public List<CSR2ParticipantDto> Participants { get; set; } = new();
    public List<CSR2EmployeeDto> Employees { get; set; } = new();
    public List<CSR2ActivityDto> Activities { get; set; } = new();
    public List<CSR2ExpenseDto> Expenses { get; set; } = new();
}
public class CSR2ParticipantDto { public string Name { get; set; } = ""; public string? PhoneNumber { get; set; } public string Role { get; set; } = ""; }
public class CSR2EmployeeDto { public int EmployeeInformationId { get; set; } public string EmployeeName { get; set; } = ""; public string? PhoneNumber { get; set; } public string RoleName { get; set; } = ""; }
public class CSR2ActivityDto { public string ActivityName { get; set; } = ""; public TimeOnly StartTime { get; set; } public TimeOnly EndTime { get; set; } public int? ParticipantsCount { get; set; } public string Description { get; set; } = ""; public string? Remarks { get; set; } public int SortOrder { get; set; } }
public class CSR2ExpenseDto { public string CategoryKey { get; set; } = ""; public string CategoryName { get; set; } = ""; public decimal PlannedAmount { get; set; } public decimal? ActualAmount { get; set; } public DateTime? ExpenseDate { get; set; } public int? ProofId { get; set; } public string? ProofFileName { get; set; } }
public class CSR2MediaDto { public int Id { get; set; } public string MediaType { get; set; } = ""; public string OriginalFileName { get; set; } = ""; public string? Caption { get; set; } public bool UseInDigitalLibrary { get; set; } public long FileSizeBytes { get; set; } }
public class CSR2DetailsDto : CSR2SaveDto { public string Status { get; set; } = "Draft"; public List<CSR2MediaDto> Media { get; set; } = new(); }
