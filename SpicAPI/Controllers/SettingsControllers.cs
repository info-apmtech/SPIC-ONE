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
		private readonly IGenericRepository<LogisticsApprovalHistory> _historyRepo;

		public WarehouseController(
			IGenericRepository<Warehouse> repo,
			IGenericRepository<LogisticsApprovalHistory> historyRepo) : base(repo)
		{
			_historyRepo = historyRepo;
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
		[HttpGet("approved")]
		public async Task<IActionResult> GetApprovedWarehouses()
		{
			var items = await _repo
				.GetAllWithInactive()
				.Where(x =>
					x.AVPApproved == true)
				.AsNoTracking()
				.OrderBy(x => x.Name)
				.ToListAsync();

			return Ok(items);
		}

		[HttpPost]
		public override async Task<IActionResult> Create([FromBody] Warehouse entity)
		{
			var userId = CurrentUserId();
			if (string.IsNullOrWhiteSpace(userId))
				return Unauthorized(new { message = "User not logged in." });

			var now = DateTime.Now;
			var role = CurrentRole();

			// Server-side duplicate SAP Code check.
			if (!string.IsNullOrWhiteSpace(entity.WarehouseCode))
			{
				var sapCode = entity.WarehouseCode.Trim();
				if (await _repo.ExistsAsync(x => x.WarehouseCode != null && x.WarehouseCode.Trim() == sapCode))
				{
					return Conflict(new { message = $"Warehouse with SAP Code '{sapCode}' already exists." });
				}
			}

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

			var userId = CurrentUserId();
			var role = CurrentRole();

			// If RM sent this record back to its original MO/MDO/JMDO creator,
			// saving the corrected record resubmits the SAME row to RM.
			// No delete/recreate is performed.
			var isCreatorResubmission =
				existing.IsSubmittedForReview &&
				existing.RMApproved == false &&
				IsCreatorRole(role) &&
				!string.IsNullOrWhiteSpace(userId) &&
				string.Equals(existing.CreatedBy, userId, StringComparison.OrdinalIgnoreCase);

			// IMPORTANT: existing Logistics.razor performs a second PUT after document upload.
			// Preserve every workflow-owned value so normal edit/document saves cannot
			// accidentally clear approval state.
			PreserveApprovalState(existing, entity);

			if (isCreatorResubmission)
			{
				// RM had sent the request back to the creator. The creator's save now
				// places it back in Pending RM while preserving the same record Id.
				entity.IsSubmittedForReview = true;
				entity.RMApproved = null;
				entity.RMApprovedBy = null;
				entity.RMApprovedAt = null;
				entity.SMApproved = null;
				entity.SMApprovedBy = null;
				entity.SMApprovedAt = null;
				entity.AVPApproved = null;
				entity.AVPApprovedBy = null;
				entity.AVPApprovedAt = null;
				// Keep ApprovalRemarks until the next workflow action so the latest
				// Send Back reason remains available to the resubmitting creator/RM.
			}

			// Server-side duplicate SAP Code check (exclude current record).
			if (!string.IsNullOrWhiteSpace(entity.WarehouseCode))
			{
				var sapCode = entity.WarehouseCode.Trim();
				if (await _repo.ExistsAsync(x => x.Id != id && x.WarehouseCode != null && x.WarehouseCode.Trim() == sapCode))
				{
					return Conflict(new { message = $"Warehouse with SAP Code '{sapCode}' already exists." });
				}
			}

			entity.UpdatedAt = DateTime.Now;
			entity.UpdatedBy = string.IsNullOrWhiteSpace(userId)
				? existing.UpdatedBy
				: userId;

			var updated = await _repo.PatchAsync(id, entity);
			if (updated == null) return NotFound();

			return Ok(new
			{
				message = isCreatorResubmission
					? "Warehouse updated and resubmitted to RM for approval."
					: "Warehouse updated successfully",
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

			var remarks = request?.Remarks?.Trim();
			if (string.IsNullOrWhiteSpace(remarks))
				return BadRequest(new { message = "Approval remarks are required." });

			var role = CurrentRole();
			var userId = CurrentUserId();
			if (string.IsNullOrWhiteSpace(userId))
				return Unauthorized(new { message = "User not logged in." });

			var now = DateTime.Now;

			if (IsRegionRole(role))
			{
				var regionId = CurrentRegionId();
				if (!regionId.HasValue || entity.RegionId != regionId.Value)
					return Forbid();

				// RM can act on the initial RM review OR when SMM sent it back to RM.
				var isInitialRmReview = entity.RMApproved == null;
				var isReturnedFromSmm = entity.RMApproved == true && entity.SMApproved == false;
				if (!isInitialRmReview && !isReturnedFromSmm)
					return BadRequest(new { message = "This Warehouse is not pending RM approval." });

				entity.RMApproved = true;
				entity.RMApprovedBy = userId;
				entity.RMApprovedAt = now;

				// Forward to SMM again. Any SMM/AVP Send Back marker is cleared.
				entity.SMApproved = null;
				entity.SMApprovedBy = null;
				entity.SMApprovedAt = null;
				entity.AVPApproved = null;
				entity.AVPApprovedBy = null;
				entity.AVPApprovedAt = null;
			}
			else if (IsStateRole(role))
			{
				var stateId = CurrentStateId();
				if (!stateId.HasValue || entity.BasicStateId != stateId.Value)
					return Forbid();

				if (entity.RMApproved != true)
					return BadRequest(new { message = "RM approval is required first." });

				// SMM can act initially OR when AVP sent it back to SMM.
				var isInitialSmmReview = entity.SMApproved == null;
				var isReturnedFromAvp = entity.SMApproved == true && entity.AVPApproved == false;
				if (!isInitialSmmReview && !isReturnedFromAvp)
					return BadRequest(new { message = "This Warehouse is not pending SMM approval." });

				entity.SMApproved = true;
				entity.SMApprovedBy = userId;
				entity.SMApprovedAt = now;

				// Forward to AVP again. Clear the AVP Send Back marker.
				entity.AVPApproved = null;
				entity.AVPApprovedBy = null;
				entity.AVPApprovedAt = null;
			}
			else if (IsUnrestrictedRole(role))
			{
				if (entity.RMApproved != true || entity.SMApproved != true)
					return BadRequest(new { message = "RM and SMM approval are required first." });

				if (entity.AVPApproved != null)
					return BadRequest(new { message = "This Warehouse is not pending final approval." });

				entity.AVPApproved = true;
				entity.AVPApprovedBy = userId;
				entity.AVPApprovedAt = now;
			}
			else
			{
				return Forbid();
			}

			entity.ApprovalRemarks = remarks;
			entity.UpdatedAt = now;
			entity.UpdatedBy = userId;

			await _repo.PatchAsync(id, entity);

			// Save every approval action in the common LogisticsApprovalHistory table.
			await SaveApprovalHistoryAsync(
				entity.Id, userId, role, now, remarks, isApproved: true);
			return Ok(new
			{
				message = IsRegionRole(role)
					? "Warehouse approved by RM and moved to SMM."
					: IsStateRole(role)
						? "Warehouse approved by SMM and moved to AVP."
						: "Warehouse finally approved."
			});
		}

		[HttpPost("{id}/reject")]
		public async Task<IActionResult> Reject(int id, [FromBody] LogisticsApprovalRequest? request)
		{
			var entity = await _repo.GetByIdAsync(id);
			if (entity == null) return NotFound(new { message = "Warehouse not found." });

			if (!entity.IsSubmittedForReview)
				return BadRequest(new { message = "This Warehouse is not submitted for approval." });

			var remarks = request?.Remarks?.Trim();
			if (string.IsNullOrWhiteSpace(remarks))
				return BadRequest(new { message = "Send Back remarks are required." });

			var role = CurrentRole();
			var userId = CurrentUserId();
			if (string.IsNullOrWhiteSpace(userId))
				return Unauthorized(new { message = "User not logged in." });

			var now = DateTime.Now;

			if (IsRegionRole(role))
			{
				var regionId = CurrentRegionId();
				if (!regionId.HasValue || entity.RegionId != regionId.Value)
					return Forbid();

				// RM can Send Back on initial review or after SMM has returned it to RM.
				var canRmAct = entity.RMApproved == null ||
					(entity.RMApproved == true && entity.SMApproved == false);
				if (!canRmAct)
					return BadRequest(new { message = "This Warehouse is not pending RM review." });

				// Send Back to MO/MDO/JMDO. Keep the row; do not delete it.
				entity.RMApproved = false;
				entity.RMApprovedBy = userId;
				entity.RMApprovedAt = now;
				entity.SMApproved = null;
				entity.SMApprovedBy = null;
				entity.SMApprovedAt = null;
				entity.AVPApproved = null;
				entity.AVPApprovedBy = null;
				entity.AVPApprovedAt = null;
			}
			else if (IsStateRole(role))
			{
				var stateId = CurrentStateId();
				if (!stateId.HasValue || entity.BasicStateId != stateId.Value)
					return Forbid();

				if (entity.RMApproved != true)
					return BadRequest(new { message = "RM approval is required first." });

				// SMM can Send Back on initial review or after AVP has returned it to SMM.
				var canSmmAct = entity.SMApproved == null ||
					(entity.SMApproved == true && entity.AVPApproved == false);
				if (!canSmmAct)
					return BadRequest(new { message = "This Warehouse is not pending SMM review." });

				// Send Back to RM.
				entity.SMApproved = false;
				entity.SMApprovedBy = userId;
				entity.SMApprovedAt = now;
				entity.AVPApproved = null;
				entity.AVPApprovedBy = null;
				entity.AVPApprovedAt = null;
			}
			else if (IsUnrestrictedRole(role))
			{
				if (entity.RMApproved != true || entity.SMApproved != true)
					return BadRequest(new { message = "RM and SMM approval are required first." });

				if (entity.AVPApproved != null)
					return BadRequest(new { message = "This Warehouse is not pending final review." });

				// Send Back to SMM.
				entity.AVPApproved = false;
				entity.AVPApprovedBy = userId;
				entity.AVPApprovedAt = now;
			}
			else
			{
				return Forbid();
			}

			entity.ApprovalRemarks = remarks;
			entity.UpdatedAt = now;
			entity.UpdatedBy = userId;

			await _repo.PatchAsync(id, entity);

			// Save every Send Back action. The Warehouse row remains saved.
			await SaveApprovalHistoryAsync(
				entity.Id, userId, role, now, remarks, isApproved: false);
			return Ok(new
			{
				message = IsRegionRole(role)
					? "Warehouse sent back to MO/MDO/JMDO."
					: IsStateRole(role)
						? "Warehouse sent back to RM."
						: "Warehouse sent back to SMM."
			});
		}

		private async Task SaveApprovalHistoryAsync(
			int sourceId,
			string approvedBy,
			string role,
			DateTime approvedAt,
			string remarks,
			bool isApproved)
		{
			await _historyRepo.CreateAsync(new LogisticsApprovalHistory
			{
				LogisticsSourceId = sourceId,
				LogisticsType = LogisticsType.Warehouse,
				ApprovedBy = approvedBy,
				Role = role,
				ApprovedAt = approvedAt,
				Remarks = remarks,
				IsApproved = isApproved
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
		private readonly IGenericRepository<LogisticsApprovalHistory> _historyRepo;

		public RackPointController(
			IGenericRepository<RackPoint> repo,
			IGenericRepository<LogisticsApprovalHistory> historyRepo) : base(repo)
		{
			_historyRepo = historyRepo;
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

			// Server-side duplicate SAP Code check.
			if (!string.IsNullOrWhiteSpace(entity.SAPCode))
			{
				var sapCode = entity.SAPCode.Trim();
				if (await _repo.ExistsAsync(x => x.SAPCode != null && x.SAPCode.Trim() == sapCode))
				{
					return Conflict(new { message = $"Rake Point with SAP Code '{sapCode}' already exists." });
				}
			}

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

			var userId = CurrentUserId();
			var role = CurrentRole();

			// If RM sent this record back to its original MO/MDO/JMDO creator,
			// saving the corrected record resubmits the SAME row to RM.
			// No delete/recreate is performed.
			var isCreatorResubmission =
				existing.IsSubmittedForReview &&
				existing.RMApproved == false &&
				IsCreatorRole(role) &&
				!string.IsNullOrWhiteSpace(userId) &&
				string.Equals(existing.CreatedBy, userId, StringComparison.OrdinalIgnoreCase);

			// IMPORTANT: existing Logistics.razor performs a second PUT after document upload.
			// Preserve every workflow-owned value so normal edit/document saves cannot
			// accidentally clear approval state.
			PreserveApprovalState(existing, entity);

			if (isCreatorResubmission)
			{
				// RM had sent the request back to the creator. The creator's save now
				// places it back in Pending RM while preserving the same record Id.
				entity.IsSubmittedForReview = true;
				entity.RMApproved = null;
				entity.RMApprovedBy = null;
				entity.RMApprovedAt = null;
				entity.SMApproved = null;
				entity.SMApprovedBy = null;
				entity.SMApprovedAt = null;
				entity.AVPApproved = null;
				entity.AVPApprovedBy = null;
				entity.AVPApprovedAt = null;
				// Keep ApprovalRemarks until the next workflow action so the latest
				// Send Back reason remains available to the resubmitting creator/RM.
			}

			// Server-side duplicate SAP Code check (exclude current record).
			if (!string.IsNullOrWhiteSpace(entity.SAPCode))
			{
				var sapCode = entity.SAPCode.Trim();
				if (await _repo.ExistsAsync(x => x.Id != id && x.SAPCode != null && x.SAPCode.Trim() == sapCode))
				{
					return Conflict(new { message = $"Rake Point with SAP Code '{sapCode}' already exists." });
				}
			}

			entity.UpdatedAt = DateTime.Now;
			entity.UpdatedBy = string.IsNullOrWhiteSpace(userId)
				? existing.UpdatedBy
				: userId;

			var updated = await _repo.PatchAsync(id, entity);
			if (updated == null) return NotFound();

			return Ok(new
			{
				message = isCreatorResubmission
					? "Rake Point updated and resubmitted to RM for approval."
					: "RackPoint updated successfully",
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

			var remarks = request?.Remarks?.Trim();
			if (string.IsNullOrWhiteSpace(remarks))
				return BadRequest(new { message = "Approval remarks are required." });

			var role = CurrentRole();
			var userId = CurrentUserId();
			if (string.IsNullOrWhiteSpace(userId))
				return Unauthorized(new { message = "User not logged in." });

			var now = DateTime.Now;

			if (IsRegionRole(role))
			{
				var regionId = CurrentRegionId();
				if (!regionId.HasValue || entity.RegionId != regionId.Value)
					return Forbid();

				// RM can act on the initial RM review OR when SMM sent it back to RM.
				var isInitialRmReview = entity.RMApproved == null;
				var isReturnedFromSmm = entity.RMApproved == true && entity.SMApproved == false;
				if (!isInitialRmReview && !isReturnedFromSmm)
					return BadRequest(new { message = "This Rake Point is not pending RM approval." });

				entity.RMApproved = true;
				entity.RMApprovedBy = userId;
				entity.RMApprovedAt = now;

				// Forward to SMM again. Any SMM/AVP Send Back marker is cleared.
				entity.SMApproved = null;
				entity.SMApprovedBy = null;
				entity.SMApprovedAt = null;
				entity.AVPApproved = null;
				entity.AVPApprovedBy = null;
				entity.AVPApprovedAt = null;
			}
			else if (IsStateRole(role))
			{
				var stateId = CurrentStateId();
				if (!stateId.HasValue || entity.BasicStateId != stateId.Value)
					return Forbid();

				if (entity.RMApproved != true)
					return BadRequest(new { message = "RM approval is required first." });

				// SMM can act initially OR when AVP sent it back to SMM.
				var isInitialSmmReview = entity.SMApproved == null;
				var isReturnedFromAvp = entity.SMApproved == true && entity.AVPApproved == false;
				if (!isInitialSmmReview && !isReturnedFromAvp)
					return BadRequest(new { message = "This Rake Point is not pending SMM approval." });

				entity.SMApproved = true;
				entity.SMApprovedBy = userId;
				entity.SMApprovedAt = now;

				// Forward to AVP again. Clear the AVP Send Back marker.
				entity.AVPApproved = null;
				entity.AVPApprovedBy = null;
				entity.AVPApprovedAt = null;
			}
			else if (IsUnrestrictedRole(role))
			{
				if (entity.RMApproved != true || entity.SMApproved != true)
					return BadRequest(new { message = "RM and SMM approval are required first." });

				if (entity.AVPApproved != null)
					return BadRequest(new { message = "This Rake Point is not pending final approval." });

				entity.AVPApproved = true;
				entity.AVPApprovedBy = userId;
				entity.AVPApprovedAt = now;
			}
			else
			{
				return Forbid();
			}

			entity.ApprovalRemarks = remarks;
			entity.UpdatedAt = now;
			entity.UpdatedBy = userId;

			await _repo.PatchAsync(id, entity);

			// Save every approval action in the common LogisticsApprovalHistory table.
			await SaveApprovalHistoryAsync(
				entity.Id, userId, role, now, remarks, isApproved: true);
			return Ok(new
			{
				message = IsRegionRole(role)
					? "Rake Point approved by RM and moved to SMM."
					: IsStateRole(role)
						? "Rake Point approved by SMM and moved to AVP."
						: "Rake Point finally approved."
			});
		}

		[HttpPost("{id}/reject")]
		public async Task<IActionResult> Reject(int id, [FromBody] LogisticsApprovalRequest? request)
		{
			var entity = await _repo.GetByIdAsync(id);
			if (entity == null) return NotFound(new { message = "Rake Point not found." });

			if (!entity.IsSubmittedForReview)
				return BadRequest(new { message = "This Rake Point is not submitted for approval." });

			var remarks = request?.Remarks?.Trim();
			if (string.IsNullOrWhiteSpace(remarks))
				return BadRequest(new { message = "Send Back remarks are required." });

			var role = CurrentRole();
			var userId = CurrentUserId();
			if (string.IsNullOrWhiteSpace(userId))
				return Unauthorized(new { message = "User not logged in." });

			var now = DateTime.Now;

			if (IsRegionRole(role))
			{
				var regionId = CurrentRegionId();
				if (!regionId.HasValue || entity.RegionId != regionId.Value)
					return Forbid();

				// RM can Send Back on initial review or after SMM has returned it to RM.
				var canRmAct = entity.RMApproved == null ||
					(entity.RMApproved == true && entity.SMApproved == false);
				if (!canRmAct)
					return BadRequest(new { message = "This Rake Point is not pending RM review." });

				// Send Back to MO/MDO/JMDO. Keep the row; do not delete it.
				entity.RMApproved = false;
				entity.RMApprovedBy = userId;
				entity.RMApprovedAt = now;
				entity.SMApproved = null;
				entity.SMApprovedBy = null;
				entity.SMApprovedAt = null;
				entity.AVPApproved = null;
				entity.AVPApprovedBy = null;
				entity.AVPApprovedAt = null;
			}
			else if (IsStateRole(role))
			{
				var stateId = CurrentStateId();
				if (!stateId.HasValue || entity.BasicStateId != stateId.Value)
					return Forbid();

				if (entity.RMApproved != true)
					return BadRequest(new { message = "RM approval is required first." });

				// SMM can Send Back on initial review or after AVP has returned it to SMM.
				var canSmmAct = entity.SMApproved == null ||
					(entity.SMApproved == true && entity.AVPApproved == false);
				if (!canSmmAct)
					return BadRequest(new { message = "This Rake Point is not pending SMM review." });

				// Send Back to RM.
				entity.SMApproved = false;
				entity.SMApprovedBy = userId;
				entity.SMApprovedAt = now;
				entity.AVPApproved = null;
				entity.AVPApprovedBy = null;
				entity.AVPApprovedAt = null;
			}
			else if (IsUnrestrictedRole(role))
			{
				if (entity.RMApproved != true || entity.SMApproved != true)
					return BadRequest(new { message = "RM and SMM approval are required first." });

				if (entity.AVPApproved != null)
					return BadRequest(new { message = "This Rake Point is not pending final review." });

				// Send Back to SMM.
				entity.AVPApproved = false;
				entity.AVPApprovedBy = userId;
				entity.AVPApprovedAt = now;
			}
			else
			{
				return Forbid();
			}

			entity.ApprovalRemarks = remarks;
			entity.UpdatedAt = now;
			entity.UpdatedBy = userId;

			await _repo.PatchAsync(id, entity);

			// Save every Send Back action. The Rake Point row remains saved.
			await SaveApprovalHistoryAsync(
				entity.Id, userId, role, now, remarks, isApproved: false);
			return Ok(new
			{
				message = IsRegionRole(role)
					? "Rake Point sent back to MO/MDO/JMDO."
					: IsStateRole(role)
						? "Rake Point sent back to RM."
						: "Rake Point sent back to SMM."
			});
		}

		private async Task SaveApprovalHistoryAsync(
			int sourceId,
			string approvedBy,
			string role,
			DateTime approvedAt,
			string remarks,
			bool isApproved)
		{
			await _historyRepo.CreateAsync(new LogisticsApprovalHistory
			{
				LogisticsSourceId = sourceId,
				LogisticsType = LogisticsType.RakePoint,
				ApprovedBy = approvedBy,
				Role = role,
				ApprovedAt = approvedAt,
				Remarks = remarks,
				IsApproved = isApproved
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

	// ---------------------------------------------------------------------
	// LOGISTICS APPROVAL HISTORY
	// Common audit trail for Warehouse + Rake Point.
	// ---------------------------------------------------------------------
	[Route("api/[controller]")]
	public class LogisticsApprovalHistoryController
		: GenericCrudController<LogisticsApprovalHistory>
	{
		public LogisticsApprovalHistoryController(
			IGenericRepository<LogisticsApprovalHistory> repo) : base(repo)
		{
		}

		[HttpGet("by-source/{logisticsType}/{sourceId:int}")]
		public async Task<IActionResult> GetBySource(LogisticsType logisticsType, int sourceId)
		{
			if (sourceId <= 0)
				return BadRequest(new { message = "Invalid Logistics source id." });

			var history = await _repo
				.GetAllWithInactive()
				.Where(x => x.LogisticsType == logisticsType && x.LogisticsSourceId == sourceId)
				.AsNoTracking()
				.OrderBy(x => x.ApprovedAt)
				.ThenBy(x => x.Id)
				.ToListAsync();

			return Ok(history);
		}
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