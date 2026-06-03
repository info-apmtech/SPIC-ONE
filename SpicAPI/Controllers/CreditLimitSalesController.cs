using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
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

				// ── 1. Load dealers and index by ALL code columns ──────────────────
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

				// Dictionary keyed by every possible code variant → dealer
				var dealerByCode = new Dictionary<string, dynamic>(StringComparer.OrdinalIgnoreCase);

				foreach (var d in dealerRawList)
				{
					void TryAdd(string? raw)
					{
						if (string.IsNullOrWhiteSpace(raw)) return;
						var key = raw.Trim().ToUpperInvariant();
						if (!dealerByCode.ContainsKey(key)) dealerByCode[key] = d;

						// Also index the numeric-only part (strip leading letter prefix)
						if (key.Length > 1 && char.IsLetter(key[0]))
						{
							var numeric = key.Substring(1);
							if (!dealerByCode.ContainsKey(numeric)) dealerByCode[numeric] = d;
						}
					}

					TryAdd(d.Code);           // raw numeric e.g. 45541541
					TryAdd(d.SpicCode);       // D-prefix   e.g. D45541541
					TryAdd(d.GreenStarCode);  // Z-prefix   e.g. Z45541541
					TryAdd(d.TnCode);         // T-prefix   e.g. T45541541
				}

				// Firm-name lookup (unchanged)
				var dealerByName = dealerRawList
					.GroupBy(d => d.FirmName.Trim().ToUpperInvariant())
					.ToDictionary(g => g.Key, g => (dynamic)g.First());

				// ── 2. Load product / category / state maps (unchanged) ────────────
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

				var stateMap = (await _db.States
					.Where(s => !string.IsNullOrWhiteSpace(s.StateName))
					.Select(s => new { s.Id, Name = s.StateName })
					.ToListAsync())
					.GroupBy(s => s.Name.Trim().ToUpperInvariant())
					.ToDictionary(g => g.Key, g => g.First().Id);

				// ── 3. Open workbook ───────────────────────────────────────────────
				using var stream = file.OpenReadStream();
				using var workbook = new XLWorkbook(stream);
				var worksheet = workbook.Worksheets.FirstOrDefault();

				if (worksheet == null)
					return BadRequest(new { Success = false, Message = "Excel worksheet not found." });

				// Read headers from row 2
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
						if (headers.TryGetValue(n, out int c)) return c;
					return -1;
				}

				int colState = ColIndex("State", "StateName", "State Name");
				int colDealerCode = ColIndex("Customer Number", "CustomerNumber", "Dealer Code", "DealerCode", "Code", "SPICCode", "GreenStarCode", "TnCode");
				int colDealerName = ColIndex("Customer Name", "CustomerName", "Dealer Name", "DealerName", "Firm Name", "FirmName");
				int colSubGroup = ColIndex("Sub Group", "SubGroup", "Product SubGroup", "ProductSubGroup", "Product", "ProductName");
				int colCategory = ColIndex("Product Group", "ProductGroup", "Category", "CategoryName");
				int colQuantity = ColIndex("Quantity", "Qty");
				int colGrossAmount = ColIndex("Gross Amount", "GrossAmount", "Amount", "Gross");

				if (colDealerCode < 0 && colDealerName < 0)
					return BadRequest(new
					{
						Success = false,
						Message = "Could not find 'Customer Number' or 'Customer Name' column. " +
								  $"Columns found: {string.Join(", ", headers.Keys)}"
					});

				if (colSubGroup < 0)
					return BadRequest(new
					{
						Success = false,
						Message = "Could not find 'Sub Group' column. " +
								  $"Columns found: {string.Join(", ", headers.Keys)}"
					});

				if (colQuantity < 0 || colGrossAmount < 0)
					return BadRequest(new
					{
						Success = false,
						Message = "Could not find 'Quantity' or 'Gross Amount' column. " +
								  $"Columns found: {string.Join(", ", headers.Keys)}"
					});

				// ── 4. Process data rows (start from row 3) ────────────────────────
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
					var subGroupRaw = Cell(colSubGroup);
					var categoryRaw = Cell(colCategory);
					var stateRaw = Cell(colState);
					var quantityRaw = Cell(colQuantity);
					var amountRaw = Cell(colGrossAmount);

					// Skip completely empty rows
					if (string.IsNullOrWhiteSpace(dealerCodeRaw) &&
						string.IsNullOrWhiteSpace(dealerNameRaw) &&
						string.IsNullOrWhiteSpace(subGroupRaw))
						continue;

					var rowErrors = new List<string>();

					// ── 4a. Resolve dealer ─────────────────────────────────────────
					dynamic? dealer = null;

					// Try by code first (covers D/Z/T/N prefix and raw numeric)
					if (!string.IsNullOrWhiteSpace(dealerCodeRaw))
					{
						var codeKey = dealerCodeRaw.Trim().ToUpperInvariant();
						if (!dealerByCode.TryGetValue(codeKey, out dealer))
						{
							// Strip prefix and try numeric only
							if (codeKey.Length > 1 && char.IsLetter(codeKey[0]))
								dealerByCode.TryGetValue(codeKey.Substring(1), out dealer);
						}
					}

					// Fallback: exact firm name
					if (dealer == null && !string.IsNullOrWhiteSpace(dealerNameRaw))
					{
						var nameKey = dealerNameRaw.Trim().ToUpperInvariant();
						dealerByName.TryGetValue(nameKey, out dealer);
					}

					// Fallback: partial firm name
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

					// Save the exact code from Excel (e.g. Z47420510) as CustomerNumber
					// so the saved value matches what was uploaded
					string dealerCode = !string.IsNullOrWhiteSpace(dealerCodeRaw)
						? dealerCodeRaw.Trim()
						: (string)dealer.Code;

					// Resolve state from Excel column if dealer has no state
					if (stateId <= 0 && !string.IsNullOrWhiteSpace(stateRaw))
						stateMap.TryGetValue(stateRaw.Trim().ToUpperInvariant(), out stateId);

					if (stateId <= 0)
					{
						invalidRows.Add(
							$"Row {rowNumber}: State could not be resolved for dealer '{dealerCode}'.");
						continue;
					}

					// ── 4b. Resolve SubGroup (Product) ─────────────────────────────
					int subGroupId = 0;
					int productGroupId = 0;

					if (!string.IsNullOrWhiteSpace(subGroupRaw))
					{
						var normSub = Norm(subGroupRaw);

						if (productMap.TryGetValue(normSub, out var prod))
						{
							subGroupId     = prod.Id;
							productGroupId = prod.CategoryId;
						}
						else
						{
							var startMatch = productMap.Keys
								.FirstOrDefault(k => k.StartsWith(normSub) || normSub.StartsWith(k));

							if (startMatch != null)
							{
								subGroupId     = productMap[startMatch].Id;
								productGroupId = productMap[startMatch].CategoryId;
							}
							else
							{
								var containsMatch = productMap.Keys
									.FirstOrDefault(k => k.Contains(normSub) || normSub.Contains(k));

								if (containsMatch != null)
								{
									subGroupId     = productMap[containsMatch].Id;
									productGroupId = productMap[containsMatch].CategoryId;
								}
								else
								{
									rowErrors.Add($"Product SubGroup '{subGroupRaw}' not found");
								}
							}
						}
					}
					else
					{
						rowErrors.Add("Product SubGroup is empty");
					}

					// ── 4c. Resolve Category (only if product didn't set it) ───────
					if (productGroupId <= 0 && !string.IsNullOrWhiteSpace(categoryRaw))
					{
						var normCat = Norm(categoryRaw);

						if (categoryMap.TryGetValue(normCat, out int catId))
						{
							productGroupId = catId;
						}
						else
						{
							var startMatch = categoryMap.Keys
								.FirstOrDefault(k => k.StartsWith(normCat) || normCat.StartsWith(k));

							if (startMatch != null)
							{
								productGroupId = categoryMap[startMatch];
							}
							else
							{
								var containsMatch = categoryMap.Keys
									.FirstOrDefault(k => k.Contains(normCat) || normCat.Contains(k));

								if (containsMatch != null)
									productGroupId = categoryMap[containsMatch];
								else
									rowErrors.Add($"Product Group '{categoryRaw}' not found");
							}
						}
					}

					// ── 4d. Parse numbers ──────────────────────────────────────────
					if (!decimal.TryParse(quantityRaw,
							NumberStyles.Any, CultureInfo.InvariantCulture,
							out decimal quantity))
					{
						rowErrors.Add($"Invalid Quantity '{quantityRaw}'");
						quantity = 0;
					}

					if (!decimal.TryParse(amountRaw,
							NumberStyles.Any, CultureInfo.InvariantCulture,
							out decimal grossAmount))
					{
						rowErrors.Add($"Invalid Gross Amount '{amountRaw}'");
						grossAmount = 0;
					}

					// ── 4e. Skip row if any errors ─────────────────────────────────
					if (rowErrors.Any())
					{
						invalidRows.Add($"Row {rowNumber}: {string.Join("; ", rowErrors)}.");
						continue;
					}

					salesRecords.Add(new DealerCreditLimitSales
					{
						FinancialYearId = financialYearId,
						StateId         = stateId,
						CustomerNumber  = dealerCode,
						CustomerId      = customerId,
						SubGroupId      = subGroupId,
						ProductGroupId  = productGroupId,
						Quantity        = quantity,
						GrossAmount     = grossAmount
					});
				}

				// ── 5. Validate we have something to insert ────────────────────────
				if (salesRecords.Count == 0)
					return BadRequest(new
					{
						Success = false,
						Message = "The uploaded file contains no valid data rows.",
						InvalidRows = invalidRows.Take(50).ToList()
					});

				// ── 6. Replace existing rows for this financial year ───────────────
				var oldRows = _db.DealerCreditLimitSalesData
					.Where(x => x.FinancialYearId == financialYearId);
				_db.DealerCreditLimitSalesData.RemoveRange(oldRows);
				await _db.SaveChangesAsync();

				// ── 7. Batch insert ────────────────────────────────────────────────
				const int batchSize = 5000;
				for (int i = 0; i < salesRecords.Count; i += batchSize)
				{
					_db.DealerCreditLimitSalesData.AddRange(
						salesRecords.Skip(i).Take(batchSize));
					await _db.SaveChangesAsync();
					_db.ChangeTracker.Clear();
				}

				// ── 8. Return summary ──────────────────────────────────────────────
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

		// ?????????????????????????????????????????????
		// FILTER QUERY
		// ?????????????????????????????????????????????

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
					x.CustomerId     == customerId &&
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
					Id              = 0,
					CustomerId      = g.Key.CustomerId,
					CustomerNumber  = g.Key.CustomerNumber,
					StateId         = g.Key.StateId,
					ProductGroupId  = g.Key.ProductGroupId,
					SubGroupId      = g.Key.SubGroupId,
					FinancialYearId = g.Key.FinancialYearId,
					Quantity        = g.Sum(x => x.Quantity),
					GrossAmount     = g.Sum(x => x.GrossAmount)
				})
				.ToListAsync();

			return Ok(data);
		}
	}
}