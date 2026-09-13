using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// Admin/CorporateAdmin-only management of the ApplicationPage catalog
	/// (Permission System - Page Management). Subclasses the generic CRUD so the
	/// full surface is inherited: GET api/PageManagement (active only),
	/// GET api/PageManagement/all (all incl. inactive), GET {id}, POST {id}/status,
	/// PUT {id} (validated update), PATCH {id}/status (deactivate/activate),
	/// DELETE {id} (never used by the UI - pages are only deactivated).
	/// Create/Update are overridden to enforce required fields and a case-insensitive
	/// unique Key (the DB unique index IX_Pages_Key is case-sensitive, so without this
	/// "StockReport" and "stockreport" could coexist). Page Key is immutable after
	/// creation so existing RoleAccess tokens ("Key.*") stay valid.
	/// </summary>
	[Authorize(Roles = "Admin,CorporateAdmin")]
	[ApiController]
	[Route("api/[controller]")]
	public class PageManagementController : GenericCrudController<ApplicationPage>
	{
		public PageManagementController(IGenericRepository<ApplicationPage> repo) : base(repo)
		{
		}

		[HttpPost]
		public override async Task<IActionResult> Create([FromBody] ApplicationPage entity)
		{
			if (string.IsNullOrWhiteSpace(entity.Key) || string.IsNullOrWhiteSpace(entity.Name))
				return BadRequest(new { message = "Key and Name are required." });

			var key = entity.Key.Trim();
			var exists = await _repo.ExistsAsync(p => p.Key.ToLower() == key.ToLower());
			if (exists)
				return Conflict(new { message = $"Key '{key}' already exists." });

			entity.Key = key;
			entity.Name = entity.Name.Trim();
			entity.IsActive = true;

			var created = await _repo.CreateAsync(entity);
			return Ok(new
			{
				message = "Page created successfully",
				data = created
			});
		}

		[HttpPut("{id}")]
		public override async Task<IActionResult> Update(int id, [FromBody] ApplicationPage entity)
		{
			var existing = await _repo.GetByIdAsync(id);
			if (existing == null) return NotFound();

			if (!existing.Key.Equals(entity.Key, System.StringComparison.Ordinal))
				return BadRequest(new { message = "Page Key cannot be changed after creation." });

			if (string.IsNullOrWhiteSpace(entity.Name))
				return BadRequest(new { message = "Name is required." });

			entity.Key = existing.Key;
			entity.Name = entity.Name.Trim();

			var updated = await _repo.PatchAsync(id, entity);
			return Ok(new
			{
				message = "Page updated successfully",
				data = updated
			});
		}
	}
}