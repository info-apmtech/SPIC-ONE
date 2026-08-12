using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
	[Route("api/[controller]")]
	public class CropController(IGenericRepository<Crop> repo) : GenericCrudController<Crop>(repo);

	[Route("api/[controller]")]
	public class CompetitorController(IGenericRepository<Competitor> repo) : GenericCrudController<Competitor>(repo);

	[Route("api/[controller]")]
	public class SectorController(IGenericRepository<Sector> repo) : GenericCrudController<Sector>(repo);

	[Route("api/[controller]")]
	public class UnitController(IGenericRepository<Unit> repo) : GenericCrudController<Unit>(repo);

	[Route("api/[controller]")]
	public class CategoryController(IGenericRepository<Category> repo) : GenericCrudController<Category>(repo);

	[Route("api/[controller]")]
	public class ProductGroupController(IGenericRepository<ProductGroup> repo) : GenericCrudController<ProductGroup>(repo);

	[Route("api/[controller]")]
	public class ProductController(IGenericRepository<Product> repo) : GenericCrudController<Product>(repo);

	// ---------------------------------------------------------------------
	// WAREHOUSE
	// Existing Warehouse CRUD/API route is preserved.
	// Only role-scoped listing, creator stamping and approval actions are added.
	// ---------------------------------------------------------------------
	[Route("api/[controller]")]
	public class WarehouseController : GenericCrudController<Warehouse>
	{
		public WarehouseController(IGenericRepository<Warehouse> repo) : base(repo)
		{
		}

		[HttpGet("all")]
		public override async Task<IActionResult> GetAllWithInactive()
		{
			var query = _repo.GetAllWithInactive();
			var role = CurrentRole();
			var userId = CurrentUserId();

			if (IsUnrestrictedRole(role))
				return Ok(await query.OrderByDescending(x => x.CreatedAt).ToListAsync());

			if (IsStateRole(role))
			{
				var stateId = CurrentStateId();
				if (!stateId.HasValue || stateId.Value <= 0)
					return Ok(new List<Warehouse>());

				query = query.Where(x => x.BasicStateId == stateId.Value);
			}
			else if (IsRegionRole(role))
			{
				var regionId = CurrentRegionId();
				if (!regionId.HasValue || regionId.Value <= 0)
					return Ok(new List<Warehouse>());

				query = query.Where(x => x.RegionId == regionId.Value);
			}
			else if (IsCreatorRole(role))
			{
				if (string.IsNullOrWhiteSpace(userId))
					return Ok(new List<Warehouse>());

				query = query.Where(x => x.CreatedBy == userId);
			}
			else
			{
				// Unknown/non-logistics roles must not receive the full master list.
				return Ok(new List<Warehouse>());
			}

			return Ok(await query.OrderByDescending(x => x.CreatedAt).ToListAsync());
		}

		[HttpPost]
		public override async Task<IActionResult> Create([FromBody] Warehouse entity)
		{
			var userId = CurrentUserId();
			if (string.IsNullOrWhiteSpace(userId))
				return Unauthorized(new { message = "User not logged in." });

			var now = DateTime.Now;
			var role = CurrentRole();

			// Existing incoming business fields are untouched.
			// Creator / workflow fields are server-owned.
			entity.CreatedBy = userId;
			entity.CreatedByName = CurrentUserName();
			entity.IsSubmittedForReview = IsCreatorRole(role);

			entity.RMApproved = null;
			entity.SMApproved = null;
			entity.AVPApproved = null;
			entity.RMApprovedBy = null;
			entity.RMApprovedAt = null;
			entity.SMApprovedBy = null;
			entity.SMApprovedAt = null;
			entity.AVPApprovedBy = null;
			entity.AVPApprovedAt = null;
			entity.ApprovalRemarks = null;

			if (entity.CreatedAt == default)
				entity.CreatedAt = now;

			entity.UpdatedAt = now;
			entity.UpdatedBy = userId;

			var created = await _repo.CreateAsync(entity);
			return Ok(new
			{
				message = IsCreatorRole(role)
					? "Warehouse created and submitted to RM for approval."
					: "Warehouse created successfully",
				data = created
			});
		}

		[HttpPut("{id}")]
		public override async Task<IActionResult> Update(int id, [FromBody] Warehouse entity)
		{
			var existing = await _repo.GetByIdAsync(id);
			if (existing == null) return NotFound();

			// IMPORTANT: existing Logistics.razor performs a second PUT after document upload.
			// Preserve every workflow-owned value so that normal edit/document saves cannot
			// accidentally clear creator or approval state through GenericRepository.PatchAsync.
			PreserveApprovalState(existing, entity);

			var userId = CurrentUserId();
			entity.UpdatedAt = DateTime.Now;
			entity.UpdatedBy = string.IsNullOrWhiteSpace(userId)
				? existing.UpdatedBy
				: userId;

			var updated = await _repo.PatchAsync(id, entity);
			if (updated == null) return NotFound();

			return Ok(new
			{
				message = "Warehouse updated successfully",
				data = updated
			});
		}

		[HttpPost("{id}/approve")]
		public async Task<IActionResult> Approve(int id, [FromBody] LogisticsApprovalRequest? request)
		{
			var entity = await _repo.GetByIdAsync(id);
			if (entity == null) return NotFound(new { message = "Warehouse not found." });

			if (!entity.IsSubmittedForReview)
				return BadRequest(new { message = "This Warehouse is not submitted for approval." });

			var role = CurrentRole();
			var userId = CurrentUserId();
			if (string.IsNullOrWhiteSpace(userId))
				return Unauthorized(new { message = "User not logged in." });

			var now = DateTime.Now;
			var remarks = request?.Remarks?.Trim();

			if (IsRegionRole(role))
			{
				var regionId = CurrentRegionId();
				if (!regionId.HasValue || entity.RegionId != regionId.Value)
					return Forbid();

				if (entity.RMApproved != null)
					return BadRequest(new { message = "RM action is already completed." });

				entity.RMApproved = true;
				entity.RMApprovedBy = userId;
				entity.RMApprovedAt = now;
			}
			else if (IsStateRole(role))
			{
				var stateId = CurrentStateId();
				if (!stateId.HasValue || entity.BasicStateId != stateId.Value)
					return Forbid();

				if (entity.RMApproved != true)
					return BadRequest(new { message = "RM approval is required first." });

				if (entity.SMApproved != null)
					return BadRequest(new { message = "SMM action is already completed." });

				entity.SMApproved = true;
				entity.SMApprovedBy = userId;
				entity.SMApprovedAt = now;
			}
			else if (IsUnrestrictedRole(role))
			{
				if (entity.RMApproved != true || entity.SMApproved != true)
					return BadRequest(new { message = "RM and SMM approval are required first." });

				if (entity.AVPApproved != null)
					return BadRequest(new { message = "Final approval is already completed." });

				entity.AVPApproved = true;
				entity.AVPApprovedBy = userId;
				entity.AVPApprovedAt = now;
			}
			else
			{
				return Forbid();
			}

			entity.ApprovalRemarks = string.IsNullOrWhiteSpace(remarks) ? entity.ApprovalRemarks : remarks;
			entity.UpdatedAt = now;
			entity.UpdatedBy = userId;

			await _repo.PatchAsync(id, entity);

			return Ok(new
			{
				message = IsRegionRole(role)
					? "Warehouse approved by RM and moved to SMM."
					: IsStateRole(role)
						? "Warehouse approved by SMM and moved to AVP."
						: "Warehouse finally approved."
			});
		}

		private static void PreserveApprovalState(Warehouse existing, Warehouse incoming)
		{
			incoming.CreatedAt = existing.CreatedAt;
			incoming.CreatedBy = existing.CreatedBy;
			incoming.CreatedByName = existing.CreatedByName;
			incoming.IsSubmittedForReview = existing.IsSubmittedForReview;
			incoming.RMApproved = existing.RMApproved;
			incoming.SMApproved = existing.SMApproved;
			incoming.AVPApproved = existing.AVPApproved;
			incoming.RMApprovedBy = existing.RMApprovedBy;
			incoming.RMApprovedAt = existing.RMApprovedAt;
			incoming.SMApprovedBy = existing.SMApprovedBy;
			incoming.SMApprovedAt = existing.SMApprovedAt;
			incoming.AVPApprovedBy = existing.AVPApprovedBy;
			incoming.AVPApprovedAt = existing.AVPApprovedAt;
			incoming.ApprovalRemarks = existing.ApprovalRemarks;
		}

		private string CurrentRole() =>
			User.FindFirst(ClaimTypes.Role)?.Value ??
			User.FindFirst("Role")?.Value ??
			string.Empty;

		private string? CurrentUserId() =>
			User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
			User.FindFirst("sub")?.Value ??
			User.FindFirst("spic:user_id")?.Value;

		private string CurrentUserName() =>
			User.FindFirst("FullName")?.Value ??
			User.FindFirst(ClaimTypes.Name)?.Value ??
			CurrentUserId() ??
			"System";

		private int? CurrentStateId() => ReadIntClaim("spic:state_id", "StateId");
		private int? CurrentRegionId() => ReadIntClaim("spic:region_id", "RegionId");

		private int? ReadIntClaim(params string[] names)
		{
			foreach (var name in names)
			{
				var value = User.FindFirst(name)?.Value;
				if (int.TryParse(value, out var id) && id > 0)
					return id;
			}

			return null;
		}

		private static bool IsCreatorRole(string role) =>
			role.Equals("MO", StringComparison.OrdinalIgnoreCase) ||
			role.Equals("MDO", StringComparison.OrdinalIgnoreCase) ||
			role.Equals("JMDO", StringComparison.OrdinalIgnoreCase);

		private static bool IsRegionRole(string role) =>
			role.Equals("RM", StringComparison.OrdinalIgnoreCase) ||
			role.Equals("RMD", StringComparison.OrdinalIgnoreCase);

		private static bool IsStateRole(string role) =>
			role.Equals("SMM", StringComparison.OrdinalIgnoreCase) ||
			role.Equals("SMD", StringComparison.OrdinalIgnoreCase);

		private static bool IsUnrestrictedRole(string role) =>
			role.Equals("AVP", StringComparison.OrdinalIgnoreCase) ||
			role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
			role.Equals("CorporateAdmin", StringComparison.OrdinalIgnoreCase) ||
			role.Equals("Director", StringComparison.OrdinalIgnoreCase);
	}

	// ---------------------------------------------------------------------
	// RAKE POINT
	// Existing RackPoint CRUD/API route is preserved.
	// Only role-scoped listing, creator stamping and approval actions are added.
	// ---------------------------------------------------------------------
	[Route("api/[controller]")]
	public class RackPointController : GenericCrudController<RackPoint>
	{
		public RackPointController(IGenericRepository<RackPoint> repo) : base(repo)
		{
		}

		[HttpGet("all")]
		public override async Task<IActionResult> GetAllWithInactive()
		{
			var query = _repo.GetAllWithInactive();
			var role = CurrentRole();
			var userId = CurrentUserId();

			if (IsUnrestrictedRole(role))
				return Ok(await query.OrderByDescending(x => x.CreatedAt).ToListAsync());

			if (IsStateRole(role))
			{
				var stateId = CurrentStateId();
				if (!stateId.HasValue || stateId.Value <= 0)
					return Ok(new List<RackPoint>());

				query = query.Where(x => x.BasicStateId == stateId.Value);
			}
			else if (IsRegionRole(role))
			{
				var regionId = CurrentRegionId();
				if (!regionId.HasValue || regionId.Value <= 0)
					return Ok(new List<RackPoint>());

				query = query.Where(x => x.RegionId == regionId.Value);
			}
			else if (IsCreatorRole(role))
			{
				if (string.IsNullOrWhiteSpace(userId))
					return Ok(new List<RackPoint>());

				query = query.Where(x => x.CreatedBy == userId);
			}
			else
			{
				return Ok(new List<RackPoint>());
			}

			return Ok(await query.OrderByDescending(x => x.CreatedAt).ToListAsync());
		}

		[HttpPost]
		public override async Task<IActionResult> Create([FromBody] RackPoint entity)
		{
			var userId = CurrentUserId();
			if (string.IsNullOrWhiteSpace(userId))
				return Unauthorized(new { message = "User not logged in." });

			var now = DateTime.Now;
			var role = CurrentRole();

			entity.CreatedBy = userId;
			entity.CreatedByName = CurrentUserName();
			entity.IsSubmittedForReview = IsCreatorRole(role);

			entity.RMApproved = null;
			entity.SMApproved = null;
			entity.AVPApproved = null;
			entity.RMApprovedBy = null;
			entity.RMApprovedAt = null;
			entity.SMApprovedBy = null;
			entity.SMApprovedAt = null;
			entity.AVPApprovedBy = null;
			entity.AVPApprovedAt = null;
			entity.ApprovalRemarks = null;

			if (entity.CreatedAt == default)
				entity.CreatedAt = now;

			entity.UpdatedAt = now;
			entity.UpdatedBy = userId;

			var created = await _repo.CreateAsync(entity);
			return Ok(new
			{
				message = IsCreatorRole(role)
					? "Rake Point created and submitted to RM for approval."
					: "Rake Point created successfully",
				data = created
			});
		}

		[HttpPut("{id}")]
		public override async Task<IActionResult> Update(int id, [FromBody] RackPoint entity)
		{
			var existing = await _repo.GetByIdAsync(id);
			if (existing == null) return NotFound();

			// Preserve approval state during normal edits and the document-path second PUT.
			PreserveApprovalState(existing, entity);

			var userId = CurrentUserId();
			entity.UpdatedAt = DateTime.Now;
			entity.UpdatedBy = string.IsNullOrWhiteSpace(userId)
				? existing.UpdatedBy
				: userId;

			var updated = await _repo.PatchAsync(id, entity);
			if (updated == null) return NotFound();

			return Ok(new
			{
				message = "RackPoint updated successfully",
				data = updated
			});
		}

		[HttpPost("{id}/approve")]
		public async Task<IActionResult> Approve(int id, [FromBody] LogisticsApprovalRequest? request)
		{
			var entity = await _repo.GetByIdAsync(id);
			if (entity == null) return NotFound(new { message = "Rake Point not found." });

			if (!entity.IsSubmittedForReview)
				return BadRequest(new { message = "This Rake Point is not submitted for approval." });

			var role = CurrentRole();
			var userId = CurrentUserId();
			if (string.IsNullOrWhiteSpace(userId))
				return Unauthorized(new { message = "User not logged in." });

			var now = DateTime.Now;
			var remarks = request?.Remarks?.Trim();

			if (IsRegionRole(role))
			{
				var regionId = CurrentRegionId();
				if (!regionId.HasValue || entity.RegionId != regionId.Value)
					return Forbid();

				if (entity.RMApproved != null)
					return BadRequest(new { message = "RM action is already completed." });

				entity.RMApproved = true;
				entity.RMApprovedBy = userId;
				entity.RMApprovedAt = now;
			}
			else if (IsStateRole(role))
			{
				var stateId = CurrentStateId();
				if (!stateId.HasValue || entity.BasicStateId != stateId.Value)
					return Forbid();

				if (entity.RMApproved != true)
					return BadRequest(new { message = "RM approval is required first." });

				if (entity.SMApproved != null)
					return BadRequest(new { message = "SMM action is already completed." });

				entity.SMApproved = true;
				entity.SMApprovedBy = userId;
				entity.SMApprovedAt = now;
			}
			else if (IsUnrestrictedRole(role))
			{
				if (entity.RMApproved != true || entity.SMApproved != true)
					return BadRequest(new { message = "RM and SMM approval are required first." });

				if (entity.AVPApproved != null)
					return BadRequest(new { message = "Final approval is already completed." });

				entity.AVPApproved = true;
				entity.AVPApprovedBy = userId;
				entity.AVPApprovedAt = now;
			}
			else
			{
				return Forbid();
			}

			entity.ApprovalRemarks = string.IsNullOrWhiteSpace(remarks) ? entity.ApprovalRemarks : remarks;
			entity.UpdatedAt = now;
			entity.UpdatedBy = userId;

			await _repo.PatchAsync(id, entity);

			return Ok(new
			{
				message = IsRegionRole(role)
					? "Rake Point approved by RM and moved to SMM."
					: IsStateRole(role)
						? "Rake Point approved by SMM and moved to AVP."
						: "Rake Point finally approved."
			});
		}

		private static void PreserveApprovalState(RackPoint existing, RackPoint incoming)
		{
			incoming.CreatedAt = existing.CreatedAt;
			incoming.CreatedBy = existing.CreatedBy;
			incoming.CreatedByName = existing.CreatedByName;
			incoming.IsSubmittedForReview = existing.IsSubmittedForReview;
			incoming.RMApproved = existing.RMApproved;
			incoming.SMApproved = existing.SMApproved;
			incoming.AVPApproved = existing.AVPApproved;
			incoming.RMApprovedBy = existing.RMApprovedBy;
			incoming.RMApprovedAt = existing.RMApprovedAt;
			incoming.SMApprovedBy = existing.SMApprovedBy;
			incoming.SMApprovedAt = existing.SMApprovedAt;
			incoming.AVPApprovedBy = existing.AVPApprovedBy;
			incoming.AVPApprovedAt = existing.AVPApprovedAt;
			incoming.ApprovalRemarks = existing.ApprovalRemarks;
		}

		private string CurrentRole() =>
			User.FindFirst(ClaimTypes.Role)?.Value ??
			User.FindFirst("Role")?.Value ??
			string.Empty;

		private string? CurrentUserId() =>
			User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
			User.FindFirst("sub")?.Value ??
			User.FindFirst("spic:user_id")?.Value;

		private string CurrentUserName() =>
			User.FindFirst("FullName")?.Value ??
			User.FindFirst(ClaimTypes.Name)?.Value ??
			CurrentUserId() ??
			"System";

		private int? CurrentStateId() => ReadIntClaim("spic:state_id", "StateId");
		private int? CurrentRegionId() => ReadIntClaim("spic:region_id", "RegionId");

		private int? ReadIntClaim(params string[] names)
		{
			foreach (var name in names)
			{
				var value = User.FindFirst(name)?.Value;
				if (int.TryParse(value, out var id) && id > 0)
					return id;
			}

			return null;
		}

		private static bool IsCreatorRole(string role) =>
			role.Equals("MO", StringComparison.OrdinalIgnoreCase) ||
			role.Equals("MDO", StringComparison.OrdinalIgnoreCase) ||
			role.Equals("JMDO", StringComparison.OrdinalIgnoreCase);

		private static bool IsRegionRole(string role) =>
			role.Equals("RM", StringComparison.OrdinalIgnoreCase) ||
			role.Equals("RMD", StringComparison.OrdinalIgnoreCase);

		private static bool IsStateRole(string role) =>
			role.Equals("SMM", StringComparison.OrdinalIgnoreCase) ||
			role.Equals("SMD", StringComparison.OrdinalIgnoreCase);

		private static bool IsUnrestrictedRole(string role) =>
			role.Equals("AVP", StringComparison.OrdinalIgnoreCase) ||
			role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
			role.Equals("CorporateAdmin", StringComparison.OrdinalIgnoreCase) ||
			role.Equals("Director", StringComparison.OrdinalIgnoreCase);
	}

	// Existing Port controller remains exactly on the old generic flow.
	[Route("api/[controller]")]
	public class PortController(IGenericRepository<Port> repo) : GenericCrudController<Port>(repo);

	[Route("api/[controller]")]
	public class BankController(IGenericRepository<Bank> repo) : GenericCrudController<Bank>(repo);

	[Route("api/[controller]")]
	public class FinancialYearController(IGenericRepository<FinancialYear> repo) : GenericCrudController<FinancialYear>(repo);

	[Route("api/[controller]")]
	public class RelationshipController(IGenericRepository<Relationship> repo) : GenericCrudController<Relationship>(repo);

	[Route("api/[controller]")]
	public class LyingWithMasterController(IGenericRepository<LyingWithMaster> repo) : GenericCrudController<LyingWithMaster>(repo);

	public sealed class LogisticsApprovalRequest
	{
		public string Remarks { get; set; } = string.Empty;
	}
}