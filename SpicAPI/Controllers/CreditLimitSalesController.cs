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
		public async Task<IActionResult> BulkUpload(
			IFormFile file,
			[FromQuery] int financialYearId)
		{
			if (financialYearId <= 0)
				return BadRequest(new
				{
					Success = false,
					Message = "Financial Year is required."
				});

			if (file == null || file.Length == 0)
				return BadRequest(new
				{
					Success = false,
					Message = "No file uploaded"
				});

			var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

			if (ext != ".xlsx" && ext != ".xls")
				return BadRequest(new
				{
					Success = false,
					Message = "Only Excel files (.xlsx/.xls) are supported"
				});

			try
			{
				// Optional: validate selected Financial Year exists
				var financialYearExists = await _db.FinancialYears
					.AnyAsync(x => x.Id == financialYearId);

				if (!financialYearExists)
					return BadRequest(new
					{
						Success = false,
						Message = "Invalid Financial Year selected."
					});

				// 1. SAFELY LOAD ALL LOOKUP TABLES
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

				var dealerNameMap = (await _db.DealerRegistrations
					.Where(d => !string.IsNullOrWhiteSpace(d.FirmName))
					.Select(d => new { d.Id, Name = d.FirmName })
					.ToListAsync())
					.GroupBy(d => d.Name.Trim().ToUpperInvariant())
					.ToDictionary(g => g.Key, g => g.First().Id);

				var dealerCodeByIdMap = await _db.DealerRegistrations
					.Where(d => !string.IsNullOrWhiteSpace(d.DealerCode))
					.Select(d => new { d.Id, d.DealerCode })
					.ToDictionaryAsync(d => d.Id, d => d.DealerCode);

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
				var invalidRows = new List<string>();

				using var stream = file.OpenReadStream();
				using var workbook = new XLWorkbook(stream);

				var worksheet = workbook.Worksheets.FirstOrDefault();

				if (worksheet == null)
					return BadRequest(new
					{
						Success = false,
						Message = "Excel worksheet not found."
					});

				var rows = worksheet.RowsUsed().Skip(1);
				var rowNumber = 1;

				foreach (var row in rows)
				{
					rowNumber++;

					// 2. READ STRINGS DIRECTLY FROM EXCEL CELLS
					var stateStr = row.Cell(1).GetString()?.Trim() ?? "";
					var dealerCodeStr = row.Cell(2).GetString()?.Trim() ?? "";
					var dealerNameStr = row.Cell(3).GetString()?.Trim() ?? "";
					var subGroupStr = row.Cell(4).GetString()?.Trim() ?? "";
					var productGroupStr = row.Cell(5).GetString()?.Trim() ?? "";
					var quantityStr = row.Cell(6).GetString()?.Trim() ?? "0";
					var grossAmountStr = row.Cell(7).GetString()?.Trim() ?? "0";

					var stateKey = stateStr.ToUpperInvariant();
					var dealerCodeKey = dealerCodeStr.ToUpperInvariant();
					var dealerNameKey = dealerNameStr.ToUpperInvariant();
					var subGroupKey = subGroupStr.ToUpperInvariant();
					var productGroupKey = productGroupStr.ToUpperInvariant();

					var stateId = stateMap.TryGetValue(stateKey, out var sId) ? sId : 0;

					var customerId = dealerMap.TryGetValue(dealerCodeKey, out var cId)
						? cId
						: dealerNameMap.TryGetValue(dealerNameKey, out var cId2)
							? cId2
							: 0;

					var subGroupId = productMap.TryGetValue(subGroupKey, out var sgId)
						? sgId
						: productMap
							.Where(kvp =>
								!string.IsNullOrWhiteSpace(subGroupKey) &&
								(kvp.Key.Contains(subGroupKey) || subGroupKey.Contains(kvp.Key)))
							.Select(kvp => kvp.Value)
							.FirstOrDefault();

					var productGroupId = categoryMap.TryGetValue(productGroupKey, out var pgId)
						? pgId
						: categoryMap
							.Where(kvp =>
								!string.IsNullOrWhiteSpace(productGroupKey) &&
								(kvp.Key.Contains(productGroupKey) || productGroupKey.Contains(kvp.Key)))
							.Select(kvp => kvp.Value)
							.FirstOrDefault();

					if (customerId <= 0)
					{
						invalidRows.Add($"Row {rowNumber}: Dealer not found");
						continue;
					}

					if (stateId <= 0)
					{
						invalidRows.Add($"Row {rowNumber}: State not found");
						continue;
					}

					if (subGroupId <= 0)
					{
						invalidRows.Add($"Row {rowNumber}: Product SubGroup not found");
						continue;
					}

					if (productGroupId <= 0)
					{
						invalidRows.Add($"Row {rowNumber}: Product Group not found");
						continue;
					}

					var quantity = int.TryParse(quantityStr, out var qty) ? qty : 0;
					var grossAmount = decimal.TryParse(grossAmountStr, out var gross) ? gross : 0;

					if (string.IsNullOrWhiteSpace(dealerCodeStr) &&
						dealerCodeByIdMap.TryGetValue(customerId, out var dealerCodeFromDb))
					{
						dealerCodeStr = dealerCodeFromDb;
					}

					var record = new DealerCreditLimitSales
					{
						StateId = stateId,
						CustomerNumber = dealerCodeStr,
						CustomerId = customerId,
						SubGroupId = subGroupId,
						ProductGroupId = productGroupId,
						FinancialYearId = financialYearId,
						Quantity = quantity,
						GrossAmount = grossAmount
					};

					salesRecords.Add(record);
				}

				if (salesRecords.Count == 0)
				{
					return BadRequest(new
					{
						Success = false,
						Message = "The uploaded file contains no valid data rows.",
						InvalidRows = invalidRows.Take(50).ToList()
					});
				}

				// IMPORTANT:
				// Keep this block if one Excel file is full replacement for selected financial year.
				// Remove this block if you want to append data instead.
				var oldRows = _db.DealerCreditLimitSalesData
					.Where(x => x.FinancialYearId == financialYearId);

				_db.DealerCreditLimitSalesData.RemoveRange(oldRows);
				await _db.SaveChangesAsync();

				// 3. SAVE TO DATABASE IN BATCHES
				const int batchSize = 5000;

				for (int i = 0; i < salesRecords.Count; i += batchSize)
				{
					var batch = salesRecords
						.Skip(i)
						.Take(batchSize)
						.ToList();

					_db.DealerCreditLimitSalesData.AddRange(batch);
					await _db.SaveChangesAsync();

					_db.ChangeTracker.Clear();
				}

				return Ok(new
				{
					Success = true,
					Message = $"Upload completed successfully. {salesRecords.Count} rows inserted.",
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
					Message = "Internal server error during bulk upload",
					Error = ex.Message
				});
			}
		}
		[HttpGet("by-filter")]
		public async Task<IActionResult> GetByFilter(
	int customerId,
	int categoryId,
	[FromQuery] string financialYearIds)
		{
			if (customerId <= 0)
				return BadRequest("CustomerId is required.");

			if (categoryId <= 0)
				return BadRequest("CategoryId is required.");

			var fyIds = financialYearIds
				.Split(',', StringSplitOptions.RemoveEmptyEntries)
				.Select(x => int.TryParse(x, out var id) ? id : 0)
				.Where(x => x > 0)
				.Distinct()
				.ToList();

			if (fyIds.Count == 0)
				return Ok(new List<DealerCreditLimitSales>());

			var data = await _db.DealerCreditLimitSalesData
				.AsNoTracking()
				.Where(x =>
					x.CustomerId == customerId &&
					x.ProductGroupId == categoryId &&
					fyIds.Contains(x.FinancialYearId))
				.GroupBy(x => new
				{
					x.CustomerId,
					x.CustomerNumber,
					x.StateId,
					x.ProductGroupId,
					x.SubGroupId,
					x.FinancialYearId
				})
				.Select(g => new DealerCreditLimitSales
				{
					Id = 0,
					CustomerId = g.Key.CustomerId,
					CustomerNumber = g.Key.CustomerNumber,
					StateId = g.Key.StateId,
					ProductGroupId = g.Key.ProductGroupId,
					SubGroupId = g.Key.SubGroupId,
					FinancialYearId = g.Key.FinancialYearId,
					Quantity = g.Sum(x => x.Quantity),
					GrossAmount = g.Sum(x => x.GrossAmount)
				})
				.ToListAsync();

			return Ok(data);
		}
	}
}