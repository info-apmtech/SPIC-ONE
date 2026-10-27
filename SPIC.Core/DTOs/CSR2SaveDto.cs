namespace SPIC.Core.DTOs;

public class CSR2SaveDto
{
    public int? Id { get; set; }
    public int CSR1Id { get; set; }
    public DateTime? ActualEventDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? EventCoordinatorEmployeeId { get; set; }
    public string? ActualLocation { get; set; }
    public int? ExpectedParticipantsCount { get; set; }
    public string? Remarks { get; set; }

    public CSR2FocusGroupDto Products1 { get; set; } = new();
    public CSR2FocusGroupDto Products2 { get; set; } = new();
    public CSR2FocusGroupDto Products3 { get; set; } = new();

    public List<CSR2ParticipantDto> Participants { get; set; } = new();
    public List<CSR2EmployeeDto> Employees { get; set; } = new();
    public List<CSR2ExpenseDto> Expenses { get; set; } = new();
}

public class CSR2DetailsDto : CSR2SaveDto
{
    public string Status { get; set; } = "";
    public List<CSR2MediaDto> Media { get; set; } = new();
}

public class CSR2FocusGroupDto
{
    public int[] CropId { get; set; } = Array.Empty<int>();
    public int[] ProductId { get; set; } = Array.Empty<int>();
}

public class CSR2ParticipantDto
{
    public int? ParticipantSourceId { get; set; }
    public string? ParticipantSource { get; set; }
    public string Role { get; set; } = "";

    // Display only; not persisted in CSR2Participants.
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }
}

public class CSR2EmployeeDto
{
    public string EmployeeInformationId { get; set; } = "";
    public string RoleName { get; set; } = "";

    // Display only; not persisted in CSR2Employees.
    public string? EmployeeName { get; set; }
    public string? PhoneNumber { get; set; }
}

public class CSR2ExpenseDto
{
    public int? Id { get; set; }
    public int ExpenseInformationId { get; set; }
    public string? ExpenseName { get; set; }
    public decimal? ActualAmount { get; set; }
    public string? BillNo { get; set; }
    public DateTime? ExpenseDate { get; set; }
    public List<CSR2ExpenseProofDto> Proofs { get; set; } = new();
}

public class CSR2ExpenseProofDto
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = "";
}

public class CSR2MediaDto
{
    public int Id { get; set; }
    public string MediaType { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public string? Caption { get; set; }
    public long FileSizeBytes { get; set; }
}
