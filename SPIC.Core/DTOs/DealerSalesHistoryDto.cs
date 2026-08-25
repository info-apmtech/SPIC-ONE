namespace SPIC.Core.DTOs;

public class DealerSalesHistoryDto
{
    public decimal AverageQuantityLifted3Years { get; set; }
    public decimal LastYearQuantityLifted { get; set; }
    public string QuantityRangeLabel { get; set; } = string.Empty;
    public string QuantityRangeValue { get; set; } = string.Empty;
    public bool IsSubDealerEligible { get; set; }
    public bool IsEmployeeEligible { get; set; }
    public List<DealerSalesYearlyDto> YearlyData { get; set; } = new();
}

public class DealerSalesYearlyDto
{
    public string FinancialYearName { get; set; } = string.Empty;
    public int FinancialYearId { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalGrossAmount { get; set; }
}

public class SubDealerDto
{
    public int Id { get; set; }
    public string SubDealerCode { get; set; } = string.Empty;
    public string FirmName { get; set; } = string.Empty;

    // Nominee / beneficiary details loaded from the Sub Dealer master
    public string? NomineeName { get; set; }
    public string? BeneficiaryName { get; set; }
    public DateTime? DOB { get; set; }
    public string? Relationship { get; set; }
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;

    // Nominee / beneficiary details loaded from the Employee master
    public string? BeneficiaryName { get; set; }
    public DateTime? DOB { get; set; }
    public string? Relationship { get; set; }
}

public class PendingApprovalItemDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string DealerCode { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? BeneficiaryName { get; set; }
    public string? NomineeName { get; set; }
    public bool? SMApproved { get; set; }
    public bool? AVPApproved { get; set; }
    public string OverallStatus { get; set; } = string.Empty;
}

public class ApprovalActionRequest
{
    public string? Remarks { get; set; }
}

public class ApprovalActionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string OverallStatus { get; set; } = string.Empty;
}

public class SubDealerEmployeeItemDto
{
    public int Id { get; set; }
    public long? BeneficiaryId { get; set; }
    public string DealerCode { get; set; } = "";
    public string MainDealerFirmName { get; set; } = "";
    public string? HQ { get; set; }
    public string? BranchDistrict { get; set; }
    public string SubDealerCode { get; set; } = "";
    public string SubDealerName { get; set; } = "";
    public string? SubDealerDistrict { get; set; }
    public string? NomineeName { get; set; }
    public string BeneficiaryName { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public DateTime? DOB { get; set; }
    public string? Relationship { get; set; }
    public string? MaritalStatus { get; set; }
    public string? EducationalQualification { get; set; }
    public bool IsActive { get; set; }
    public bool? SMApproved { get; set; }
    public bool? AVPApproved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = "";
}
