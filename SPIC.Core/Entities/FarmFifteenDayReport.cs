using System;
using System.Collections.Generic;

namespace SPIC.Core.Entities
{
    public class FarmFifteenDayReport
    {
        public int Id { get; set; }
        public required string ReportingPeriod { get; set; }
        public string FinancialYear { get; set; } = "2026-27";
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
        public string Status { get; set; } = "Draft"; // "Draft" or "Submitted"
        public string SubmittedBy { get; set; } = "";
        public DateTime? SubmittedAt { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetProfit { get; set; }
        public string? InventoryRemarks { get; set; }
        public string? IncomeRemarks { get; set; }
        public string? ExpenseRemarks { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public List<FarmFifteenDayInventoryItem> InventoryItems { get; set; } = new();
        public List<FarmFifteenDayIncomeItem> IncomeItems { get; set; } = new();
        public List<FarmFifteenDayExpenseItem> ExpenseItems { get; set; } = new();
    }

    public class FarmFifteenDayInventoryItem
    {
        public int Id { get; set; }
        public int ReportId { get; set; }
        public FarmFifteenDayReport? Report { get; set; }
        public required string ProductName { get; set; }
        public string? IconKey { get; set; }
        public int Opening { get; set; }
        public int Purchase { get; set; }
        public int Hatched { get; set; }
        public int Sold { get; set; }
        public int Mortality { get; set; }
        public int Others { get; set; }
        public int Closing { get; set; }
    }

    public class FarmFifteenDayIncomeItem
    {
        public int Id { get; set; }
        public int ReportId { get; set; }
        public FarmFifteenDayReport? Report { get; set; }
        public string Group { get; set; } = "LiveStock"; // "LiveStock" or "Orchard"
        public required string ProductName { get; set; }
        public string? IconKey { get; set; }
        public int SoldQuantity { get; set; }
        public string Uom { get; set; } = "";
        public decimal Rate { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class FarmFifteenDayExpenseItem
    {
        public int Id { get; set; }
        public int ReportId { get; set; }
        public FarmFifteenDayReport? Report { get; set; }
        public required string Category { get; set; }
        public decimal Amount { get; set; }
        public bool IsSubCategory { get; set; }
        public bool IsAutoCalculated { get; set; }
    }
}
