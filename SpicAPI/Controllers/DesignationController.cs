using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// Designation CRUD + the page catalog endpoint consumed by the Designation
	/// permission grid.  Create/Update also sync DesignationPermission rows so
	/// the normalized table stays consistent with the RoleAccess CSV that the
	/// runtime still reads.
	/// </summary>
	[Route("api/[controller]")]
	public class DesignationController : GenericCrudController<Designation>
	{
		private readonly IGenericRepository<ApplicationPage> _pageRepo;
		private readonly AppDbContext _db;

		public DesignationController(
			IGenericRepository<Designation> repo,
			IGenericRepository<ApplicationPage> pageRepo,
			AppDbContext db) : base(repo)
		{
			_pageRepo = pageRepo;
			_db = db;
		}

		/// <summary>
		/// Active application pages for the Designation permission grid,
		/// sorted by SortOrder. Readable by any authenticated user.
		/// </summary>
		[HttpGet("pagecatalog")]
		public async Task<IActionResult> GetPageCatalog()
		{
			var pages = await _pageRepo.GetAll()
				.OrderBy(p => p.SortOrder)
				.ToListAsync();
			return Ok(pages);
		}

		[HttpPost]
		public override async Task<IActionResult> Create([FromBody] Designation entity)
		{
			var created = await _repo.CreateAsync(entity);
			await SyncPermissionsAsync(created.Id, created.RoleAccess);
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

			await SyncPermissionsAsync(id, entity.RoleAccess);
			return Ok(new
			{
				message = "Designation updated successfully",
				data = updated
			});
		}

		// ----------------------------------------------------------------
		//  DesignationPermission sync
		// ----------------------------------------------------------------

		/// <summary>
		/// Parse RoleAccess → upsert/delete DesignationPermission rows for
		/// pages still in the active catalog.  Tokens referencing deactivated
		/// or removed pages are left untouched (no row created, existing row
		/// preserved) so nothing is lost during a deactivation cycle.
		/// </summary>
		private async Task SyncPermissionsAsync(int designationId, string? roleAccess)
		{
			// 1.  Parse RoleAccess into page→actions map
			var granted = ParseGrantedActions(roleAccess);

			// 2.  Active catalog: Key (lowered) → Id
			var catalogPages = await _pageRepo.GetAll().ToListAsync();
			var keyToId = catalogPages
				.ToDictionary(p => p.Key.ToLowerInvariant(), p => p.Id);

			// 3.  Existing rows for this designation
			var existing = await _db.DesignationPermissions
				.Where(p => p.DesignationId == designationId)
				.ToListAsync();
			var existingByPageId = existing.ToDictionary(p => p.PageId);

			// 4.  Upsert rows for catalog pages that have granted actions
			var touchedPageIds = new HashSet<int>();

			foreach (var (key, actions) in granted)
			{
				if (!keyToId.TryGetValue(key.ToLowerInvariant(), out var pageId))
					continue;

				touchedPageIds.Add(pageId);

				var hasView = actions.Contains("View", StringComparer.OrdinalIgnoreCase);
				var hasEntry = actions.Contains("Entry", StringComparer.OrdinalIgnoreCase);
				var hasUpdate = actions.Contains("Update", StringComparer.OrdinalIgnoreCase);
				var hasDelete = actions.Contains("Delete", StringComparer.OrdinalIgnoreCase);

				if (!hasView && !hasEntry && !hasUpdate && !hasDelete)
					continue;

				if (existingByPageId.TryGetValue(pageId, out var row))
				{
					if (row.View == hasView && row.Entry == hasEntry &&
						row.Update == hasUpdate && row.Delete == hasDelete)
						continue; // already in sync

					row.View = hasView;
					row.Entry = hasEntry;
					row.Update = hasUpdate;
					row.Delete = hasDelete;
					_db.DesignationPermissions.Update(row);
				}
				else
				{
					_db.DesignationPermissions.Add(new DesignationPermission
					{
						DesignationId = designationId,
						PageId = pageId,
						View = hasView,
						Entry = hasEntry,
						Update = hasUpdate,
						Delete = hasDelete
					});
				}
			}

			// 5.  Delete rows for active-catalog pages that are no longer
			//     selected at all.  Rows for OFF-catalog pages (deactivated
			//     or removed) are deliberately left untouched.
			var activePageIds = new HashSet<int>(keyToId.Values);
			foreach (var row in existing)
			{
				if (!activePageIds.Contains(row.PageId))
					continue; // off-catalog → leave alone
				if (touchedPageIds.Contains(row.PageId))
					continue; // already handled above

				_db.DesignationPermissions.Remove(row);
			}

			await _db.SaveChangesAsync();
		}

		/// <summary>
		/// Parses RoleAccess CSV into a case-insensitive map of
		/// pageKey → set of granted action names.  A legacy bare "Page"
		/// token grants all four actions.
		/// </summary>
		private static Dictionary<string, HashSet<string>> ParseGrantedActions(string? csv)
		{
			var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
			if (string.IsNullOrWhiteSpace(csv))
				return result;

			var allActions = new[] { "View", "Entry", "Update", "Delete" };

			foreach (var raw in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				var dot = raw.IndexOf('.');
				string page;
				string? action;

				if (dot < 0)
				{
					page = raw;
					action = null; // legacy bare token = all actions
				}
				else
				{
					page = raw.Substring(0, dot);
					action = raw.Substring(dot + 1);
				}

				if (!result.TryGetValue(page, out var set))
					result[page] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				if (action == null)
					foreach (var a in allActions) set.Add(a);
				else
					set.Add(action);
			}

			return result;
		}
	}
}