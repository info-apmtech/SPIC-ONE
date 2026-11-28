using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using Tesseract;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// Publicity Material Tracking (route list in SPIC.Core/DTOs/PublicityDtos.cs,
    /// plan in docs/publicity-tracking-plan.md). Stage and Status after final approval are DERIVED
    /// from the dispatch + acknowledgement rows by <see cref="Recompute"/>, so every write ends by
    /// calling it and no screen can disagree with another.
    ///
    /// Roles: RMD (RMDO) raises / acknowledges / submits transfer proof for its HQs;
    /// RM, SMD, SMM, MDAdmin (ADMIN-MD CS), MDHOD (DGM-MD CS) approve in that order;
    /// MDAdmin, MDHOD, Admin, SuperAdmin, CorporateAdmin dispatch and resolve exceptions.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PublicityController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public PublicityController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        private const long MaxFileBytes = 10L * 1024 * 1024;
        private static readonly string[] AllowedExt = { ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".doc", ".docx" };

        // Stage sequence only - WHO holds each stage is resolved by IsMyTurnAsync/MyStageAsync below,
        // since ADMIN-MD CS / DGM-MD CS are identified by designation, not a plain AppRole match.
        private static readonly PublicityStage[] ChainStages =
        {
            PublicityStage.RmReview, PublicityStage.SmdReview, PublicityStage.SmmReview,
            PublicityStage.AdminMdCsReview, PublicityStage.DgmMdCsReview
        };

        private static readonly AppRole[] SuperRoles = { AppRole.Admin, AppRole.SuperAdmin };

        private static readonly string[] KnownCouriers =
        {
            "Blue Dart Express", "DTDC", "Delhivery", "Professional Couriers", "India Post", "Speed Post",
            "Ecom Express", "Xpressbees", "Gati", "TCI Express", "VRL Logistics", "ST Courier", "Shree Maruti"
        };

        // ================================================================ lookups

        [HttpGet("lookups")]
        public async Task<IActionResult> Lookups([FromQuery] int? regionId)
        {
            List<PubPerson> people;
            if (regionId.HasValue && regionId.Value > 0 && await IsDispatcherAsync())
            {
                var query = from u in _db.Users.AsNoTracking()
                            join e in _db.Employeelogins.AsNoTracking() on u.Id equals e.UserId
                            where e.IsActive && e.RegionId == regionId.Value
                            select new { u.Id, u.Name, u.UserName, u.Role };
                people = (await query.ToListAsync())
                    .Select(p => new PubPerson { UserId = p.Id, Name = p.Name ?? p.UserName ?? p.Id, Role = RoleLabel(p.Role) })
                    .GroupBy(p => p.UserId).Select(g => g.First()).OrderBy(p => p.Name).ToList();
            }
            else
            {
                people = await GetAuthorizedPeopleAsync();
            }
            var me = await MyNameAsync();
            if (people.All(p => p.UserId != Uid()))
                people.Insert(0, new PubPerson { UserId = Uid(), Name = me, Role = RoleLabel(MyRole()) });

            var couriers = await _db.SasCouriers.AsNoTracking().Where(c => c.IsActive).Select(c => c.Name).ToListAsync();
            var vendors = await _db.PublicityDispatches.AsNoTracking().Where(d => d.VendorName != null)
                .Select(d => d.VendorName!).Distinct().Take(100).ToListAsync();

            return Ok(new PubLookupsDto
            {
                Materials = await _db.PublicityMaterialTypes.AsNoTracking().Where(m => m.IsActive)
                    .OrderBy(m => m.SortOrder).ThenBy(m => m.Name)
                    .Select(m => new PubMaterialOption { Id = m.Id, Name = m.Name, Category = m.Category }).ToListAsync(),
                Products = await _db.Products.AsNoTracking().Where(p => p.IsActive)
                    .OrderBy(p => p.Name).Select(p => new PubOption { Id = p.Id, Name = p.Name }).ToListAsync(),
                Couriers = couriers.Concat(KnownCouriers).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(),
                Vendors = vendors.OrderBy(x => x).ToList(),
                People = people,
                MyUserId = Uid(),
                MyName = me,
                MyStateId = ClaimInt("spic:state_id"),
                MyRegionId = ClaimInt("spic:region_id"),
                MyHqId = ClaimInt("spic:hq_id"),
                MyScope = await ScopeLabelAsync()
            });
        }

        // ================================================================ material master
        // Every endpoint below is gated on CanManageMaterialMasterAsync() - the Designation-grid
        // PublicityMaterialMaster permission (plus the IsSuper()/AppRole.MDAdmin bypasses it itself
        // preserves) - NOT IsAdminMdCsAsync()'s hardcoded Designation-name match, which remains in use
        // elsewhere in this controller for stage/notification/review logic only (2026-11-27 fix).

        [HttpGet("materials")]
        public async Task<IActionResult> MaterialsAdmin()
        {
            if (!await CanManageMaterialMasterAsync()) return Forbid();
            var rows = await _db.PublicityMaterialTypes.AsNoTracking()
                .OrderBy(m => m.Category).ThenBy(m => m.SortOrder).ThenBy(m => m.Name)
                .Select(m => new PubMaterialAdminDto { Id = m.Id, Name = m.Name, Category = m.Category, IsActive = m.IsActive, SortOrder = m.SortOrder })
                .ToListAsync();
            return Ok(rows);
        }

        [HttpPost("materials")]
        public async Task<IActionResult> CreateMaterial([FromBody] PubMaterialUpsertDto dto)
        {
            if (!await CanManageMaterialMasterAsync()) return Forbid();
            var error = await ValidateMaterialAsync(dto, null);
            if (error is not null) return BadRequest(error);

            var maxSort = await _db.PublicityMaterialTypes.AsNoTracking()
                .Where(m => m.Category == dto.Category).Select(m => (int?)m.SortOrder).MaxAsync() ?? 0;
            var m2 = new PublicityMaterialType
            {
                Name = dto.Name.Trim(),
                Category = dto.Category,
                IsActive = dto.IsActive,
                SortOrder = maxSort + 1
            };
            _db.PublicityMaterialTypes.Add(m2);
            await _db.SaveChangesAsync();
            return Ok(new PubMaterialAdminDto { Id = m2.Id, Name = m2.Name, Category = m2.Category, IsActive = m2.IsActive, SortOrder = m2.SortOrder });
        }

        [HttpPut("materials/{id:int}")]
        public async Task<IActionResult> UpdateMaterial(int id, [FromBody] PubMaterialUpsertDto dto)
        {
            if (!await CanManageMaterialMasterAsync()) return Forbid();
            var m2 = await _db.PublicityMaterialTypes.FirstOrDefaultAsync(m => m.Id == id);
            if (m2 is null) return NotFound();
            var error = await ValidateMaterialAsync(dto, id);
            if (error is not null) return BadRequest(error);

            m2.Name = dto.Name.Trim();
            m2.Category = dto.Category;
            m2.IsActive = dto.IsActive;
            await _db.SaveChangesAsync();
            return Ok(new PubMaterialAdminDto { Id = m2.Id, Name = m2.Name, Category = m2.Category, IsActive = m2.IsActive, SortOrder = m2.SortOrder });
        }

        [HttpDelete("materials/{id:int}")]
        public async Task<IActionResult> DeleteMaterial(int id)
        {
            if (!await CanManageMaterialMasterAsync()) return Forbid();
            var m2 = await _db.PublicityMaterialTypes.FirstOrDefaultAsync(m => m.Id == id);
            if (m2 is null) return NotFound();

            // FK_PublicityRequests_PublicityMaterialTypes_MaterialTypeId is ReferentialAction.Restrict
            // (V31_PublicityTracking) - the database would already reject this delete if any request
            // (including a soft-deleted one, IsDeleted is a display flag only, not a real row removal)
            // references it. Checking first gives a clear message instead of a raw DbUpdateException,
            // and - per the master plan - deactivation is the safe alternative so existing requests
            // keep displaying their saved material type (PublicityForm/PubDetailDto.Material already
            // resolve the name with no IsActive filter either way).
            var inUse = await _db.PublicityRequests.AsNoTracking().CountAsync(r => r.MaterialTypeId == id);
            if (inUse > 0)
                return BadRequest(new
                {
                    Message = $"Cannot delete \"{m2.Name}\" - it is used by {inUse} publicity request(s). Deactivate it instead to hide it from new requests."
                });

            _db.PublicityMaterialTypes.Remove(m2);
            await _db.SaveChangesAsync();
            return Ok(new { Success = true });
        }

        private async Task<string?> ValidateMaterialAsync(PubMaterialUpsertDto dto, int? id)
        {
            var name = dto.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name)) return "Enter the material name.";
            if (name.Length > 100) return "Material name must be 100 characters or fewer.";
            if (!Enum.IsDefined(typeof(PublicityMaterialCategory), dto.Category)) return "Select a valid category.";
            // Duplicate check only ever blocks another ACTIVE row with the same name in the same
            // category - an inactive (deactivated) material keeping its old name never blocks a
            // fresh/renamed active one from reusing it, consistent with "prefer deactivation over
            // deletion" (the deactivated row stays only for historical FK integrity, not as a name
            // reservation).
            if (dto.IsActive)
            {
                var dup = await _db.PublicityMaterialTypes.AsNoTracking().AnyAsync(m =>
                    m.Id != (id ?? 0) && m.IsActive && m.Category == dto.Category && m.Name.ToLower() == name.ToLower());
                if (dup) return "An active material with this name already exists in this category.";
            }
            return null;
        }

        private async Task<List<PubPerson>> GetAuthorizedPeopleAsync()
        {
            var myUserId = Uid();
            var myName = await MyNameAsync();
            var myRole = MyRole();

            var result = new List<PubPerson>
            {
                new PubPerson { UserId = myUserId, Name = myName, Role = RoleLabel(myRole) }
            };

            var subLogins = _db.Employeelogins.AsNoTracking().Where(e => e.IsActive && e.UserId != myUserId);
            bool hasSubordinates = false;

            if (IsSuper())
            {
                hasSubordinates = true;
            }
            else if (myRole is AppRole.SMD or AppRole.SMM)
            {
                var myStateId = ClaimInt("spic:state_id");
                if (myStateId > 0)
                {
                    hasSubordinates = true;
                    var stateRoles = new[] { AppRole.RM, AppRole.RMD, AppRole.MDO, AppRole.MO, AppRole.JMDO };
                    subLogins = subLogins.Where(e => e.StateId == myStateId && stateRoles.Contains(e.Role));
                }
            }
            else if (myRole == AppRole.RM)
            {
                var myRegionId = ClaimInt("spic:region_id");
                if (myRegionId > 0)
                {
                    hasSubordinates = true;
                    var regionRoles = new[] { AppRole.RMD, AppRole.MDO, AppRole.MO, AppRole.JMDO };
                    subLogins = subLogins.Where(e => e.RegionId == myRegionId && regionRoles.Contains(e.Role));
                }
            }
            else if (myRole is AppRole.RMD or AppRole.MDO)
            {
                var myHqs = await MyHqIdsAsync();
                if (myHqs.Count > 0)
                {
                    hasSubordinates = true;
                    var hqRoles = new[] { AppRole.MO, AppRole.JMDO };
                    subLogins = subLogins.Where(e => myHqs.Contains(e.HeadquartersId) && hqRoles.Contains(e.Role));
                }
            }
            else if (myRole == AppRole.MO)
            {
                var myHqs = await MyHqIdsAsync();
                if (myHqs.Count > 0)
                {
                    hasSubordinates = true;
                    subLogins = subLogins.Where(e => myHqs.Contains(e.HeadquartersId) && e.Role == AppRole.JMDO);
                }
            }

            if (hasSubordinates)
            {
                var subordinates = await (
                    from e in subLogins
                    join u in _db.Users.AsNoTracking() on e.UserId equals u.Id
                    select new { u.Id, Name = u.Name ?? u.UserName ?? u.Id, e.Role }
                ).Distinct().ToListAsync();

                var sorted = subordinates
                    .GroupBy(p => p.Id)
                    .Select(g => g.First())
                    .OrderBy(p => p.Name)
                    .Select(p => new PubPerson { UserId = p.Id, Name = p.Name, Role = RoleLabel(p.Role) });

                result.AddRange(sorted);
            }

            return result;
        }

        // ================================================================ list + stats

        [HttpGet("requests/stats")]
        public async Task<IActionResult> Stats([FromQuery] int? category = null)
        {
            var q = await ScopedAsync();
            if (category.HasValue)
            {
                q = q.Where(r => (int)r.MaterialType!.Category == category.Value);
            }
            var rows = await q.Select(r => new { r.Status, r.Stage, r.IsGiftArticle }).ToListAsync();
            var mine = await MyStageAsync();
            return Ok(new PubStatsDto
            {
                Total = rows.Count(r => r.Status != PublicityStatus.Draft),
                Draft = rows.Count(r => r.Status is PublicityStatus.Draft or PublicityStatus.Returned),
                PendingApproval = rows.Count(r => r.Status is PublicityStatus.PendingApproval or PublicityStatus.PendingFinalApproval),
                FinalApproved = rows.Count(r => r.Status == PublicityStatus.FinalApproved),
                InDispatch = rows.Count(r => r.Status is PublicityStatus.InDispatch or PublicityStatus.ReInitiationInProgress or PublicityStatus.TransferInProgress),
                AckPending = rows.Count(r => r.Status == PublicityStatus.AcknowledgementPending),
                Completed = rows.Count(r => r.Status is PublicityStatus.Completed or PublicityStatus.ExceptionResolved),
                Exception = rows.Count(r => r.Status == PublicityStatus.ExceptionRaised),
                PendingMine = rows.Count(r => IsPendingAt(r.Status, r.Stage, mine)),
                WithDgm = rows.Count(r => IsPendingAt(r.Status, r.Stage, PublicityStage.DgmMdCsReview)),
                DispatchPending = rows.Count(r => r.Status == PublicityStatus.FinalApproved),
                Returned = rows.Count(r => r.Status == PublicityStatus.Returned),
                ApprovedByMe = mine is null ? 0 : rows.Count(r => r.Stage > mine && r.Status is not (PublicityStatus.Draft or PublicityStatus.Returned or PublicityStatus.Rejected)),
                FinalApproval = mine is null ? 0 : rows.Count(r => r.Stage > mine && r.Stage <= PublicityStage.DgmMdCsReview && r.Status is PublicityStatus.PendingApproval or PublicityStatus.PendingFinalApproval),
                PublicityMaterialCount = rows.Count(r => !r.IsGiftArticle),
                GiftArticlesCount = rows.Count(r => r.IsGiftArticle)
            });
        }

        [HttpGet("requests")]
        public async Task<IActionResult> List([FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int? productId, [FromQuery] int? materialTypeId, [FromQuery] int? status, [FromQuery] int? stage,
            [FromQuery] int? stateId, [FromQuery] int? regionId, [FromQuery] int? hqId, [FromQuery] string? q,
            [FromQuery] bool awaitingMe = false, [FromQuery] bool submittedOnly = false,
            [FromQuery] int? category = null,
            [FromQuery] int page = 1, [FromQuery] int size = 16)
        {
            var query = await ScopedAsync();
            if (category.HasValue) query = query.Where(r => (int)r.MaterialType!.Category == category.Value);
            if (from.HasValue) query = query.Where(r => r.CreatedAt >= from.Value.Date);
            if (to.HasValue) query = query.Where(r => r.CreatedAt < to.Value.Date.AddDays(1));
            if (productId > 0) query = query.Where(r => r.ProductId == productId || r.Items.Any(i => i.ProductId == productId));
            if (materialTypeId > 0) query = query.Where(r => r.MaterialTypeId == materialTypeId);
            if (status.HasValue) query = query.Where(r => (int)r.Status == status.Value);
            if (stage.HasValue) query = query.Where(r => (int)r.Stage == stage.Value);
            if (stateId > 0) query = query.Where(r => r.StateId == stateId);
            if (regionId > 0) query = query.Where(r => r.RegionId == regionId);
            if (hqId > 0) query = query.Where(r => r.HeadquarterId == hqId);
            if (submittedOnly) query = query.Where(r => r.Status != PublicityStatus.Draft);
            if (awaitingMe)
            {
                var mine = await MyStageAsync();
                if (mine is null) query = query.Where(r => false);
                else query = query.Where(r => r.Stage == mine &&
                    (r.Status == PublicityStatus.PendingApproval || r.Status == PublicityStatus.PendingFinalApproval));
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                var productIds = await _db.Products.AsNoTracking().Where(p => p.Name.ToLower().Contains(term)).Select(p => p.Id).ToListAsync();
                query = query.Where(r => r.IndentNo.ToLower().Contains(term) || productIds.Contains(r.ProductId)
                    || r.Items.Any(i => productIds.Contains(i.ProductId))
                    || r.MaterialType!.Name.ToLower().Contains(term));
            }

            size = Math.Clamp(size, 1, 100);
            page = Math.Max(1, page);
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(r => r.Id).Skip((page - 1) * size).Take(size).ToListAsync();
            return Ok(new PubPageDto { Total = total, Items = await RowsAsync(items) });
        }

        /// <summary>Approver consolidation (State / Region review): requests waiting at my stage.</summary>
        [HttpGet("consolidation")]
        public async Task<IActionResult> Consolidation([FromQuery] int? stage)
        {
            var mine = stage.HasValue && IsSuper() ? (PublicityStage)stage.Value : await MyStageAsync();
            if (mine is null) return Forbid();

            var groups = await BuildRegionGroupsAsync(mine.Value);
            var allRows = groups.SelectMany(g => g.Rows).ToList();

            var scope = await ScopeLabelAsync();
            var nextIndex = Array.FindIndex(ChainStages, s => s == mine) + 1;
            var lastSubmitted = groups.Select(g => g.LastUpdated).Where(d => d.HasValue).Select(d => d!.Value)
                .DefaultIfEmpty(DateTime.Now).Max();
            var submissionId = allRows.Count > 0
                ? $"SUB-{lastSubmitted.Year}-{(allRows.FirstOrDefault()?.Id ?? 12):D4}"
                : $"SUB-{DateTime.Now.Year}-0012";

            return Ok(new PubConsolidationDto
            {
                Scope = scope,
                StageLabel = StageLabel(mine.Value),
                NextLabel = nextIndex < ChainStages.Length ? StageLabel(ChainStages[nextIndex]).Replace(" Review", "") : "Dispatch",
                SubmissionId = submissionId,
                SubmittedAt = lastSubmitted,
                Regions = groups.Count,
                Hqs = groups.Sum(g => g.Hqs),
                Requests = groups.Sum(g => g.EligibleCount),
                Quantity = groups.Sum(g => g.Quantity),
                RegionsFinalized = groups.Count(g => g.Status is PubRegionStatus.Approved or PubRegionStatus.ZeroConfirmed),
                ApprovedRegions = groups.Count(g => g.Status == PubRegionStatus.Approved),
                ZeroConfirmedRegions = groups.Count(g => g.Status == PubRegionStatus.ZeroConfirmed),
                Groups = groups
            });
        }

        // SMD approves everything currently eligible in one Region as a block - this does NOT
        // advance any request's Stage/Status (that only happens for the whole state at once, in
        // ReviewBulk, once every Region is finalized - see ReviewBulk's own comment for why). Every
        // precondition is re-derived from BuildRegionGroupsAsync's live data, never trusted from the
        // client - the client's own enabled/disabled button is UX only.
        [HttpPost("regions/{stateId:int}/{regionId:int}/approve")]
        public async Task<IActionResult> ApproveRegion(int stateId, int regionId, [FromBody] PubRegionActionDto dto)
        {
            var mine = await MyStageAsync();
            if (mine != PublicityStage.SmdReview) return Forbid();
            var myStateId = ClaimInt("spic:state_id");
            if (myStateId <= 0 || myStateId != stateId) return Forbid();

            var groups = await BuildRegionGroupsAsync(mine.Value);
            var region = groups.FirstOrDefault(g => g.StateId == stateId && g.RegionId == regionId);
            if (region is null) return NotFound(new { Success = false, Message = "Region not found for your state." });
            if (region.StillAtRmCount > 0)
                return BadRequest(new { Success = false, Message = $"{region.StillAtRmCount} request(s) from this Region have not yet reached you from RM." });
            if (region.EligibleCount == 0)
                return BadRequest(new { Success = false, Message = "This Region has no requirements to approve." });
            if (region.ReviewedCount < region.EligibleCount)
                return BadRequest(new { Success = false, Message = $"{region.EligibleCount - region.ReviewedCount} request(s) in this Region still need to be reviewed." });

            await UpsertRegionFinalizationAsync(stateId, regionId, PublicityRegionFinalizationStatus.Approved);
            return Ok(new { Success = true });
        }

        // Finalize / Acknowledge a Zero-requirement Region: SMD can only finalize a zero-requirement
        // region after the RM has explicitly submitted Zero Requirement (Status == ZeroSubmittedByRm).
        [HttpPost("regions/{stateId:int}/{regionId:int}/finalize-zero")]
        public async Task<IActionResult> FinalizeZeroRegion(int stateId, int regionId, [FromBody] PubRegionActionDto? dto = null)
        {
            var mine = await MyStageAsync();
            if (mine != PublicityStage.SmdReview) return Forbid();
            var myStateId = ClaimInt("spic:state_id");
            if (myStateId <= 0 || myStateId != stateId) return Forbid();

            var groups = await BuildRegionGroupsAsync(mine.Value);
            var region = groups.FirstOrDefault(g => g.StateId == stateId && g.RegionId == regionId);
            if (region is null) return NotFound(new { Success = false, Message = "Region not found for your state." });
            if (region.EligibleCount > 0 || region.StillAtRmCount > 0)
                return BadRequest(new { Success = false, Message = "This Region has requirements - review and approve them instead of finalizing zero." });
            if (region.Status != PubRegionStatus.ZeroSubmittedByRm)
                return BadRequest(new { Success = false, Message = "Cannot finalize zero requirement: the assigned RM has not submitted Zero Requirement for this Region." });

            await UpsertRegionFinalizationAsync(stateId, regionId, PublicityRegionFinalizationStatus.ZeroConfirmed);
            return Ok(new { Success = true });
        }

        // RM explicitly submits Zero Requirement for their assigned Region.
        // SMD must NOT call this endpoint (rejected).
        [HttpPost("regions/{stateId:int}/{regionId:int}/confirm-zero")]
        [HttpPost("regions/{regionId:int}/confirm-zero")]
        public async Task<IActionResult> ConfirmZeroRegion(int regionId, int? stateId = null, [FromBody] PubRegionActionDto? dto = null)
        {
            var role = MyRole();
            if (role == AppRole.SMD)
            {
                return BadRequest(new { Success = false, Message = "SMD cannot confirm zero on behalf of a Region. Only the assigned RM can submit zero requirements." });
            }
            if (role != AppRole.RM && !IsSuper()) return Forbid();

            var myStateId = ClaimInt("spic:state_id");
            var myRegionId = ClaimInt("spic:region_id");
            var targetStateId = stateId ?? myStateId;

            if (!IsSuper())
            {
                if (myStateId <= 0 || myStateId != targetStateId || myRegionId <= 0 || myRegionId != regionId)
                    return Forbid();
            }

            var groups = await BuildRegionGroupsAsync(PublicityStage.RmReview);
            var region = groups.FirstOrDefault(g => g.RegionId == regionId);
            if (region is not null && (region.EligibleCount > 0 || region.StillAtRmCount > 0))
                return BadRequest(new { Success = false, Message = "This Region has active publicity requests - approve them instead of submitting zero requirement." });

            await UpsertRegionFinalizationAsync(targetStateId, regionId, PublicityRegionFinalizationStatus.RmZeroSubmitted);
            return Ok(new { Success = true });
        }

        private async Task UpsertRegionFinalizationAsync(int stateId, int regionId, PublicityRegionFinalizationStatus status)
        {
            var name = await MyNameAsync();
            var uid = Uid();
            var existing = await _db.PublicityRegionFinalizations.FirstOrDefaultAsync(f => f.StateId == stateId && f.RegionId == regionId);
            if (existing is null)
            {
                var created = new PublicityRegionFinalization
                {
                    StateId = stateId, RegionId = regionId, Status = status,
                    FinalizedByUserId = uid, FinalizedByName = name, FinalizedAt = DateTime.Now
                };
                _db.PublicityRegionFinalizations.Add(created);
                try
                {
                    await _db.SaveChangesAsync();
                    return;
                }
                catch (DbUpdateException)
                {
                    // Another concurrent request already finalized this exact Region first (the
                    // unique index on (StateId, RegionId) is the real guarantee here, this check
                    // above is only a best-effort reduction of how often the race is hit) - detach
                    // the failed insert and fall through to update the row that won instead.
                    _db.Entry(created).State = EntityState.Detached;
                    existing = await _db.PublicityRegionFinalizations.FirstOrDefaultAsync(f => f.StateId == stateId && f.RegionId == regionId);
                }
            }
            if (existing is not null)
            {
                existing.Status = status;
                existing.FinalizedByUserId = uid;
                existing.FinalizedByName = name;
                existing.FinalizedAt = DateTime.Now;
                // A fresh explicit decision always means a new round for this Region - clear any
                // earlier submission marker so BuildRegionGroupsAsync treats it as current again
                // (not still "already submitted" from a prior, now-superseded round).
                existing.SubmittedAt = null;
                await _db.SaveChangesAsync();
            }
        }

        // Builds one PubRegionDto per Region that must be accounted for. For the SMD stage
        // specifically, this is every Region still active in the state's master data, UNION any
        // Region (active or not) that currently has a request somewhere at-or-before the SMD stage -
        // so a request against a since-deactivated Region is never silently dropped, while a Region
        // whose only history is already fully resolved (long past the SMD stage) doesn't force it to
        // appear forever (there's no "cycle/batch" concept anywhere in this module - see
        // PublicityRegionFinalization's own doc comment - so "has real, still-relevant data" is
        // judged live, every call). Every OTHER stage keeps the original behaviour (one entry per
        // Region that actually has a pending request) - only the SMD stage groups multiple Regions
        // under one state-wide approver and needs the "zero Regions accounted for" treatment; RM's
        // own ScopedAsync already narrows to a single Region, and SMM only ever receives work that
        // already passed through this exact Region-finalization gate.
        private async Task<List<PubRegionDto>> BuildRegionGroupsAsync(PublicityStage mine)
        {
            var scoped = (await ScopedAsync()).Where(r => r.Status != PublicityStatus.Draft &&
                r.Status != PublicityStatus.Rejected && r.Status != PublicityStatus.Returned);
            var pending = await scoped.Where(r => r.Stage == mine &&
                (r.Status == PublicityStatus.PendingApproval || r.Status == PublicityStatus.PendingFinalApproval)).ToListAsync();
            var below = await scoped.Where(r => r.Stage < mine && r.Stage >= PublicityStage.RmReview)
                .Select(r => new { r.StateId, r.RegionId }).ToListAsync();

            var rows = await RowsAsync(pending);
            var rowByRequestId = pending.Select((r, i) => (r.Id, Row: rows[i])).ToDictionary(x => x.Id, x => x.Row);

            var regionKeys = pending.Select(r => (r.StateId, r.RegionId))
                .Concat(below.Select(b => (b.StateId, b.RegionId)))
                .Distinct().ToHashSet();

            if (mine == PublicityStage.SmdReview)
            {
                var myStateId = ClaimInt("spic:state_id");
                if (myStateId is > 0)
                {
                    var rmRegionIds = await _db.Employeelogins.AsNoTracking()
                        .Where(el => el.StateId == myStateId && el.Role == AppRole.RM && el.IsActive && el.RegionId > 0)
                        .Join(_db.Regions.AsNoTracking().Where(r => r.IsActive), el => el.RegionId, r => r.Id, (el, r) => r.Id)
                        .Distinct()
                        .ToListAsync();
                    foreach (var rid in rmRegionIds) regionKeys.Add((StateId: myStateId, RegionId: rid));
                }
            }
            else if (mine == PublicityStage.RmReview)
            {
                var myStateId = ClaimInt("spic:state_id");
                var myRegionId = ClaimInt("spic:region_id");
                if (myStateId > 0 && myRegionId > 0)
                {
                    regionKeys.Add((StateId: myStateId, RegionId: myRegionId));
                }
            }

            var names = await NamesAsync(regionKeys.Select(k => k.StateId), regionKeys.Select(k => k.RegionId), Array.Empty<int>());
            var relevantStateIds = regionKeys.Select(k => k.StateId).Distinct().ToList();
            var relevantRegionIds = regionKeys.Select(k => k.RegionId).Distinct().ToList();
            var hqCountsByRegion = await _db.Headquarters.AsNoTracking()
                .Where(h => relevantRegionIds.Contains(h.RegionId))
                .GroupBy(h => h.RegionId)
                .Select(g => new { RegionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RegionId, x => x.Count);
            // PublicityRegionFinalization is exclusively an RM/SMD-stage concept (only
            // ConfirmZeroRegion/ApproveRegion/FinalizeZeroRegion ever write one, gated to those two
            // stages) - looking it up for any other stage (AdminMdCsReview, DgmMdCsReview, SmmReview)
            // would leak an unrelated SMD reviewer's name/timestamp onto that stage's own region row.
            var finalizationsApply = mine is PublicityStage.SmdReview or PublicityStage.RmReview;
            var finalizations = finalizationsApply
                ? await _db.PublicityRegionFinalizations.AsNoTracking().Where(f => relevantStateIds.Contains(f.StateId)).ToListAsync()
                : new List<PublicityRegionFinalization>();

            var groups = new List<PubRegionDto>();
            foreach (var key in regionKeys)
            {
                var regionPending = pending.Where(r => r.StateId == key.StateId && r.RegionId == key.RegionId).ToList();
                var stillAtRm = below.Count(b => b.StateId == key.StateId && b.RegionId == key.RegionId);
                var reviewedCount = regionPending.Count(r => r.SmdReviewedAt != null);
                var finalization = finalizationsApply ? finalizations.FirstOrDefault(f => f.StateId == key.StateId && f.RegionId == key.RegionId) : null;

                // A finalization whose SubmittedAt is already set belongs to an already-submitted
                // round (see ReviewBulk and PublicityRegionFinalization's own doc comment) - it can
                // never be "effective" for a NEW round's gating, regardless of staleness, but it also
                // must never be silently dropped: with nothing currently pending for this Region
                // (confirmed by regionPending.Count == 0 && stillAtRm == 0 - the only way an Approved
                // row's requests could have left SmdReview), this IS the correct, final state to show.
                var isConsumed = finalization?.SubmittedAt is not null;
                var isAlreadySubmitted = isConsumed && regionPending.Count == 0 && stillAtRm == 0;

                // Staleness: a Region finalized earlier is only still valid if nothing new/unreviewed
                // has appeared for it since - see PublicityRegionFinalization's own doc comment.
                var isStale = finalization is not null && !isConsumed && (
                    stillAtRm > 0 ||
                    (finalization.Status == PublicityRegionFinalizationStatus.Approved && regionPending.Any(r => r.SmdReviewedAt is null)) ||
                    (finalization.Status is PublicityRegionFinalizationStatus.ZeroConfirmed or PublicityRegionFinalizationStatus.RmZeroSubmitted && regionPending.Count > 0));
                var effective = (finalization is not null && !isConsumed && !isStale) ? finalization : null;

                PubRegionStatus status;
                if (isAlreadySubmitted) status = PubRegionStatus.SubmittedToSmm;
                else if (effective?.Status == PublicityRegionFinalizationStatus.Approved) status = PubRegionStatus.Approved;
                else if (effective?.Status == PublicityRegionFinalizationStatus.ZeroConfirmed) status = PubRegionStatus.ZeroConfirmed;
                else if (effective?.Status == PublicityRegionFinalizationStatus.RmZeroSubmitted) status = PubRegionStatus.ZeroSubmittedByRm;
                else if (stillAtRm > 0 || regionPending.Count == 0) status = PubRegionStatus.PendingReview;
                else if (reviewedCount == 0) status = PubRegionStatus.Submitted;
                else if (reviewedCount < regionPending.Count) status = PubRegionStatus.InReview;
                else status = PubRegionStatus.Reviewed;

                groups.Add(new PubRegionDto
                {
                    StateId = key.StateId,
                    RegionId = key.RegionId,
                    State = names.States.GetValueOrDefault(key.StateId, ""),
                    Region = names.Regions.GetValueOrDefault(key.RegionId, ""),
                    Hqs = hqCountsByRegion.TryGetValue(key.RegionId, out var hqc) && hqc > 0
                        ? hqc
                        : regionPending.Select(r => r.HeadquarterId).Distinct().Count(),
                    Quantity = regionPending.Sum(r => r.Quantity),
                    EligibleCount = regionPending.Count,
                    ReviewedCount = reviewedCount,
                    StillAtRmCount = stillAtRm,
                    Status = status,
                    // Historical/informational - who reviewed or declared this Region and when, shown
                    // regardless of whether that decision is still "effective" for the current round.
                    FinalizedByName = finalization?.FinalizedByName,
                    FinalizedAt = finalization?.FinalizedAt,
                    LastUpdated = regionPending.Count > 0
                        ? regionPending.Max(r => r.UpdatedAt ?? r.SubmittedAt ?? r.CreatedAt)
                        : (finalization?.SubmittedAt ?? finalization?.FinalizedAt),
                    Rows = regionPending.Select(r => rowByRequestId[r.Id]).ToList()
                });
            }

            return groups.OrderBy(g => g.State).ThenBy(g => g.Region).ToList();
        }

        // ================================================================ details

        [HttpGet("requests/{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var r = await LoadAsync(id);
            if (r is null || !await CanReadAsync(r)) return NotFound(new { Success = false, Message = "Request not found." });
            return Ok(await DetailAsync(r));
        }

        // ================================================================ create / edit / submit

        [HttpPost("requests")]
        public async Task<IActionResult> Create([FromBody] PubUpsertDto dto)
        {
            // Anyone in the workflow may raise a request (not only the RMDO); Submit() below starts
            // the chain at the right stage for whoever actually created it. Admin/SuperAdmin keep
            // the usual testing bypass.
            var isChainRole = MyRole() is AppRole.RMD or AppRole.MDO or AppRole.MO or AppRole.JMDO or AppRole.RM or AppRole.SMD or AppRole.SMM;
            if (!isChainRole && !await IsAdminMdCsAsync() && !await IsDgmMdCsAsync() && !IsSuper()) return Forbid();
            var error = await ValidateAsync(dto);
            if (error != null) return BadRequest(new { Success = false, Message = error });
            var name = await MyNameAsync();
            var r = new PublicityRequest
            {
                IndentNo = await NextNoAsync("IND"),
                CreatedByUserId = Uid(),
                CreatedByName = name,
                Stage = PublicityStage.IndentCreation,
                Status = PublicityStatus.Draft
            };
            await ApplyAsync(r, dto);
            r.OriginalQuantity = r.Quantity;
            _db.PublicityRequests.Add(r);
            await _db.SaveChangesAsync();
            await AddEvent(r, PublicityStage.IndentCreation, "Created", name, dto.Remarks);
            if (dto.Submit) await Submit(r, name, dto.Remarks);
            await _db.SaveChangesAsync();
            return Ok(new { r.Id, r.IndentNo });
        }

        [HttpPut("requests/{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] PubUpsertDto dto)
        {
            var r = await _db.PublicityRequests.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (r is null) return NotFound(new { Success = false, Message = "Request not found." });
            // the approver whose turn it is may edit every field (RM / SMD / SMM / ADMIN-MD CS "Edit Request");
            // the request stays on its stage and the change list goes into the approval history.
            // DGM-MD CS (final approval) is approve-only - never an edit stage, for anyone including Admin.
            var approverEdit = !CanEdit(r) && r.Status is PublicityStatus.PendingApproval or PublicityStatus.PendingFinalApproval
                && r.Stage != PublicityStage.DgmMdCsReview && await IsMyTurnAsync(r.Stage) && await CanReadAsync(r);
            if (!CanEdit(r) && !approverEdit) return BadRequest(new { Success = false, Message = "Only your draft or returned request can be edited." });
            var error = await ValidateAsync(dto, id);
            if (error != null) return BadRequest(new { Success = false, Message = error });
            var before = new { r.MaterialTypeId, r.ProductId, r.Quantity, r.HeadquarterId, r.RequestedByUserId, r.Remarks };
            await ApplyAsync(r, dto);
            r.UpdatedAt = DateTime.UtcNow;
            if (approverEdit)
            {
                // r.Remarks is the ONE original requester remarks field, shared read-only by every
                // role's edit screen - never let an approver's edit (RM/SMD/SMM/ADMIN-MD CS) overwrite
                // it, or earlier reviewers' and the original creator's remarks are destroyed the moment
                // a later stage edits anything else. Whatever this approver actually typed is instead
                // saved as the Remarks of THEIR OWN "Edited" PublicityEvent below (attributed to their
                // own Stage/ActorRole/ActorName) - exactly what PubDetailDto.ApproverRemarks already
                // surfaces as the latest reviewer's own remarks, kept distinct from the original.
                var reviewerRemarks = Clean(dto.Remarks);
                // The edit screen's Remarks box is pre-filled with the original remarks for every
                // role (so it doubles as a read-only display when nobody touches it) - comparing
                // against before.Remarks is what tells "the reviewer actually typed something new"
                // apart from "the box still shows what it always showed", so re-saving an untouched
                // box never fabricates an edit remark.
                var remarksChanged = reviewerRemarks != before.Remarks;
                r.Remarks = before.Remarks;
                if (r.Stage == PublicityStage.SmdReview && await IsMyTurnAsync(r.Stage))
                {
                    r.SmdReviewedAt = DateTime.Now;
                    r.SmdReviewedByName = await MyNameAsync();
                }
                var changes = new List<string>();
                if (before.Quantity != r.Quantity) changes.Add($"Quantity {before.Quantity} -> {r.Quantity}");
                if (before.MaterialTypeId != r.MaterialTypeId) changes.Add("Material changed");
                if (before.ProductId != r.ProductId) changes.Add("Product changed");
                if (before.HeadquarterId != r.HeadquarterId) changes.Add("Location changed");
                if (before.RequestedByUserId != r.RequestedByUserId) changes.Add("Requested by changed");
                var changeNote = changes.Count > 0 ? string.Join("; ", changes) + "." : null;
                var eventRemarks = !remarksChanged || string.IsNullOrWhiteSpace(reviewerRemarks) ? changeNote
                    : changeNote is null ? reviewerRemarks : $"{changeNote} Remarks: {reviewerRemarks}";
                if (!string.IsNullOrWhiteSpace(eventRemarks)) await AddEvent(r, r.Stage, "Edited", await MyNameAsync(), eventRemarks);
            }
            else
            {
                r.OriginalQuantity = r.Quantity;
                if (dto.Submit) await Submit(r, await MyNameAsync(), dto.Remarks);
            }
            await _db.SaveChangesAsync();
            return Ok(new { r.Id, r.IndentNo });
        }

        // ================================================================ specific product (bulk)

        /// <summary>Parses a "Product, Quantity" Excel upload into unsaved rows for the "Specific
        /// Product" Request Items table - nothing is persisted here, the client reviews/edits the
        /// parsed rows and only POSTs requests/bulk once the user actually submits/saves.</summary>
        [HttpPost("requests/import-excel")]
        [RequestSizeLimit(10L * 1024 * 1024)]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            var isChainRole = MyRole() is AppRole.RMD or AppRole.MDO or AppRole.MO or AppRole.JMDO or AppRole.RM or AppRole.SMD or AppRole.SMM;
            if (!isChainRole && !await IsAdminMdCsAsync() && !await IsDgmMdCsAsync() && !IsSuper()) return Forbid();
            if (file is null || file.Length == 0) return BadRequest(new { Success = false, Message = "No file uploaded." });
            if (file.Length > 10 * 1024 * 1024) return BadRequest(new { Success = false, Message = "File must be under 10 MB." });
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls") return BadRequest(new { Success = false, Message = "Only Excel files (.xlsx/.xls) are supported." });

            var products = await _db.Products.AsNoTracking().Where(p => p.IsActive).Select(p => new { p.Id, p.Name }).ToListAsync();
            var byName = products.GroupBy(p => p.Name.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.Ordinal);

            List<PubBulkImportRowDto> result;
            try
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var ws = workbook.Worksheets.First();
                var headerRow = ws.Row(1);
                var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
                if (lastCol == 0) return BadRequest(new { Success = false, Message = "Empty worksheet or missing header row." });

                var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (var c = 1; c <= lastCol; c++)
                {
                    var h = headerRow.Cell(c).GetString().Trim().Replace(" ", "").ToLowerInvariant();
                    if (!string.IsNullOrEmpty(h) && !headerMap.ContainsKey(h)) headerMap[h] = c;
                }
                string GetCell(IXLRow row, string key) => headerMap.TryGetValue(key, out var col) ? row.Cell(col).GetString().Trim() : "";
                if (!headerMap.ContainsKey("product")) return BadRequest(new { Success = false, Message = "Missing required column: Product." });
                if (!headerMap.ContainsKey("quantity")) return BadRequest(new { Success = false, Message = "Missing required column: Quantity." });

                result = new List<PubBulkImportRowDto>();
                foreach (var row in ws.RowsUsed().Skip(1).Take(500))
                {
                    var productName = GetCell(row, "product");
                    var qtyText = GetCell(row, "quantity");
                    if (string.IsNullOrWhiteSpace(productName) && string.IsNullOrWhiteSpace(qtyText)) continue;
                    var dto = new PubBulkImportRowDto { RowNumber = row.RowNumber(), ProductName = productName };
                    var errors = new List<string>();
                    if (string.IsNullOrWhiteSpace(productName)) errors.Add("Product is required.");
                    else if (byName.TryGetValue(productName.Trim().ToLowerInvariant(), out var pid)) dto.ProductId = pid;
                    else errors.Add($"Product \"{productName}\" not found.");
                    if (!int.TryParse(qtyText, out var qty) || qty < 0) errors.Add("Enter a valid quantity.");
                    else dto.Quantity = qty;
                    if (errors.Count > 0) dto.Error = string.Join(" ", errors);
                    result.Add(dto);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = $"Could not read the Excel file: {ex.Message}" });
            }
            if (result.Count == 0) return BadRequest(new { Success = false, Message = "No data rows found in the uploaded file." });
            return Ok(result);
        }

        /// <summary>"Specific Product" submit: one PublicityRequest PER ITEM (same Validate/Apply path
        /// Create() uses), but only the FIRST item is the group's "root" (GroupId stays null); every
        /// later item gets GroupId = root.Id, so the whole Excel import is ONE group - PublicityReview's
        /// list/stats/consolidation only ever surface GroupId == null rows (see ScopedAsync-derived
        /// queries), collapsing the group to one visible "request". Each item still keeps its own real,
        /// unique IndentNo (the DB's unique index on IndentNo is untouched) and its own full dispatch/
        /// acknowledgement/exception lifecycle once the group reaches Dispatch stage - grouping only
        /// ever affects the REVIEW chain (IndentCreation..DgmMdCsReview, see ReviewOneAsync's sibling
        /// sync) and the list-screen display, never dispatch/transfer/exception, which keep operating
        /// per-row exactly as every other request already does. Three passes - validate all, create all
        /// (one transaction, so a bad row never leaves others orphaned), then Created/Submit ONCE on the
        /// root with children mirroring its resulting Stage/Status - so one Excel upload produces
        /// exactly one history entry/notification, never one per row.</summary>
        [HttpPost("requests/bulk")]
        public async Task<IActionResult> CreateBulk([FromBody] PubBulkUpsertDto dto)
        {
            var isChainRole = MyRole() is AppRole.RMD or AppRole.MDO or AppRole.MO or AppRole.JMDO or AppRole.RM or AppRole.SMD or AppRole.SMM;
            if (!isChainRole && !await IsAdminMdCsAsync() && !await IsDgmMdCsAsync() && !IsSuper()) return Forbid();
            if (dto.Items is null || dto.Items.Count == 0) return BadRequest(new { Success = false, Message = "Add at least one item." });
            if (dto.Items.Count > 200) return BadRequest(new { Success = false, Message = "Too many items in a single request (max 200)." });

            // This endpoint is the "Specific Product" Excel-import flow only (PublicityForm's
            // ShowBulk) - unlike the single-item Create/Update path, the server here definitely knows
            // the intended category (there is no General/Gift Articles equivalent of this endpoint),
            // so it can and must reject a General-category material slipping through, not just check
            // existence/active like ValidateAsync already does for every row below.
            var materialCategory = await _db.PublicityMaterialTypes.AsNoTracking()
                .Where(m => m.Id == dto.MaterialTypeId).Select(m => (PublicityMaterialCategory?)m.Category).FirstOrDefaultAsync();
            if (materialCategory != PublicityMaterialCategory.Specific)
                return BadRequest(new { Success = false, Message = "Select a Specific Product material type for this import." });

            PubUpsertDto RowDto(PubBulkItemDto item) => new()
            {
                MaterialTypeId = dto.MaterialTypeId, ProductId = item.ProductId, Quantity = item.Quantity,
                RequestedByUserId = dto.RequestedByUserId, StateId = dto.StateId, RegionId = dto.RegionId,
                HeadquarterId = dto.HeadquarterId, Remarks = dto.Remarks, Submit = dto.Submit
            };

            for (var i = 0; i < dto.Items.Count; i++)
            {
                var error = await ValidateAsync(RowDto(dto.Items[i]));
                if (error != null) return BadRequest(new { Success = false, Message = $"Row {i + 1}: {error}" });
            }

            var name = await MyNameAsync();
            var requestedUserId = string.IsNullOrWhiteSpace(dto.RequestedByUserId) ? Uid() : dto.RequestedByUserId;
            var requestedName = await _db.Users.AsNoTracking().Where(u => u.Id == requestedUserId)
                .Select(u => u.Name ?? u.UserName).FirstOrDefaultAsync() ?? name;

            var r = new PublicityRequest
            {
                IndentNo = await NextNoAsync("IND"),
                CreatedByUserId = Uid(),
                CreatedByName = name,
                Stage = PublicityStage.IndentCreation,
                Status = PublicityStatus.Draft,
                MaterialTypeId = dto.MaterialTypeId,
                StateId = dto.StateId,
                RegionId = dto.RegionId,
                HeadquarterId = dto.HeadquarterId,
                Remarks = Clean(dto.Remarks),
                IsGiftArticle = false,
                ProductId = dto.Items[0].ProductId,
                Quantity = dto.Items.Sum(x => x.Quantity),
                OriginalQuantity = dto.Items.Sum(x => x.Quantity),
                RequestedByUserId = requestedUserId,
                RequestedByName = requestedName
            };

            foreach (var item in dto.Items)
            {
                r.Items.Add(new PublicityRequestItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    OriginalQuantity = item.Quantity
                });
            }

            _db.PublicityRequests.Add(r);
            await _db.SaveChangesAsync();
            await AddEvent(r, PublicityStage.IndentCreation, "Created", name, dto.Remarks);
            if (dto.Submit) await Submit(r, name, dto.Remarks);
            await _db.SaveChangesAsync();

            var result = new PubBulkCreatedDto
            {
                Id = r.Id,
                IndentNo = r.IndentNo,
                TotalQuantity = r.Quantity,
                Items = r.Items.Select(i => new PubBulkCreatedItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            };
            return Ok(result);
        }

        /// <summary>Edit a Specific Product (multi-item) request's own item rows - the ONE write path
        /// missing for PublicityForm's edit page (Update/ApplyAsync above only ever touch the parent's
        /// own ProductId/Quantity mirror, never r.Items). Same authorization shape as Update: the
        /// creator's own Draft/Returned request, or the reviewer whose turn it currently is (never DGM-
        /// MD CS, approve-only). Items missing from the submitted list are deleted; Id == null rows are
        /// new items; everything else is matched by Id and updated in place - so saved item ids are
        /// preserved across edits (needed to attach a freshly re-uploaded design file to the right item,
        /// see Upload's requestItemId param) instead of recreating every row from scratch. Never touches
        /// Stage/Status/RequestedByUserId/Remarks - those stay on the existing single-item Update
        /// endpoint, which PublicityForm's edit page calls separately for the parent-level fields.</summary>
        [HttpPut("requests/{id:int}/items")]
        public async Task<IActionResult> UpdateItems(int id, [FromBody] PubItemsUpdateDto dto)
        {
            var r = await _db.PublicityRequests.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (r is null) return NotFound(new { Success = false, Message = "Request not found." });
            if (r.Items.Count == 0) return BadRequest(new { Success = false, Message = "This request does not have multiple items." });

            var approverEdit = !CanEdit(r) && r.Status is PublicityStatus.PendingApproval or PublicityStatus.PendingFinalApproval
                && r.Stage != PublicityStage.DgmMdCsReview && await IsMyTurnAsync(r.Stage) && await CanReadAsync(r);
            if (!CanEdit(r) && !approverEdit) return BadRequest(new { Success = false, Message = "Only your draft or returned request can be edited." });

            if (dto.Items is null || dto.Items.Count == 0) return BadRequest(new { Success = false, Message = "Keep at least one item." });
            if (dto.Items.Count > 200) return BadRequest(new { Success = false, Message = "Too many items in a single request (max 200)." });
            foreach (var item in dto.Items)
            {
                if (item.Quantity < 0) return BadRequest(new { Success = false, Message = "Enter a valid quantity for every item." });
                if (!await _db.Products.AnyAsync(p => p.Id == item.ProductId)) return BadRequest(new { Success = false, Message = "Select a valid product for every item." });
            }

            var existingById = r.Items.ToDictionary(i => i.Id);
            var keepIds = dto.Items.Where(i => i.Id is > 0).Select(i => i.Id!.Value).ToHashSet();
            var changes = new List<string>();
            foreach (var removed in r.Items.Where(i => !keepIds.Contains(i.Id)).ToList())
            {
                changes.Add($"Removed a line item (product #{removed.ProductId}).");
                _db.PublicityRequestItems.Remove(removed);
            }
            foreach (var incoming in dto.Items)
            {
                if (incoming.Id is int existingId && existingById.TryGetValue(existingId, out var item))
                {
                    if (item.ProductId != incoming.ProductId || item.Quantity != incoming.Quantity)
                        changes.Add($"Item #{item.Id}: product #{item.ProductId} -> #{incoming.ProductId}, quantity {item.Quantity} -> {incoming.Quantity}.");
                    item.ProductId = incoming.ProductId;
                    item.Quantity = incoming.Quantity;
                    if (!approverEdit) item.OriginalQuantity = incoming.Quantity;
                }
                else
                {
                    r.Items.Add(new PublicityRequestItem { ProductId = incoming.ProductId, Quantity = incoming.Quantity, OriginalQuantity = incoming.Quantity });
                    changes.Add($"Added a line item (product #{incoming.ProductId}).");
                }
            }

            // Keep the parent row's own ProductId/Quantity mirror (CreateBulk sets it to the first
            // item's product + the items' summed quantity, read by Stats/dispatch/list screens that
            // don't know about Items) in sync with whatever the items list now actually is.
            r.ProductId = dto.Items[0].ProductId;
            r.Quantity = dto.Items.Sum(i => i.Quantity);
            if (!approverEdit) r.OriginalQuantity = r.Quantity;
            r.UpdatedAt = DateTime.UtcNow;

            if (approverEdit && changes.Count > 0)
                await AddEvent(r, r.Stage, "Edited", await MyNameAsync(), $"Request items updated: {string.Join(" ", changes)}");

            await _db.SaveChangesAsync();

            var result = new PubBulkCreatedDto
            {
                Id = r.Id,
                IndentNo = r.IndentNo,
                TotalQuantity = r.Quantity,
                Items = r.Items.Select(i => new PubBulkCreatedItemDto { Id = i.Id, ProductId = i.ProductId, Quantity = i.Quantity }).ToList()
            };
            return Ok(result);
        }

        [HttpDelete("requests/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _db.PublicityRequests.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (r is null) return NotFound(new { Success = false, Message = "Request not found." });
            if (r.Status != PublicityStatus.Draft || r.CreatedByUserId != Uid())
                return BadRequest(new { Success = false, Message = "Only your own draft can be deleted." });
            r.IsDeleted = true;
            await _db.SaveChangesAsync();
            return Ok(new { Success = true });
        }

        // ================================================================ approvals

        [HttpPost("requests/{id:int}/review")]
        public async Task<IActionResult> Review(int id, [FromBody] PubReviewDto dto)
        {
            var r = await _db.PublicityRequests.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (r is null || !await CanReadAsync(r)) return NotFound(new { Success = false, Message = "Request not found." });
            var initialStage = r.Stage;
            var error = await ReviewOneAsync(r, dto.Action, dto.Quantity, dto.Remarks);
            if (error != null) return BadRequest(new { Success = false, Message = error });
            await _db.SaveChangesAsync();

            // Auto-mark matching unread approval notification as read for the current reviewer
            var me = Uid();
            var expectedRole = initialStage switch
            {
                PublicityStage.RmReview => AppRole.RM,
                PublicityStage.SmdReview => AppRole.SMD,
                PublicityStage.SmmReview => AppRole.SMM,
                PublicityStage.AdminMdCsReview => AppRole.MDAdmin,
                PublicityStage.DgmMdCsReview => AppRole.MDHOD,
                _ => (AppRole?)null
            };

            var notifQuery = (await MyNotificationsAsync())
                .Where(x => x.RequestId == id && x.Type == PublicityNotificationType.Approval && x.ReadAt == null);

            if (expectedRole.HasValue)
            {
                notifQuery = notifQuery.Where(x => x.TargetUserId == me || x.TargetRole == expectedRole.Value);
            }

            var notifIds = await notifQuery.Select(x => x.Id).ToListAsync();
            if (notifIds.Count > 0)
            {
                var readNow = DateTime.UtcNow;
                await _db.PublicityNotifications
                    .Where(x => notifIds.Contains(x.Id) && x.ReadAt == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReadAt, readNow));
            }

            var unreadCount = await (await MyNotificationsAsync()).CountAsync(n => n.ReadAt == null);
            return Ok(new { Success = true, r.Stage, r.Status, UnreadNotificationsCount = unreadCount });
        }

        // A toggle, not a one-way action (see PubMarkReviewedDto's own doc comment) - distinct from
        // Review()'s edit/return/reject/approve: this never changes Stage/Status/Quantity, it only
        // records "I looked at this" so the Region-level approval gate (ApproveRegion) has something
        // to check beyond "did the quantity get edited". Only meaningful at the request's OWN stage
        // (not every approver stage needs this concept - only SMD's Region-level grouping does
        // today), so this is intentionally restricted to SmdReview requests only.
        [HttpPost("requests/{id:int}/mark-reviewed")]
        public async Task<IActionResult> MarkRequestReviewed(int id, [FromBody] PubMarkReviewedDto dto)
        {
            var r = await _db.PublicityRequests.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (r is null || !await CanReadAsync(r)) return NotFound(new { Success = false, Message = "Request not found." });
            if (r.Stage != PublicityStage.SmdReview || r.Status is not (PublicityStatus.PendingApproval or PublicityStatus.PendingFinalApproval))
                return BadRequest(new { Success = false, Message = "This request is not waiting for SMD review." });
            if (!await IsMyTurnAsync(r.Stage)) return BadRequest(new { Success = false, Message = "This request is not waiting for your review." });

            if (dto.Reviewed)
            {
                r.SmdReviewedAt = DateTime.Now;
                r.SmdReviewedByName = await MyNameAsync();
            }
            else
            {
                r.SmdReviewedAt = null;
                r.SmdReviewedByName = null;
            }
            await _db.SaveChangesAsync();
            return Ok(new { Success = true, r.SmdReviewedAt, r.SmdReviewedByName });
        }

        [HttpPost("requests/review-bulk")]
        public async Task<IActionResult> ReviewBulk([FromBody] PubBulkReviewDto dto)
        {
            // Real enforcement of "every region must be finalized before the SMD can forward to
            // SMM" - the Razor page's disabled button is UX only; a direct call to this endpoint
            // must be blocked here too, or the consolidation requirement has no actual teeth.
            // ReviewBulk is always a bulk-approve-forward (see ReviewOneAsync(r, "Approve", ...)
            // below - there is no other action this endpoint performs), so this check applies
            // unconditionally whenever the caller is acting at the SMD stage.
            var mine = await MyStageAsync();
            List<int>? serverDerivedIds = null;
            List<(int StateId, int RegionId)>? finalizedKeys = null;
            if (mine == PublicityStage.SmdReview)
            {
                var groups = await BuildRegionGroupsAsync(mine.Value);
                var unfinalized = groups.Where(g => g.Status is not (PubRegionStatus.Approved or PubRegionStatus.ZeroConfirmed)).ToList();
                if (unfinalized.Count > 0)
                {
                    var details = string.Join(", ", unfinalized.Select(u => $"{u.Region} ({RegionStatusDescription(u)})"));
                    return BadRequest(new
                    {
                        Success = false,
                        Message = $"{unfinalized.Count} of {groups.Count} region(s) are not yet finalized ({details}). Every Region must be Approved or have its RM Zero Requirement finalized before forwarding to SMM."
                    });
                }
                // Never trust the client for WHICH requests to advance at this stage - derive them
                // from the Regions this same call just confirmed are Approved (ZeroConfirmed regions
                // have nothing to advance).
                serverDerivedIds = groups.Where(g => g.Status == PubRegionStatus.Approved)
                    .SelectMany(g => g.Rows).Select(r => r.Id).ToList();
                finalizedKeys = groups.Select(g => (g.StateId, g.RegionId)).ToList();
            }

            var scoped = await ScopedAsync();
            var ids = (serverDerivedIds ?? dto.Ids).Distinct().ToList();
            var items = await _db.PublicityRequests.Where(r => ids.Contains(r.Id) && !r.IsDeleted).ToListAsync();
            var allowed = (await scoped.Where(r => ids.Contains(r.Id)).Select(r => r.Id).ToListAsync()).ToHashSet();
            var done = 0;
            var approvedIds = new List<int>();
            foreach (var r in items.Where(r => allowed.Contains(r.Id)))
            {
                if (await ReviewOneAsync(r, "Approve", null, dto.Remarks) is null)
                {
                    done++;
                    approvedIds.Add(r.Id);
                }
            }

            // Close out every Region this submission covered (Approved and ZeroConfirmed alike - a
            // ZeroConfirmed Region never has a Row to advance above, but its decision is just as much
            // "submitted" now) so a later page load can tell "already forwarded to SMM" apart from
            // "nothing has happened yet" - see PublicityRegionFinalization.SubmittedAt's own doc
            // comment and BuildRegionGroupsAsync's isAlreadySubmitted check.
            if (finalizedKeys is { Count: > 0 })
            {
                var stateIds = finalizedKeys.Select(k => k.StateId).Distinct().ToList();
                var rows = await _db.PublicityRegionFinalizations
                    .Where(f => stateIds.Contains(f.StateId) && f.SubmittedAt == null
                        && (f.Status == PublicityRegionFinalizationStatus.Approved || f.Status == PublicityRegionFinalizationStatus.ZeroConfirmed))
                    .ToListAsync();
                var now = DateTime.Now;
                foreach (var row in rows.Where(f => finalizedKeys.Contains((f.StateId, f.RegionId))))
                    row.SubmittedAt = now;
            }

            await _db.SaveChangesAsync();

            // After consolidation approval successfully commits:
            // Auto-mark the current approver's unread approval notifications for the approved requests as read
            if (approvedIds.Count > 0)
            {
                var me = Uid();
                var expectedRole = mine switch
                {
                    PublicityStage.RmReview => AppRole.RM,
                    PublicityStage.SmdReview => AppRole.SMD,
                    PublicityStage.SmmReview => AppRole.SMM,
                    PublicityStage.AdminMdCsReview => AppRole.MDAdmin,
                    PublicityStage.DgmMdCsReview => AppRole.MDHOD,
                    _ => (AppRole?)null
                };

                var notifQuery = (await MyNotificationsAsync())
                    .Where(x => approvedIds.Contains(x.RequestId) && x.Type == PublicityNotificationType.Approval && x.ReadAt == null);

                if (expectedRole.HasValue)
                {
                    notifQuery = notifQuery.Where(x => x.TargetUserId == me || x.TargetRole == expectedRole.Value);
                }

                var notifIds = await notifQuery.Select(x => x.Id).ToListAsync();

                if (notifIds.Count > 0)
                {
                    var readNow = DateTime.UtcNow;
                    await _db.PublicityNotifications
                        .Where(x => notifIds.Contains(x.Id) && x.ReadAt == null)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReadAt, readNow));
                }
            }

            var unreadCount = await (await MyNotificationsAsync()).CountAsync(n => n.ReadAt == null);
            return Ok(new PubBulkResultDto { Success = true, Approved = done, UnreadNotificationsCount = unreadCount });
        }

        private async Task<string?> ReviewOneAsync(PublicityRequest r, string action, int? qty, string? remarks)
        {
            if (r.Status is not (PublicityStatus.PendingApproval or PublicityStatus.PendingFinalApproval))
                return "This request is not waiting for approval.";
            if (!await IsMyTurnAsync(r.Stage)) return "This request is not waiting for your approval.";
            var name = await MyNameAsync();
            var normalized = action?.Trim().ToLowerInvariant();
            if (normalized == "edit")
            {
                if (r.Stage == PublicityStage.DgmMdCsReview) return "DGM - MD CS is approve-only; the quantity cannot be edited at this stage.";
                if (qty is not >= 0) return "Enter a valid quantity.";
                var note = $"Quantity changed from {r.Quantity} to {qty}." + (string.IsNullOrWhiteSpace(remarks) ? "" : " " + remarks.Trim());
                r.Quantity = qty.Value;
                r.SmdReviewedAt = null;
                r.SmdReviewedByName = null;
                await AddEvent(r, r.Stage, "Edited", name, note);
            }
            else if (normalized == "return")
            {
                if (string.IsNullOrWhiteSpace(remarks)) return "Enter the reason for returning the request.";
                await AddEvent(r, r.Stage, "Returned", name, remarks);
                r.Status = PublicityStatus.Returned;
                await NotifyAsync(PublicityNotificationType.Approval, r, null, r.CreatedByUserId, null, null,
                    $"Request {r.IndentNo} returned", $"{StageLabel(r.Stage)}: {remarks!.Trim()}", "Edit and resubmit the request.");
            }
            else if (normalized == "reject")
            {
                if (string.IsNullOrWhiteSpace(remarks)) return "Enter the reason for rejecting the request.";
                await AddEvent(r, r.Stage, "Rejected", name, remarks);
                r.Status = PublicityStatus.Rejected;
                await NotifyAsync(PublicityNotificationType.Approval, r, null, r.CreatedByUserId, null, null,
                    $"Request {r.IndentNo} rejected", $"{StageLabel(r.Stage)}: {remarks!.Trim()}", "View the reason and correct the request.");
            }
            else
            {
                
                if (qty is >= 0 && qty != r.Quantity && r.Stage != PublicityStage.DgmMdCsReview)
                {
                    await AddEvent(r, r.Stage, "Edited", name, $"Quantity changed from {r.Quantity} to {qty}.");
                    r.Quantity = qty.Value;
                }
                await AddEvent(r, r.Stage, "Approved", name, remarks);
                var index = Array.FindIndex(ChainStages, s => s == r.Stage);
                if (index + 1 < ChainStages.Length)
                {
                    r.Stage = ChainStages[index + 1];
                    r.Status = r.Stage >= PublicityStage.AdminMdCsReview ? PublicityStatus.PendingFinalApproval : PublicityStatus.PendingApproval;
                    await NotifyNextApproverAsync(r);
                }
                else
                {
                    r.Stage = PublicityStage.Dispatch;
                    r.Status = PublicityStatus.FinalApproved;
                    await NotifyAsync(PublicityNotificationType.Approval, r, null, r.CreatedByUserId, null, null,
                        $"Request {r.IndentNo} final approved", "Your publicity material request is approved and ready for dispatch.", null);
                    await NotifyReadyForDispatchAsync(r);
                }
            }
            r.UpdatedAt = DateTime.UtcNow;
            return null;
        }

        // ================================================================ dispatch (admin)

        [HttpPost("requests/{id:int}/dispatches")]
        public async Task<IActionResult> CreateDispatch(int id, [FromBody] PubDispatchCreateDto dto)
        {
            if (!await IsDispatcherAsync()) return Forbid();
            var r = await LoadAsync(id, tracking: true);
            if (r is null) return NotFound(new { Success = false, Message = "Request not found." });
            var name = await MyNameAsync();
            var d = new PublicityDispatch
            {
                RequestId = r.Id,
                Kind = dto.Kind,
                DestinationAddress = Clean(dto.DestinationAddress),
                Remarks = Clean(dto.Remarks),
                CreatedByUserId = Uid(),
                CreatedByName = name
            };

            if (dto.Kind == PublicityDispatchKind.Transfer)
            {
                if (dto.Quantity <= 0) return BadRequest(new { Success = false, Message = "Enter the dispatch quantity." });
                // ExceptionResolved = this request's own shortage/damage is already fully resolved but
                // it still has unresolved excess stock to give away (see RecomputeAsync) - a transfer OUT
                // of that excess must still be creatable, even though the request itself now reads as
                // effectively complete everywhere else.
                if (r.Status is not (PublicityStatus.ExceptionRaised or PublicityStatus.ExceptionResolved))
                    return BadRequest(new { Success = false, Message = "A stock transfer resolves a raised exception only." });
                var source = dto.SourceRequestId is > 0 ? await LoadAsync(dto.SourceRequestId.Value, tracking: true) : null;
                if (source is null || source.Id == r.Id) return BadRequest(new { Success = false, Message = "Select the source request that holds excess stock." });

                // Specific Product, multi-item request: a transfer must resolve exactly one of the
                // destination's own products - never a mix - so it validates against that product's
                // own outstanding requirement, and against the source's own excess for that same
                // product (never a different one), per ProductExcessAsync above.
                PublicityRequestItem? destItem = null;
                if (r.Items.Count > 1)
                {
                    if (dto.RequestItemId is not > 0) return BadRequest(new { Success = false, Message = "Select the product this transfer is for." });
                    destItem = r.Items.FirstOrDefault(i => i.Id == dto.RequestItemId);
                    if (destItem is null) return BadRequest(new { Success = false, Message = "Invalid product for this request." });

                    var ackIds = await _db.PublicityDispatches.AsNoTracking()
                        .Where(x => x.RequestId == r.Id && x.Acknowledgement != null && !x.Acknowledgement.IsDraft)
                        .Select(x => x.Id).ToListAsync();
                    var itemRows = await _db.PublicityDispatchItems.AsNoTracking()
                        .Where(x => ackIds.Contains(x.DispatchId) && x.RequestItemId == destItem.Id)
                        .ToListAsync();
                    var received = itemRows.Sum(x => x.ReceivedQty);
                    var damaged = itemRows.Sum(x => x.DamagedQty);
                    var deficit = Math.Max(0, destItem.Quantity - (received - damaged));

                    var activeReplacementIds = await _db.PublicityDispatches.AsNoTracking()
                        .Where(x => x.RequestId == r.Id && x.Kind != PublicityDispatchKind.Original && (x.Acknowledgement == null || x.Acknowledgement.IsDraft))
                        .Select(x => x.Id).ToListAsync();
                    var activeCovered = await _db.PublicityDispatchItems.AsNoTracking()
                        .Where(x => activeReplacementIds.Contains(x.DispatchId) && x.RequestItemId == destItem.Id)
                        .SumAsync(x => (int?)x.DispatchedQty) ?? 0;
                    var itemOutstanding = Math.Max(0, deficit - activeCovered);
                    if (dto.Quantity > itemOutstanding)
                        return BadRequest(new { Success = false, Message = $"Transfer quantity cannot exceed the remaining requirement for this product ({itemOutstanding} Nos)." });
                }

                int remaining;
                if (destItem != null)
                {
                    (_, remaining) = await ProductExcessAsync(source, destItem.ProductId);
                }
                else
                {
                    var ss = await SummaryAsync(source);
                    remaining = ss.Excess - ss.TransferredOut;
                }
                if (dto.Quantity > remaining) return BadRequest(new { Success = false, Message = $"Only {remaining} Nos of excess remain at the source HQ." });
                d.Quantity = dto.Quantity;
                d.DispatchNo = await NextNoAsync("TRF");
                d.Status = PublicityDispatchStatus.TransferRequested;
                d.SourceHqId = source.HeadquarterId;
                d.SourceRequestId = source.Id;
                _db.PublicityDispatches.Add(d);
                await _db.SaveChangesAsync();
                if (destItem != null)
                {
                    _db.PublicityDispatchItems.Add(new PublicityDispatchItem
                    {
                        DispatchId = d.Id, RequestItemId = destItem.Id, ProductId = destItem.ProductId,
                        DispatchedQty = dto.Quantity, ReceivedQty = 0, DamagedQty = 0
                    });
                    await _db.SaveChangesAsync();
                }
                await AddEvent(r, PublicityStage.InterHqTransfer, "Transfer Requested", name, dto.Remarks, d.Id);
                await AddEvent(source, PublicityStage.TransferCreation, "Transfer Order Received", name, $"{d.Quantity} Nos to {r.IndentNo}", d.Id);
                var hqs = await NamesAsync(Array.Empty<int>(), Array.Empty<int>(), new[] { r.HeadquarterId });
                await NotifyAsync(PublicityNotificationType.StockTransfer, source, d.Id, source.CreatedByUserId, AppRole.RMD, source.HeadquarterId,
                    "Stock Transfer Order Received",
                    $"{d.Quantity} Nos {await ProductMaterialAsync(r)} to be dispatched to {hqs.Hqs.GetValueOrDefault(r.HeadquarterId, "the destination HQ")}",
                    $"From your available excess stock (Request ID: {source.IndentNo})");
                await RecomputeAsync(source);
            }
            else
            {
                // Idempotency: Prevent duplicate dispatches on repeated clicks or network retries
                var cleanTracking = Clean(dto.TrackingNo);
                var recentDuplicate = await _db.PublicityDispatches.AsNoTracking()
                    .Where(x => x.RequestId == r.Id && x.Kind == dto.Kind && x.CreatedByUserId == Uid()
                                && x.CreatedAt >= DateTime.UtcNow.AddSeconds(-20))
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();
                if (recentDuplicate != null && cleanTracking == recentDuplicate.TrackingNo && (dto.Quantity == 0 || dto.Quantity == recentDuplicate.Quantity))
                {
                    return Ok(new { recentDuplicate.Id, recentDuplicate.DispatchNo });
                }

                if (dto.Kind == PublicityDispatchKind.Original)
                {
                    if (r.Status != PublicityStatus.FinalApproved) return BadRequest(new { Success = false, Message = "The request is not final approved yet." });
                }
                else
                {
                    if (r.Status != PublicityStatus.ExceptionRaised) return BadRequest(new { Success = false, Message = "Re-initiation resolves a raised exception only." });
                }

                if (string.IsNullOrWhiteSpace(dto.VendorName) || string.IsNullOrWhiteSpace(dto.Transporter) || dto.DispatchDate is null)
                    return BadRequest(new { Success = false, Message = "Vendor, transporter and dispatch date are required." });
                if (dto.DeliveryAddressId is not > 0) return BadRequest(new { Success = false, Message = "Select a delivery address." });
                var addr = await _db.PublicityDeliveryAddresses.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == dto.DeliveryAddressId && a.IsActive);
                if (addr is null) return BadRequest(new { Success = false, Message = "Select a valid, active delivery address." });

                var hasItems = r.Items.Count > 0;
                var productNames = hasItems
                    ? await _db.Products.AsNoTracking()
                        .Where(p => r.Items.Select(i => i.ProductId).Contains(p.Id))
                        .ToDictionaryAsync(p => p.Id, p => p.Name)
                    : new Dictionary<int, string>();

                List<(PublicityRequestItem Item, int Qty)> itemsToDispatch = new();

                if (hasItems && dto.Items != null && dto.Items.Count > 0)
                {
                    var reqItemsMap = r.Items.ToDictionary(i => i.Id);
                    foreach (var it in dto.Items)
                    {
                        if (!reqItemsMap.TryGetValue(it.RequestItemId, out var reqItem))
                            return BadRequest(new { Success = false, Message = $"Invalid product item ID {it.RequestItemId}." });

                        var pName = productNames.GetValueOrDefault(reqItem.ProductId, $"Product #{reqItem.ProductId}");

                        if (it.DispatchQty < 0)
                            return BadRequest(new { Success = false, Message = $"Dispatch quantity for {pName} cannot be negative." });

                        if (dto.Kind == PublicityDispatchKind.Original)
                        {
                            var prevDispatchIds = await _db.PublicityDispatches.AsNoTracking()
                                .Where(x => x.RequestId == r.Id && x.Kind == PublicityDispatchKind.Original)
                                .Select(x => x.Id).ToListAsync();

                            var prevDispatched = await _db.PublicityDispatchItems.AsNoTracking()
                                .Where(x => prevDispatchIds.Contains(x.DispatchId) && x.RequestItemId == reqItem.Id)
                                .SumAsync(x => (int?)x.DispatchedQty) ?? 0;

                            var remaining = Math.Max(0, reqItem.Quantity - prevDispatched);
                            if (it.DispatchQty > remaining)
                                return BadRequest(new { Success = false, Message = $"Dispatch quantity for {pName} cannot exceed remaining approved quantity ({remaining} Nos)." });
                        }
                        else // ReInitiation
                        {
                            var ackDispatchIds = await _db.PublicityDispatches.AsNoTracking()
                                .Where(x => x.RequestId == r.Id && x.Acknowledgement != null && !x.Acknowledgement.IsDraft)
                                .Select(x => x.Id).ToListAsync();

                            var ackItemRows = await _db.PublicityDispatchItems.AsNoTracking()
                                .Where(x => ackDispatchIds.Contains(x.DispatchId) && x.RequestItemId == reqItem.Id)
                                .ToListAsync();

                            var totalReceived = ackItemRows.Sum(x => x.ReceivedQty);
                            var totalDamaged = ackItemRows.Sum(x => x.DamagedQty);
                            var usable = Math.Max(0, totalReceived - totalDamaged);
                            var deficit = Math.Max(0, reqItem.Quantity - usable);

                            var activeReplacementIds = await _db.PublicityDispatches.AsNoTracking()
                                .Where(x => x.RequestId == r.Id && x.Kind != PublicityDispatchKind.Original && (x.Acknowledgement == null || x.Acknowledgement.IsDraft))
                                .Select(x => x.Id).ToListAsync();

                            var activeCovered = await _db.PublicityDispatchItems.AsNoTracking()
                                .Where(x => activeReplacementIds.Contains(x.DispatchId) && x.RequestItemId == reqItem.Id)
                                .SumAsync(x => (int?)x.DispatchedQty) ?? 0;

                            var outstandingReplacement = Math.Max(0, deficit - activeCovered);
                            if (it.DispatchQty > outstandingReplacement)
                                return BadRequest(new { Success = false, Message = $"Re-dispatch quantity for {pName} cannot exceed outstanding replacement quantity ({outstandingReplacement} Nos)." });
                        }

                        if (it.DispatchQty > 0)
                        {
                            itemsToDispatch.Add((reqItem, it.DispatchQty));
                        }
                    }

                    d.Quantity = itemsToDispatch.Sum(x => x.Qty);
                }
                else
                {
                    // General Product / Gift Articles / single item fallback
                    if (dto.Kind == PublicityDispatchKind.Original)
                    {
                        d.Quantity = r.Quantity;
                    }
                    else
                    {
                        d.Quantity = (await SummaryAsync(r)).Outstanding;
                    }
                }

                if (d.Quantity <= 0) return BadRequest(new { Success = false, Message = "There is no quantity to dispatch for this request." });

                d.DeliveryAddressId = addr.Id;
                d.DestinationAddress = await ComposeAddressTextAsync(addr);
                d.DispatchNo = await NextNoAsync("DSP");
                d.Status = PublicityDispatchStatus.Dispatched;
                d.VendorName = dto.VendorName!.Trim();
                d.Transporter = dto.Transporter!.Trim();
                d.TrackingNo = cleanTracking;
                d.VehicleNo = Clean(dto.VehicleNo);
                d.DispatchDate = dto.DispatchDate;
                d.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;
                await SetResponsibleAsync(d, dto.ResponsibleUserId);
                _db.PublicityDispatches.Add(d);
                await _db.SaveChangesAsync();

                // Save product-wise dispatch line items (zero-quantity items do not create dispatch lines)
                if (itemsToDispatch.Count > 0)
                {
                    foreach (var (reqItem, qty) in itemsToDispatch)
                    {
                        _db.PublicityDispatchItems.Add(new PublicityDispatchItem
                        {
                            DispatchId = d.Id,
                            RequestItemId = reqItem.Id,
                            ProductId = reqItem.ProductId,
                            DispatchedQty = qty,
                            ReceivedQty = 0,
                            DamagedQty = 0
                        });
                    }
                    await _db.SaveChangesAsync();
                }

                if (dto.Kind == PublicityDispatchKind.Original)
                    await AddEvent(r, PublicityStage.Dispatch, "Note Approved", name, "Dispatch / note approval document uploaded.", d.Id);
                await AddEvent(r, dto.Kind == PublicityDispatchKind.Original ? PublicityStage.Dispatch : PublicityStage.ReInitiation,
                    "Dispatched", name, $"{d.DispatchNo}: {d.Quantity} Nos via {d.Transporter}", d.Id);
                await NotifyAsync(PublicityNotificationType.Dispatch, r, d.Id, r.CreatedByUserId, null, null,
                    $"Material dispatched ({d.DispatchNo})", $"{d.Quantity} Nos dispatched via {d.Transporter}.", d.TrackingNo is null ? null : $"Tracking No: {d.TrackingNo}");
            }

            await RecomputeAsync(r);
            await _db.SaveChangesAsync();
            return Ok(new { d.Id, d.DispatchNo });
        }

        // ================================================================ delivery addresses (admin)

        // A small, shared, Publicity-specific address book (regional offices etc.). Read/create from
        // the dispatch page stays gated on IsDispatcherAsync() exactly as before - unchanged. The admin
        // management list/update/delete below (2026-11-26) are a separate, narrower surface gated on
        // IsAdminMdCsAsync()/IsSuper() only, same convention as the Material Master endpoints above -
        // every write is independently re-checked server-side regardless of what the sidebar/page hide.
        [HttpGet("delivery-addresses")]
        public async Task<IActionResult> DeliveryAddresses()
        {
            if (!await IsDispatcherAsync()) return Forbid();
            var rows = await _db.PublicityDeliveryAddresses.AsNoTracking().Where(a => a.IsActive)
                .OrderBy(a => a.AddressLabel).Take(500).ToListAsync();
            return Ok(await ToDeliveryAddressDtosAsync(rows));
        }

        [HttpGet("delivery-addresses/admin")]
        public async Task<IActionResult> DeliveryAddressesAdmin()
        {
            if (!await IsAdminMdCsAsync() && !IsSuper()) return Forbid();
            var rows = await _db.PublicityDeliveryAddresses.AsNoTracking()
                .OrderBy(a => a.AddressLabel).ToListAsync();
            return Ok(await ToDeliveryAddressDtosAsync(rows));
        }

        private async Task<List<PubDeliveryAddressDto>> ToDeliveryAddressDtosAsync(List<PublicityDeliveryAddress> rows)
        {
            var stateIds = rows.Select(a => a.StateId).Distinct().ToList();
            var districtIds = rows.Where(a => a.DistrictId.HasValue).Select(a => a.DistrictId!.Value).Distinct().ToList();
            var states = await _db.States.AsNoTracking().Where(s => stateIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.StateName);
            var districts = await _db.Districts.AsNoTracking().Where(d => districtIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.DistrictName);
            return rows.Select(a => new PubDeliveryAddressDto
            {
                Id = a.Id, AddressLabel = a.AddressLabel, RecipientName = a.RecipientName, ContactPerson = a.ContactPerson,
                MobileNumber = a.MobileNumber, AddressLine1 = a.AddressLine1, AddressLine2 = a.AddressLine2, Landmark = a.Landmark,
                StateId = a.StateId, StateName = states.GetValueOrDefault(a.StateId),
                DistrictId = a.DistrictId, DistrictName = a.DistrictId is int did ? districts.GetValueOrDefault(did) : null,
                City = a.City, PinCode = a.PinCode, IsActive = a.IsActive
            }).ToList();
        }

        [HttpPost("delivery-addresses")]
        public async Task<IActionResult> CreateDeliveryAddress([FromBody] PubDeliveryAddressCreateDto dto)
        {
            if (!await IsDispatcherAsync()) return Forbid();
            var error = await ValidateDeliveryAddressAsync(dto.AddressLabel, dto.RecipientName, dto.ContactPerson, dto.MobileNumber,
                dto.AddressLine1, dto.AddressLine2, dto.Landmark, dto.StateId, dto.DistrictId, dto.City, dto.PinCode);
            if (error != null) return BadRequest(new { Success = false, Message = error });

            var a = new PublicityDeliveryAddress
            {
                AddressLabel = dto.AddressLabel.Trim(), RecipientName = dto.RecipientName.Trim(), ContactPerson = Clean(dto.ContactPerson),
                MobileNumber = dto.MobileNumber!.Trim(), AddressLine1 = dto.AddressLine1.Trim(), AddressLine2 = Clean(dto.AddressLine2),
                Landmark = Clean(dto.Landmark), StateId = dto.StateId, DistrictId = dto.DistrictId > 0 ? dto.DistrictId : null,
                City = dto.City.Trim(), PinCode = dto.PinCode!.Trim(), CreatedByUserId = Uid(), CreatedByName = await MyNameAsync()
            };
            _db.PublicityDeliveryAddresses.Add(a);
            await _db.SaveChangesAsync();
            return Ok(new { a.Id });
        }

        [HttpPut("delivery-addresses/{id:int}")]
        public async Task<IActionResult> UpdateDeliveryAddress(int id, [FromBody] PubDeliveryAddressUpdateDto dto)
        {
            if (!await IsAdminMdCsAsync() && !IsSuper()) return Forbid();
            var a = await _db.PublicityDeliveryAddresses.FirstOrDefaultAsync(x => x.Id == id);
            if (a is null) return NotFound();
            var error = await ValidateDeliveryAddressAsync(dto.AddressLabel, dto.RecipientName, dto.ContactPerson, dto.MobileNumber,
                dto.AddressLine1, dto.AddressLine2, dto.Landmark, dto.StateId, dto.DistrictId, dto.City, dto.PinCode);
            if (error != null) return BadRequest(new { Success = false, Message = error });

            // Editing/deactivating this row never touches any dispatch already created against it - a
            // dispatch copies the address text into its own DestinationAddress at creation time (see
            // the PublicityDeliveryAddress entity's doc comment); only this live row changes.
            a.AddressLabel = dto.AddressLabel.Trim(); a.RecipientName = dto.RecipientName.Trim(); a.ContactPerson = Clean(dto.ContactPerson);
            a.MobileNumber = dto.MobileNumber!.Trim(); a.AddressLine1 = dto.AddressLine1.Trim(); a.AddressLine2 = Clean(dto.AddressLine2);
            a.Landmark = Clean(dto.Landmark); a.StateId = dto.StateId; a.DistrictId = dto.DistrictId > 0 ? dto.DistrictId : null;
            a.City = dto.City.Trim(); a.PinCode = dto.PinCode!.Trim(); a.IsActive = dto.IsActive;
            a.UpdatedByUserId = Uid(); a.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { a.Id });
        }

        [HttpDelete("delivery-addresses/{id:int}")]
        public async Task<IActionResult> DeleteDeliveryAddress(int id)
        {
            if (!await IsAdminMdCsAsync() && !IsSuper()) return Forbid();
            var a = await _db.PublicityDeliveryAddresses.FirstOrDefaultAsync(x => x.Id == id);
            if (a is null) return NotFound();

            // Same "prefer deactivation over deletion" pattern as DeleteMaterial above: a dispatch's
            // own DestinationAddress text is a frozen snapshot, but PublicityDispatch.DeliveryAddressId
            // is still a live FK to this row (used e.g. to re-select it on a future dispatch) - block
            // the hard delete if any dispatch references it and point the admin at Deactivate instead.
            var inUse = await _db.PublicityDispatches.AsNoTracking().CountAsync(d => d.DeliveryAddressId == id);
            if (inUse > 0)
                return BadRequest(new
                {
                    Message = $"Cannot delete \"{a.AddressLabel}\" - it is used by {inUse} dispatch record(s). Deactivate it instead to hide it from new dispatches."
                });

            _db.PublicityDeliveryAddresses.Remove(a);
            await _db.SaveChangesAsync();
            return Ok(new { Success = true });
        }

        private async Task<string?> ValidateDeliveryAddressAsync(string addressLabel, string recipientName, string? contactPerson,
            string mobileNumber, string addressLine1, string? addressLine2, string? landmark, int stateId, int? districtId, string city, string pinCode)
        {
            string? Req(string? v, string label, int max) =>
                string.IsNullOrWhiteSpace(v) ? $"Enter the {label}." : v.Trim().Length > max ? $"{label} is too long." : null;
            return Req(addressLabel, "address name / label", 150)
                ?? Req(recipientName, "recipient / office name", 200)
                ?? Req(addressLine1, "address line 1", 300)
                ?? Req(city, "district / city", 150)
                ?? (contactPerson is { Length: > 150 } ? "Contact person is too long." : null)
                ?? (addressLine2 is { Length: > 300 } ? "Address line 2 is too long." : null)
                ?? (landmark is { Length: > 150 } ? "Landmark is too long." : null)
                ?? (!Regex.IsMatch(mobileNumber?.Trim() ?? "", @"^[6-9]\d{9}$") ? "Enter a valid 10-digit Indian mobile number." : null)
                ?? (!Regex.IsMatch(pinCode?.Trim() ?? "", @"^\d{6}$") ? "Enter a valid 6-digit PIN code." : null)
                ?? (!await _db.States.AnyAsync(s => s.Id == stateId && s.IsActive) ? "Select the state." : null)
                ?? (districtId is > 0 && !await _db.Districts.AnyAsync(d => d.Id == districtId && d.StateId == stateId && d.IsActive)
                    ? "Select a district that belongs to the selected state." : null);
        }

        private async Task<string> ComposeAddressTextAsync(PublicityDeliveryAddress a)
        {
            var stateName = await _db.States.AsNoTracking().Where(s => s.Id == a.StateId).Select(s => s.StateName).FirstOrDefaultAsync();
            var parts = new List<string?> { a.RecipientName, a.AddressLine1, a.AddressLine2,
                string.IsNullOrWhiteSpace(a.Landmark) ? null : $"Near {a.Landmark}", a.City, stateName, a.PinCode };
            var text = string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
            return string.IsNullOrWhiteSpace(a.MobileNumber) ? text : $"{text} (Ph: {a.MobileNumber})";
        }

        [HttpPost("dispatches/{id:int}/status")]
        public async Task<IActionResult> DispatchStatus(int id, [FromBody] PubDispatchStatusDto dto)
        {
            if (!await IsDispatcherAsync()) return Forbid();
            var d = await _db.PublicityDispatches.FirstOrDefaultAsync(x => x.Id == id);
            if (d is null) return NotFound(new { Success = false, Message = "Dispatch not found." });
            if (d.Status == PublicityDispatchStatus.Acknowledged) return BadRequest(new { Success = false, Message = "This dispatch is already acknowledged." });
            if (dto.Status is PublicityDispatchStatus.TransferRequested or PublicityDispatchStatus.ProofSubmitted or PublicityDispatchStatus.Acknowledged)
                return BadRequest(new { Success = false, Message = "That status cannot be set here." });
            if (d.Status == PublicityDispatchStatus.TransferRequested)
                return BadRequest(new { Success = false, Message = "Courier proof is pending from the source RMDO." });

            var r = (await LoadAsync(d.RequestId, tracking: true))!;
            var name = await MyNameAsync();
            if (!string.IsNullOrWhiteSpace(dto.Transporter)) d.Transporter = dto.Transporter.Trim();
            if (!string.IsNullOrWhiteSpace(dto.TrackingNo)) d.TrackingNo = dto.TrackingNo.Trim();
            if (dto.DispatchDate.HasValue) d.DispatchDate = dto.DispatchDate;
            if (dto.ExpectedDeliveryDate.HasValue) d.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;
            if (!string.IsNullOrWhiteSpace(dto.CurrentLocation)) d.CurrentLocation = dto.CurrentLocation.Trim();
            if (Clean(dto.VehicleNo) is { } vehicleNo) d.VehicleNo = vehicleNo;
            if (!string.IsNullOrWhiteSpace(dto.Remarks)) d.Remarks = dto.Remarks.Trim();
            // Admin Review confirming a transfer (Dispatched) is the one place the admin assigns who at
            // the destination is responsible for it - Mark In Transit / Mark Delivered never send this,
            // so SetResponsibleAsync only ever runs here when the caller actually provided a value.
            if (!string.IsNullOrWhiteSpace(dto.ResponsibleUserId)) await SetResponsibleAsync(d, dto.ResponsibleUserId);
            var label = dto.Status switch
            {
                PublicityDispatchStatus.Dispatched => d.Kind == PublicityDispatchKind.Transfer ? "Transfer Created" : "Dispatched",
                PublicityDispatchStatus.InTransit => "In Transit",
                _ => "Delivered"
            };
            if (dto.Status >= PublicityDispatchStatus.InTransit && d.InTransitAt is null) d.InTransitAt = DateTime.UtcNow;
            if (dto.Status == PublicityDispatchStatus.Delivered) d.DeliveredAt = DateTime.UtcNow;
            d.DispatchDate ??= DateTime.UtcNow;
            d.Status = dto.Status;
            await AddEvent(r, d.Kind == PublicityDispatchKind.Transfer ? PublicityStage.InterHqTransfer : PublicityStage.Delivery, label, name, d.CurrentLocation, d.Id);
            if (dto.Status == PublicityDispatchStatus.Delivered)
                await NotifyAsync(PublicityNotificationType.Dispatch, r, d.Id, r.CreatedByUserId, null, null,
                    $"Material delivered ({d.DispatchNo})", "Please submit the receipt acknowledgement.", null);
            if (dto.Status == PublicityDispatchStatus.Dispatched && d.Kind == PublicityDispatchKind.Transfer)
                // Admin just confirmed the courier proof and created the transfer (Step 3/4 of the stock
                // transfer workflow) - the destination's own responsible employee (r.CreatedByUserId,
                // same recipient the existing Delivered-notify above already uses) needs to know
                // material is now actually in transit toward them.
                await NotifyAsync(PublicityNotificationType.StockTransfer, r, d.Id, r.CreatedByUserId, null, null,
                    $"Stock Transfer Dispatched ({d.DispatchNo})", $"{d.Quantity} Nos dispatched via {d.Transporter ?? "the assigned courier"} and in transit.", "Track the incoming transfer.");
            await RecomputeAsync(r);
            if (d.SourceRequestId is int sid && await LoadAsync(sid, tracking: true) is { } source)
            {
                await RecomputeAsync(source);
                if (dto.Status == PublicityDispatchStatus.Dispatched && d.Kind == PublicityDispatchKind.Transfer)
                    // The source RMDO who submitted the proof is told the admin accepted it and the
                    // transfer has actually been created - same recipient resolution CanSubmitProofAsync
                    // already uses (creator-of-source-request, widened to the source HQ's RMDO/MDO team).
                    await NotifyAsync(PublicityNotificationType.StockTransfer, source, d.Id, source.CreatedByUserId, AppRole.RMD, source.HeadquarterId,
                        $"Transfer Approved ({d.DispatchNo})", "Admin confirmed the dispatch details - the transfer is now in transit.", null);
            }
            await _db.SaveChangesAsync();
            return Ok(new { Success = true });
        }

        // ================================================================ transfer proof (source RMDO)

        [HttpPost("dispatches/{id:int}/proof")]
        public async Task<IActionResult> Proof(int id, [FromBody] PubProofDto dto)
        {
            var d = await _db.PublicityDispatches.FirstOrDefaultAsync(x => x.Id == id);
            if (d is null || d.Kind != PublicityDispatchKind.Transfer) return NotFound(new { Success = false, Message = "Transfer order not found." });
            if (!await CanSubmitProofAsync(d)) return Forbid();
            if (!await _db.PublicityFiles.AnyAsync(f => f.DispatchId == d.Id && f.Kind == PublicityFileKind.CourierProof))
                return BadRequest(new { Success = false, Message = "Upload the courier / dispatch proof first." });

            // The source RMDO only uploads the proof document now (no manual/editable courier fields on
            // that page anymore) - whatever OCR silently read off the document is accepted as-is, and
            // DispatchDate falls back to the submission moment rather than blocking on a value nobody is
            // ever asked to enter.
            var name = await MyNameAsync();
            d.Transporter = Clean(dto.Transporter) ?? d.Transporter;
            d.TrackingNo = Clean(dto.TrackingNo) ?? d.TrackingNo;
            d.DispatchDate = dto.DispatchDate ?? d.DispatchDate ?? DateTime.UtcNow;
            d.VehicleNo = Clean(dto.VehicleNo) ?? d.VehicleNo;
            if (!string.IsNullOrWhiteSpace(dto.Remarks)) d.Remarks = dto.Remarks.Trim();
            d.Status = PublicityDispatchStatus.ProofSubmitted;
            d.ProofByName = name;
            d.ProofAt = DateTime.UtcNow;
            await SetResponsibleAsync(d, Uid());
            var r = (await LoadAsync(d.RequestId, tracking: true))!;
            await AddEvent(r, PublicityStage.InterHqTransfer, "Proof Submitted", name, $"{d.Transporter} {d.TrackingNo}", d.Id);
            if (d.SourceRequestId is int sid && await LoadAsync(sid, tracking: true) is { } source)
            {
                await AddEvent(source, PublicityStage.TransferCreation, "Proof Submitted", name, $"{d.DispatchNo} to {r.IndentNo}", d.Id);
                await RecomputeAsync(source);
            }
            await NotifyAsync(PublicityNotificationType.StockTransfer, r, d.Id, null, AppRole.MDAdmin, null,
                $"Courier Proof Submitted - Review Required ({d.DispatchNo})", $"{name} uploaded the courier proof for {d.Quantity} Nos.", "Review and confirm the transfer.");
            await RecomputeAsync(r);
            await _db.SaveChangesAsync();
            return Ok(new { Success = true });
        }

        /// <summary>Courier receipt image -> transporter, AWB, date, vehicle (best effort, never fails).</summary>
        [HttpPost("dispatches/ocr")]
        [RequestSizeLimit(MaxFileBytes)]
        public async Task<IActionResult> Ocr(IFormFile? file)
        {
            var result = new PubOcrDto();
            if (file is null || file.Length == 0) return Ok(result);
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp")) return Ok(result);
            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                string text;
                using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                using (var img = Pix.LoadFromMemory(ms.ToArray()))
                using (var page = engine.Process(img))
                    text = page.GetText() ?? "";
                var flat = Regex.Replace(text, @"\s+", " ");
                var couriers = (await _db.SasCouriers.AsNoTracking().Select(c => c.Name).ToListAsync()).Concat(KnownCouriers);
                result.Transporter = couriers.FirstOrDefault(c => flat.Contains(c.Split(' ')[0] + (c.Contains(' ') ? " " + c.Split(' ')[1] : ""), StringComparison.OrdinalIgnoreCase))
                    ?? couriers.FirstOrDefault(c => flat.Contains(c.Split(' ')[0], StringComparison.OrdinalIgnoreCase));
                var awb = Regex.Match(flat, @"(?:AWB|Airway\s*Bill|Tracking|Consignment|C\.?N\.?)\s*(?:No\.?|Number|#)?\s*[:\-]?\s*([A-Z0-9\-]{6,20})", RegexOptions.IgnoreCase);
                if (!awb.Success) awb = Regex.Match(flat, @"\b([A-Z]{2}\d{8,11}[A-Z]{0,2}|\d{9,14})\b");
                if (awb.Success) result.TrackingNo = awb.Groups[1].Value.ToUpperInvariant();
                var vehicle = Regex.Match(flat, @"\b([A-Z]{2}\s?\d{1,2}\s?[A-Z]{1,3}\s?\d{4})\b");
                if (vehicle.Success) result.VehicleNo = vehicle.Groups[1].Value.ToUpperInvariant();
                var date = Regex.Match(flat, @"\b(\d{1,2}[\/\-\.]\d{1,2}[\/\-\.]\d{2,4}|\d{1,2}\s+[A-Za-z]{3,9}\s+\d{4})\b");
                if (date.Success && DateTime.TryParseExact(date.Groups[1].Value.Replace('.', '/').Replace('-', '/'),
                        new[] { "d/M/yyyy", "d/M/yy", "d MMM yyyy", "d MMMM yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    result.DispatchDate = dt;
                result.Found = result.Transporter != null || result.TrackingNo != null || result.DispatchDate != null;
            }
            catch (Exception)
            {
                // OCR is a convenience: the user fills the fields by hand when it cannot read the image.
            }
            return Ok(result);
        }

        // ================================================================ acknowledgement (destination RMDO)

        [HttpPut("dispatches/{id:int}/ack")]
        public async Task<IActionResult> Acknowledge(int id, [FromBody] PubAckUpsertDto dto)
        {
            var d = await _db.PublicityDispatches.Include(x => x.Acknowledgement).FirstOrDefaultAsync(x => x.Id == id);
            if (d is null) return NotFound(new { Success = false, Message = "Dispatch not found." });
            var r = (await LoadAsync(d.RequestId, tracking: true))!;
            if (!await CanAcknowledgeAsync(r, d)) return BadRequest(new { Success = false, Message = "This dispatch cannot be acknowledged now." });

            // Product-wise acknowledgement (Specific Product, multi-item requests only - 2026-11-27).
            // dto.Items is only ever sent by PublicityDetails.razor when r.Items.Count > 1; every
            // General Product / Gift Articles / single-item request keeps using dto.ReceivedQty/
            // IsDamaged/DamagedQty exactly as before (hasItems is false, nothing below runs).
            var hasItems = dto.Items is { Count: > 0 };
            int receivedQty, damagedQty;
            bool isDamaged;
            // Which products does THIS dispatch actually cover? (2026-11-27 fix - previously every
            // r.Items.Id was "valid" regardless of dispatch, so a partial replacement dispatch that
            // only re-sent 2 of 5 products still demanded - and accepted - an acknowledgement entry
            // for the other 3 untouched products, which were never part of it.) Same "item's full
            // requirement minus what OTHER dispatches already received" math used below to persist
            // DispatchedQty, computed once up front so validation and persistence agree on exactly
            // the same set: the very first dispatch of the request covers every item (even a
            // 0-approved one), any later dispatch only covers items where that leaves something > 0.
            Dictionary<int, int>? priorReceivedByItemForValidation = null;
            HashSet<int> applicableItemIds;
            if (hasItems)
            {
                var requestDispatchIdsForValidation = await _db.PublicityDispatches.AsNoTracking()
                    .Where(x => x.RequestId == r.Id).Select(x => x.Id).ToListAsync();
                var isFirstDispatch = requestDispatchIdsForValidation.Count == 0 || d.Id == requestDispatchIdsForValidation.Min();
                priorReceivedByItemForValidation = await _db.PublicityDispatchItems.AsNoTracking()
                    .Where(x => requestDispatchIdsForValidation.Contains(x.DispatchId) && x.DispatchId != d.Id && x.RequestItemId != null)
                    .GroupBy(x => x.RequestItemId!.Value)
                    .Select(g => new { RequestItemId = g.Key, Received = g.Sum(x => x.ReceivedQty) })
                    .ToDictionaryAsync(x => x.RequestItemId, x => x.Received);
                // If THIS dispatch already has explicit PublicityDispatchItem row(s) - e.g. a stock
                // transfer created for one specific product via PublicityResolveExcess.razor, which
                // now writes its own row at creation time - trust that explicit record completely
                // instead of the "nonzero outstanding" heuristic below: a transfer created for exactly
                // one product must only ever require/accept an acknowledgement for that one product,
                // never every OTHER product that also happens to still be short on the same request.
                var explicitItemIds = (await _db.PublicityDispatchItems.AsNoTracking()
                        .Where(x => x.DispatchId == d.Id && x.RequestItemId != null).Select(x => x.RequestItemId!.Value).ToListAsync())
                    .ToHashSet();
                applicableItemIds = explicitItemIds.Count > 0 ? explicitItemIds
                    : isFirstDispatch ? r.Items.Select(i => i.Id).ToHashSet()
                    : r.Items.Where(i => Math.Max(0, i.Quantity - priorReceivedByItemForValidation.GetValueOrDefault(i.Id)) > 0)
                        .Select(i => i.Id).ToHashSet();
            }
            else applicableItemIds = new HashSet<int>();
            if (hasItems)
            {
                var seen = new HashSet<int>();
                foreach (var it in dto.Items!)
                {
                    if (!applicableItemIds.Contains(it.RequestItemId)) return BadRequest(new { Success = false, Message = "Invalid product line item." });
                    if (!seen.Add(it.RequestItemId)) return BadRequest(new { Success = false, Message = "Duplicate product line item submitted." });
                    if (it.ReceivedQty < 0) return BadRequest(new { Success = false, Message = "Enter the received quantity for every product." });
                    if (it.IsDamaged && (it.DamagedQty <= 0 || it.DamagedQty > it.ReceivedQty))
                        return BadRequest(new { Success = false, Message = "Damaged quantity must be between 1 and the received quantity for every damaged product." });
                    if (!it.IsDamaged && it.DamagedQty != 0)
                        return BadRequest(new { Success = false, Message = "A product in good condition cannot have a damaged quantity." });
                }
                if (applicableItemIds.Any(vid => !seen.Contains(vid)))
                    return BadRequest(new { Success = false, Message = "Enter the acknowledgement for every product in this dispatch." });
                receivedQty = dto.Items!.Sum(i => i.ReceivedQty);
                damagedQty = dto.Items!.Sum(i => i.IsDamaged ? i.DamagedQty : 0);
                isDamaged = damagedQty > 0;
            }
            else
            {
                if (dto.ReceivedQty < 0) return BadRequest(new { Success = false, Message = "Enter the received quantity." });
                if (dto.IsDamaged && (dto.DamagedQty <= 0 || dto.DamagedQty > dto.ReceivedQty))
                    return BadRequest(new { Success = false, Message = "Damaged quantity must be between 1 and the received quantity." });
                receivedQty = dto.ReceivedQty;
                damagedQty = dto.IsDamaged ? dto.DamagedQty : 0;
                isDamaged = dto.IsDamaged;
            }

            var ack = d.Acknowledgement ?? new PublicityAcknowledgement { DispatchId = d.Id, RequestId = r.Id };
            if (d.Acknowledgement is null) _db.PublicityAcknowledgements.Add(ack);
            // These three fields are ALWAYS the aggregate (sum across products when hasItems, the
            // single dto value otherwise) - every downstream computation (SummaryAsync, the
            // notification text below, RowsAsync's exception-type read, PubAckDto) only ever reads
            // these aggregate fields and is therefore completely unaware of, and unaffected by,
            // product-wise acknowledgement - zero changes needed anywhere else.
            ack.ReceivedQty = receivedQty;
            ack.IsDamaged = isDamaged;
            ack.DamagedQty = damagedQty;
            ack.Remarks = Clean(dto.Remarks);
            ack.UpdatedAt = DateTime.UtcNow;
            // Acknowledged By is never taken from the client - CanAcknowledgeAsync above already
            // proved the CALLER is the one and only authorized party for this dispatch (the assigned
            // Responsible Person, or the Transfer-kind destination rule), so the acknowledger's
            // identity can only ever be the authenticated user, never a dto-supplied id.
            ack.AcknowledgedByUserId = Uid();
            ack.AcknowledgedByName = await MyNameAsync();

            if (hasItems)
            {
                // DispatchedQty per product is this dispatch's own responsibility: the item's full
                // requirement minus whatever OTHER dispatches of this same request already received
                // against it. For the very first dispatch there are no prior rows, so this is simply
                // the item's full quantity; for a later re-initiation/transfer it is correctly only
                // the item's remaining outstanding - without needing the dispatch-creation flow to
                // target a specific product at all (CreateDispatch is completely untouched). Reuses
                // the exact same prior-received figures the applicability check above already computed,
                // so the two can never disagree on which products belong to this dispatch.
                var priorReceivedByItem = priorReceivedByItemForValidation!;
                var existingByReqItem = (await _db.PublicityDispatchItems.Where(x => x.DispatchId == d.Id).ToListAsync())
                    .Where(x => x.RequestItemId.HasValue).ToDictionary(x => x.RequestItemId!.Value);
                foreach (var it in dto.Items!)
                {
                    var reqItem = r.Items.First(i => i.Id == it.RequestItemId);
                    var dispatchedQty = Math.Max(0, reqItem.Quantity - priorReceivedByItem.GetValueOrDefault(it.RequestItemId));
                    if (existingByReqItem.TryGetValue(it.RequestItemId, out var row))
                    {
                        row.ProductId = reqItem.ProductId;
                        if (row.DispatchedQty == 0) row.DispatchedQty = dispatchedQty;
                        row.ReceivedQty = it.ReceivedQty;
                        row.DamagedQty = it.IsDamaged ? it.DamagedQty : 0;
                        row.Remarks = Clean(it.Remarks);
                    }
                    else
                    {
                        _db.PublicityDispatchItems.Add(new PublicityDispatchItem
                        {
                            DispatchId = d.Id,
                            RequestItemId = it.RequestItemId,
                            ProductId = reqItem.ProductId,
                            DispatchedQty = dispatchedQty,
                            ReceivedQty = it.ReceivedQty,
                            DamagedQty = it.IsDamaged ? it.DamagedQty : 0,
                            Remarks = Clean(it.Remarks)
                        });
                    }
                }
            }

            if (dto.Submit)
            {
                ack.IsDraft = false;
                ack.SubmittedAt = DateTime.UtcNow;
                d.Status = PublicityDispatchStatus.Acknowledged;
                d.DeliveredAt ??= DateTime.UtcNow;
                var shortage = Math.Max(0, d.Quantity - ack.ReceivedQty);
                var excess = Math.Max(0, ack.ReceivedQty - d.Quantity);
                var type = ExceptionType(shortage, excess, ack.DamagedQty);
                var name = await MyNameAsync();
                await AddEvent(r, PublicityStage.Acknowledgement, type == "None" ? "Acknowledged" : "Exception Raised", name,
                    ack.Remarks ?? $"Received {ack.ReceivedQty} of {d.Quantity} Nos.", d.Id);
                if (type != "None")
                {
                    if (excess > 0 && shortage == 0 && ack.DamagedQty == 0)
                    {
                        var pm = await ProductMaterialPartsAsync(r);
                        var sub = $"{d.DispatchNo} - {pm.Product} ({pm.Material}) - Dispatched: {d.Quantity} Nos, Received: {ack.ReceivedQty} Nos, Excess: {excess} Nos - Reported by {name} on {DateTime.Now:dd MMM yyyy}";
                        await NotifyAsync(PublicityNotificationType.Exception, r, d.Id, null, AppRole.MDAdmin, null,
                            "Excess Material Reported",
                            $"Request {r.IndentNo}: {name} acknowledged receiving {ack.ReceivedQty} Nos against {d.Quantity} Nos dispatched. Excess: {excess} Nos. Review the exception and available excess stock.",
                            sub);
                    }
                    else
                    {
                        var parts = new List<string>();
                        if (shortage > 0) parts.Add("Shortage");
                        if (excess > 0) parts.Add("Excess");
                        if (ack.DamagedQty > 0) parts.Add("Damaged");
                        await NotifyAsync(PublicityNotificationType.Exception, r, d.Id, null, AppRole.MDAdmin, null,
                            $"Exception Raised - {string.Join(" + ", parts)}",
                            $"{r.IndentNo} reported {string.Join(" and ", parts).ToLowerInvariant()} material ({(shortage > 0 ? $"{shortage} Nos short" : "")}{(shortage > 0 && (excess > 0 || ack.DamagedQty > 0) ? ", " : "")}{(excess > 0 ? $"{excess} Nos excess" : "")}{(excess > 0 && ack.DamagedQty > 0 ? ", " : "")}{(ack.DamagedQty > 0 ? $"{ack.DamagedQty} Nos damaged" : "")}). Review required.",
                            ack.Remarks);
                    }
                }
                else if (d.Kind == PublicityDispatchKind.Transfer)
                    // Clean transfer acknowledgement (no shortage/damage) - this is specifically the
                    // TRANSFER's own acknowledgement (this exact dispatch row, never the original
                    // dispatch's), so ADMIN-MD CS is told the transfer itself actually completed.
                    await NotifyAsync(PublicityNotificationType.StockTransfer, r, d.Id, null, AppRole.MDAdmin, null,
                        $"Transfer Acknowledgement Received ({d.DispatchNo})", $"{r.IndentNo} confirmed receipt of {ack.ReceivedQty} Nos.", null);
                await _db.SaveChangesAsync();
                var before = r.Status;
                await RecomputeAsync(r);
                if (r.Status == PublicityStatus.Completed && before != PublicityStatus.Completed)
                    await AddEvent(r, PublicityStage.Completed, "Completed", name, "All required usable quantity received.");
                if (d.SourceRequestId is int sid && await LoadAsync(sid, tracking: true) is { } source) await RecomputeAsync(source);
            }
            await _db.SaveChangesAsync();
            return Ok(new { Success = true, r.Status });
        }

        // ================================================================ files

        [HttpPost("requests/{id:int}/files")]
        [RequestSizeLimit(60L * 1024 * 1024)]
        public async Task<IActionResult> Upload(int id, [FromQuery] PublicityFileKind kind, [FromQuery] int? dispatchId, [FromQuery] int? requestItemId, [FromForm] List<IFormFile> files)
        {
            var r = await LoadAsync(id);
            if (r is null || !await CanReadAsync(r)) return NotFound(new { Success = false, Message = "Request not found." });
            if (files is null || files.Count == 0) return BadRequest(new { Success = false, Message = "No file uploaded." });
            if (dispatchId.HasValue && !await _db.PublicityDispatches.AnyAsync(d => d.Id == dispatchId && (d.RequestId == id || d.SourceRequestId == id)))
                return BadRequest(new { Success = false, Message = "Dispatch does not belong to this request." });
            if (requestItemId.HasValue && !await _db.PublicityRequestItems.AnyAsync(i => i.Id == requestItemId && i.RequestId == id))
                return BadRequest(new { Success = false, Message = "Request item does not belong to this request." });
            foreach (var f in files)
            {
                if (f.Length == 0 || f.Length > MaxFileBytes) return BadRequest(new { Success = false, Message = $"\"{f.FileName}\" must be under 10 MB." });
                if (!AllowedExt.Contains(Path.GetExtension(f.FileName).ToLowerInvariant()))
                    return BadRequest(new { Success = false, Message = "Only PDF, JPG, PNG, DOC and DOCX files are allowed." });
            }
            var name = await MyNameAsync();
            var folder = Path.Combine(UploadsRoot(), "Publicity", id.ToString());
            Directory.CreateDirectory(folder);
            var saved = new List<PublicityFile>();
            foreach (var f in files)
            {
                var safe = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{SafeName(f.FileName)}";
                await using (var stream = System.IO.File.Create(Path.Combine(folder, safe)))
                    await f.CopyToAsync(stream);
                var row = new PublicityFile
                {
                    RequestId = id, DispatchId = dispatchId, RequestItemId = requestItemId, Kind = kind, FileName = Path.GetFileName(f.FileName),
                    Path = $"Publicity/{id}/{safe}", SizeBytes = f.Length, UploadedByName = name
                };
                _db.PublicityFiles.Add(row);
                saved.Add(row);
            }
            await _db.SaveChangesAsync();
            return Ok(saved.Select(ToFile).ToList());
        }

        [HttpDelete("files/{id:int}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var f = await _db.PublicityFiles.FirstOrDefaultAsync(x => x.Id == id);
            if (f is null) return NotFound(new { Success = false, Message = "File not found." });
            var r = await LoadAsync(f.RequestId);
            if (r is null || !await CanReadAsync(r)) return NotFound(new { Success = false, Message = "File not found." });
            if (f.UploadedByName != await MyNameAsync() && !await IsDispatcherAsync()) return Forbid();
            _db.PublicityFiles.Remove(f);
            await _db.SaveChangesAsync();
            return Ok(new { Success = true });
        }

        [HttpGet("file/{*path}")]
        public IActionResult ViewFile(string path, [FromQuery] bool download = false)
        {
            if (string.IsNullOrWhiteSpace(path) || path.Contains("..") || path.Contains(':') ||
                !path.StartsWith("Publicity/", StringComparison.OrdinalIgnoreCase))
                return NotFound();
            var root = UploadsRoot();
            var full = Path.GetFullPath(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(full)) return NotFound();
            var type = Path.GetExtension(full).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf", ".png" => "image/png", ".webp" => "image/webp",
                ".jpg" or ".jpeg" => "image/jpeg", ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
            var name = Path.GetFileName(full);
            return download ? PhysicalFile(full, type, name.Length > 18 ? name[18..] : name) : PhysicalFile(full, type);
        }

        // ================================================================ notifications

        [HttpGet("notifications")]
        public async Task<IActionResult> Notifications([FromQuery] int? type)
        {
            var q = await MyNotificationsAsync();
            if (type.HasValue) q = q.Where(n => (int)n.Type == type.Value);
            var rows = await q.OrderByDescending(n => n.Id).Take(200).ToListAsync();
            return Ok(rows.Select(ToNotice).ToList());
        }

        // Cheap poll target for the header bell / PublicityReview Notifications button badge - same
        // "mine" scoping as the full list (MyNotificationsAsync), just a COUNT so a 20-30s poll loop
        // doesn't pull all 200 rows just to show a number.
        [HttpGet("notifications/unread-count")]
        public async Task<IActionResult> UnreadNotificationCount()
        {
            var count = await (await MyNotificationsAsync()).CountAsync(n => n.ReadAt == null);
            return Ok(new PubUnreadCountDto { Count = count });
        }

        [HttpGet("notifications/{id:int}")]
        public async Task<IActionResult> Notification(int id)
        {
            var n = await (await MyNotificationsAsync()).FirstOrDefaultAsync(x => x.Id == id);
            if (n is null) return NotFound(new { Success = false, Message = "Notification not found." });
            var dto = new PubNotificationDetailDto { Notice = ToNotice(n) };
            if (n.Type == PublicityNotificationType.Approval && n.DispatchId is null)
            {
                var status = await _db.PublicityRequests.AsNoTracking().Where(x => x.Id == n.RequestId && !x.IsDeleted)
                    .Select(x => (PublicityStatus?)x.Status).FirstOrDefaultAsync();
                dto.CanDispatch = status == PublicityStatus.FinalApproved && await IsAdminMdCsAsync();
            }
            var d = n.DispatchId is int did ? await _db.PublicityDispatches.AsNoTracking().Include(x => x.Acknowledgement).FirstOrDefaultAsync(x => x.Id == did) : null;
            if (d is not null)
            {
                var isStockTransfer = n.Type == PublicityNotificationType.StockTransfer || d.Kind == PublicityDispatchKind.Transfer || d.SourceRequestId.HasValue;
                dto.IsTransfer = isStockTransfer;

                var dest = await LoadAsync(d.RequestId);
                var pm = dest is not null ? await ProductMaterialPartsAsync(dest) : (Product: "", Material: "");
                dto.Product = pm.Product;
                dto.Material = pm.Material;
                dto.Status = DispatchStatusLabel(d.Status, d.Kind);

                if (isStockTransfer)
                {
                    var source = d.SourceRequestId is int sid ? await LoadAsync(sid) : null;
                    var names = await NamesAsync(Array.Empty<int>(), Array.Empty<int>(), new[] { dest?.HeadquarterId ?? 0, source?.HeadquarterId ?? 0 });
                    dto.DestinationIndentNo = dest?.IndentNo;
                    dto.DestinationHq = dest is not null ? names.Hqs.GetValueOrDefault(dest.HeadquarterId) : null;
                    dto.TransferQty = d.Quantity;
                    dto.Reason = d.Remarks ?? (dto.DestinationHq is not null ? $"Shortage resolution for {dto.DestinationHq}" : null);
                    if (source is not null)
                    {
                        var s = await SummaryAsync(source);
                        dto.SourceIndentNo = source.IndentNo;
                        dto.SourceHq = names.Hqs.GetValueOrDefault(source.HeadquarterId);
                        dto.ReportedExcess = s.Excess;
                        // every other transfer order on this excess, shipped or not, so two open orders never both show the full excess
                        dto.AlreadyTransferred = s.TransferredOut - d.Quantity;
                        dto.RemainingExcess = Math.Max(0, s.Excess - dto.AlreadyTransferred);
                    }
                }
                else
                {
                    var names = await NamesAsync(Array.Empty<int>(), Array.Empty<int>(), new[] { dest?.HeadquarterId ?? 0 });
                    dto.DispatchNo = d.DispatchNo;
                    dto.RequestIndentNo = dest?.IndentNo;
                    dto.DispatchQty = d.Quantity;
                    dto.DispatchDate = d.DispatchDate;
                    dto.VendorName = d.VendorName;
                    dto.DestinationHq = dest is not null ? names.Hqs.GetValueOrDefault(dest.HeadquarterId) : null;
                    dto.DestinationAddress = d.DestinationAddress;
                    dto.Transporter = d.Transporter;
                    dto.TrackingNo = d.TrackingNo;

                    if (d.Acknowledgement is { IsDraft: false } ack)
                    {
                        dto.ReceivedQty = ack.ReceivedQty;
                        dto.ExcessQty = Math.Max(0, ack.ReceivedQty - d.Quantity);
                        dto.ShortageQty = Math.Max(0, d.Quantity - ack.ReceivedQty);
                        dto.DamagedQty = ack.DamagedQty;
                        dto.ReportedByName = ack.AcknowledgedByName;
                        dto.ReportedDate = ack.SubmittedAt;
                    }
                }
            }
            else if (n.RequestId is int reqId)
            {
                var req = await LoadAsync(reqId);
                if (req is not null)
                {
                    var pm = await ProductMaterialPartsAsync(req);
                    dto.Product = pm.Product;
                    dto.Material = pm.Material;
                    dto.RequestIndentNo = req.IndentNo;
                }
            }
            return Ok(dto);
        }

        [HttpPost("notifications/{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var ids = await (await MyNotificationsAsync()).Where(x => id == 0 || x.Id == id).Select(x => x.Id).ToListAsync();
            await _db.PublicityNotifications.Where(x => ids.Contains(x.Id) && x.ReadAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReadAt, DateTime.UtcNow));
            var count = await (await MyNotificationsAsync()).CountAsync(n => n.ReadAt == null);
            return Ok(new PubUnreadCountDto { Count = count });
        }

        // Called once a request's details have ALREADY loaded successfully (PublicityDetails.razor),
        // so opening IND-2026-0006 directly from the Submitted Request list - without ever visiting
        // Notifications - still acknowledges that request's own notifications. Same user-scoping
        // (MyNotificationsAsync()) and same idempotent ExecuteUpdateAsync-only-where-still-unread
        // pattern as MarkRead above, just filtered by RequestId instead of a single notification Id -
        // every PublicityNotification row always carries its real RequestId (never null), so this
        // reaches exactly the notifications about THIS request for THIS user, nothing from any other
        // request and nothing belonging to another user. A request with zero matching notifications
        // (or all already read) is a normal zero-row update, not an error.
        [HttpPost("notifications/by-request/{requestId:int}/read")]
        public async Task<IActionResult> MarkReadByRequest(int requestId)
        {
            var ids = await (await MyNotificationsAsync()).Where(x => x.RequestId == requestId).Select(x => x.Id).ToListAsync();
            await _db.PublicityNotifications.Where(x => ids.Contains(x.Id) && x.ReadAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReadAt, DateTime.UtcNow));
            var count = await (await MyNotificationsAsync()).CountAsync(n => n.ReadAt == null);
            return Ok(new PubUnreadCountDto { Count = count });
        }

        // ================================================================ derived state

        /// <summary>Stage / Status after final approval follow the dispatch + acknowledgement rows.</summary>
        private async Task RecomputeAsync(PublicityRequest r)
        {
            if (r.Status is PublicityStatus.Draft or PublicityStatus.Returned or PublicityStatus.Rejected
                or PublicityStatus.PendingApproval or PublicityStatus.PendingFinalApproval) return;
            await _db.SaveChangesAsync();
            var incoming = await _db.PublicityDispatches.AsNoTracking().Where(d => d.RequestId == r.Id).ToListAsync();
            var s = await SummaryAsync(r);
            var open = incoming.Where(d => d.Status != PublicityDispatchStatus.Acknowledged).ToList();
            var outgoingOpen = await _db.PublicityDispatches.AnyAsync(d => d.SourceRequestId == r.Id && d.Status < PublicityDispatchStatus.Dispatched);

            var remainingExcess = Math.Max(0, s.Excess - s.TransferredOut);
            // 2026-11-27 fix: this used to ALSO force "unresolved" forever once s.Damaged > 0 and
            // every dispatch was Acknowledged - s.Damaged is a running lifetime total (every
            // acknowledgement's DamagedQty ever recorded, including the ORIGINAL exception's), so it
            // never goes back to 0 even after a replacement dispatch fully covers the shortfall -
            // the request stayed stuck at "Exception Raised" forever once any damage was ever
            // reported, even when every required usable quantity had since been received. s.Outstanding
            // already fully captures whether damage still represents an unresolved problem (Usable =
            // Received - Damaged feeds directly into it, per-product since the fix above), so it alone
            // - plus genuinely unresolved excess still needing a transfer out - decides completion.
            // 2026-11-27 fix: "unresolved exception" used to mean "Outstanding>0 OR remainingExcess>0"
            // as a single flag, which kept the WHOLE request pinned at Exception Raised forever once any
            // excess was ever reported - even after every shortage/damage requirement was fully resolved
            // by a replacement, and even though excess-stock handling is a distinct obligation (giving
            // away surplus to ANOTHER request) from "did this destination receive what it needed" (this
            // request's own completion). Split so the destination's own requirement - s.Outstanding,
            // which already nets every ack's usable quantity against the requested quantity - solely
            // decides Exception Raised; a lingering unresolved EXCESS with nothing else outstanding
            // instead moves to the existing PublicityStatus.ExceptionResolved (already wired into the
            // Completed KPI and rendered green - see Stats()/PublicityApi.cs - but never actually
            // reached by any transition before this fix), so the request shows as effectively complete
            // everywhere while the Resolve Exception page (CanResolve, widened below) remains reachable
            // to actually transfer the surplus out. A GENUINELY unresolved shortage/damage requirement
            // still blocks completion exactly as before.
            var isExcessVerified = _db.PublicityEvents.Local.Any(e => e.RequestId == r.Id && e.Action == "Excess Verified")
                || await _db.PublicityEvents.AsNoTracking().AnyAsync(e => e.RequestId == r.Id && e.Action == "Excess Verified");
            var hasUnresolvedShortage = s.Outstanding > 0;
            var hasUnresolvedExcess = remainingExcess > 0;

            (PublicityStage, PublicityStatus) next;
            if (incoming.Count == 0) next = (PublicityStage.Dispatch, PublicityStatus.FinalApproved);
            else if (open.Any(d => d.Status < PublicityDispatchStatus.Dispatched)) next = (PublicityStage.InterHqTransfer, PublicityStatus.TransferInProgress);
            else if (open.Any(d => d.Status == PublicityDispatchStatus.Delivered)) next = (PublicityStage.Acknowledgement, PublicityStatus.AcknowledgementPending);
            else if (open.Any(d => d.Kind == PublicityDispatchKind.Transfer)) next = (PublicityStage.InterHqTransfer, PublicityStatus.TransferInProgress);
            else if (open.Any(d => d.Kind == PublicityDispatchKind.ReInitiation)) next = (PublicityStage.ReInitiation, PublicityStatus.ReInitiationInProgress);
            else if (open.Count > 0) next = (PublicityStage.Delivery, PublicityStatus.InDispatch);
            else if (hasUnresolvedShortage) next = (PublicityStage.ExceptionHandling, PublicityStatus.ExceptionRaised);
            else if (outgoingOpen) next = (PublicityStage.TransferCreation, PublicityStatus.TransferOrderReceived);
            else if (hasUnresolvedExcess && !isExcessVerified) next = (PublicityStage.ExceptionHandling, PublicityStatus.ExceptionRaised);
            else next = (PublicityStage.Completed, PublicityStatus.Completed);

            if (r.Stage != next.Item1 || r.Status != next.Item2)
            {
                (r.Stage, r.Status) = next;
                r.UpdatedAt = DateTime.UtcNow;
            }
        }

        private async Task<PubSummaryDto> SummaryAsync(PublicityRequest r)
        {
            var incoming = await _db.PublicityDispatches.AsNoTracking().Include(d => d.Acknowledgement)
                .Where(d => d.RequestId == r.Id).ToListAsync();
            var acks = incoming.Where(d => d.Acknowledgement is { IsDraft: false }).ToList();
            var s = new PubSummaryDto
            {
                Requested = r.Quantity,
                Dispatched = incoming.Sum(d => d.Quantity),
                OriginalDispatched = incoming.Where(d => d.Kind == PublicityDispatchKind.Original).Sum(d => d.Quantity),
                OriginalReceived = acks.Where(d => d.Kind == PublicityDispatchKind.Original).Sum(d => d.Acknowledgement!.ReceivedQty),
                ReplacementDispatched = incoming.Where(d => d.Kind != PublicityDispatchKind.Original).Sum(d => d.Quantity),
                ReplacementReceived = acks.Where(d => d.Kind != PublicityDispatchKind.Original).Sum(d => d.Acknowledgement!.ReceivedQty),
                Received = acks.Sum(d => d.Acknowledgement!.ReceivedQty),
                Damaged = acks.Sum(d => d.Acknowledgement!.DamagedQty),
                Shortage = acks.Sum(d => Math.Max(0, d.Quantity - d.Acknowledgement!.ReceivedQty)),
                TransferredOut = await _db.PublicityDispatches.Where(d => d.SourceRequestId == r.Id).SumAsync(d => (int?)d.Quantity) ?? 0
            };
            if (r.Items.Count > 1)
            {
                // Specific Product, multi-item request (2026-11-27 fix): the whole-request net below
                // (Usable = Received - Damaged, Outstanding = Requested - Usable) silently lets one
                // product's excess cancel a DIFFERENT product's shortage once summed across the whole
                // request - e.g. a 27-Nos excess on one product was masking a real 7-Nos shortage on
                // two others, showing "0 shortage, 20 excess" overall while the per-product table
                // correctly showed 7 shortage / 3 damaged / 27 excess. Products are not interchangeable
                // units, so every figure here is instead summed from each product's OWN independently-
                // clamped shortage/excess/outstanding (mirrors PubDispatchItemDto's per-item Shortage/
                // ExcessQty exactly, just totalled at the request level) - never netted against a
                // different product. Only dispatches with a SUBMITTED (non-draft) acknowledgement count,
                // same as every other figure in this method.
                var ackDispatchIds = acks.Select(d => d.Id).ToHashSet();
                var itemRows = await _db.PublicityDispatchItems.AsNoTracking()
                    .Where(x => ackDispatchIds.Contains(x.DispatchId) && x.RequestItemId != null)
                    .ToListAsync();
                var byItem = itemRows.GroupBy(x => x.RequestItemId!.Value)
                    .ToDictionary(g => g.Key, g => (Received: g.Sum(x => x.ReceivedQty), Damaged: g.Sum(x => x.DamagedQty)));

                int totalShortage = 0, totalExcess = 0, totalOutstanding = 0;
                foreach (var item in r.Items)
                {
                    var (received, damaged) = byItem.GetValueOrDefault(item.Id);
                    totalShortage += Math.Max(0, item.Quantity - received);
                    totalExcess += Math.Max(0, received - item.Quantity);
                    totalOutstanding += Math.Max(0, item.Quantity - (received - damaged));
                }
                s.Shortage = totalShortage;
                s.Excess = totalExcess;
                s.Outstanding = totalOutstanding;
                s.Usable = s.Received - s.Damaged;
            }
            else
            {
                s.Usable = s.Received - s.Damaged;
                s.Outstanding = Math.Max(0, s.Requested - s.Usable);
                s.Excess = Math.Max(0, s.Usable - s.Requested);
            }
            // 2026-11-27 fix: ExceptionType used to be built from s.Shortage/s.Damaged - both lifetime
            // sums across every ack ever submitted (original + every replacement) - so a fully-replaced
            // shortage/damage kept showing "Shortage"/"Damage" forever, even once Outstanding (which
            // already nets correctly across every ack) reached 0. s.Shortage/s.Damaged themselves are
            // left untouched (still the true historical totals shown elsewhere, e.g. Quantity Details) -
            // only the label driving Stage/Status/the KPI count is rederived from the current, already-
            // netted Outstanding/remaining-excess figures, mirrored into RowsAsync's own batched version
            // of this same formula for the list/consolidation screens.
            var excessVerifiedEvent = _db.PublicityEvents.Local
                .Where(e => e.RequestId == r.Id && e.Action == "Excess Verified")
                .OrderByDescending(e => e.At)
                .FirstOrDefault()
                ?? await _db.PublicityEvents.AsNoTracking()
                .Where(e => e.RequestId == r.Id && e.Action == "Excess Verified")
                .OrderByDescending(e => e.At)
                .FirstOrDefaultAsync();
            if (excessVerifiedEvent is not null)
            {
                s.ExcessVerified = true;
                s.ExcessVerifiedAt = excessVerifiedEvent.At;
                s.ExcessVerifiedByName = excessVerifiedEvent.ActorName;
            }

            var remainingExcess = Math.Max(0, s.Excess - s.TransferredOut);
            s.ExceptionType = ExceptionType(s.Outstanding, remainingExcess, s.Outstanding > 0 ? s.Damaged : 0);
            s.ExceptionStatus = s.ExceptionType == "None" ? "None"
                : s.ExceptionType == "Excess" ? (s.ExcessVerified ? "Resolved" : "Reported")
                : acks.Count == 0 ? "None" : s.Outstanding > 0 ? "Raised" : "Resolved";
            s.MaterialCondition = acks.Count == 0 ? "-"
                : s.Damaged == 0 && s.Shortage == 0 ? "Good"
                : s.Outstanding > 0 ? $"Partially received - {s.Damaged} Nos damaged, {s.Shortage} Nos shortage"
                : "Completed with replacement";
            return s;
        }

        // Per-product excess/remaining for ONE candidate source request, scoped to productId - never
        // nets against a different product. Single/-item sources keep using the existing whole-request
        // SummaryAsync figures (byte-for-byte unchanged). Multi-item sources compute this one item's own
        // received/excess from PublicityDispatchItem rows, and attribute "already transferred out" only
        // to transfers explicitly tagged (via a PublicityDispatchItem row) with this same ProductId;
        // a historical transfer created before this tagging existed (no item rows at all) is conservatively
        // charged against EVERY product of that multi-item source - this can only ever under-report
        // availability, never let two different products double-claim the same transferred stock.
        private async Task<(int Excess, int Remaining)> ProductExcessAsync(PublicityRequest source, int productId)
        {
            if (source.Items.Count > 1)
            {
                var item = source.Items.FirstOrDefault(i => i.ProductId == productId);
                if (item is null) return (0, 0);
                var ackIds = await _db.PublicityDispatches.AsNoTracking()
                    .Where(d => d.RequestId == source.Id && d.Acknowledgement != null && !d.Acknowledgement.IsDraft)
                    .Select(d => d.Id).ToListAsync();
                var received = await _db.PublicityDispatchItems.AsNoTracking()
                    .Where(x => ackIds.Contains(x.DispatchId) && x.RequestItemId == item.Id)
                    .SumAsync(x => (int?)x.ReceivedQty) ?? 0;
                var excess = Math.Max(0, received - item.Quantity);

                var outgoing = await _db.PublicityDispatches.AsNoTracking()
                    .Where(d => d.SourceRequestId == source.Id).ToListAsync();
                var outgoingIds = outgoing.Select(d => d.Id).ToList();
                var taggedDispatchIds = (await _db.PublicityDispatchItems.AsNoTracking()
                    .Where(x => outgoingIds.Contains(x.DispatchId)).Select(x => x.DispatchId).Distinct().ToListAsync()).ToHashSet();
                var taggedForThisProductIds = (await _db.PublicityDispatchItems.AsNoTracking()
                    .Where(x => outgoingIds.Contains(x.DispatchId) && x.ProductId == productId)
                    .Select(x => x.DispatchId).Distinct().ToListAsync()).ToHashSet();
                var transferredOut = outgoing.Where(d => taggedForThisProductIds.Contains(d.Id)).Sum(d => d.Quantity)
                    + outgoing.Where(d => !taggedDispatchIds.Contains(d.Id)).Sum(d => d.Quantity);
                return (excess, Math.Max(0, excess - transferredOut));
            }
            var s = await SummaryAsync(source);
            return (s.Excess, Math.Max(0, s.Excess - s.TransferredOut));
        }

        private async Task<List<PubExcessDto>> ExcessSourcesForProductAsync(PublicityRequest r, int productId)
        {
            var verifiedRequestIds = (await _db.PublicityEvents.AsNoTracking()
                .Where(e => e.Action == "Excess Verified")
                .Select(e => e.RequestId).Distinct().ToListAsync()).ToHashSet();

            var candidates = await _db.PublicityRequests.AsNoTracking().Where(x => !x.IsDeleted && x.Id != r.Id &&
                x.MaterialTypeId == r.MaterialTypeId &&
                (x.Status == PublicityStatus.Completed || x.Status == PublicityStatus.TransferOrderReceived
                    || x.Status == PublicityStatus.ExceptionRaised || x.Status == PublicityStatus.ExceptionResolved) &&
                (x.ProductId == productId || x.Items.Any(i => i.ProductId == productId)) &&
                verifiedRequestIds.Contains(x.Id)).Take(50).ToListAsync();
            var srcNames = await NamesAsync(candidates.Select(c => c.StateId), candidates.Select(c => c.RegionId), candidates.Select(c => c.HeadquarterId));
            var result = new List<PubExcessDto>();
            foreach (var c in candidates)
            {
                var (excess, remaining) = await ProductExcessAsync(c, productId);
                if (remaining > 0)
                {
                    var ackUser = await _db.PublicityAcknowledgements.AsNoTracking()
                        .Where(a => a.RequestId == c.Id && !a.IsDraft && a.AcknowledgedByName != null)
                        .Select(a => a.AcknowledgedByName).FirstOrDefaultAsync();

                    result.Add(new PubExcessDto
                    {
                        RequestId = c.Id, IndentNo = c.IndentNo, Hq = srcNames.Hqs.GetValueOrDefault(c.HeadquarterId, ""),
                        StateId = c.StateId, State = srcNames.States.GetValueOrDefault(c.StateId, ""),
                        RegionId = c.RegionId, Region = srcNames.Regions.GetValueOrDefault(c.RegionId, ""),
                        HqId = c.HeadquarterId,
                        ReportedExcess = excess, Remaining = remaining, LastUpdated = c.UpdatedAt,
                        SourceEmployeeName = ackUser ?? c.CreatedByName
                    });
                }
            }
            return result;
        }

        [HttpGet("requests/{id:int}/excess-sources")]
        public async Task<IActionResult> ExcessSources(int id, [FromQuery] int productId)
        {
            var r = await LoadAsync(id, tracking: false);
            if (r is null) return NotFound(new { Success = false, Message = "Request not found." });
            if (!await IsDispatcherAsync() || r.Status is not (PublicityStatus.ExceptionRaised or PublicityStatus.ExceptionResolved))
                return BadRequest(new { Success = false, Message = "This request cannot be resolved now." });
            var validProductIds = r.Items.Count > 0 ? r.Items.Select(i => i.ProductId).ToHashSet() : new HashSet<int> { r.ProductId };
            if (!validProductIds.Contains(productId)) return BadRequest(new { Success = false, Message = "Invalid product for this request." });
            return Ok(await ExcessSourcesForProductAsync(r, productId));
        }

        /// <summary>ADMIN-MD-CS verifies reported excess stock for an excess-only exception. The excess is
        /// retained at the holding HQ for future stock transfers and the request is marked Completed.</summary>
        [HttpPost("requests/{id:int}/verify-excess")]
        public async Task<IActionResult> VerifyExcess(int id)
        {
            if (!await IsDispatcherAsync()) return Forbid();
            var r = await LoadAsync(id, tracking: true);
            if (r is null) return NotFound(new { Success = false, Message = "Request not found." });

            var s = await SummaryAsync(r);
            if (s.Excess <= 0)
                return BadRequest(new { Success = false, Message = "This request has no reported excess stock." });
            if (s.Outstanding > 0)
                return BadRequest(new { Success = false, Message = "This request has an unresolved shortage or damage. Only excess-only exceptions can be verified directly." });

            var alreadyVerified = _db.PublicityEvents.Local.Any(e => e.RequestId == r.Id && e.Action == "Excess Verified")
                || await _db.PublicityEvents.AsNoTracking().AnyAsync(e => e.RequestId == r.Id && e.Action == "Excess Verified");
            if (!alreadyVerified)
            {
                var name = await MyNameAsync();
                var hqName = (await _db.Headquarters.AsNoTracking().Where(h => h.Id == r.HeadquarterId).Select(h => h.HeadquarterName).FirstOrDefaultAsync()) ?? "HQ";
                await AddEvent(r, PublicityStage.Completed, "Excess Verified", name,
                    $"Excess stock verified ({s.Excess} Nos at {hqName}) and retained for future stock transfers.");
                await _db.SaveChangesAsync();

                await RecomputeAsync(r);
                if (r.Status == PublicityStatus.Completed)
                {
                    await AddEvent(r, PublicityStage.Completed, "Completed", name,
                        "All required usable quantity received and excess stock verified.");
                }

                // Mark unread exception notifications for this request as read for ADMIN-MD-CS
                var notifIds = await (await MyNotificationsAsync())
                    .Where(x => x.RequestId == r.Id && x.Type == PublicityNotificationType.Exception && x.ReadAt == null)
                    .Select(x => x.Id)
                    .ToListAsync();
                if (notifIds.Count > 0)
                {
                    var readNow = DateTime.UtcNow;
                    await _db.PublicityNotifications
                        .Where(x => notifIds.Contains(x.Id) && x.ReadAt == null)
                        .ExecuteUpdateAsync(up => up.SetProperty(x => x.ReadAt, readNow));
                }

                await _db.SaveChangesAsync();
            }

            return Ok(new { Success = true, Message = "Excess stock verified successfully.", r.Status, r.Stage });
        }

        private static string ExceptionType(int shortage, int excess, int damaged)
        {
            var parts = new List<string>();
            if (shortage > 0) parts.Add("Shortage");
            if (excess > 0) parts.Add("Excess");
            if (damaged > 0) parts.Add("Damage");
            return parts.Count == 0 ? "None" : string.Join(" + ", parts);
        }

        // ================================================================ projections

        private async Task<List<PubRowDto>> RowsAsync(List<PublicityRequest> items)
        {
            var names = await NamesAsync(items.Select(r => r.StateId), items.Select(r => r.RegionId), items.Select(r => r.HeadquarterId));
            var requestIds = items.Select(r => r.Id).ToList();
            var itemsByReq = await _db.PublicityRequestItems.AsNoTracking()
                .Where(i => requestIds.Contains(i.RequestId))
                .ToListAsync();
            var allProductIds = items.Select(r => r.ProductId)
                .Concat(itemsByReq.Select(i => i.ProductId))
                .Distinct().ToList();
            var products = await _db.Products.AsNoTracking().Where(p => allProductIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);
            var materials = await _db.PublicityMaterialTypes.AsNoTracking().ToDictionaryAsync(m => m.Id, m => m);
            // One query feeds both the per-request design-file COUNT (DesignFilesCount, used by the
            // flat list/KPI screens) and the per-ITEM file (Items[].DesignFile*, used by consolidation's
            // nested product table and PubDetailDto.Items) - a design file is tagged with RequestItemId
            // only when it was uploaded against a specific item (see Upload's requestItemId param).
            var designFiles = await _db.PublicityFiles.AsNoTracking()
                .Where(f => requestIds.Contains(f.RequestId) && f.Kind == PublicityFileKind.Design)
                .ToListAsync();
            var designFilesCountByReq = designFiles.GroupBy(f => f.RequestId).ToDictionary(g => g.Key, g => g.Count());
            var itemFiles = designFiles.Where(f => f.RequestItemId.HasValue).GroupBy(f => f.RequestItemId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.Id).First());
            var mine = await MyStageAsync();
            var ackRows = await _db.PublicityAcknowledgements.AsNoTracking()
                .Include(a => a.Dispatch)
                .Where(a => requestIds.Contains(a.RequestId) && !a.IsDraft)
                .ToListAsync();
            var acksByReq = ackRows.GroupBy(a => a.RequestId).ToDictionary(g => g.Key, g => g.ToList());
            // 2026-11-27 fix: a request's exception label must reflect only what is STILL unresolved,
            // never a lifetime sum of every ack ever submitted - the previous per-ack-clamped sum above
            // (shortage/excess/damaged summed across the original AND every replacement ack) never
            // decreased once an exception occurred, so a fully-replaced shortage/damage kept showing
            // "Excess + Damage" forever even after a clean replacement ack fully covered the requirement.
            // Mirrors SummaryAsync's own Outstanding/Excess netting (batched here instead of called
            // per-row, since these list/consolidation entities never have r.Items eagerly loaded).
            var ackedDispatchIds = ackRows.Select(a => a.DispatchId).ToList();
            var itemAgg = (await _db.PublicityDispatchItems.AsNoTracking()
                    .Where(x => ackedDispatchIds.Contains(x.DispatchId) && x.RequestItemId != null)
                    .ToListAsync())
                .GroupBy(x => x.RequestItemId!.Value)
                .ToDictionary(g => g.Key, g => (Received: g.Sum(x => x.ReceivedQty), Damaged: g.Sum(x => x.DamagedQty)));
            var transferredOutByReq = (await _db.PublicityDispatches.AsNoTracking()
                    .Where(d => d.SourceRequestId != null && requestIds.Contains(d.SourceRequestId.Value))
                    .GroupBy(d => d.SourceRequestId!.Value)
                    .Select(g => new { RequestId = g.Key, Qty = g.Sum(d => d.Quantity) })
                    .ToListAsync())
                .ToDictionary(x => x.RequestId, x => x.Qty);
            var rmApprovedByName = await _db.PublicityEvents.AsNoTracking()
                .Where(e => requestIds.Contains(e.RequestId) &&
                    ((e.Stage == PublicityStage.RmReview && e.Action == "Approved") ||
                     (e.Stage == PublicityStage.IndentCreation && e.Action == "Submitted" && e.ActorRole == "RM")))
                .GroupBy(e => e.RequestId)
                .Select(g => new { RequestId = g.Key, Name = g.OrderByDescending(e => e.At).ThenByDescending(e => e.Id).First().ActorName })
                .ToDictionaryAsync(x => x.RequestId, x => x.Name);
            return items.Select(r =>
            {
                var reqItems = itemsByReq.Where(i => i.RequestId == r.Id).ToList();
                var mat = materials.GetValueOrDefault(r.MaterialTypeId);
                var category = mat?.Category ?? PublicityMaterialCategory.General;
                var reqAcks = acksByReq.GetValueOrDefault(r.Id) ?? new();
                string? excType = null;
                if (reqAcks.Count > 0)
                {
                    int outstanding, excess;
                    if (reqItems.Count > 1)
                    {
                        int totalOutstanding = 0, totalExcess = 0;
                        foreach (var item in reqItems)
                        {
                            var (received, damaged) = itemAgg.GetValueOrDefault(item.Id);
                            totalOutstanding += Math.Max(0, item.Quantity - (received - damaged));
                            totalExcess += Math.Max(0, received - item.Quantity);
                        }
                        outstanding = totalOutstanding;
                        excess = totalExcess;
                    }
                    else
                    {
                        var received = reqAcks.Sum(a => a.ReceivedQty);
                        var damaged = reqAcks.Sum(a => a.DamagedQty);
                        var usable = received - damaged;
                        outstanding = Math.Max(0, r.Quantity - usable);
                        excess = Math.Max(0, usable - r.Quantity);
                    }
                    var remainingExcess = Math.Max(0, excess - transferredOutByReq.GetValueOrDefault(r.Id));
                    var lifetimeDamaged = reqAcks.Sum(a => a.DamagedQty);
                    var rawType = ExceptionType(outstanding, remainingExcess, outstanding > 0 ? lifetimeDamaged : 0);
                    if (rawType != "None") excType = rawType;
                }
                return new PubRowDto
                {
                    Id = r.Id, IndentNo = r.IndentNo, Quantity = r.Quantity, OriginalQuantity = r.OriginalQuantity,
                    Product = products.GetValueOrDefault(r.ProductId, ""), Material = mat?.Name ?? "",
                    Hq = names.Hqs.GetValueOrDefault(r.HeadquarterId, ""), State = names.States.GetValueOrDefault(r.StateId, ""),
                    Region = names.Regions.GetValueOrDefault(r.RegionId, ""),
                    Stage = r.Stage, Status = r.Status, CreatedAt = r.CreatedAt,
                    AwaitingMe = IsPendingAt(r.Status, r.Stage, mine),
                    StatusNote = r.Status == PublicityStatus.Returned ? $"Return by {StageLabel(r.Stage).Replace(" Review", "")}" : null,
                    CreatedByName = r.CreatedByName,
                    RmApprovedByName = rmApprovedByName.GetValueOrDefault(r.Id),
                    SmdReviewedAt = r.SmdReviewedAt,
                    SmdReviewedByName = r.SmdReviewedByName,
                    ItemCount = reqItems.Count > 0 ? reqItems.Count : (r.ProductId > 0 ? 1 : 0),
                    DesignFilesCount = designFilesCountByReq.GetValueOrDefault(r.Id, 0),
                    MaterialCategory = category,
                    IsGiftArticle = r.IsGiftArticle,
                    ExceptionType = excType,
                    // The one fix this bug report is about: every caller of RowsAsync (the flat list,
                    // Stats, and - critically - BuildRegionGroupsAsync's consolidation rows) used to get
                    // an empty Items list, so a Specific Product (multi-item) request's consolidation
                    // row could only ever show r.Product - the parent's own mirrored "first item"
                    // field, never the other N-1 products. Populated for every row now, single- or
                    // multi-item alike (empty for a plain single-item request, same as before).
                    Items = reqItems.Select(i =>
                    {
                        var f = itemFiles.GetValueOrDefault(i.Id);
                        return new PubRequestItemDto
                        {
                            Id = i.Id,
                            ProductId = i.ProductId,
                            ProductName = products.GetValueOrDefault(i.ProductId, $"Product #{i.ProductId}"),
                            Quantity = i.Quantity,
                            OriginalQuantity = i.OriginalQuantity,
                            DesignFileId = f?.Id,
                            DesignFileName = f?.FileName,
                            DesignFilePath = f?.Path,
                            DesignFileSizeBytes = f?.SizeBytes ?? 0
                        };
                    }).ToList()
                };
            }).ToList();
        }

        private async Task<PubDetailDto> DetailAsync(PublicityRequest r)
        {
            var names = await NamesAsync(new[] { r.StateId }, new[] { r.RegionId }, new[] { r.HeadquarterId });
            var pm = await ProductMaterialPartsAsync(r);
            var events = r.Events.OrderBy(e => e.At).ThenBy(e => e.Id).ToList();
            var incoming = await _db.PublicityDispatches.AsNoTracking().Include(d => d.Acknowledgement)
                .Where(d => d.RequestId == r.Id).OrderBy(d => d.Id).ToListAsync();
            // an incoming transfer's courier proof is stored against the SOURCE request
            var incomingIds = incoming.Select(d => d.Id).ToList();
            var files = await _db.PublicityFiles.AsNoTracking()
                .Where(f => f.RequestId == r.Id || (f.DispatchId != null && incomingIds.Contains(f.DispatchId.Value)))
                .OrderBy(f => f.Id).ToListAsync();
            var outgoing = await _db.PublicityDispatches.AsNoTracking().Include(d => d.Acknowledgement)
                .Where(d => d.SourceRequestId == r.Id).OrderBy(d => d.Id).ToListAsync();
            var transferFiles = await _db.PublicityFiles.AsNoTracking().Where(f => f.DispatchId != null &&
                outgoing.Select(o => o.Id).Contains(f.DispatchId.Value)).ToListAsync();
            var requestedRole = r.RequestedByUserId is null ? null
                : RoleLabel(await _db.Users.AsNoTracking().Where(u => u.Id == r.RequestedByUserId).Select(u => (AppRole?)u.Role).FirstOrDefaultAsync());

            var canReview = r.Status is PublicityStatus.PendingApproval or PublicityStatus.PendingFinalApproval && await IsMyTurnAsync(r.Stage);
            var isDispatcher = await IsDispatcherAsync();

            var itemProductIds = r.Items.Select(i => i.ProductId).Distinct().ToList();
            var itemProducts = await _db.Products.AsNoTracking().Where(p => itemProductIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);
            var itemFiles = files.Where(f => f.RequestItemId.HasValue).GroupBy(f => f.RequestItemId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.Id).First());

            var summary = await SummaryAsync(r);
            var dto = new PubDetailDto
            {
                Id = r.Id, IndentNo = r.IndentNo, Stage = r.Stage, Status = r.Status, CreatedAt = r.CreatedAt,
                MaterialTypeId = r.MaterialTypeId, Material = pm.Material, ProductId = r.ProductId, Product = pm.Product,
                Quantity = r.Quantity, RequestedByUserId = r.RequestedByUserId, RequestedByName = r.RequestedByName ?? r.CreatedByName,
                RequestedByRole = requestedRole ?? "RMDO",
                StateId = r.StateId, RegionId = r.RegionId, HeadquarterId = r.HeadquarterId,
                State = names.States.GetValueOrDefault(r.StateId, ""), Region = names.Regions.GetValueOrDefault(r.RegionId, ""),
                Hq = names.Hqs.GetValueOrDefault(r.HeadquarterId, ""), Remarks = r.Remarks,
                MaterialCategory = r.MaterialType?.Category ?? PublicityMaterialCategory.General,
                IsGiftArticle = r.IsGiftArticle,
                Items = r.Items.Select(i =>
                {
                    var f = itemFiles.GetValueOrDefault(i.Id);
                    return new PubRequestItemDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductName = itemProducts.GetValueOrDefault(i.ProductId, $"Product #{i.ProductId}"),
                        Quantity = i.Quantity,
                        OriginalQuantity = i.OriginalQuantity,
                        DesignFileId = f?.Id,
                        DesignFileName = f?.FileName,
                        DesignFilePath = f?.Path,
                        DesignFileSizeBytes = f?.SizeBytes ?? 0
                    };
                }).ToList(),
                CanEdit = CanEdit(r),
                CanReview = canReview,
                CanDispatch = isDispatcher && r.Status == PublicityStatus.FinalApproved,
                // ExceptionResolved = shortage/damage already fully resolved, only a leftover excess-
                // stock transfer-out remains - Resolve Exception must stay reachable for that, even
                // though the request now reads as effectively complete everywhere else (see RecomputeAsync).
                // If excess has already been verified for an excess-only request, CanResolve stays true
                // for viewing historical verification state on the page.
                CanResolve = isDispatcher && (r.Status is PublicityStatus.ExceptionRaised or PublicityStatus.ExceptionResolved || (r.Status == PublicityStatus.Completed && summary.ExcessVerified)),
                SmdReviewedAt = r.SmdReviewedAt,
                SmdReviewedByName = r.SmdReviewedByName,
                Files = files.Concat(transferFiles).DistinctBy(f => f.Id).Select(ToFile).ToList(),
                Summary = summary
            };

            // owner shown in the header
            var last = events.LastOrDefault(e => e.ActorRole != null && e.ActorUserId != r.CreatedByUserId);
            if (r.Status is PublicityStatus.PendingApproval or PublicityStatus.PendingFinalApproval)
            {
                var who = await PersonForAsync(r.Stage, r);
                var stageLabel = StageOwnerLabel(r.Stage);
                dto.OwnerName = who ?? stageLabel;
                dto.OwnerRole = stageLabel;
            }
            else if (r.Stage >= PublicityStage.Dispatch)
            {
                var d = incoming.LastOrDefault();
                dto.OwnerCaption = "Dispatch Owner";
                dto.OwnerName = d?.CreatedByName ?? last?.ActorName ?? "ADMIN-MD CS";
                dto.OwnerRole = d is null ? last?.ActorRole ?? "ADMIN-MD CS"
                    : events.LastOrDefault(e => e.DispatchId == d.Id && e.ActorName == d.CreatedByName)?.ActorRole ?? "ADMIN-MD CS";
            }
            else
            {
                dto.OwnerName = last?.ActorName ?? r.CreatedByName;
                dto.OwnerRole = last?.ActorRole ?? "RMDO";
            }

            var exceptionEvent = events.LastOrDefault(e => e.Action == "Exception Raised");
            dto.ExceptionRaisedBy = exceptionEvent is null ? null : $"{exceptionEvent.ActorName} ({exceptionEvent.ActorRole})";
            dto.ExceptionRaisedAt = exceptionEvent?.At;
            dto.ExceptionRemarks = exceptionEvent?.Remarks;
            dto.ApproverRemarks = events.LastOrDefault(e => e.Stage is >= PublicityStage.RmReview and <= PublicityStage.DgmMdCsReview
                && !string.IsNullOrWhiteSpace(e.Remarks))?.Remarks;

            dto.Timeline = BuildTimeline(r, events, incoming);
            dto.History = BuildHistory(r, events);
            foreach (var d in incoming) dto.Dispatches.Add(await DispatchDtoAsync(d, r, files, names.Hqs.GetValueOrDefault(r.HeadquarterId, "")));
            foreach (var d in outgoing)
            {
                var dest = await LoadAsync(d.RequestId);
                var destHq = (await NamesAsync(Array.Empty<int>(), Array.Empty<int>(), new[] { dest!.HeadquarterId })).Hqs.GetValueOrDefault(dest.HeadquarterId, "");
                var x = await DispatchDtoAsync(d, dest, transferFiles, destHq);
                x.CanSubmitProof = await CanSubmitProofAsync(d);
                dto.OutgoingTransfers.Add(x);
            }

            if (dto.CanDispatch || dto.CanResolve)
            {
                dto.KnownAddresses = await _db.PublicityDispatches.AsNoTracking()
                    .Where(d => d.DestinationAddress != null && d.Request!.HeadquarterId == r.HeadquarterId)
                    .Select(d => d.DestinationAddress!).Distinct().Take(20).ToListAsync();
            }
            if (dto.CanResolve && dto.Summary.Outstanding > 0)
            {
                var verifiedRequestIds = (await _db.PublicityEvents.AsNoTracking()
                    .Where(e => e.Action == "Excess Verified")
                    .Select(e => e.RequestId).Distinct().ToListAsync()).ToHashSet();

                var candidates = await _db.PublicityRequests.AsNoTracking().Where(x => !x.IsDeleted && x.Id != r.Id &&
                    x.ProductId == r.ProductId && x.MaterialTypeId == r.MaterialTypeId &&
                    (x.Status == PublicityStatus.Completed || x.Status == PublicityStatus.TransferOrderReceived
                    || x.Status == PublicityStatus.ExceptionRaised || x.Status == PublicityStatus.ExceptionResolved) &&
                    verifiedRequestIds.Contains(x.Id)).Take(50).ToListAsync();
                var srcNames = await NamesAsync(candidates.Select(c => c.StateId), candidates.Select(c => c.RegionId), candidates.Select(c => c.HeadquarterId));
                foreach (var c in candidates)
                {
                    var s = await SummaryAsync(c);
                    var remaining = s.Excess - s.TransferredOut;
                    if (remaining > 0)
                    {
                        var ackUser = await _db.PublicityAcknowledgements.AsNoTracking()
                            .Where(a => a.RequestId == c.Id && !a.IsDraft && a.AcknowledgedByName != null)
                            .Select(a => a.AcknowledgedByName).FirstOrDefaultAsync();

                        dto.ExcessSources.Add(new PubExcessDto
                        {
                            RequestId = c.Id, IndentNo = c.IndentNo, Hq = srcNames.Hqs.GetValueOrDefault(c.HeadquarterId, ""),
                            StateId = c.StateId, State = srcNames.States.GetValueOrDefault(c.StateId, ""),
                            RegionId = c.RegionId, Region = srcNames.Regions.GetValueOrDefault(c.RegionId, ""),
                            HqId = c.HeadquarterId,
                            ReportedExcess = s.Excess, Remaining = remaining, LastUpdated = c.UpdatedAt,
                            SourceEmployeeName = ackUser ?? c.CreatedByName
                        });
                    }
                }
            }
            return dto;
        }

        private async Task<PubDispatchDto> DispatchDtoAsync(PublicityDispatch d, PublicityRequest dest, List<PublicityFile> files, string destHq)
        {
            var source = d.SourceRequestId is int sid ? await LoadAsync(sid) : null;
            var sourceHq = source is null ? null
                : (await NamesAsync(Array.Empty<int>(), Array.Empty<int>(), new[] { source.HeadquarterId })).Hqs.GetValueOrDefault(source.HeadquarterId);
            var destNames = await NamesAsync(new[] { dest.StateId }, new[] { dest.RegionId }, Array.Empty<int>());
            // A Transfer dispatch never collects its own delivery address (CreateDispatch's Transfer
            // branch has no address step) - fall back to whatever address the destination request's own
            // most recent dispatch was actually sent to, so the source RMDO creating the transfer knows
            // where to physically ship without guessing or fabricating a new one.
            var destinationAddress = d.DestinationAddress ?? (d.Kind == PublicityDispatchKind.Transfer
                ? await _db.PublicityDispatches.AsNoTracking()
                    .Where(x => x.RequestId == dest.Id && x.DestinationAddress != null)
                    .OrderByDescending(x => x.CreatedAt).Select(x => x.DestinationAddress).FirstOrDefaultAsync()
                : null);
            var a = d.Acknowledgement;
            var dto = new PubDispatchDto
            {
                Id = d.Id, DispatchNo = d.DispatchNo, RequestId = d.RequestId, RequestIndentNo = dest.IndentNo, Kind = d.Kind, Status = d.Status,
                SourceName = d.Kind == PublicityDispatchKind.Transfer ? sourceHq ?? "Source HQ" : d.VendorName ?? "Vendor",
                SourceHqId = d.SourceHqId, SourceRequestId = d.SourceRequestId, SourceIndentNo = source?.IndentNo,
                DestinationHq = destHq, DestinationState = destNames.States.GetValueOrDefault(dest.StateId), DestinationRegion = destNames.Regions.GetValueOrDefault(dest.RegionId),
                DestinationAddress = destinationAddress, DeliveryAddressId = d.DeliveryAddressId, Quantity = d.Quantity,
                Transporter = d.Transporter, TrackingNo = d.TrackingNo, VehicleNo = d.VehicleNo,
                ResponsibleUserId = d.ResponsibleUserId, ResponsibleName = d.ResponsibleName, ResponsibleRole = d.ResponsibleRole,
                DispatchDate = d.DispatchDate, ExpectedDeliveryDate = d.ExpectedDeliveryDate, CurrentLocation = d.CurrentLocation,
                DeliveredAt = d.DeliveredAt, Remarks = d.Remarks, CreatedByName = d.CreatedByName, CreatedAt = d.CreatedAt,
                ProofByName = d.ProofByName, ProofAt = d.ProofAt,
                Files = files.Where(f => f.DispatchId == d.Id).Select(ToFile).ToList(),
                CanAcknowledge = await CanAcknowledgeAsync(dest, d),
                Ack = a is null ? null : new PubAckDto
                {
                    Id = a.Id, ReceivedQty = a.ReceivedQty, IsDamaged = a.IsDamaged, DamagedQty = a.DamagedQty,
                    ShortageQty = Math.Max(0, d.Quantity - a.ReceivedQty), ExcessQty = Math.Max(0, a.ReceivedQty - d.Quantity),
                    AcknowledgedByUserId = a.AcknowledgedByUserId, AcknowledgedByName = a.AcknowledgedByName, Remarks = a.Remarks,
                    IsDraft = a.IsDraft, SubmittedAt = a.SubmittedAt,
                    ExceptionType = ExceptionType(Math.Max(0, d.Quantity - a.ReceivedQty), Math.Max(0, a.ReceivedQty - d.Quantity), a.DamagedQty)
                }
            };

            // Product-wise breakdown (Specific Product with line items - dest.Items is
            // empty for General Product/Gift Articles requests, so dto.Items stays
            // empty and the client falls back to the single-quantity form exactly as before).
            if (dest.Items.Count > 0)
            {
                var productIds = dest.Items.Select(i => i.ProductId).Distinct().ToList();
                var productNames = await _db.Products.AsNoTracking().Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);
                var existingRows = (await _db.PublicityDispatchItems.AsNoTracking().Where(x => x.DispatchId == d.Id).ToListAsync())
                    .Where(x => x.RequestItemId.HasValue).ToDictionary(x => x.RequestItemId!.Value);
                // "Already received before THIS dispatch" (not keyed by draft/submitted - a draft
                // row's ReceivedQty is 0 or whatever was typed and not yet counted against anyone
                // else) - only OTHER dispatches' rows count, so this dispatch's own live preview /
                // stored row is never self-referential.
                var requestDispatchIds = await _db.PublicityDispatches.AsNoTracking()
                    .Where(x => x.RequestId == dest.Id).Select(x => x.Id).ToListAsync();
                var priorReceivedByItem = await _db.PublicityDispatchItems.AsNoTracking()
                    .Where(x => requestDispatchIds.Contains(x.DispatchId) && x.DispatchId != d.Id && x.RequestItemId != null)
                    .GroupBy(x => x.RequestItemId!.Value)
                    .Select(g => new { RequestItemId = g.Key, Received = g.Sum(x => x.ReceivedQty) })
                    .ToDictionaryAsync(x => x.RequestItemId, x => x.Received);
                // 2026-11-27 fix: a dispatch that only replaces SOME products (a partial re-
                // initiation) must only ever show those products here - the very first dispatch of
                // the request still shows every item (even a 0-approved one), matching Acknowledge's
                // identical applicability rule so the read view and the write validation never disagree.
                var isFirstDispatch = requestDispatchIds.Count == 0 || d.Id == requestDispatchIds.Min();
                dto.Items = dest.Items.Where(i => isFirstDispatch || existingRows.ContainsKey(i.Id)
                        || Math.Max(0, i.Quantity - priorReceivedByItem.GetValueOrDefault(i.Id)) > 0)
                    .Select(i =>
                {
                    var productName = productNames.GetValueOrDefault(i.ProductId, $"Product #{i.ProductId}");
                    if (existingRows.TryGetValue(i.Id, out var row))
                        return new PubDispatchItemDto
                        {
                            Id = row.Id, RequestItemId = i.Id, ProductId = i.ProductId, ProductName = productName,
                            DispatchedQty = row.DispatchedQty, ReceivedQty = row.ReceivedQty, DamagedQty = row.DamagedQty,
                            IsDamaged = row.DamagedQty > 0, Remarks = row.Remarks
                        };
                    // Never yet acknowledged on THIS dispatch - a live preview so the form has a row
                    // and a correct Dispatched Qty to show before the user types anything.
                    var dispatchedQty = Math.Max(0, i.Quantity - priorReceivedByItem.GetValueOrDefault(i.Id));
                    return new PubDispatchItemDto { RequestItemId = i.Id, ProductId = i.ProductId, ProductName = productName, DispatchedQty = dispatchedQty };
                }).ToList();
            }
            var acked = a is { IsDraft: false };
            var ackPill = !acked ? "Pending" : dto.Ack!.ExceptionType == "None" ? "Completed" : dto.Ack!.ExceptionType == "Excess" ? "Excess Reported" : "Exception Raised";
            var ackState = !acked ? 0 : dto.Ack!.ExceptionType == "None" ? 2 : 3;
            if (d.Kind == PublicityDispatchKind.Transfer)
            {
                dto.Steps = Steps(
                    ("Transfer Request Sent", $"By {d.CreatedByName}", d.CreatedAt, true),
                    ("Proof Submitted", d.ProofByName is null ? "Pending from RMDO" : $"By {d.ProofByName}", d.ProofAt, d.Status >= PublicityDispatchStatus.ProofSubmitted),
                    ("Admin Review", d.Status >= PublicityDispatchStatus.Dispatched ? "Reviewed" : d.Status == PublicityDispatchStatus.ProofSubmitted ? "In Progress" : "Pending", null, d.Status >= PublicityDispatchStatus.Dispatched),
                    ("Transfer Created", d.Transporter, d.Status >= PublicityDispatchStatus.Dispatched ? d.DispatchDate : null, d.Status >= PublicityDispatchStatus.Dispatched),
                    ("In Transit", d.CurrentLocation ?? (d.ExpectedDeliveryDate is { } e ? $"Expected Delivery {e:dd MMM yyyy}" : null), d.InTransitAt, d.Status >= PublicityDispatchStatus.InTransit),
                    ("Acknowledgement", ackPill, a?.SubmittedAt, acked));
                // The current/active milestone (e.g. "Proof Submitted" while proof is still pending)
                // must never render with the same checkmark icon as a completed one - only the State==1
                // (current, not done) step gets the spinner, same convention as the 3-step timeline below.
                var currentStep = dto.Steps.FirstOrDefault(s => s.State == 1);
                if (currentStep is not null) currentStep.UseSpinner = true;
            }
            else
            {
                // Three-step delivery timeline (Dispatched -> In Transit -> Acknowledged) - "In
                // Transit" represents dispatched material awaiting acknowledgement in general, not a
                // literal courier-verified scan, so its Done flag is keyed off the same `acked` signal
                // as the Acknowledged step itself (not the separate InTransit/Delivered enum values,
                // which nothing advances anymore now that Mark In Transit / Mark Delivered are removed
                // from this kind's UI - see PublicityDetails.razor).
                dto.Steps = Steps(
                    ("Dispatched", $"Material handed over to {d.Transporter}", d.CreatedAt, true),
                    ("In Transit", d.CurrentLocation ?? (d.ExpectedDeliveryDate is { } e ? $"Expected Delivery {e:dd MMM yyyy}" : null), d.InTransitAt, acked),
                    ("Acknowledged", ackPill, a?.SubmittedAt, acked));
                if (dto.Steps[1].State == 1) dto.Steps[1].UseSpinner = true;
            }
            if (acked) dto.Steps[^1].State = ackState;
            return dto;
        }

        /// <summary>Done steps get state 2, the first not-done step is current (1), the rest pending (0).</summary>
        private static List<PubStepDto> Steps(params (string Label, string? Pill, DateTime? At, bool Done)[] steps)
        {
            var list = new List<PubStepDto>();
            var currentSet = false;
            foreach (var s in steps)
            {
                var state = s.Done ? 2 : currentSet ? 0 : 1;
                if (!s.Done) currentSet = true;
                list.Add(new PubStepDto { Label = s.Label, Pill = s.Done ? s.Pill : state == 1 ? s.Pill ?? "In Progress" : "Pending", At = s.Done ? s.At : null, State = state });
            }
            return list;
        }

        private static List<PubStepDto> BuildTimeline(PublicityRequest r, List<PublicityEvent> events, List<PublicityDispatch> incoming)
        {
            PublicityEvent? Last(PublicityStage st) => events.LastOrDefault(e => e.Stage == st);
            var submitted = r.Status != PublicityStatus.Draft;
            var steps = new List<PubStepDto>
            {
                new() { Label = "Indent Creation", Pill = submitted ? "Completed" : "Draft", At = r.CreatedAt, State = submitted ? 2 : 1, Note = $"By: {r.CreatedByName}" }
            };
            var labels = new[] { "RM Review", "SMD Review", "SMM Review", "ADMIN-MD-CS", "DGM-MD-CS" };
            for (var i = 0; i < ChainStages.Length; i++)
            {
                var ev = Last(ChainStages[i]);
                var passed = r.Stage > ChainStages[i] && r.Status is not (PublicityStatus.Draft);
                var here = r.Stage == ChainStages[i] && submitted;
                steps.Add(new PubStepDto
                {
                    Label = labels[i],
                    Pill = passed ? (i == ChainStages.Length - 1 ? "Final Approved" : "Approved")
                        : here ? r.Status switch { PublicityStatus.Returned => "Returned", PublicityStatus.Rejected => "Rejected", _ => "Pending Approval" } : "Pending",
                    At = passed || here ? ev?.At : null,
                    State = passed ? 2 : here ? (r.Status is PublicityStatus.Returned or PublicityStatus.Rejected ? 3 : 1) : 0
                });
            }
            var approved = r.Stage >= PublicityStage.Dispatch;
            var dispatched = incoming.Count > 0;
            var anyAck = incoming.Any(d => d.Status == PublicityDispatchStatus.Acknowledged);
            // ExceptionResolved counts as done here too - shortage/damage is fully resolved, only a
            // leftover excess-stock transfer-out remains (see RecomputeAsync/Stats' Completed KPI).
            var done = r.Status is PublicityStatus.Completed or PublicityStatus.ExceptionResolved;
            steps.Add(new PubStepDto
            {
                Label = "Dispatch",
                Pill = !dispatched ? (approved ? "Pending Dispatch" : "Pending")
                    : incoming.All(d => d.Status >= PublicityDispatchStatus.Delivered) ? "Delivered"
                    : incoming.Any(d => d.Status >= PublicityDispatchStatus.InTransit) ? "In Transit" : "Dispatched",
                At = incoming.FirstOrDefault()?.CreatedAt, State = dispatched ? 2 : approved ? 1 : 0
            });
            steps.Add(new PubStepDto
            {
                Label = "Acknowledgement",
                Pill = anyAck ? (r.Status == PublicityStatus.ExceptionRaised ? "Exception Raised" : "Completed") : dispatched ? "Pending" : "Pending",
                At = Last(PublicityStage.Acknowledgement)?.At,
                State = anyAck ? (r.Status is PublicityStatus.ExceptionRaised ? 3 : 2) : dispatched ? 1 : 0
            });
            steps.Add(new PubStepDto
            {
                Label = "Completion", Pill = done ? "Completed" : "Pending", At = done ? Last(PublicityStage.Completed)?.At ?? r.UpdatedAt : null,
                State = done ? 2 : anyAck && !done ? 1 : 0
            });
            return steps;
        }

        private static List<PubHistoryDto> BuildHistory(PublicityRequest r, List<PublicityEvent> events)
        {
            string Label(PublicityEvent e) => e.Action switch
            {
                "Submitted" => "Submitted to RM",
                "Note Approved" => "Note Approval",
                _ => e.Stage switch
                {
                    PublicityStage.IndentCreation => "Indent Creation",
                    PublicityStage.Delivery or PublicityStage.Dispatch or PublicityStage.ReInitiation => "Dispatch",
                    PublicityStage.InterHqTransfer or PublicityStage.TransferCreation => "Inter-HQ Transfer",
                    _ => StageLabel(e.Stage)
                }
            };
            var list = events.Select(e => new PubHistoryDto
            {
                Stage = Label(e), Action = e.Action, By = e.ActorName, Role = e.ActorRole, At = e.At, Remarks = e.Remarks,
                State = e.Action is "Edited" or "Returned" or "Exception Raised" ? 1 : e.Action == "Rejected" ? 3 : 2
            }).ToList();
            // Each label's own stage - used below to tell a stage the request hasn't reached YET
            // apart from one it has already passed, whether via a real persisted event or because a
            // chain role (e.g. the RM itself) created the request and skipped every stage at or below
            // its own rung (see Submit()'s creatorIndex doc comment). A skipped stage is "behind" the
            // request's current position, not "ahead" of it, so it must never render as Not Started -
            // that label is reserved for stages genuinely still to come.
            var sequence = new[]
            {
                ("Indent Creation", PublicityStage.IndentCreation), ("Submitted to RM", PublicityStage.IndentCreation),
                ("RM Review", PublicityStage.RmReview), ("SMD Review", PublicityStage.SmdReview),
                ("SMM Review", PublicityStage.SmmReview), ("ADMIN - MD CS Review", PublicityStage.AdminMdCsReview),
                ("DGM - MD CS Review", PublicityStage.DgmMdCsReview), ("Note Approval", PublicityStage.Dispatch),
                ("Dispatch", PublicityStage.Dispatch), ("Acknowledgement", PublicityStage.Acknowledgement)
            };
            var current = r.Status is PublicityStatus.PendingApproval or PublicityStatus.PendingFinalApproval ? StageLabel(r.Stage)
                : r.Status == PublicityStatus.FinalApproved ? "Note Approval"
                : r.Status is PublicityStatus.InDispatch or PublicityStatus.AcknowledgementPending ? "Acknowledgement" : null;
            // the stage waiting now always gets its own "Pending Review" row (it may already have
            // older rows, e.g. RM Review after a return and resubmit); then the stages not reached yet
            if (current != null) list.Add(new PubHistoryDto { Stage = current, Action = "Pending Review", State = 1 });
            foreach (var (label, stage) in sequence.Where(x => x.Item2 > r.Stage && x.Item1 != current && list.All(h => h.Stage != x.Item1)))
                list.Add(new PubHistoryDto { Stage = label, Action = "Not Started", State = 0 });
            return list;
        }

        // ================================================================ rules

        private bool CanEdit(PublicityRequest r) =>
            r.Status is PublicityStatus.Draft or PublicityStatus.Returned && (r.CreatedByUserId == Uid() || IsSuper());

        private async Task<bool> IsMyTurnAsync(PublicityStage stage)
        {
            if (IsSuper()) return ChainStages.Contains(stage);
            return stage switch
            {
                PublicityStage.RmReview => MyRole() == AppRole.RM,
                PublicityStage.SmdReview => MyRole() == AppRole.SMD,
                PublicityStage.SmmReview => MyRole() == AppRole.SMM,
                PublicityStage.AdminMdCsReview => await IsAdminMdCsAsync(),
                PublicityStage.DgmMdCsReview => await IsDgmMdCsAsync(),
                _ => false
            };
        }

        private async Task<PublicityStage?> MyStageAsync()
        {
            if (MyRole() == AppRole.RM) return PublicityStage.RmReview;
            if (MyRole() == AppRole.SMD) return PublicityStage.SmdReview;
            if (MyRole() == AppRole.SMM) return PublicityStage.SmmReview;
            if (await IsAdminMdCsAsync()) return PublicityStage.AdminMdCsReview;
            if (await IsDgmMdCsAsync()) return PublicityStage.DgmMdCsReview;
            return null;
        }

        private static bool IsPendingAt(PublicityStatus status, PublicityStage stage, PublicityStage? mine) =>
            mine != null && stage == mine && status is PublicityStatus.PendingApproval or PublicityStatus.PendingFinalApproval;

        private async Task<bool> IsDispatcherAsync() =>
            MyRole() is AppRole.Admin or AppRole.SuperAdmin or AppRole.CorporateAdmin
            || await IsAdminMdCsAsync() || await IsDgmMdCsAsync();

        private bool IsSuper() => MyRole() is AppRole r && SuperRoles.Contains(r);

        private async Task<bool> CanAcknowledgeAsync(PublicityRequest r, PublicityDispatch d)
        {
            if (d.Status is not (PublicityDispatchStatus.Dispatched or PublicityDispatchStatus.InTransit or PublicityDispatchStatus.Delivered)) return false;
            if (IsSuper()) return true;
            if (d.Kind != PublicityDispatchKind.Transfer)
                // Original / ReInitiation: only the specifically assigned Responsible Person may
                // acknowledge - never the original requester/creator and never "any RMD/MDO in the
                // HQ" (the Requested By employee and the dispatch's Responsible Person are not always
                // the same person, per this workflow's own authorization requirement).
                return d.ResponsibleUserId == Uid();
            // Transfer kind has no single named Responsible Person (SetResponsibleAsync is never
            // called for it in CreateDispatch) - preserve its existing destination-HQ rule unchanged.
            if (r.CreatedByUserId == Uid() || r.RequestedByUserId == Uid()) return true;
            return (MyRole() is AppRole.RMD or AppRole.MDO) && (await MyHqIdsAsync()).Contains(r.HeadquarterId);
        }

        private async Task<bool> CanSubmitProofAsync(PublicityDispatch d)
        {
            if (d.Kind != PublicityDispatchKind.Transfer || d.Status > PublicityDispatchStatus.ProofSubmitted) return false;
            if (IsSuper()) return true;
            var source = d.SourceRequestId is int sid ? await _db.PublicityRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sid) : null;
            if (source?.CreatedByUserId == Uid()) return true;
            return MyRole() is AppRole.RMD or AppRole.MDO && d.SourceHqId is int hq && (await MyHqIdsAsync()).Contains(hq);
        }

        /// <summary>Field roles see their HQs (and transfers sourced from them), RM its region,
        /// SMD / SMM their state, MD CS and admins everything. Drafts are private.</summary>
        private async Task<IQueryable<PublicityRequest>> ScopedAsync()
        {
            var me = Uid();
            var q = _db.PublicityRequests.AsNoTracking().Where(r => !r.IsDeleted && (r.Status != PublicityStatus.Draft || r.CreatedByUserId == me));
            // ADMIN-MD CS / DGM-MD CS are CommonRole users: check before the switch below, so they
            // get "MD CS and admins everything" instead of falling into the RMDO-style HQ bucket that
            // AppRole.CommonRole would otherwise hit (that bucket stays for any other CommonRole use).
            if (MyRole() == AppRole.CommonRole && (await IsAdminMdCsAsync() || await IsDgmMdCsAsync())) return q;
            switch (MyRole())
            {
                case AppRole.RM:
                    var region = ClaimInt("spic:region_id");
                    return q.Where(r => r.RegionId == region || r.CreatedByUserId == me);
                case AppRole.SMD or AppRole.SMM:
                    var state = ClaimInt("spic:state_id");
                    return q.Where(r => r.StateId == state || r.CreatedByUserId == me);
                case AppRole.RMD or AppRole.MDO or AppRole.MO or AppRole.JMDO or AppRole.CommonRole:
                    var hqs = await MyHqIdsAsync();
                    return q.Where(r => r.CreatedByUserId == me || r.RequestedByUserId == me || hqs.Contains(r.HeadquarterId));
                default:
                    return q;
            }
        }

        private async Task<bool> CanReadAsync(PublicityRequest r)
        {
            if (await (await ScopedAsync()).AnyAsync(x => x.Id == r.Id)) return true;
            // a source HQ reads the destination request of a transfer it has to ship
            var hqs = await MyHqIdsAsync();
            return await _db.PublicityDispatches.AnyAsync(d => d.RequestId == r.Id && d.SourceHqId != null && hqs.Contains(d.SourceHqId.Value));
        }

        private List<int>? _hqs;

        private async Task<List<int>> MyHqIdsAsync()
        {
            if (_hqs != null) return _hqs;
            var set = new HashSet<int>();
            var hq = ClaimInt("spic:hq_id");
            if (hq > 0) set.Add(hq);
            var empId = await _db.Employeelogins.AsNoTracking().Where(e => e.UserId == Uid()).Select(e => e.EmployeeInformationID).FirstOrDefaultAsync();
            if (empId > 0)
            {
                foreach (var csv in await _db.RMDHQMappings.AsNoTracking().Where(m => m.EmployeeId == empId).Select(m => m.HeadquartersIds).ToListAsync())
                    foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        if (int.TryParse(part.Trim(), out var id)) set.Add(id);
            }
            return _hqs = set.ToList();
        }

        private async Task<IQueryable<PublicityNotification>> MyNotificationsAsync()
        {
            var me = Uid();
            var role = MyRole();
            var hqs = await MyHqIdsAsync();
            // Notify() always writes AppRole.RMD / AppRole.MDAdmin as TargetRole; match it against
            // whichever AppRole/designation combo actually plays that part today (RMDO = RMD or MDO;
            // ADMIN-MD CS / DGM-MD CS = CommonRole + that designation), not a literal enum equality.
            var matchingRoles = new List<AppRole>();
            if (role.HasValue) matchingRoles.Add(role.Value);
            if (role is AppRole.RMD or AppRole.MDO) { matchingRoles.Add(AppRole.RMD); matchingRoles.Add(AppRole.MDO); }
            if (role == AppRole.CommonRole)
            {
                if (await IsAdminMdCsAsync()) matchingRoles.Add(AppRole.MDAdmin);
                if (await IsDgmMdCsAsync()) matchingRoles.Add(AppRole.MDHOD);
            }
            return _db.PublicityNotifications.AsNoTracking().Where(n => n.TargetUserId == me ||
                (n.TargetRole != null && matchingRoles.Contains(n.TargetRole.Value) && (n.TargetHqId == null || hqs.Contains(n.TargetHqId.Value))));
        }

        // ================================================================ write helpers

        private async Task<string?> ValidateAsync(PubUpsertDto dto, int? requestId = null)
        {
            if (dto.Quantity < 0) return "Enter the required quantity.";
            var material = await _db.PublicityMaterialTypes.AsNoTracking()
                .Where(m => m.Id == dto.MaterialTypeId).Select(m => new { m.IsActive }).FirstOrDefaultAsync();
            if (material is null) return "Select the type of publicity material.";
            if (!material.IsActive)
            {
                // Deactivated materials are rejected for a brand-new pick, but an existing request
                // that already held this (now-inactive) material before the edit must still be
                // allowed to save unrelated changes without being forced to switch it - see
                // PublicityMaterialMaster's deactivate action and PublicityForm's historical-material
                // handling (PubDetailDto.Material resolves the name with no IsActive filter either).
                var unchanged = requestId.HasValue && await _db.PublicityRequests.AsNoTracking()
                    .AnyAsync(r => r.Id == requestId.Value && r.MaterialTypeId == dto.MaterialTypeId);
                if (!unchanged) return "The selected publicity material type is no longer active. Please choose another.";
            }
            if (!await _db.Products.AnyAsync(p => p.Id == dto.ProductId)) return "Select the product.";
            if (!await _db.Headquarters.AnyAsync(h => h.Id == dto.HeadquarterId && h.RegionId == dto.RegionId)) return "Select the headquarter.";
            if (!await _db.Regions.AnyAsync(x => x.Id == dto.RegionId && x.StateId == dto.StateId)) return "Select the region.";

            if (!string.IsNullOrWhiteSpace(dto.RequestedByUserId) && dto.RequestedByUserId != Uid())
            {
                bool isExistingMatch = false;
                if (requestId.HasValue && requestId.Value > 0)
                {
                    isExistingMatch = await _db.PublicityRequests.AsNoTracking()
                        .AnyAsync(r => r.Id == requestId.Value && r.RequestedByUserId == dto.RequestedByUserId);
                }

                if (!isExistingMatch)
                {
                    var allowed = await GetAuthorizedPeopleAsync();
                    if (allowed.All(p => p.UserId != dto.RequestedByUserId))
                    {
                        return "The selected requested-by employee is not authorized under your hierarchy.";
                    }
                }
            }

            return null;
        }

        private async Task ApplyAsync(PublicityRequest r, PubUpsertDto dto)
        {
            r.MaterialTypeId = dto.MaterialTypeId;
            r.ProductId = dto.ProductId;
            r.Quantity = dto.Quantity;
            r.StateId = dto.StateId;
            r.RegionId = dto.RegionId;
            r.HeadquarterId = dto.HeadquarterId;
            r.Remarks = Clean(dto.Remarks);
            r.IsGiftArticle = dto.IsGiftArticle;
            r.RequestedByUserId = string.IsNullOrWhiteSpace(dto.RequestedByUserId) ? Uid() : dto.RequestedByUserId;
            r.RequestedByName = await _db.Users.AsNoTracking().Where(u => u.Id == r.RequestedByUserId)
                .Select(u => u.Name ?? u.UserName).FirstOrDefaultAsync() ?? await MyNameAsync();
        }

        private async Task Submit(PublicityRequest r, string name, string? remarks)
        {
            // Starts at the stage AFTER the creator's own rung in the chain - a plain RMDO (or
            // Admin/SuperAdmin, or any role outside the chain) starts at the very first stage (RM);
            // a chain role creating its own request (e.g. SMM) skips RM and SMD, since it already
            // outranks them, and goes straight to the stage above it (MDAdmin here).
            int creatorIndex;
            if (MyRole() == AppRole.RM) creatorIndex = 0;
            else if (MyRole() == AppRole.SMD) creatorIndex = 1;
            else if (MyRole() == AppRole.SMM) creatorIndex = 2;
            else if (await IsAdminMdCsAsync()) creatorIndex = 3;
            else if (await IsDgmMdCsAsync()) creatorIndex = 4;
            else creatorIndex = -1; // RMDO (RMD/MDO) or anyone else outside the chain - the normal first stage
            var startIndex = creatorIndex + 1;
            if (startIndex < ChainStages.Length)
            {
                r.Stage = ChainStages[startIndex];
                r.Status = r.Stage >= PublicityStage.AdminMdCsReview ? PublicityStatus.PendingFinalApproval : PublicityStatus.PendingApproval;
                await NotifyNextApproverAsync(r);
            }
            else
            {
                // The creator IS the last rung (DGM - MD CS): nothing left to approve.
                r.Stage = PublicityStage.Dispatch;
                r.Status = PublicityStatus.FinalApproved;
                await NotifyAsync(PublicityNotificationType.Approval, r, null, r.CreatedByUserId, null, null,
                    $"Request {r.IndentNo} final approved", "Your publicity material request is approved and ready for dispatch.", null);
                await NotifyReadyForDispatchAsync(r);
            }
            r.SubmittedAt = DateTime.UtcNow;
            await AddEvent(r, PublicityStage.IndentCreation, "Submitted", name, remarks);
        }

        private async Task AddEvent(PublicityRequest r, PublicityStage stage, string action, string name, string? remarks, int? dispatchId = null) =>
            _db.PublicityEvents.Add(new PublicityEvent
            {
                RequestId = r.Id, DispatchId = dispatchId, Stage = stage, Action = action, ActorUserId = Uid(),
                ActorName = name, ActorRole = await MyRoleLabelAsync(), Remarks = Clean(remarks)
            });

        /// <summary>Business Event + Entity + Recipient dedup: an identical, still-unread notification
        /// for the same (Type, RequestId, DispatchId, TargetUserId, TargetRole, TargetHqId, Title) is
        /// never inserted twice - this is what actually makes a page refresh or a retried POST safe,
        /// since every Notify() call site already only ever runs from a successfully persisted business
        /// transition, never from a GET/page-render. Once the existing one is read, a genuinely repeated
        /// action (e.g. proof re-submitted a second time) still gets its own fresh notification.</summary>
        private async Task NotifyAsync(PublicityNotificationType type, PublicityRequest r, int? dispatchId, string? userId, AppRole? role, int? hqId,
            string title, string message, string? sub)
        {
            var exists = await _db.PublicityNotifications.AnyAsync(n =>
                n.Type == type && n.RequestId == r.Id && n.DispatchId == dispatchId && n.TargetUserId == userId &&
                n.TargetRole == role && n.TargetHqId == hqId && n.Title == title);
            if (exists) return;
            _db.PublicityNotifications.Add(new PublicityNotification
            {
                Type = type, RequestId = r.Id, DispatchId = dispatchId, TargetUserId = userId, TargetRole = role,
                TargetHqId = hqId, Title = title, Message = message, SubText = sub
            });
        }

        private static string DispatchStatusLabel(PublicityDispatchStatus s, PublicityDispatchKind k) => s switch
        {
            PublicityDispatchStatus.TransferRequested => "Awaiting Dispatch Proof",
            PublicityDispatchStatus.ProofSubmitted => "Proof Submitted to Admin",
            PublicityDispatchStatus.Dispatched => k == PublicityDispatchKind.Transfer ? "Transfer Created" : "Dispatched",
            PublicityDispatchStatus.InTransit => "In Transit",
            PublicityDispatchStatus.Delivered => "Delivered",
            _ => "Completed"
        };

        // DGM - MD CS's final approval (both the normal chain in ReviewOneAsync and the chain-skip
        // case in Submit() where the creator already outranks every stage) always notifies the
        // ORIGINAL REQUESTER ("your request is approved") immediately above/below this call - that
        // notification was never reaching ADMIN-MD-CS, the role that actually has to act on it, which
        // is the root cause of Issue 1 (ADMIN-MD-CS's Notifications page showing nothing). This adds
        // the missing ADDITIONAL notification, reusing the exact same PublicityNotification mechanism
        // (AppRole.MDAdmin is already matched to IsAdminMdCsAsync() by MyNotificationsAsync, and
        // PublicityNotificationType.Approval already renders under both "All" and "Dispatch &
        // Approvals" in PublicityNotifications.razor) - no new notification system, table, or category.
        private async Task NotifyReadyForDispatchAsync(PublicityRequest r)
        {
            var names = await NamesAsync(new[] { r.StateId }, new[] { r.RegionId }, new[] { r.HeadquarterId });
            var pm = await ProductMaterialPartsAsync(r);
            var sub = $"{names.States.GetValueOrDefault(r.StateId)} / {names.Regions.GetValueOrDefault(r.RegionId)} / {names.Hqs.GetValueOrDefault(r.HeadquarterId)}"
                + $" - {pm.Material} - {r.Quantity} Nos - Approved {DateTime.Now:dd MMM yyyy, hh:mm tt}";
            await NotifyAsync(PublicityNotificationType.Approval, r, null, null, AppRole.MDAdmin, null,
                "Ready for Dispatch", $"Publicity Request {r.IndentNo} has received final approval from DGM-MD-CS and is ready for dispatch.", sub);
        }

        private async Task SetResponsibleAsync(PublicityDispatch d, string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;
            var u = await _db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => new { x.Name, x.UserName, x.Role }).FirstOrDefaultAsync();
            if (u is null) return;
            d.ResponsibleUserId = userId;
            d.ResponsibleName = u.Name ?? u.UserName;
            d.ResponsibleRole = RoleLabel(u.Role);
        }

        private async Task<string> NextNoAsync(string prefix)
        {
            var head = $"{prefix}-{DateTime.Now.Year}-";
            var codes = prefix == "IND"
                ? await _db.PublicityRequests.Where(x => x.IndentNo.StartsWith(head)).Select(x => x.IndentNo).ToListAsync()
                : await _db.PublicityDispatches.Where(x => x.DispatchNo.StartsWith(head)).Select(x => x.DispatchNo).ToListAsync();
            var max = codes.Select(c => int.TryParse(c[head.Length..], out var n) ? n : 0).DefaultIfEmpty(0).Max();
            return head + (max + 1).ToString("0000");
        }

        // ================================================================ read helpers

        private async Task<PublicityRequest?> LoadAsync(int id, bool tracking = false)
        {
            var q = _db.PublicityRequests.Include(r => r.Events).Include(r => r.Items).Include(r => r.MaterialType).Where(r => r.Id == id && !r.IsDeleted);
            return await (tracking ? q : q.AsNoTracking()).FirstOrDefaultAsync();
        }

        private async Task<(Dictionary<int, string> States, Dictionary<int, string> Regions, Dictionary<int, string> Hqs)> NamesAsync(
            IEnumerable<int> states, IEnumerable<int> regions, IEnumerable<int> hqs)
        {
            var s = states.Distinct().ToList();
            var g = regions.Distinct().ToList();
            var h = hqs.Distinct().ToList();
            return (await _db.States.AsNoTracking().Where(x => s.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.StateName),
                await _db.Regions.AsNoTracking().Where(x => g.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.RegionName),
                await _db.Headquarters.AsNoTracking().Where(x => h.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.HeadquarterName));
        }

        private async Task<(string Product, string Material)> ProductMaterialPartsAsync(PublicityRequest r)
        {
            var mat = await _db.PublicityMaterialTypes.AsNoTracking().Where(m => m.Id == r.MaterialTypeId).Select(m => m.Name).FirstOrDefaultAsync() ?? "";
            string prod;
            if (r.Items.Count > 1)
            {
                prod = $"{r.Items.Count} Products";
            }
            else if (r.Items.Count == 1)
            {
                var pid = r.Items[0].ProductId;
                prod = await _db.Products.AsNoTracking().Where(p => p.Id == pid).Select(p => p.Name).FirstOrDefaultAsync() ?? "";
            }
            else
            {
                prod = await _db.Products.AsNoTracking().Where(p => p.Id == r.ProductId).Select(p => p.Name).FirstOrDefaultAsync() ?? "";
            }
            return (prod, mat);
        }

        private async Task<string> ProductMaterialAsync(PublicityRequest r)
        {
            var pm = await ProductMaterialPartsAsync(r);
            return $"{pm.Product} {pm.Material.ToLowerInvariant()}".Trim();
        }

        /// <summary>The person holding the approval role for the request's area (first match), for the header.</summary>
        private static string StageOwnerLabel(PublicityStage stage) => stage switch
        {
            PublicityStage.RmReview => "RM",
            PublicityStage.SmdReview => "SMD",
            PublicityStage.SmmReview => "SMM",
            PublicityStage.AdminMdCsReview => "ADMIN-MD CS",
            PublicityStage.DgmMdCsReview => "DGM-MD CS",
            _ => "Approver"
        };

        /// <summary>Who holds this stage, by name - ADMIN-MD CS / DGM-MD CS are CommonRole users
        /// identified by designation, so they can't be found by a plain Role == x match alone.</summary>
        // 2026-11-27 fix: notifies whoever becomes responsible for r.Stage right after it was just
        // set - called from Submit()'s and ReviewOneAsync()'s normal (non-return/reject/final) branches,
        // which previously advanced Stage/Status with no notification at all, so an RM (and every other
        // approver in the chain) never learned a request was waiting on them. Every stage is now targeted
        // by specific UserId, never a role+HQ broadcast: RM/SMD/SMM are a single region/state-scoped
        // person with no reliable HQ claim to broadcast against (see PersonForAsync - an RM's scope is
        // RegionId, not HQ); AdminMdCsReview/DgmMdCsReview can genuinely have MORE THAN ONE matching user
        // (confirmed in practice - e.g. a dedicated MDAdmin-role account AND a separate CommonRole user
        // with the "ADMIN MD CS" designation can both exist at once), and PublicityNotification.ReadAt is
        // a single column with no per-recipient tracking - a role+HQ broadcast row's ReadAt is shared by
        // every matching user, so marking it read for the admin who just approved would incorrectly mark
        // it read for every OTHER admin too, who never opened or acted on it. Fanning out one row per
        // matching user (via ApproverUserIdsAsync) gives each admin their own independent ReadAt, exactly
        // like RM/SMD/SMM already have - never a shared row two different people's read-state can collide on.
        private async Task NotifyNextApproverAsync(PublicityRequest r)
        {
            const string title = "Publicity Request Awaiting Your Approval";
            var message = $"{r.IndentNo} has been submitted and is awaiting your review.";
            switch (r.Stage)
            {
                case PublicityStage.RmReview:
                case PublicityStage.SmdReview:
                case PublicityStage.SmmReview:
                    var userId = await PersonIdForAsync(r.Stage, r);
                    if (userId != null)
                        await NotifyAsync(PublicityNotificationType.Approval, r, null, userId, null, null, title, message, null);
                    break;
                case PublicityStage.AdminMdCsReview:
                case PublicityStage.DgmMdCsReview:
                    foreach (var id in await ApproverUserIdsAsync(r.Stage))
                        await NotifyAsync(PublicityNotificationType.Approval, r, null, id, null, null, title, message, null);
                    break;
            }
        }

        /// <summary>Every user currently qualified to act at an all-India stage (AdminMdCsReview/
        /// DgmMdCsReview) - unlike RM/SMD/SMM, there is no single region/state owner, so this returns
        /// every match rather than the first one (see NotifyNextApproverAsync's doc comment).</summary>
        private async Task<List<string>> ApproverUserIdsAsync(PublicityStage stage)
        {
            var q = from u in _db.Users.AsNoTracking()
                    join e in _db.Employeelogins.AsNoTracking() on u.Id equals e.UserId
                    where e.IsActive
                    select new { u.Id, u.Role, Designation = u.Designation != null ? u.Designation.Name : null };
            q = stage switch
            {
                PublicityStage.AdminMdCsReview => q.Where(x => x.Role == AppRole.MDAdmin ||
                    (x.Role == AppRole.CommonRole && AdminMdCsDesignations.Contains(x.Designation))),
                PublicityStage.DgmMdCsReview => q.Where(x => x.Role == AppRole.MDHOD ||
                    (x.Role == AppRole.CommonRole && DgmMdCsDesignations.Contains(x.Designation))),
                _ => q.Where(_ => false)
            };
            return await q.Select(x => x.Id).Distinct().ToListAsync();
        }

        /// <summary>Same geographic-scope rules as <see cref="PersonForAsync"/>, returning the specific
        /// responsible user's Id instead of their display Name - needed to target a single-recipient
        /// notification (RM/SMD/SMM each have exactly one region/state owner, never a role+HQ broadcast
        /// since an RM has no HQ claim at all).</summary>
        private async Task<string?> PersonIdForAsync(PublicityStage stage, PublicityRequest r)
        {
            var q = from u in _db.Users.AsNoTracking()
                    join e in _db.Employeelogins.AsNoTracking() on u.Id equals e.UserId
                    where e.IsActive
                    select new { u.Id, u.Role, e.StateId, e.RegionId };
            q = stage switch
            {
                PublicityStage.RmReview => q.Where(x => x.Role == AppRole.RM && x.RegionId == r.RegionId),
                PublicityStage.SmdReview => q.Where(x => x.Role == AppRole.SMD && x.StateId == r.StateId),
                PublicityStage.SmmReview => q.Where(x => x.Role == AppRole.SMM && x.StateId == r.StateId),
                _ => q.Where(_ => false)
            };
            return await q.Select(x => x.Id).FirstOrDefaultAsync();
        }

        private async Task<string?> PersonForAsync(PublicityStage stage, PublicityRequest r)
        {
            var q = from u in _db.Users.AsNoTracking()
                    join e in _db.Employeelogins.AsNoTracking() on u.Id equals e.UserId
                    where e.IsActive
                    select new { u.Name, u.Role, Designation = u.Designation != null ? u.Designation.Name : null, e.StateId, e.RegionId };
            q = stage switch
            {
                PublicityStage.RmReview => q.Where(x => x.Role == AppRole.RM && x.RegionId == r.RegionId),
                PublicityStage.SmdReview => q.Where(x => x.Role == AppRole.SMD && x.StateId == r.StateId),
                PublicityStage.SmmReview => q.Where(x => x.Role == AppRole.SMM && x.StateId == r.StateId),
                PublicityStage.AdminMdCsReview => q.Where(x => x.Role == AppRole.MDAdmin ||
                    (x.Role == AppRole.CommonRole && AdminMdCsDesignations.Contains(x.Designation))),
                PublicityStage.DgmMdCsReview => q.Where(x => x.Role == AppRole.MDHOD ||
                    (x.Role == AppRole.CommonRole && DgmMdCsDesignations.Contains(x.Designation))),
                _ => q
            };
            return await q.Select(x => x.Name).FirstOrDefaultAsync();
        }

        /// <summary>The location chip: "Tamil Nadu > Chennai Region", "Tamil Nadu (State Level)", "All India".</summary>
        private async Task<string> ScopeLabelAsync()
        {
            var names = await NamesAsync(new[] { ClaimInt("spic:state_id") }, new[] { ClaimInt("spic:region_id") }, new[] { ClaimInt("spic:hq_id") });
            var state = names.States.Values.FirstOrDefault();
            var region = names.Regions.Values.FirstOrDefault();
            return MyRole() switch
            {
                AppRole.SMD or AppRole.SMM when state != null => $"{state} (State Level)",
                AppRole.RM when state != null => $"{state} > {region}",
                AppRole.RMD or AppRole.MDO or AppRole.MO or AppRole.JMDO when state != null => $"{state} > {names.Hqs.Values.FirstOrDefault() ?? region}",
                _ => "All India"
            };
        }

        private async Task<string> MyNameAsync()
        {
            var me = Uid();
            return await _db.Users.AsNoTracking().Where(u => u.Id == me).Select(u => u.Name ?? u.UserName).FirstOrDefaultAsync()
                   ?? User.Identity?.Name ?? "";
        }

        private static PubFileDto ToFile(PublicityFile f) => new()
        {
            Id = f.Id, DispatchId = f.DispatchId, RequestItemId = f.RequestItemId, Kind = f.Kind,
            FileName = f.FileName, Path = f.Path,
            SizeBytes = f.SizeBytes, UploadedByName = f.UploadedByName, UploadedAt = f.UploadedAt
        };

        private static PubNotificationDto ToNotice(PublicityNotification n) => new()
        {
            Id = n.Id, Type = n.Type, Title = n.Title, Message = n.Message, SubText = n.SubText,
            RequestId = n.RequestId, DispatchId = n.DispatchId, CreatedAt = n.CreatedAt, IsRead = n.ReadAt != null
        };

        public static string StageLabel(PublicityStage s) => s switch
        {
            PublicityStage.IndentCreation => "Indent Creation",
            PublicityStage.RmReview => "RM Review",
            PublicityStage.SmdReview => "SMD Review",
            PublicityStage.SmmReview => "SMM Review",
            PublicityStage.AdminMdCsReview => "ADMIN - MD CS Review",
            PublicityStage.DgmMdCsReview => "DGM - MD CS Review",
            PublicityStage.Dispatch => "Dispatch",
            PublicityStage.Delivery => "Delivery",
            PublicityStage.Acknowledgement => "Acknowledgement",
            PublicityStage.ExceptionHandling => "Exception Handling",
            PublicityStage.ReInitiation => "Re-initiation",
            PublicityStage.InterHqTransfer => "Inter-HQ Transfer",
            PublicityStage.TransferCreation => "Transfer Creation",
            _ => "Completed"
        };

        private static string RegionStatusDescription(PubRegionDto g) => g.Status switch
        {
            PubRegionStatus.Approved => "Finalized",
            PubRegionStatus.ZeroConfirmed => "Finalized",
            PubRegionStatus.ZeroSubmittedByRm => "Zero Submitted by RM",
            PubRegionStatus.Reviewed => "Reviewed by SMD",
            PubRegionStatus.InReview => "In SMD Review",
            PubRegionStatus.Submitted => "Submitted to SMD",
            PubRegionStatus.SubmittedToSmm => "Already Submitted to SMM",
            _ => "Pending RM Submission"
        };

        private static string? RoleLabel(AppRole? role) => role switch
        {
            null => null,
            AppRole.RMD or AppRole.MDO => "RMDO",
            AppRole.MDAdmin => "ADMIN-MD CS",
            AppRole.MDHOD => "DGM-MD CS",
            _ => role.ToString()
        };

        /// <summary>The caller's own label for activity-log "Designation" and the Requested By list -
        /// designation-aware, so the real ADMIN-MD CS / DGM-MD CS people (CommonRole + that
        /// designation) show correctly instead of "CommonRole".</summary>
        private async Task<string?> MyRoleLabelAsync()
        {
            if (await IsAdminMdCsAsync()) return "ADMIN-MD CS";
            if (await IsDgmMdCsAsync()) return "DGM-MD CS";
            return RoleLabel(MyRole());
        }

        private string Uid() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private AppRole? MyRole() =>
            Enum.TryParse<AppRole>(User.FindFirst(ClaimTypes.Role)?.Value, true, out var role) ? role : null;

        private string? _designationName;
        private bool _designationLoaded;

        /// <summary>Resolved once per request (same pattern as Services/Lab/LabAccess.cs) - the
        /// Designation.Name for the caller, regardless of their AppRole.</summary>
        private async Task<string?> MyDesignationNameAsync()
        {
            if (_designationLoaded) return _designationName;
            _designationLoaded = true;
            var me = Uid();
            _designationName = await _db.Users.AsNoTracking().Where(u => u.Id == me)
                .Select(u => u.Designation != null ? u.Designation.Name : null).FirstOrDefaultAsync();
            return _designationName;
        }

        private string? _designationRoleAccess;
        private bool _designationRoleAccessLoaded;

        /// <summary>Resolved once per request - the caller's Designation.RoleAccess CSV, the same
        /// "Page.Action" token source RoleAccessPermissions.HasPage reads everywhere else in the app
        /// (e.g. WelfareSchemeApprovalController, LibraryController). Separate cache from
        /// MyDesignationNameAsync above (different column, same Users->Designation join) rather than
        /// widening that one, so nothing already calling it is affected.</summary>
        private async Task<string?> MyDesignationRoleAccessAsync()
        {
            if (_designationRoleAccessLoaded) return _designationRoleAccess;
            _designationRoleAccessLoaded = true;
            var me = Uid();
            _designationRoleAccess = await _db.Users.AsNoTracking().Where(u => u.Id == me)
                .Select(u => u.Designation != null ? u.Designation.RoleAccess : null).FirstOrDefaultAsync();
            return _designationRoleAccess;
        }

        // Publicity Material Master (2026-11-27 authorization fix): a normal Designation-grid
        // permission (PagePermission.PublicityMaterialMaster / RoleAccessPermissions.HasPage),
        // deliberately SEPARATE from IsAdminMdCsAsync() above - that helper is still used, unchanged,
        // by every stage/notification/review check elsewhere in this controller and must not be
        // touched. Only two bypasses are preserved here, both pre-existing and legitimate: IsSuper()
        // (Admin/SuperAdmin, same as every other admin surface) and AppRole.MDAdmin - a dedicated
        // ROLE (not a CommonRole + Designation-name string match), the same bypass IsAdminMdCsAsync()
        // itself already grants, so an MDAdmin account's existing access is preserved exactly even
        // before anyone grants its Designation the new permission via Designation Management. A
        // CommonRole user (including one named "ADMIN MD CS"/"ADMIN-MD CS") now gets access ONLY
        // through an explicit grant on their Designation, never through the designation's name.
        private async Task<bool> CanManageMaterialMasterAsync()
        {
            if (IsSuper()) return true;
            if (MyRole() == AppRole.MDAdmin) return true;
            var roleAccess = await MyDesignationRoleAccessAsync();
            return RoleAccessPermissions.HasPage(roleAccess, PagePermission.PublicityMaterialMaster);
        }

        // ADMIN-MD CS and DGM-MD CS are CommonRole users identified by Designation name, not a
        // dedicated AppRole - the AppRole.MDAdmin / AppRole.MDHOD enum members are still honoured
        // (a couple of accounts hold them directly), but the real people doing the job hold
        // CommonRole + one of these designations (confirmed against the live Designations table).
        private static readonly string[] AdminMdCsDesignations = { "ADMIN MD CS", "ADMIN-MD CS" };
        private static readonly string[] DgmMdCsDesignations = { "DGM MD CS", "DGM-MD CS" };

        private async Task<bool> IsAdminMdCsAsync()
        {
            if (MyRole() == AppRole.MDAdmin) return true;
            if (MyRole() != AppRole.CommonRole) return false;
            var designation = await MyDesignationNameAsync();
            return AdminMdCsDesignations.Contains(designation);
        }

        private async Task<bool> IsDgmMdCsAsync()
        {
            if (MyRole() == AppRole.MDHOD) return true;
            if (MyRole() != AppRole.CommonRole) return false;
            var designation = await MyDesignationNameAsync();
            return DgmMdCsDesignations.Contains(designation);
        }

        private int ClaimInt(string type) => int.TryParse(User.FindFirst(type)?.Value, out var v) ? v : 0;

        private string UploadsRoot() => Path.GetFullPath(Path.Combine(_env.ContentRootPath, "Uploads"));

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string SafeName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (var ch in Path.GetFileName(fileName)) sb.Append(invalid.Contains(ch) || ch == ' ' ? '_' : ch);
            var safe = sb.ToString();
            return safe.Length > 80 ? safe[^80..] : safe;
        }
    }
}
