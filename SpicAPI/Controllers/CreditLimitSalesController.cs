using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using Spic.Infrastructure.Data; // Ensure this matches your AppDbContext namespace
using System;
using System.Collections.Generic;
using System.Linq;
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

        // Injecting AppDbContext to allow dictionary lookups during bulk upload
        public CreditLimitSalesController(
            IGenericRepository<DealerCreditLimitSales> creditLimitSalesRepo,
            AppDbContext db)
        {
            _creditLimitSalesRepo = creditLimitSalesRepo;
            _db = db;
        }

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

        [HttpPost("bulk-upload")]
        public async Task<IActionResult> BulkUpload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Success = false, Message = "No file uploaded" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                return BadRequest(new { Success = false, Message = "Only Excel files (.xlsx/.xls) are supported" });

            try
            {
                // 1. SAFELY LOAD ALL LOOKUP TABLES (Grouped to prevent Duplicate Key Crashes)
                // Load lookups into memory first so we can safely normalize strings
                var stateMap = (await _db.States
                    .Where(s => !string.IsNullOrWhiteSpace(s.StateName))
                    .Select(s => new { s.Id, Name = s.StateName })
                    .ToListAsync())
                    .GroupBy(s => s.Name.Trim().ToUpperInvariant())
                    .ToDictionary(g => g.Key, g => g.First().Id);

                var dealerMap = (await _db.DealerRegistrations
                    .Where(d => !string.IsNullOrWhiteSpace(d.DealerCode))
                    .Select(d => new { d.Id, Code = d.DealerCode })
                    .ToListAsync())
                    .GroupBy(d => d.Code.Trim().ToUpperInvariant())
                    .ToDictionary(g => g.Key, g => g.First().Id);

                // Also build a map by dealer FirmName so uploads that include Dealer Name instead of DealerCode still map
                var dealerNameMap = (await _db.DealerRegistrations
                    .Where(d => !string.IsNullOrWhiteSpace(d.FirmName))
                    .Select(d => new { d.Id, Name = d.FirmName })
                    .ToListAsync())
                    .GroupBy(d => d.Name.Trim().ToUpperInvariant())
                    .ToDictionary(g => g.Key, g => g.First().Id);

                var productMap = (await _db.Products
                    .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                    .Select(p => new { p.Id, Name = p.Name })
                    .ToListAsync())
                    .GroupBy(p => p.Name.Trim().ToUpperInvariant())
                    .ToDictionary(g => g.Key, g => g.First().Id);

                var categoryMap = (await _db.Categories
                    .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                    .Select(c => new { c.Id, Name = c.Name })
                    .ToListAsync())
                    .GroupBy(c => c.Name.Trim().ToUpperInvariant())
                    .ToDictionary(g => g.Key, g => g.First().Id);

                var salesRecords = new List<DealerCreditLimitSales>();

                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.First();

                var rows = worksheet.RowsUsed().Skip(1);

                foreach (var row in rows)
                {
                    // 2. READ STRINGS DIRECTLY FROM EXCEL CELLS (7-Column Layout)
                    var stateStr = row.Cell(1).GetString()?.Trim() ?? "";
                    var dealerCodeStr = row.Cell(2).GetString()?.Trim() ?? "";
                    // Cell(3) is Dealer Name — some uploads use Dealer Name instead of Dealer Code
                    var dealerNameStr = row.Cell(3).GetString()?.Trim() ?? "";
                    var subGroupStr = row.Cell(4).GetString()?.Trim() ?? "";
                    var productGroupStr = row.Cell(5).GetString()?.Trim() ?? "";
                    var quantityStr = row.Cell(6).GetString()?.Trim() ?? "0";
                    var grossAmountStr = row.Cell(7).GetString()?.Trim() ?? "0";

                    // Normalize lookup keys
                    var stateKey = stateStr.ToUpperInvariant();
                    var dealerCodeKey = dealerCodeStr.ToUpperInvariant();
                    var dealerNameKey = dealerNameStr.ToUpperInvariant();
                    var subGroupKey = subGroupStr.ToUpperInvariant();
                    var productGroupKey = productGroupStr.ToUpperInvariant();
                    // 3. SECURELY MAP EXCEL STRINGS TO DATABASE IDs
                    var record = new DealerCreditLimitSales
                    {
                        StateId = stateMap.TryGetValue(stateKey, out var sId) ? sId : 0,
                        // Prefer mapping by DealerCode. If not found, attempt mapping by Dealer Name.
                        CustomerNumber = dealerCodeStr,
                        CustomerId = dealerMap.TryGetValue(dealerCodeKey, out var cId) ? cId : (dealerNameMap.TryGetValue(dealerNameKey, out var cId2) ? cId2 : 0),
                        SubGroupId = productMap.TryGetValue(subGroupKey, out var sgId) ? sgId : productMap.Where(kvp => kvp.Key.Contains(subGroupKey) || subGroupKey.Contains(kvp.Key)).Select(kvp => kvp.Value).FirstOrDefault(),
                        ProductGroupId = categoryMap.TryGetValue(productGroupKey, out var pgId) ? pgId : categoryMap.Where(kvp => kvp.Key.Contains(productGroupKey) || productGroupKey.Contains(kvp.Key)).Select(kvp => kvp.Value).FirstOrDefault(),

                        Quantity = int.TryParse(quantityStr, out var qty) ? qty : 0,
                        GrossAmount = double.TryParse(grossAmountStr, out var gross) ? gross : 0
                    };

                    // If DealerCode missing but we found an ID by name, populate CustomerNumber from DB
                    if (record.CustomerId > 0 && string.IsNullOrWhiteSpace(record.CustomerNumber))
                    {
                        var dr = await _db.DealerRegistrations.FindAsync(record.CustomerId);
                        if (dr != null && !string.IsNullOrWhiteSpace(dr.DealerCode))
                            record.CustomerNumber = dr.DealerCode;
                    }

                    // Only save row if a Dealer Code or ID exists
                    if (!string.IsNullOrWhiteSpace(record.CustomerNumber) || record.CustomerId > 0)
                    {
                        salesRecords.Add(record);
                    }
                }

                if (salesRecords.Count == 0)
                    return BadRequest(new { Success = false, Message = "The uploaded file contains no valid data rows." });

                // 4. SAVE TO DATABASE
                foreach (var item in salesRecords)
                {
                    await _creditLimitSalesRepo.CreateAsync(item);
                }

                return Ok(new
                {
                    message = $"Upload completed successfully. {salesRecords.Count} rows inserted."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Internal server error during bulk upload", Error = ex.Message });
            }
        }
    }
}