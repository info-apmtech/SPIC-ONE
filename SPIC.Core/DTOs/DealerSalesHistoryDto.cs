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
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
}
