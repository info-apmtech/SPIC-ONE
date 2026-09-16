using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// SDWA Company Details Master — CRUD + Guest House mapping.
	/// Manages company records and their association with guest houses.
	/// </summary>
	[Authorize]
	[ApiController]
	[Route("api/[controller]")]
	public class SdwaCompanyController : ControllerBase
	{
		private readonly AppDbContext _db;

		public SdwaCompanyController(AppDbContext db)
		{
			_db = db;
		}

		// GET /api/SdwaCompany/all
		// All companies with guest house mappings (for the master grid).
		[HttpGet("all")]
		public async Task<IActionResult> GetAll()
		{
			var companies = await _db.SdwaCompanies
				.AsNoTracking()
				.Include(c => c.CompanyGuestHouses)
					.ThenInclude(cgh => cgh.GuestHouse)
				.OrderByDescending(c => c.CreatedAt)
				.Select(c => new SdwaCompanyDto
				{
					Id = c.Id,
					CompanyName = c.CompanyName,
					ShortCode = c.ShortCode,
					GSTIN = c.GSTIN,
					IsActive = c.IsActive,
					CreatedAt = c.CreatedAt,
					GuestHouseIds = c.CompanyGuestHouses.Select(cgh => cgh.GuestHouseId).ToList(),
					GuestHouseNames = c.CompanyGuestHouses
						.Where(cgh => cgh.GuestHouse != null)
						.Select(cgh => cgh.GuestHouse!.Name)
						.ToList()
				})
				.ToListAsync();

			return Ok(companies);
		}

		// GET /api/SdwaCompany/by-guest-house/{guestHouseId}
		// Active companies mapped to a specific guest house (for GuestDetails dropdown).
		[HttpGet("by-guest-house/{guestHouseId:int}")]
		public async Task<IActionResult> GetByGuestHouse(int guestHouseId)
		{
			var companies = await _db.SdwaCompanyGuestHouses
				.AsNoTracking()
				.Where(cgh => cgh.GuestHouseId == guestHouseId && cgh.SdwaCompany != null && cgh.SdwaCompany.IsActive)
				.Select(cgh => new CompanyLookupDto
				{
					Id = cgh.SdwaCompany!.Id,
					CompanyName = cgh.SdwaCompany.CompanyName,
					ShortCode = cgh.SdwaCompany.ShortCode,
					GSTIN = cgh.SdwaCompany.GSTIN
				})
				.Distinct()
				.OrderBy(c => c.CompanyName)
				.ToListAsync();

			return Ok(companies);
		}

		// GET /api/SdwaCompany/{id}
		// Single company with its guest house mapping.
		[HttpGet("{id:int}")]
		public async Task<IActionResult> GetById(int id)
		{
			var company = await _db.SdwaCompanies
				.AsNoTracking()
				.Include(c => c.CompanyGuestHouses)
				.FirstOrDefaultAsync(c => c.Id == id);

			if (company == null)
				return NotFound();

			var dto = new SdwaCompanyDto
			{
				Id = company.Id,
				CompanyName = company.CompanyName,
				ShortCode = company.ShortCode,
				GSTIN = company.GSTIN,
				IsActive = company.IsActive,
				CreatedAt = company.CreatedAt,
				GuestHouseIds = company.CompanyGuestHouses.Select(cgh => cgh.GuestHouseId).ToList(),
				GuestHouseNames = new()
			};

			return Ok(dto);
		}

		// POST /api/SdwaCompany
		// Create a new company with guest house mapping.
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] SdwaCompanyCreateRequest request)
		{
			if (string.IsNullOrWhiteSpace(request.CompanyName))
				return BadRequest(new { Success = false, Message = "Company Name is required." });

			if (string.IsNullOrWhiteSpace(request.GSTIN))
				return BadRequest(new { Success = false, Message = "GSTIN is required." });

			// Check duplicate GSTIN
			var gstinExists = await _db.SdwaCompanies.AnyAsync(c =>
				c.GSTIN == request.GSTIN && c.Id != (request.Id ?? 0));
			if (gstinExists)
				return BadRequest(new { Success = false, Message = "A company with this GSTIN already exists." });

			if (request.GuestHouseIds == null || request.GuestHouseIds.Count == 0)
				return BadRequest(new { Success = false, Message = "At least one Guest House must be selected." });

			var company = new SdwaCompany
			{
				CompanyName = request.CompanyName.Trim(),
				ShortCode = request.ShortCode?.Trim(),
				GSTIN = request.GSTIN.Trim(),
				IsActive = request.IsActive,
				CreatedBy = User.Identity?.Name,
				CreatedAt = DateTime.Now,
				UpdatedBy = User.Identity?.Name,
				UpdatedAt = DateTime.Now
			};

			_db.SdwaCompanies.Add(company);
			await _db.SaveChangesAsync();

			// Add guest house mappings
			foreach (var ghId in request.GuestHouseIds.Distinct())
			{
				_db.SdwaCompanyGuestHouses.Add(new SdwaCompanyGuestHouse
				{
					SdwaCompanyId = company.Id,
					GuestHouseId = ghId
				});
			}
			await _db.SaveChangesAsync();

			return Ok(new { Success = true, company.Id, Message = "Company created successfully." });
		}

		// PUT /api/SdwaCompany/{id}
		// Update an existing company with guest house mapping.
		[HttpPut("{id:int}")]
		public async Task<IActionResult> Update(int id, [FromBody] SdwaCompanyCreateRequest request)
		{
			if (string.IsNullOrWhiteSpace(request.CompanyName))
				return BadRequest(new { Success = false, Message = "Company Name is required." });

			if (string.IsNullOrWhiteSpace(request.GSTIN))
				return BadRequest(new { Success = false, Message = "GSTIN is required." });

			var company = await _db.SdwaCompanies
				.Include(c => c.CompanyGuestHouses)
				.FirstOrDefaultAsync(c => c.Id == id);

			if (company == null)
				return NotFound();

			// Check duplicate GSTIN (excluding self)
			var gstinExists = await _db.SdwaCompanies.AnyAsync(c =>
				c.GSTIN == request.GSTIN && c.Id != id);
			if (gstinExists)
				return BadRequest(new { Success = false, Message = "A company with this GSTIN already exists." });

			if (request.GuestHouseIds == null || request.GuestHouseIds.Count == 0)
				return BadRequest(new { Success = false, Message = "At least one Guest House must be selected." });

			company.CompanyName = request.CompanyName.Trim();
			company.ShortCode = request.ShortCode?.Trim();
			company.GSTIN = request.GSTIN.Trim();
			company.IsActive = request.IsActive;
			company.UpdatedBy = User.Identity?.Name;
			company.UpdatedAt = DateTime.Now;

			// Replace guest house mappings
			var existingMappings = company.CompanyGuestHouses.ToList();
			_db.SdwaCompanyGuestHouses.RemoveRange(existingMappings);

			foreach (var ghId in request.GuestHouseIds.Distinct())
			{
				_db.SdwaCompanyGuestHouses.Add(new SdwaCompanyGuestHouse
				{
					SdwaCompanyId = id,
					GuestHouseId = ghId
				});
			}

			await _db.SaveChangesAsync();
			return Ok(new { Success = true, Message = "Company updated successfully." });
		}

		// DELETE /api/SdwaCompany/{id}
		[HttpDelete("{id:int}")]
		public async Task<IActionResult> Delete(int id)
		{
			var company = await _db.SdwaCompanies
				.Include(c => c.CompanyGuestHouses)
				.FirstOrDefaultAsync(c => c.Id == id);

			if (company == null)
				return NotFound();

			// Check if company is used in any bookings
			var hasBookings = await _db.GuestHouseBookingGuests.AnyAsync(g => g.SdwaCompanyId == id);
			if (hasBookings)
				return BadRequest(new { Success = false, Message = "This company cannot be deleted because it is linked to existing bookings." });

			_db.SdwaCompanyGuestHouses.RemoveRange(company.CompanyGuestHouses);
			_db.SdwaCompanies.Remove(company);
			await _db.SaveChangesAsync();

			return Ok(new { Success = true, Message = "Company deleted successfully." });
		}

		// PATCH /api/SdwaCompany/{id}/status
		[HttpPatch("{id:int}/status")]
		public async Task<IActionResult> ChangeStatus(int id, [FromQuery] bool isActive)
		{
			var company = await _db.SdwaCompanies.FindAsync(id);
			if (company == null)
				return NotFound();

			company.IsActive = isActive;
			company.UpdatedAt = DateTime.Now;
			company.UpdatedBy = User.Identity?.Name;
			await _db.SaveChangesAsync();

			return Ok(new { Success = true, Message = isActive ? "Company activated." : "Company deactivated." });
		}
	}

	// ---- DTOs ----

	public class SdwaCompanyDto
	{
		public int Id { get; set; }
		public string CompanyName { get; set; } = "";
		public string? ShortCode { get; set; }
		public string? GSTIN { get; set; }
		public bool IsActive { get; set; }
		public DateTime CreatedAt { get; set; }
		public List<int> GuestHouseIds { get; set; } = new();
		public List<string> GuestHouseNames { get; set; } = new();
	}

	public class SdwaCompanyCreateRequest
	{
		public int? Id { get; set; }
		public string CompanyName { get; set; } = "";
		public string? ShortCode { get; set; }
		public string? GSTIN { get; set; }
		public bool IsActive { get; set; } = true;
		public List<int> GuestHouseIds { get; set; } = new();
	}

	public class CompanyLookupDto
	{
		public int Id { get; set; }
		public string CompanyName { get; set; } = "";
		public string? ShortCode { get; set; }
		public string? GSTIN { get; set; }
	}
}
