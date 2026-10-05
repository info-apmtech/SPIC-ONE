using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// Base controller providing full CRUD operations for any entity.
	/// No Logistics-specific change is required here: the new entity properties
	/// are automatically handled by the existing generic repository/controller.
	/// </summary>
	[Authorize]
	[ApiController]
	public abstract class GenericCrudController<T> : ControllerBase where T : class
	{
		protected readonly IGenericRepository<T> _repo;

		protected GenericCrudController(IGenericRepository<T> repo)
		{
			_repo = repo;
		}

		[HttpGet]
		public virtual async Task<IActionResult> GetAll()
		{
			var items = await _repo.GetAll().ToListAsync();
			return Ok(items);
		}

		[HttpGet("all")]
		public virtual async Task<IActionResult> GetAllWithInactive()
		{
			var items = await _repo.GetAllWithInactive().ToListAsync();
			return Ok(items);
		}

		[HttpGet("byDealer/{dealerId}")]
		public virtual async Task<IActionResult> GetByDealerId(int dealerId)
		{
			var prop = typeof(T).GetProperty("DealerId");
			if (prop == null)
				return BadRequest($"{typeof(T).Name} does not have a DealerId property.");

			var param = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
			var body = System.Linq.Expressions.Expression.Equal(
				System.Linq.Expressions.Expression.Property(param, prop),
				System.Linq.Expressions.Expression.Constant(dealerId));
			var predicate = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, param);

			var items = await _repo.GetWhere(predicate).AsNoTracking().ToListAsync();
			return Ok(items);
		}

		[HttpGet("{id}")]
		public virtual async Task<IActionResult> GetById(int id)
		{
			var item = await _repo.GetByIdAsync(id);
			if (item == null) return NotFound();
			return Ok(item);
		}

		[HttpPost]
		public virtual async Task<IActionResult> Create([FromBody] T entity)
		{
			var created = await _repo.CreateAsync(entity);
			return Ok(new
			{
				message = $"{typeof(T).Name} created successfully",
				data = created
			});
		}

		[HttpPut("{id}")]
		public virtual async Task<IActionResult> Update(int id, [FromBody] T entity)
		{
			var updated = await _repo.PatchAsync(id, entity);
			if (updated == null) return NotFound();
			return Ok(new
			{
				message = $"{typeof(T).Name} updated successfully",
				data = updated
			});
		}

		[HttpPatch("{id}/status")]
		public virtual async Task<IActionResult> ChangeStatus(int id, [FromQuery] bool isActive)
		{
			var updated = await _repo.ChangeStatusAsync(id, isActive);
			if (updated == null) return NotFound();
			return Ok(new
			{
				message = isActive
					? $"{typeof(T).Name} activated successfully"
					: $"{typeof(T).Name} deactivated successfully",
				data = updated
			});
		}

		[HttpDelete("{id}")]
		public virtual async Task<IActionResult> Delete(int id)
		{
			var deleted = await _repo.DeleteAsync(id);
			if (!deleted) return NotFound();
			return Ok(new { message = $"{typeof(T).Name} deleted successfully" });
		}
	}
}