using Microsoft.AspNetCore.Mvc;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// Designation CRUD. The permission grid it backs is built entirely from the
	/// PagePermission enum and persisted as Designation.RoleAccess; there is no
	/// normalized page/permission table any more.
	/// </summary>
	[Route("api/[controller]")]
	public class DesignationController : GenericCrudController<Designation>
	{
		public DesignationController(IGenericRepository<Designation> repo) : base(repo)
		{
		}

		[HttpPost]
		public override async Task<IActionResult> Create([FromBody] Designation entity)
		{
			var created = await _repo.CreateAsync(entity);
			return Ok(new
			{
				message = "Designation created successfully",
				data = created
			});
		}

		[HttpPut("{id}")]
		public override async Task<IActionResult> Update(int id, [FromBody] Designation entity)
		{
			var existing = await _repo.GetByIdAsync(id);
			if (existing == null) return NotFound();

			var updated = await _repo.PatchAsync(id, entity);
			if (updated == null) return NotFound();

			return Ok(new
			{
				message = "Designation updated successfully",
				data = updated
			});
		}
	}
}
