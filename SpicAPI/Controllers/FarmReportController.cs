using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using System.Security.Claims;
using ClosedXML.Excel;

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

        public class InventoryMovementRowDto
        {
            public int ReportId { get; set; }
            public string ReportingPeriod { get; set; } = "";
            public DateTime PeriodStartDate { get; set; }
            public DateTime PeriodEndDate { get; set; }
            public string Status { get; set; } = "Draft";
            public int Opening { get; set; }
            public int Purchase { get; set; }
            public int Hatched { get; set; }
            public int Sold { get; set; }
            public int Mortality { get; set; }
            public int Others { get; set; }
            public int Closing { get; set; }
        }

        public class InventorySummaryStatsDto
        {
            public int TotalPurchased { get; set; }
            public int TotalHatched { get; set; }
            public int TotalSold { get; set; }
            public int TotalMortality { get; set; }
            public int ClosingStock { get; set; }
            public DateTime? ClosingStockAsOf { get; set; }
            public string Uom { get; set; } = "";
        }

        public class InventorySummaryResponseDto
        {
            public string? SelectedProduct { get; set; }
            public List<string> ReportingPeriods { get; set; } = new();
            public List<string> AvailableFinancialYears { get; set; } = new() { "2026-27", "2025-26", "2024-25" };
            public List<int> AvailableYears { get; set; } = new();
            public InventorySummaryStatsDto Summary { get; set; } = new();
            public List<InventoryMovementRowDto> Rows { get; set; } = new();
        }

        public class IncomeReportRowDto
        {
            public int ReportId { get; set; }
            public string ReportingPeriod { get; set; } = "";
            public DateTime PeriodStartDate { get; set; }
            public DateTime PeriodEndDate { get; set; }
            public decimal LivestockIncome { get; set; }
            public decimal OrchardIncome { get; set; }
            public decimal TotalIncome { get; set; }
            public string Status { get; set; } = "Draft";
        }

        public class ProductIncomeDto
        {
            public string ProductName { get; set; } = "";
            public decimal TotalAmount { get; set; }
        }

        public class IncomeSummaryResponseDto
        {
            public decimal TotalIncomeTillDate { get; set; }
            public DateTime? EarliestPeriodStart { get; set; }
            public int SalesRecordsCount { get; set; }
            public int LivestockProductsCount { get; set; }
            public int OrchardProductsCount { get; set; }
            public string? LatestReportingPeriod { get; set; }
            public DateTime? LatestPeriodEnd { get; set; }
            /// <summary>Days left in the (hardcoded, see GetIncomeSummary) edit-grace-window after
            /// LatestPeriodEnd - null once the window has closed, so the UI can show "no longer
            /// editable" instead of a negative number.</summary>
            public int? GraceDaysRemaining { get; set; }
            public List<IncomeReportRowDto> Rows { get; set; } = new();
            public List<ProductIncomeDto> TopLivestockProducts { get; set; } = new();
            public decimal TotalLivestockIncome { get; set; }
            public int OtherLivestockProductCount { get; set; }
            public List<ProductIncomeDto> TopOrchardProducts { get; set; } = new();
            public decimal TotalOrchardIncome { get; set; }
            public int OtherOrchardProductCount { get; set; }
            public List<int> AvailableYears { get; set; } = new();
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

        // GET api/FarmReport/inventory-summary
        // Real stock-movement history for one product across every 15-day report, filtered the same
        // way the Inventory Summary page's own Filters panel does (year/month/reporting
        // period/product - category only narrows which products api/FarmProduct/products returns for
        // the tab list, so it isn't repeated here). Never hardcoded: the reporting-period dropdown
        // options and the row data both come straight from FarmFifteenDayReports/InventoryItems.
        [HttpGet("inventory-summary")]
        public async Task<IActionResult> GetInventorySummary(
            [FromQuery] string? financialYear,
            [FromQuery] int? year,
            [FromQuery] int? month,
            [FromQuery] string? reportingPeriod,
            [FromQuery] string? product)
        {
            var (rows, periodOptions) = await QueryInventoryRowsAsync(financialYear, year, month, reportingPeriod, product);

            var uom = await _db.Set<FarmProduct>()
                .Where(p => p.Name == product)
                .Select(p => p.Uom)
                .FirstOrDefaultAsync() ?? "";

            var summary = new InventorySummaryStatsDto
            {
                TotalPurchased = rows.Sum(r => r.Purchase),
                TotalHatched = rows.Sum(r => r.Hatched),
                TotalSold = rows.Sum(r => r.Sold),
                TotalMortality = rows.Sum(r => r.Mortality),
                ClosingStock = rows.Count > 0 ? rows[^1].Closing : 0,
                ClosingStockAsOf = rows.Count > 0 ? rows[^1].PeriodEndDate : null,
                Uom = uom
            };

            var distinctYears = await _db.FarmFifteenDayReports
                .Select(r => r.FinancialYear)
                .Where(y => !string.IsNullOrEmpty(y))
                .Distinct()
                .ToListAsync();
            var availableYears = new List<string> { "2026-27", "2025-26", "2024-25" };
            foreach (var y in distinctYears)
                if (!availableYears.Contains(y)) availableYears.Add(y);

            // Calendar years actually present in the data (distinct from the "2026-27" financial-year
            // strings above) - the Year filter picks a literal PeriodStartDate.Year.
            var calendarYears = await _db.FarmFifteenDayReports
                .Select(r => r.PeriodStartDate.Year)
                .Distinct()
                .OrderDescending()
                .ToListAsync();

            return Ok(new InventorySummaryResponseDto
            {
                SelectedProduct = product,
                ReportingPeriods = periodOptions,
                AvailableFinancialYears = availableYears,
                AvailableYears = calendarYears,
                Summary = summary,
                Rows = rows
            });
        }

        // Shared by GetInventorySummary and ExportInventorySummaryExcel so the on-screen table and
        // the exported file can never disagree about which rows a given filter set matches.
        private async Task<(List<InventoryMovementRowDto> Rows, List<string> PeriodOptions)> QueryInventoryRowsAsync(
            string? financialYear, int? year, int? month, string? reportingPeriod, string? product)
        {
            var reportsQuery = _db.FarmFifteenDayReports.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(financialYear))
                reportsQuery = reportsQuery.Where(r => r.FinancialYear == financialYear);
            if (year is > 0)
                reportsQuery = reportsQuery.Where(r => r.PeriodStartDate.Year == year.Value);
            if (month is > 0)
                reportsQuery = reportsQuery.Where(r => r.PeriodStartDate.Month == month.Value);
            if (!string.IsNullOrWhiteSpace(reportingPeriod) && !string.Equals(reportingPeriod, "All Periods", StringComparison.OrdinalIgnoreCase))
                reportsQuery = reportsQuery.Where(r => r.ReportingPeriod == reportingPeriod);

            var periodOptions = await reportsQuery
                .OrderBy(r => r.PeriodStartDate)
                .Select(r => r.ReportingPeriod)
                .Distinct()
                .ToListAsync();

            var rows = new List<InventoryMovementRowDto>();
            if (!string.IsNullOrWhiteSpace(product) && !string.Equals(product, "All Product", StringComparison.OrdinalIgnoreCase))
            {
                rows = await reportsQuery
                    .OrderBy(r => r.PeriodStartDate)
                    .SelectMany(r => r.InventoryItems
                        .Where(i => i.ProductName == product)
                        .Select(i => new InventoryMovementRowDto
                        {
                            ReportId = r.Id,
                            ReportingPeriod = r.ReportingPeriod,
                            PeriodStartDate = r.PeriodStartDate,
                            PeriodEndDate = r.PeriodEndDate,
                            Status = r.Status,
                            Opening = i.Opening,
                            Purchase = i.Purchase,
                            Hatched = i.Hatched,
                            Sold = i.Sold,
                            Mortality = i.Mortality,
                            Others = i.Others,
                            Closing = i.Closing
                        }))
                    .ToListAsync();
            }

            return (rows, periodOptions);
        }

        // GET api/FarmReport/inventory-summary/export - same filters as inventory-summary, returned
        // as a real .xlsx (ClosedXML, same library/pattern AckCycleController's own Excel export uses).
        [HttpGet("inventory-summary/export")]
        public async Task<IActionResult> ExportInventorySummaryExcel(
            [FromQuery] string? financialYear,
            [FromQuery] int? year,
            [FromQuery] int? month,
            [FromQuery] string? reportingPeriod,
            [FromQuery] string? product)
        {
            var (rows, _) = await QueryInventoryRowsAsync(financialYear, year, month, reportingPeriod, product);

            using var workbook = new XLWorkbook();
            var sheetName = string.IsNullOrWhiteSpace(product) ? "Stock Movement" : product!.Length > 28 ? product[..28] : product;
            var worksheet = workbook.Worksheets.Add(sheetName);

            string[] headers = { "Period", "Opening Stock", "Purchase", "Hatched", "Sales", "Mortality", "Others", "Closing Stock" };
            for (var c = 0; c < headers.Length; c++)
                worksheet.Cell(1, c + 1).Value = headers[c];

            var headerRange = worksheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#065f46");
            headerRange.Style.Font.FontColor = XLColor.White;

            var rowNum = 2;
            foreach (var row in rows)
            {
                worksheet.Cell(rowNum, 1).Value = $"{row.PeriodStartDate:dd} - {row.PeriodEndDate:dd} {row.PeriodStartDate:MMM yyyy}";
                worksheet.Cell(rowNum, 2).Value = row.Opening;
                worksheet.Cell(rowNum, 3).Value = row.Purchase;
                worksheet.Cell(rowNum, 4).Value = row.Hatched;
                worksheet.Cell(rowNum, 5).Value = row.Sold;
                worksheet.Cell(rowNum, 6).Value = row.Mortality;
                worksheet.Cell(rowNum, 7).Value = row.Others;
                worksheet.Cell(rowNum, 8).Value = row.Closing;
                rowNum++;
            }

            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(1);

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileProduct = string.IsNullOrWhiteSpace(product) ? "Inventory" : product!.Replace(" ", "");
            return File(
                stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"InventorySummary_{fileProduct}_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
        }

        // How many days after a period closes the JMDO/Farm Office may still amend its income entry -
        // matches the Income Summary design's "Only 4 days Update after <date>" note. No real
        // deadline/grace-period concept exists anywhere else in this module yet (see
        // FarmFifteenDayReport - no Deadline/IsEditable field), so this is the one place that defines
        // it; move it onto the entity/config if a real per-report deadline is ever needed.
        private const int IncomeEditGraceDays = 4;

        // GET api/FarmReport/income-summary
        // Real income history across every 15-day report, filtered by calendar year and a
        // from/to-month range (both inclusive) - matching the Income Summary page's own Filters
        // panel. incomeCategory ("LiveStock"/"Orchard"/null=All) narrows every total and the table's
        // own Livestock/Orchard columns to just that group, same convention as inventory-summary's
        // category handling on the client side.
        [HttpGet("income-summary")]
        public async Task<IActionResult> GetIncomeSummary(
            [FromQuery] int? year,
            [FromQuery] int? fromMonth,
            [FromQuery] int? toMonth,
            [FromQuery] string? incomeCategory)
        {
            var reportsQuery = _db.FarmFifteenDayReports.AsNoTracking().AsQueryable();
            if (year is > 0)
                reportsQuery = reportsQuery.Where(r => r.PeriodStartDate.Year == year.Value);
            if (fromMonth is >= 1 and <= 12)
                reportsQuery = reportsQuery.Where(r => r.PeriodStartDate.Month >= fromMonth.Value);
            if (toMonth is >= 1 and <= 12)
                reportsQuery = reportsQuery.Where(r => r.PeriodStartDate.Month <= toMonth.Value);

            var reports = await reportsQuery
                .Include(r => r.IncomeItems)
                .OrderByDescending(r => r.PeriodStartDate)
                .ToListAsync();

            var categoryFilter = string.IsNullOrWhiteSpace(incomeCategory) || string.Equals(incomeCategory, "All", StringComparison.OrdinalIgnoreCase)
                ? null
                : incomeCategory;

            bool Included(string group) => categoryFilter is null || string.Equals(categoryFilter, group, StringComparison.OrdinalIgnoreCase);

            var rows = reports.Select(r =>
            {
                var liveStockIncome = r.IncomeItems.Where(i => i.Group == "LiveStock").Sum(i => i.TotalAmount);
                var orchardIncome = r.IncomeItems.Where(i => i.Group == "Orchard").Sum(i => i.TotalAmount);
                return new IncomeReportRowDto
                {
                    ReportId = r.Id,
                    ReportingPeriod = r.ReportingPeriod,
                    PeriodStartDate = r.PeriodStartDate,
                    PeriodEndDate = r.PeriodEndDate,
                    LivestockIncome = Included("LiveStock") ? liveStockIncome : 0,
                    OrchardIncome = Included("Orchard") ? orchardIncome : 0,
                    TotalIncome = (Included("LiveStock") ? liveStockIncome : 0) + (Included("Orchard") ? orchardIncome : 0),
                    Status = r.Status
                };
            }).ToList();

            var allIncomeItems = reports.SelectMany(r => r.IncomeItems)
                .Where(i => Included(i.Group))
                .ToList();

            var liveStockProducts = allIncomeItems.Where(i => i.Group == "LiveStock")
                .GroupBy(i => i.ProductName)
                .Select(g => new ProductIncomeDto { ProductName = g.Key, TotalAmount = g.Sum(i => i.TotalAmount) })
                .OrderByDescending(p => p.TotalAmount)
                .ToList();
            var orchardProducts = allIncomeItems.Where(i => i.Group == "Orchard")
                .GroupBy(i => i.ProductName)
                .Select(g => new ProductIncomeDto { ProductName = g.Key, TotalAmount = g.Sum(i => i.TotalAmount) })
                .OrderByDescending(p => p.TotalAmount)
                .ToList();

            var latest = reports.OrderByDescending(r => r.PeriodStartDate).FirstOrDefault();
            int? graceDaysRemaining = null;
            if (latest is not null)
            {
                var deadline = latest.PeriodEndDate.AddDays(IncomeEditGraceDays);
                var remaining = (deadline.Date - DateTime.Today).Days;
                if (remaining >= 0) graceDaysRemaining = remaining;
            }

            var calendarYears = await _db.FarmFifteenDayReports
                .Select(r => r.PeriodStartDate.Year)
                .Distinct()
                .OrderDescending()
                .ToListAsync();

            return Ok(new IncomeSummaryResponseDto
            {
                TotalIncomeTillDate = rows.Sum(r => r.TotalIncome),
                EarliestPeriodStart = reports.Count > 0 ? reports.Min(r => r.PeriodStartDate) : null,
                SalesRecordsCount = allIncomeItems.Count,
                LivestockProductsCount = liveStockProducts.Count,
                OrchardProductsCount = orchardProducts.Count,
                LatestReportingPeriod = latest?.ReportingPeriod,
                LatestPeriodEnd = latest?.PeriodEndDate,
                GraceDaysRemaining = graceDaysRemaining,
                Rows = rows,
                TopLivestockProducts = liveStockProducts.Take(5).ToList(),
                TotalLivestockIncome = liveStockProducts.Sum(p => p.TotalAmount),
                OtherLivestockProductCount = Math.Max(0, liveStockProducts.Count - 5),
                TopOrchardProducts = orchardProducts.Take(5).ToList(),
                TotalOrchardIncome = orchardProducts.Sum(p => p.TotalAmount),
                OtherOrchardProductCount = Math.Max(0, orchardProducts.Count - 5),
                AvailableYears = calendarYears
            });
        }

        // GET api/FarmReport/income-summary/export - same filters as income-summary, as a real .xlsx.
        [HttpGet("income-summary/export")]
        public async Task<IActionResult> ExportIncomeSummaryExcel(
            [FromQuery] int? year,
            [FromQuery] int? fromMonth,
            [FromQuery] int? toMonth,
            [FromQuery] string? incomeCategory)
        {
            var reportsQuery = _db.FarmFifteenDayReports.AsNoTracking().AsQueryable();
            if (year is > 0)
                reportsQuery = reportsQuery.Where(r => r.PeriodStartDate.Year == year.Value);
            if (fromMonth is >= 1 and <= 12)
                reportsQuery = reportsQuery.Where(r => r.PeriodStartDate.Month >= fromMonth.Value);
            if (toMonth is >= 1 and <= 12)
                reportsQuery = reportsQuery.Where(r => r.PeriodStartDate.Month <= toMonth.Value);

            var reports = await reportsQuery
                .Include(r => r.IncomeItems)
                .OrderByDescending(r => r.PeriodStartDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Income Summary");

            string[] headers = { "S.No", "Reporting Period", "Livestock Income", "Orchard Income", "Total Income", "Status" };
            for (var c = 0; c < headers.Length; c++)
                worksheet.Cell(1, c + 1).Value = headers[c];

            var headerRange = worksheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#065f46");
            headerRange.Style.Font.FontColor = XLColor.White;

            var rowNum = 2;
            var sNo = 1;
            foreach (var r in reports)
            {
                var liveStockIncome = r.IncomeItems.Where(i => i.Group == "LiveStock").Sum(i => i.TotalAmount);
                var orchardIncome = r.IncomeItems.Where(i => i.Group == "Orchard").Sum(i => i.TotalAmount);
                worksheet.Cell(rowNum, 1).Value = sNo++;
                worksheet.Cell(rowNum, 2).Value = r.ReportingPeriod;
                worksheet.Cell(rowNum, 3).Value = liveStockIncome;
                worksheet.Cell(rowNum, 4).Value = orchardIncome;
                worksheet.Cell(rowNum, 5).Value = liveStockIncome + orchardIncome;
                worksheet.Cell(rowNum, 6).Value = r.Status;
                rowNum++;
            }

            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(1);

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"IncomeSummary_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
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
