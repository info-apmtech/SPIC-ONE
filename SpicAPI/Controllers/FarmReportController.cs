using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FarmReportController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<FarmReportController> _logger;

        public FarmReportController(AppDbContext db, ILogger<FarmReportController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // =====================================================================
        //  DTOs
        // =====================================================================

        public class SaveFarmReportDto
        {
            public int? Id { get; set; }
            public string ReportingPeriod { get; set; } = "";
            public string FinancialYear { get; set; } = "2026-27";
            public DateTime? PeriodStartDate { get; set; }
            public DateTime? PeriodEndDate { get; set; }
            public string Status { get; set; } = "Draft"; // "Draft" or "Submitted"
            public string? SubmittedBy { get; set; }
            public decimal TotalIncome { get; set; }
            public decimal TotalExpense { get; set; }
            public decimal NetProfit { get; set; }
            public string? InventoryRemarks { get; set; }
            public string? IncomeRemarks { get; set; }
            public string? ExpenseRemarks { get; set; }
            public List<InventoryItemDto> InventoryItems { get; set; } = new();
            public List<IncomeItemDto> IncomeItems { get; set; } = new();
            public List<ExpenseItemDto> ExpenseItems { get; set; } = new();
        }

        public class InventoryItemDto
        {
            public int Id { get; set; }
            public string ProductName { get; set; } = "";
            public string? IconKey { get; set; }
            public int Opening { get; set; }
            public int Purchase { get; set; }
            public int Hatched { get; set; }
            public int Sold { get; set; }
            public int Mortality { get; set; }
            public int Others { get; set; }
            public int Closing { get; set; }
        }

        public class IncomeItemDto
        {
            public int Id { get; set; }
            public string Group { get; set; } = "LiveStock";
            public string ProductName { get; set; } = "";
            public string? IconKey { get; set; }
            public int SoldQuantity { get; set; }
            public string Uom { get; set; } = "";
            public decimal Rate { get; set; }
            public decimal TotalAmount { get; set; }
        }

        public class ExpenseItemDto
        {
            public int Id { get; set; }
            public string Category { get; set; } = "";
            public decimal Amount { get; set; }
            public bool IsSubCategory { get; set; }
            public bool IsAutoCalculated { get; set; }
        }

        public class FarmReportDetailsDto
        {
            public int Id { get; set; }
            public string ReportingPeriod { get; set; } = "";
            public string FinancialYear { get; set; } = "";
            public DateTime PeriodStartDate { get; set; }
            public DateTime PeriodEndDate { get; set; }
            public string Status { get; set; } = "Draft";
            public string SubmittedBy { get; set; } = "";
            public DateTime? SubmittedAt { get; set; }
            public decimal TotalIncome { get; set; }
            public decimal TotalExpense { get; set; }
            public decimal NetProfit { get; set; }
            public string? InventoryRemarks { get; set; }
            public string? IncomeRemarks { get; set; }
            public string? ExpenseRemarks { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public List<InventoryItemDto> InventoryItems { get; set; } = new();
            public List<IncomeItemDto> IncomeItems { get; set; } = new();
            public List<ExpenseItemDto> ExpenseItems { get; set; } = new();
        }

        public class ReportHistoryItemDto
        {
            public int Id { get; set; }
            public string ReportingPeriod { get; set; } = "";
            public string FinancialYear { get; set; } = "";
            public decimal Income { get; set; }
            public decimal Expense { get; set; }
            public decimal NetResult { get; set; }
            public string Status { get; set; } = "Draft";
            public DateTime? SubmittedAt { get; set; }
            public string SubmittedBy { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public DateTime? PeriodStartDate { get; set; }
            public DateTime? PeriodEndDate { get; set; }
        }

        public class FinanceSummaryDto
        {
            public decimal AnnualBudget { get; set; } = 2345000;
            public decimal TotalIncome { get; set; }
            public decimal TotalExpense { get; set; }
            public decimal NetProfit { get; set; }
            public decimal Roi { get; set; }
            public List<string> AvailableFinancialYears { get; set; } = new() { "2026-27", "2025-26", "2024-25" };
            public List<ChartTrendPointDto> ChartPoints { get; set; } = new();
            public List<ReportHistoryItemDto> History { get; set; } = new();
        }

        public class ChartTrendPointDto
        {
            public string PeriodLabel { get; set; } = "";
            public decimal Income { get; set; }
            public decimal Expense { get; set; }
            public decimal NetProfit { get; set; }
            public DateTime? Date { get; set; }
        }

        // =====================================================================
        //  ENDPOINTS
        // =====================================================================

        /// <summary>
        /// Save a 15-day report as Draft or Submitted.
        /// </summary>
        [HttpPost("save")]
        public async Task<IActionResult> SaveReport([FromBody] SaveFarmReportDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid report payload");

            try
            {
                FarmFifteenDayReport? report = null;

                if (dto.Id.HasValue && dto.Id.Value > 0)
                {
                    report = await _db.FarmFifteenDayReports
                        .Include(r => r.InventoryItems)
                        .Include(r => r.IncomeItems)
                        .Include(r => r.ExpenseItems)
                        .FirstOrDefaultAsync(r => r.Id == dto.Id.Value);
                }

                if (report == null && !string.IsNullOrWhiteSpace(dto.ReportingPeriod))
                {
                    report = await _db.FarmFifteenDayReports
                        .Include(r => r.InventoryItems)
                        .Include(r => r.IncomeItems)
                        .Include(r => r.ExpenseItems)
                        .FirstOrDefaultAsync(r => r.ReportingPeriod == dto.ReportingPeriod.Trim());
                }

                bool isNew = report == null;
                if (isNew)
                {
                    report = new FarmFifteenDayReport
                    {
                        ReportingPeriod = string.IsNullOrWhiteSpace(dto.ReportingPeriod) ? "01 Apr 2026 - 15 Apr 2026" : dto.ReportingPeriod.Trim(),
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.FarmFifteenDayReports.Add(report);
                }

                report!.FinancialYear = string.IsNullOrWhiteSpace(dto.FinancialYear) ? "2026-27" : dto.FinancialYear;
                report.PeriodStartDate = dto.PeriodStartDate ?? new DateTime(2026, 4, 1);
                report.PeriodEndDate = dto.PeriodEndDate ?? new DateTime(2026, 4, 15);
                report.Status = dto.Status ?? "Draft";
                report.TotalIncome = dto.TotalIncome;
                report.TotalExpense = dto.TotalExpense;
                report.NetProfit = dto.NetProfit;
                report.InventoryRemarks = dto.InventoryRemarks;
                report.IncomeRemarks = dto.IncomeRemarks;
                report.ExpenseRemarks = dto.ExpenseRemarks;
                report.UpdatedAt = DateTime.UtcNow;

                var currentUserName = User.Identity?.Name ?? dto.SubmittedBy;
                if (string.IsNullOrWhiteSpace(currentUserName))
                    currentUserName = "Farm Office";

                if (dto.Status == "Submitted")
                {
                    report.SubmittedBy = currentUserName;
                    report.SubmittedAt = DateTime.UtcNow;
                }

                // Replace child items
                if (!isNew)
                {
                    _db.FarmFifteenDayInventoryItems.RemoveRange(report.InventoryItems);
                    _db.FarmFifteenDayIncomeItems.RemoveRange(report.IncomeItems);
                    _db.FarmFifteenDayExpenseItems.RemoveRange(report.ExpenseItems);
                }

                report.InventoryItems = dto.InventoryItems.Select(x => new FarmFifteenDayInventoryItem
                {
                    ProductName = x.ProductName,
                    IconKey = x.IconKey,
                    Opening = x.Opening,
                    Purchase = x.Purchase,
                    Hatched = x.Hatched,
                    Sold = x.Sold,
                    Mortality = x.Mortality,
                    Others = x.Others,
                    Closing = x.Closing
                }).ToList();

                report.IncomeItems = dto.IncomeItems.Select(x => new FarmFifteenDayIncomeItem
                {
                    Group = x.Group,
                    ProductName = x.ProductName,
                    IconKey = x.IconKey,
                    SoldQuantity = x.SoldQuantity,
                    Uom = x.Uom,
                    Rate = x.Rate,
                    TotalAmount = x.TotalAmount
                }).ToList();

                report.ExpenseItems = dto.ExpenseItems.Select(x => new FarmFifteenDayExpenseItem
                {
                    Category = x.Category,
                    Amount = x.Amount,
                    IsSubCategory = x.IsSubCategory,
                    IsAutoCalculated = x.IsAutoCalculated
                }).ToList();

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = report.Status == "Submitted" ? "Report submitted successfully." : "Draft saved successfully.",
                    reportId = report.Id,
                    status = report.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving 15-day report");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private static FarmReportDetailsDto MapToDetailsDto(FarmFifteenDayReport r)
        {
            return new FarmReportDetailsDto
            {
                Id = r.Id,
                ReportingPeriod = r.ReportingPeriod,
                FinancialYear = r.FinancialYear,
                PeriodStartDate = r.PeriodStartDate,
                PeriodEndDate = r.PeriodEndDate,
                Status = r.Status,
                SubmittedBy = r.SubmittedBy,
                SubmittedAt = r.SubmittedAt,
                TotalIncome = r.TotalIncome,
                TotalExpense = r.TotalExpense,
                NetProfit = r.NetProfit,
                InventoryRemarks = r.InventoryRemarks,
                IncomeRemarks = r.IncomeRemarks,
                ExpenseRemarks = r.ExpenseRemarks,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                InventoryItems = r.InventoryItems.Select(i => new InventoryItemDto
                {
                    Id = i.Id,
                    ProductName = i.ProductName,
                    IconKey = i.IconKey,
                    Opening = i.Opening,
                    Purchase = i.Purchase,
                    Hatched = i.Hatched,
                    Sold = i.Sold,
                    Mortality = i.Mortality,
                    Others = i.Others,
                    Closing = i.Closing
                }).ToList(),
                IncomeItems = r.IncomeItems.Select(i => new IncomeItemDto
                {
                    Id = i.Id,
                    Group = i.Group,
                    ProductName = i.ProductName,
                    IconKey = i.IconKey,
                    SoldQuantity = i.SoldQuantity,
                    Uom = i.Uom,
                    Rate = i.Rate,
                    TotalAmount = i.TotalAmount
                }).ToList(),
                ExpenseItems = r.ExpenseItems.Select(e => new ExpenseItemDto
                {
                    Id = e.Id,
                    Category = e.Category,
                    Amount = e.Amount,
                    IsSubCategory = e.IsSubCategory,
                    IsAutoCalculated = e.IsAutoCalculated
                }).ToList()
            };
        }

        /// <summary>
        /// Get report by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetReportById(int id)
        {
            var report = await _db.FarmFifteenDayReports
                .Include(r => r.InventoryItems)
                .Include(r => r.IncomeItems)
                .Include(r => r.ExpenseItems)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
                return NotFound(new { message = $"Report with ID {id} not found." });

            return Ok(MapToDetailsDto(report));
        }

        /// <summary>
        /// Get report for a period or get the latest.
        /// </summary>
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestReport([FromQuery] string? period = null)
        {
            IQueryable<FarmFifteenDayReport> query = _db.FarmFifteenDayReports
                .Include(r => r.InventoryItems)
                .Include(r => r.IncomeItems)
                .Include(r => r.ExpenseItems);

            FarmFifteenDayReport? report;
            if (!string.IsNullOrWhiteSpace(period))
            {
                var cleanPeriod = period.Trim();
                report = await query.FirstOrDefaultAsync(r => r.ReportingPeriod.ToLower() == cleanPeriod.ToLower());
            }
            else
            {
                report = await query.OrderByDescending(r => r.PeriodStartDate).FirstOrDefaultAsync();
            }

            if (report == null)
                return Ok((object?)null);

            return Ok(MapToDetailsDto(report));
        }

        /// <summary>
        /// Get history for the Finance Summary table.
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetReportHistory(
            [FromQuery] string? financialYear = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var query = _db.FarmFifteenDayReports.AsQueryable();

            if (!string.IsNullOrWhiteSpace(financialYear))
                query = query.Where(r => r.FinancialYear == financialYear);

            if (fromDate.HasValue)
                query = query.Where(r => r.PeriodStartDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(r => r.PeriodEndDate <= toDate.Value);

            var reports = await query
                .OrderByDescending(r => r.PeriodStartDate)
                .Select(r => new ReportHistoryItemDto
                {
                    Id = r.Id,
                    ReportingPeriod = r.ReportingPeriod,
                    FinancialYear = r.FinancialYear,
                    Income = r.TotalIncome,
                    Expense = r.TotalExpense,
                    NetResult = r.NetProfit,
                    Status = r.Status,
                    SubmittedAt = r.SubmittedAt,
                    SubmittedBy = r.SubmittedBy,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(reports);
        }

        /// <summary>
        /// Get overall finance summary including KPIs, chart trend points, and history.
        /// </summary>
        [HttpGet("finance-summary")]
        public async Task<IActionResult> GetOverallFinanceSummary([FromQuery] string? financialYear = "2026-27")
        {
            if (string.IsNullOrWhiteSpace(financialYear))
                financialYear = "2026-27";

            var reports = await _db.FarmFifteenDayReports
                .Include(r => r.InventoryItems)
                .Include(r => r.IncomeItems)
                .Include(r => r.ExpenseItems)
                .Where(r => r.FinancialYear == financialYear)
                .OrderBy(r => r.PeriodStartDate)
                .ToListAsync();

            decimal totalIncome = reports.Sum(r => r.TotalIncome);
            decimal totalExpense = reports.Sum(r => r.TotalExpense);
            decimal netProfit = totalIncome - totalExpense;

            decimal annualBudget = financialYear switch
            {
                "2026-27" => 2345000m,
                "2025-26" => 2150000m,
                "2024-25" => 1900000m,
                _ => 2000000m
            };

            try
            {
                var fy = await _db.Set<FinancialYear>().FirstOrDefaultAsync(f => f.Name == financialYear);
                if (fy != null)
                {
                    var ab = await _db.Set<AnnualBudgeting>().FirstOrDefaultAsync(b => b.FinancialYearId == fy.Id && b.IsActive);
                    if (ab != null && ab.Amount > 0)
                    {
                        annualBudget = ab.Amount;
                    }
                }
            }
            catch { }

            decimal roi = annualBudget > 0 ? Math.Round((netProfit / annualBudget) * 100m, 1) : 0m;

            var chartPoints = reports.Select(r => new ChartTrendPointDto
            {
                PeriodLabel = FormatPeriodLabel(r.ReportingPeriod, r.PeriodStartDate, r.PeriodEndDate),
                Income = r.TotalIncome,
                Expense = r.TotalExpense,
                NetProfit = r.NetProfit,
                Date = r.PeriodStartDate
            }).ToList();

            var history = reports
                .OrderByDescending(r => r.PeriodStartDate)
                .Select(r => new ReportHistoryItemDto
                {
                    Id = r.Id,
                    ReportingPeriod = r.ReportingPeriod,
                    FinancialYear = r.FinancialYear,
                    Income = r.TotalIncome,
                    Expense = r.TotalExpense,
                    NetResult = r.NetProfit,
                    Status = r.Status,
                    SubmittedAt = r.SubmittedAt,
                    SubmittedBy = r.SubmittedBy,
                    CreatedAt = r.CreatedAt,
                    PeriodStartDate = r.PeriodStartDate,
                    PeriodEndDate = r.PeriodEndDate
                }).ToList();

            var distinctYears = await _db.FarmFifteenDayReports
                .Select(r => r.FinancialYear)
                .Where(y => !string.IsNullOrEmpty(y))
                .Distinct()
                .ToListAsync();

            var availableYears = new List<string> { "2026-27", "2025-26", "2024-25" };
            foreach (var y in distinctYears)
            {
                if (!availableYears.Contains(y))
                    availableYears.Add(y);
            }

            return Ok(new FinanceSummaryDto
            {
                AnnualBudget = annualBudget,
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                NetProfit = netProfit,
                Roi = roi,
                AvailableFinancialYears = availableYears,
                ChartPoints = chartPoints,
                History = history
            });
        }

        private static string FormatPeriodLabel(string period, DateTime? start, DateTime? end)
        {
            if (start.HasValue && end.HasValue)
            {
                return $"{start.Value:dd}-{end.Value:dd} {start.Value:MMM}";
            }
            if (start.HasValue)
            {
                return $"{start.Value:dd} {start.Value:MMM}";
            }
            var clean = period.Replace("  ", " ").Trim();
            var parts = clean.Split('-');
            if (parts.Length >= 2)
            {
                var s = parts[0].Trim();
                var right = parts[1].Trim();
                var subParts = right.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (subParts.Length >= 2)
                {
                    return $"{s}-{subParts[0]} {subParts[1]}";
                }
            }
            return period;
        }
    }
}
