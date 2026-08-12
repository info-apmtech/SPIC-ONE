using Microsoft.AspNetCore.Mvc;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace SpicAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class PVTMasterController : ControllerBase
	{
		private readonly AppDbContext _context;

		public PVTMasterController(AppDbContext context)
		{
			_context = context;
		}

		/// <summary>
		/// Get all active PVT Master records for dropdown.
		/// Existing API behavior is unchanged.
		/// </summary>
		[HttpGet("all")]
		public IActionResult GetAll()
		{
			var records = _context.PVTMasters
				.Where(x => x.IsActive)
				.Select(x => new
				{
					x.Id,
					x.Code,
					x.Name
				})
				.OrderBy(x => x.Name)
				.ToList();

			return Ok(records);
		}

		/// <summary>
		/// Search PVT Master by code or name.
		/// </summary>
		[HttpGet("search")]
		public IActionResult Search(string query)
		{
			if (string.IsNullOrWhiteSpace(query))
				return BadRequest(new { message = "Query is required" });

			var records = _context.PVTMasters
				.Where(x => x.IsActive &&
					(x.Code.Contains(query) || x.Name.Contains(query)))
				.Select(x => new
				{
					x.Id,
					x.Code,
					x.Name
				})
				.OrderBy(x => x.Name)
				.Take(20)
				.ToList();

			return Ok(records);
		}

		/// <summary>
		/// Save single PVT Master record.
		/// </summary>
		[HttpPost("save")]
		public async Task<IActionResult> SavePVTMaster([FromBody] PVTMasterSaveDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
				return BadRequest(new { message = "Code and Name are required" });

			try
			{
				var pvtMaster = new PVTMaster
				{
					Code = dto.Code.Trim(),
					Name = dto.Name.Trim(),
					IsActive = true,
					CreatedAt = DateTime.Now,
					UpdatedAt = DateTime.Now,
					CreatedBy = "System",
					UpdatedBy = "System"
				};

				_context.PVTMasters.Add(pvtMaster);
				await _context.SaveChangesAsync();

				return Ok(new
				{
					message = "PVT Master saved successfully",
					id = pvtMaster.Id
				});
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = $"Save failed: {ex.Message}" });
			}
		}
	}

	public class PVTMasterSaveDto
	{
		public string Code { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
	}
}