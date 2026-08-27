using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using SPIC.Core.DTOs;
using Spic.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CreditLimitSalesController : ControllerBase
    {
        private readonly IGenericRepository<DealerCreditLimitSales> _creditLimitSalesRepo;
        private readonly AppDbContext _db;

        public CreditLimitSalesController(
            IGenericRepository<DealerCreditLimitSales> creditLimitSalesRepo,
            AppDbContext db)
        {
            _creditLimitSalesRepo = creditLimitSalesRepo;
            _db = db;
        }

        private static string Norm(string s) => s
            .Trim()
            .ToUpperInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "")
            .Replace("FERTILIZERS", "FERTILIZER")
            .Replace("FERTILISER", "FERTILIZER")
            .Replace("FERTLIZER", "FERTILIZER")
            .Replace("FETILIZER", "FERTILIZER");

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DealerCreditLimitSales model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var created = await _creditLimitSalesRepo.CreateAsync(model);
            return Ok(new { message = "Credit limit sales record created successfully", data = created });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _creditLimitSalesRepo.GetAll().ToListAsync();
            return Ok(items);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllWithInactive()
        {
            var items = await _creditLimitSalesRepo.GetAllWithInactive().ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _creditLimitSalesRepo.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DealerCreditLimitSales entity)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var updated = await _creditLimitSalesRepo.PatchAsync(id, entity);
            if (updated == null) return NotFound();
            return Ok(new { message = "Credit limit sales record updated successfully", data = updated });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _creditLimitSalesRepo.DeleteAsync(id);
            if (!deleted) return NotFound();
            return Ok(new { message = "Credit limit sales record deleted successfully" });
        }

        [HttpPatch("{id}/status")]
        public IActionResult ToggleStatus(int id, [FromQuery] bool isActive)
        {
            return BadRequest("Status toggling is not supported for this entity.");
        }
        //    [HttpGet("sample-template")]
        //    public async Task<IActionResult> SampleTemplate()
        //    {
        //        var headers = new[]
        //        {
        //    "State", "Customer Number", "Customer Name",
        //    "Product", "Product Group", "Category",
        //    "Quantity", "Gross Amount"
        //};

        //        // ── Pull a couple of REAL rows from the DB so the sample uploads cleanly ──
        //        var sampleRows = new List<string[]>();

        //        var dealers = await _db.DealerRegistrations
        //            .Where(d => !string.IsNullOrWhiteSpace(d.DealerCode) && !string.IsNullOrWhiteSpace(d.FirmName))
        //            .Select(d => new { d.DealerCode, d.FirmName, d.StateId })
        //            .Take(2)
        //            .ToListAsync();

        //        var products = await _db.Products
        //            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
        //            .Select(p => new { p.Name, p.CategoryId, p.ProductGroupId })
        //            .Take(2)
        //            .ToListAsync();

        //        var stateNameById = await _db.States
        //            .ToDictionaryAsync(s => s.Id, s => s.StateName);
        //        var categoryNameById = await _db.Categories
        //            .ToDictionaryAsync(c => c.Id, c => c.Name);
        //        var groupNameById = await _db.ProductGroups
        //            .ToDictionaryAsync(g => g.Id, g => g.Name);

        //        for (int i = 0; i < Math.Min(dealers.Count, 2); i++)
        //        {
        //            var d = dealers[i];
        //            var p = products.ElementAtOrDefault(i) ?? products.FirstOrDefault();

        //            var stateName = stateNameById.TryGetValue(d.StateId, out var sn) ? sn : "";
        //            var productName = p?.Name ?? "";
        //            var categoryName = p != null && categoryNameById.TryGetValue(p.CategoryId, out var cn) ? cn : "";
        //            var groupName = p?.ProductGroupId != null && groupNameById.TryGetValue(p.ProductGroupId.Value, out var gn) ? gn : "";

        //            sampleRows.Add(new[]
        //            {
        //        stateName, d.DealerCode, d.FirmName,
        //        productName, groupName, categoryName,
        //        (100 + i * 50).ToString(), ((100 + i * 50) * 266.5m).ToString("F2")
        //    });
        //        }

        //        // Fallback: if DB is empty, show placeholder text so the file isn't blank
        //        if (sampleRows.Count == 0)
        //        {
        //            sampleRows.Add(new[]
        //            {
        //        "<Existing State>", "<Existing Dealer Code>", "<Existing Dealer Name>",
        //        "<Existing Product>", "<Existing Product Group>", "<Existing Category>",
        //        "100", "26650.00"
        //    });
        //        }

        //        using var wb = new XLWorkbook();
        //        var ws = wb.Worksheets.Add("CreditLimitSales");

        //        // Row 1: title (NOT merged — merge interferes with header parsing on row 2)
        //        var titleCell = ws.Cell(1, 1);
        //        titleCell.Value = "Credit Limit Sales - Bulk Upload Template";
        //        titleCell.Style.Font.Bold = true;
        //        titleCell.Style.Font.FontColor = XLColor.FromHtml("#374151");

        //        // Row 2: headers (controller parses this row)
        //        for (int i = 0; i < headers.Length; i++)
        //        {
        //            var cell = ws.Cell(2, i + 1);
        //            cell.Value = headers[i];
        //            cell.Style.Font.Bold = true;
        //            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#059669");
        //            cell.Style.Font.FontColor = XLColor.White;
        //            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        //        }

        //        // Row 3+: real sample data
        //        for (int r = 0; r < sampleRows.Count; r++)
        //            for (int c = 0; c < sampleRows[r].Length; c++)
        //                ws.Cell(r + 3, c + 1).Value = sampleRows[r][c];

        //        ws.Columns().AdjustToContents();

        //        using var ms = new MemoryStream();
        //        wb.SaveAs(ms);
        //        var bytes = ms.ToArray();

        //        return File(bytes,
        //            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        //            "CreditLimitSales_Sample_Template.xlsx");
        //    }
        [HttpGet("sample-template")]
        public IActionResult SampleTemplate()
        {
            var headers = new[]
            {
            "State", "Customer Number", "Customer Name",
            "Product", "Product Group", "Category",
            "Quantity", "Gross Amount"
        };

            var sampleRows = new[]
            {
            new[] { "Tamil Nadu", "D1001", "Sri Krishna Agencies", "Urea 50kg", "Urea Group", "Fertilizer", "100", "26650.00" },
            new[] { "Tamil Nadu", "D1002", "Bharathi Traders",     "DAP 50kg",  "DAP Group",  "Fertilizer", "50",  "67500.00" },
        };

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("CreditLimitSales");

            // Row 1: title (the upload reads headers from row 2, so row 1 is free for a title)
            var titleCell = ws.Cell(1, 1);
            titleCell.Value = "Credit Limit Sales - Bulk Upload Template ";
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontColor = XLColor.FromHtml("#374151");
            //ws.Range(1, 1, 1, headers.Length).Merge();

            // Row 2: headers (this is the row the controller parses)
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(2, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#059669");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Row 3+: sample data
            for (int r = 0; r < sampleRows.Length; r++)
                for (int c = 0; c < sampleRows[r].Length; c++)
                    ws.Cell(r + 3, c + 1).Value = sampleRows[r][c];

            ws.Columns().AdjustToContents();
            //ws.SheetView.Freeze(2, 0);
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "CreditLimitSales_Sample_Template.xlsx");
        }
        [HttpPost("bulk-upload")]
        public async Task<IActionResult> BulkUpload(
            IFormFile file,
            [FromQuery] int financialYearId)
        {
            if (financialYearId <= 0)
                return BadRequest(new { Success = false, Message = "Financial Year is required." });

            if (file == null || file.Length == 0)
                return BadRequest(new { Success = false, Message = "No file uploaded." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                return BadRequest(new { Success = false, Message = "Only Excel files (.xlsx/.xls) are supported." });

            try
            {
                var financialYearExists = await _db.FinancialYears
                    .AnyAsync(x => x.Id == financialYearId);

                if (!financialYearExists)
                    return BadRequest(new { Success = false, Message = "Invalid Financial Year selected." });

                var dealerRawList = await _db.DealerRegistrations
                    .Where(d => !string.IsNullOrWhiteSpace(d.DealerCode))
                    .Select(d => new
                    {
                        d.Id,
                        d.StateId,
                        Code = d.DealerCode,
                        SpicCode = d.SPICCode,
                        GreenStarCode = d.GreenStarCode,
                        TnCode = d.TnCode,
                        d.FirmName
                    })
                    .ToListAsync();

                var dealerByCode = new Dictionary<string, dynamic>(StringComparer.OrdinalIgnoreCase);

                foreach (var d in dealerRawList)
                {
                    void TryAdd(string? raw)
                    {
                        if (string.IsNullOrWhiteSpace(raw)) return;

                        var key = raw.Trim().ToUpperInvariant();

                        if (!dealerByCode.ContainsKey(key))
                            dealerByCode[key] = d;

                        if (key.Length > 1 && char.IsLetter(key[0]))
                        {
                            var numeric = key.Substring(1);
                            if (!dealerByCode.ContainsKey(numeric))
                                dealerByCode[numeric] = d;
                        }
                    }

                    TryAdd(d.Code);
                    TryAdd(d.SpicCode);
                    TryAdd(d.GreenStarCode);
                    TryAdd(d.TnCode);
                }

                var dealerByName = dealerRawList
                    .Where(d => !string.IsNullOrWhiteSpace(d.FirmName))
                    .GroupBy(d => d.FirmName.Trim().ToUpperInvariant())
                    .ToDictionary(g => g.Key, g => (dynamic)g.First());

                var productMap = (await _db.Products
                    .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                    .Select(p => new { p.Id, p.CategoryId, p.Name })
                    .ToListAsync())
                    .GroupBy(p => Norm(p.Name))
                    .ToDictionary(g => g.Key, g => g.First());

                var categoryMap = (await _db.Categories
                    .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync())
                    .GroupBy(c => Norm(c.Name))
                    .ToDictionary(g => g.Key, g => g.First().Id);

                var productGroupMap = (await _db.ProductGroups
                    .Where(pg => !string.IsNullOrWhiteSpace(pg.Name))
                    .Select(pg => new { pg.Id, pg.Name })
                    .ToListAsync())
                    .GroupBy(pg => Norm(pg.Name))
                    .ToDictionary(g => g.Key, g => g.First().Id);

                var stateMap = (await _db.States
                    .Where(s => !string.IsNullOrWhiteSpace(s.StateName))
                    .Select(s => new { s.Id, Name = s.StateName })
                    .ToListAsync())
                    .GroupBy(s => s.Name.Trim().ToUpperInvariant())
                    .ToDictionary(g => g.Key, g => g.First().Id);

                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                    return BadRequest(new { Success = false, Message = "Excel worksheet not found." });

                var headerRow = worksheet.Row(2);
                var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var cell in headerRow.CellsUsed())
                {
                    var name = cell.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(name) && !headers.ContainsKey(name))
                        headers[name] = cell.Address.ColumnNumber;
                }

                int ColIndex(params string[] names)
                {
                    foreach (var n in names)
                        if (headers.TryGetValue(n, out int c))
                            return c;

                    return -1;
                }

                int colState = ColIndex("State", "StateName", "State Name");
                int colDealerCode = ColIndex("Customer Number", "CustomerNumber", "Dealer Code", "DealerCode", "Code", "SPICCode", "GreenStarCode", "TnCode");
                int colDealerName = ColIndex("Customer Name", "CustomerName", "Dealer Name", "DealerName", "Firm Name", "FirmName");

                int colProduct = ColIndex(
    "Product Name",
    "ProductName",
    "Product",
    "Sub Group",
    "SubGroup",
    "Product SubGroup",
    "ProductSubGroup"
);

                int colProductGroup = ColIndex(
                    "Product Groups",
                    "ProductGroups",
                    "Product Group",
                    "ProductGroup",
                    "Product Group Name",
                    "ProductGroupName"
                );

                int colCategory = ColIndex(
                    "Categories",
                    "Category",
                    "CategoryName"
                );

                int colQuantity = ColIndex("Quantity", "Qty");
                int colGrossAmount = ColIndex("Gross Amount", "GrossAmount", "Amount", "Gross");

                if (colDealerCode < 0 && colDealerName < 0)
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Could not find 'Customer Number' or 'Customer Name' column. " +
                                  $"Columns found: {string.Join(", ", headers.Keys)}"
                    });

                if (colProduct < 0)
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Could not find 'Product' or 'Sub Group' column. " +
                                  $"Columns found: {string.Join(", ", headers.Keys)}"
                    });

                if (colQuantity < 0 || colGrossAmount < 0)
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Could not find 'Quantity' or 'Gross Amount' column. " +
                                  $"Columns found: {string.Join(", ", headers.Keys)}"
                    });

                var salesRecords = new List<DealerCreditLimitSales>();
                var invalidRows = new List<string>();
                int rowNumber = 2;

                foreach (var row in worksheet.RowsUsed().Skip(2))
                {
                    rowNumber++;

                    string Cell(int colIdx) =>
                        colIdx > 0 ? row.Cell(colIdx).GetString()?.Trim() ?? "" : "";

                    var dealerCodeRaw = Cell(colDealerCode);
                    var dealerNameRaw = Cell(colDealerName);
                    var productRaw = Cell(colProduct);
                    var categoryRaw = Cell(colCategory);
                    var productGroupRaw = Cell(colProductGroup);
                    var stateRaw = Cell(colState);
                    var quantityRaw = Cell(colQuantity);
                    var amountRaw = Cell(colGrossAmount);

                    if (string.IsNullOrWhiteSpace(dealerCodeRaw) &&
                        string.IsNullOrWhiteSpace(dealerNameRaw) &&
                        string.IsNullOrWhiteSpace(productRaw))
                        continue;

                    var rowErrors = new List<string>();

                    dynamic? dealer = null;

                    if (!string.IsNullOrWhiteSpace(dealerCodeRaw))
                    {
                        var codeKey = dealerCodeRaw.Trim().ToUpperInvariant();

                        if (!dealerByCode.TryGetValue(codeKey, out dealer))
                        {
                            if (codeKey.Length > 1 && char.IsLetter(codeKey[0]))
                                dealerByCode.TryGetValue(codeKey.Substring(1), out dealer);
                        }
                    }

                    if (dealer == null && !string.IsNullOrWhiteSpace(dealerNameRaw))
                    {
                        var nameKey = dealerNameRaw.Trim().ToUpperInvariant();
                        dealerByName.TryGetValue(nameKey, out dealer);
                    }

                    if (dealer == null && !string.IsNullOrWhiteSpace(dealerNameRaw))
                    {
                        var nameKey = dealerNameRaw.Trim().ToUpperInvariant();
                        var partial = dealerByName.Keys
                            .FirstOrDefault(k => k.StartsWith(nameKey) || nameKey.StartsWith(k));

                        if (partial != null)
                            dealer = dealerByName[partial];
                    }

                    if (dealer == null)
                    {
                        invalidRows.Add(
                            $"Row {rowNumber}: Dealer not found " +
                            $"(Code='{dealerCodeRaw}', Name='{dealerNameRaw}').");
                        continue;
                    }

                    int customerId = dealer.Id;
                    int stateId = dealer.StateId;

                    string dealerCode = !string.IsNullOrWhiteSpace(dealerCodeRaw)
                        ? dealerCodeRaw.Trim()
                        : (string)dealer.Code;

                    if (stateId <= 0 && !string.IsNullOrWhiteSpace(stateRaw))
                        stateMap.TryGetValue(stateRaw.Trim().ToUpperInvariant(), out stateId);

                    if (stateId <= 0)
                    {
                        invalidRows.Add(
                            $"Row {rowNumber}: State could not be resolved for dealer '{dealerCode}'.");
                        continue;
                    }

                    int productId = 0;
                    int categoryId = 0;
                    int productGroupId = 0;

                    if (!string.IsNullOrWhiteSpace(productRaw))
                    {
                        var normProduct = Norm(productRaw);

                        if (productMap.TryGetValue(normProduct, out var prod))
                        {
                            productId = prod.Id;
                            categoryId = prod.CategoryId;
                        }
                        else
                        {
                            var startMatch = productMap.Keys
                                .FirstOrDefault(k => k.StartsWith(normProduct) || normProduct.StartsWith(k));

                            if (startMatch != null)
                            {
                                productId = productMap[startMatch].Id;
                                categoryId = productMap[startMatch].CategoryId;
                            }
                            else
                            {
                                var containsMatch = productMap.Keys
                                    .FirstOrDefault(k => k.Contains(normProduct) || normProduct.Contains(k));

                                if (containsMatch != null)
                                {
                                    productId = productMap[containsMatch].Id;
                                    categoryId = productMap[containsMatch].CategoryId;
                                }
                                else
                                {
                                    rowErrors.Add($"Product '{productRaw}' not found");
                                }
                            }
                        }
                    }
                    else
                    {
                        rowErrors.Add("Product is empty");
                    }

                    if (categoryId <= 0 && !string.IsNullOrWhiteSpace(categoryRaw))
                    {
                        var normCat = Norm(categoryRaw);

                        if (categoryMap.TryGetValue(normCat, out int catId))
                        {
                            categoryId = catId;
                        }
                        else
                        {
                            var startMatch = categoryMap.Keys
                                .FirstOrDefault(k => k.StartsWith(normCat) || normCat.StartsWith(k));

                            if (startMatch != null)
                            {
                                categoryId = categoryMap[startMatch];
                            }
                            else
                            {
                                var containsMatch = categoryMap.Keys
                                    .FirstOrDefault(k => k.Contains(normCat) || normCat.Contains(k));

                                if (containsMatch != null)
                                    categoryId = categoryMap[containsMatch];
                                else
                                    rowErrors.Add($"Category '{categoryRaw}' not found");
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(productGroupRaw))
                    {
                        var normGroup = Norm(productGroupRaw);

                        if (productGroupMap.TryGetValue(normGroup, out int groupId))
                        {
                            productGroupId = groupId;
                        }
                        else
                        {
                            var startMatch = productGroupMap.Keys
                                .FirstOrDefault(k => k.StartsWith(normGroup) || normGroup.StartsWith(k));

                            if (startMatch != null)
                            {
                                productGroupId = productGroupMap[startMatch];
                            }
                            else
                            {
                                var containsMatch = productGroupMap.Keys
                                    .FirstOrDefault(k => k.Contains(normGroup) || normGroup.Contains(k));

                                if (containsMatch != null)
                                    productGroupId = productGroupMap[containsMatch];
                                else
                                    rowErrors.Add($"Product Group '{productGroupRaw}' not found");
                            }
                        }
                    }

                    if (!decimal.TryParse(quantityRaw,
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out decimal quantity))
                    {
                        rowErrors.Add($"Invalid Quantity '{quantityRaw}'");
                        quantity = 0;
                    }

                    if (!decimal.TryParse(amountRaw,
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out decimal grossAmount))
                    {
                        rowErrors.Add($"Invalid Gross Amount '{amountRaw}'");
                        grossAmount = 0;
                    }

                    if (rowErrors.Any())
                    {
                        invalidRows.Add($"Row {rowNumber}: {string.Join("; ", rowErrors)}.");
                        continue;
                    }

                    salesRecords.Add(new DealerCreditLimitSales
                    {
                        FinancialYearId = financialYearId,
                        StateId = stateId,
                        CustomerNumber = dealerCode,
                        CustomerId = customerId,
                        ProductId = productId,
                        CategoryId = categoryId,
                        ProductGroupId = productGroupId,
                        Quantity = quantity,
                        GrossAmount = grossAmount
                    });
                }

                if (salesRecords.Count == 0)
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "The uploaded file contains no valid data rows.",
                        InvalidRows = invalidRows.Take(50).ToList()
                    });

                var oldRows = _db.DealerCreditLimitSalesData
                    .Where(x => x.FinancialYearId == financialYearId);

                _db.DealerCreditLimitSalesData.RemoveRange(oldRows);
                await _db.SaveChangesAsync();

                const int batchSize = 5000;

                for (int i = 0; i < salesRecords.Count; i += batchSize)
                {
                    _db.DealerCreditLimitSalesData.AddRange(
                        salesRecords.Skip(i).Take(batchSize));

                    await _db.SaveChangesAsync();
                    _db.ChangeTracker.Clear();
                }

                var summary = new StringBuilder();
                summary.Append($"Upload completed. {salesRecords.Count} rows inserted");

                if (invalidRows.Count > 0)
                    summary.Append($", {invalidRows.Count} rows skipped");

                summary.Append(".");

                return Ok(new
                {
                    Success = true,
                    Message = summary.ToString(),
                    FinancialYearId = financialYearId,
                    InsertedRows = salesRecords.Count,
                    SkippedRows = invalidRows.Count,
                    InvalidRows = invalidRows.Take(50).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Internal server error during bulk upload.",
                    Error = ex.Message
                });
            }
        }

        [HttpGet("by-filter")]
        public async Task<IActionResult> GetByFilter(
            [FromQuery] int customerId,
            [FromQuery] int categoryId,
            [FromQuery] string financialYearIds)
        {
            if (customerId <= 0) return BadRequest("CustomerId is required.");
            if (categoryId <= 0) return BadRequest("CategoryId is required.");

            var fyIds = (financialYearIds ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x.Trim(), out var id) ? id : 0)
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (fyIds.Count == 0)
                return Ok(new List<DealerCreditLimitSales>());

            var data = await _db.DealerCreditLimitSalesData
                .AsNoTracking()
                .Where(x =>
                    x.CustomerId == customerId &&
                    x.CategoryId == categoryId &&
                    fyIds.Contains(x.FinancialYearId))
                .GroupBy(x => new
                {
                    x.CustomerId,
                    x.CustomerNumber,
                    x.StateId,
                    x.CategoryId,
                    x.ProductGroupId,
                    x.ProductId,
                    x.FinancialYearId
                })
                .Select(g => new DealerCreditLimitSales
                {
                    Id = 0,
                    CustomerId = g.Key.CustomerId,
                    CustomerNumber = g.Key.CustomerNumber,
                    StateId = g.Key.StateId,
                    CategoryId = g.Key.CategoryId,
                    ProductGroupId = g.Key.ProductGroupId,
                    ProductId = g.Key.ProductId,
                    FinancialYearId = g.Key.FinancialYearId,
                    Quantity = g.Sum(x => x.Quantity),
                    GrossAmount = g.Sum(x => x.GrossAmount)
                })
                .ToListAsync();

            return Ok(data);
        }

        // Dealer sales summary for the MO approval page.
        // Returns the financial years that hold CreditLimitSales data for the
        // given dealer, plus per-(financial-year, product) quantities and
        // gross amounts so the frontend can roll these up into the
        // Urea / DAP / 20:20 / SSP / Total / Turnover columns.
        [HttpGet("dealer-sales-summary/{dealerId:int}")]
        public async Task<IActionResult> GetDealerSalesSummary(int dealerId)
        {
            if (dealerId <= 0)
                return BadRequest("DealerId is required.");

            var products = await _db.Products.AsNoTracking().ToListAsync();
            var financialYears = await _db.FinancialYears.AsNoTracking().ToListAsync();

            var grouped = await _db.DealerCreditLimitSalesData
                .AsNoTracking()
                .Where(x => x.CustomerId == dealerId)
                .GroupBy(x => new { x.FinancialYearId, x.ProductId })
                .Select(g => new
                {
                    g.Key.FinancialYearId,
                    g.Key.ProductId,
                    Quantity = g.Sum(x => x.Quantity),
                    GrossAmount = g.Sum(x => x.GrossAmount)
                })
                .ToListAsync();

            // Always offer the 3 most recent active financial years in the
            // dropdowns, regardless of whether the dealer has sales data for
            // them. Per-year sales figures are populated when data exists and
            // shown as "-" otherwise (mirroring the CreditLimit page behaviour).
            var availableYears = financialYears
                .Where(f => f.IsActive)
                .OrderByDescending(f => f.StartDate)
                .Take(3)
                .Select(f => new DealerSalesYearOptionDto { Id = f.Id, Name = f.Name })
                .ToList();

            var productSales = new List<DealerSalesProductDto>();

            foreach (var g in grouped)
            {
                var product = products.FirstOrDefault(p => p.Id == g.ProductId);
                if (product == null || string.IsNullOrWhiteSpace(product.Name))
                    continue;

                productSales.Add(new DealerSalesProductDto
                {
                    FinancialYearId = g.FinancialYearId,
                    ProductId = g.ProductId,
                    ProductName = product.Name,
                    Quantity = g.Quantity,
                    GrossAmount = g.GrossAmount
                });
            }

            return Ok(new DealerSalesSummaryDto
            {
                AvailableYears = availableYears,
                ProductSales = productSales
            });
        }
    }
}