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

        // ================================================================ material master (ADMIN-MD CS only)
        // Every endpoint below is gated on IsAdminMdCsAsync() (plus the super-admin bypass every other
        // admin-only Publicity action already allows) - never DGM-MD CS, SMM, SMD, RM or RMDO, even
        // though those roles DO reach the ordinary Lookups endpoint above for request creation.

        [HttpGet("materials")]
        public async Task<IActionResult> MaterialsAdmin()
        {
            if (!await IsAdminMdCsAsync() && !IsSuper()) return Forbid();
            var rows = await _db.PublicityMaterialTypes.AsNoTracking()
                .OrderBy(m => m.Category).ThenBy(m => m.SortOrder).ThenBy(m => m.Name)
                .Select(m => new PubMaterialAdminDto { Id = m.Id, Name = m.Name, Category = m.Category, IsActive = m.IsActive, SortOrder = m.SortOrder })
                .ToListAsync();
            return Ok(rows);
        }

        [HttpPost("materials")]
        public async Task<IActionResult> CreateMaterial([FromBody] PubMaterialUpsertDto dto)
        {
            if (!await IsAdminMdCsAsync() && !IsSuper()) return Forbid();
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
            if (!await IsAdminMdCsAsync() && !IsSuper()) return Forbid();
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
            if (!await IsAdminMdCsAsync() && !IsSuper()) return Forbid();
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
        public async Task<IActionResult> Stats()
        {
            var q = await ScopedAsync();
            var rows = await q.Select(r => new { r.Status, r.Stage }).ToListAsync();
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
                FinalApproval = mine is null ? 0 : rows.Count(r => r.Stage > mine && r.Stage <= PublicityStage.DgmMdCsReview && r.Status is PublicityStatus.PendingApproval or PublicityStatus.PendingFinalApproval)
            });
        }

        [HttpGet("requests")]
        public async Task<IActionResult> List([FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int? productId, [FromQuery] int? materialTypeId, [FromQuery] int? status, [FromQuery] int? stage,
            [FromQuery] int? stateId, [FromQuery] int? regionId, [FromQuery] int? hqId, [FromQuery] string? q,
            [FromQuery] bool awaitingMe = false, [FromQuery] bool submittedOnly = false,
            [FromQuery] int page = 1, [FromQuery] int size = 16)
        {
            var query = await ScopedAsync();
            if (from.HasValue) query = query.Where(r => r.CreatedAt >= from.Value.Date);
            if (to.HasValue) query = query.Where(r => r.CreatedAt < to.Value.Date.AddDays(1));
            if (productId > 0) query = query.Where(r => r.ProductId == productId);
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

        /// <summary>"Specific Product" submit: one PublicityRequest per Item, sharing every other
        /// field - reuses the exact same Validate/Apply/Submit path Create() uses, looped, so every
        /// resulting row goes through the existing RM -> SMD -> SMM -> ADMIN -> DGM chain completely
        /// unchanged. Two passes (validate all, then create all) so one bad row never leaves earlier
        /// rows already persisted.</summary>
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
            var created = new List<PubBulkCreatedDto>();
            foreach (var item in dto.Items)
            {
                var r = new PublicityRequest
                {
                    IndentNo = await NextNoAsync("IND"), CreatedByUserId = Uid(), CreatedByName = name,
                    Stage = PublicityStage.IndentCreation, Status = PublicityStatus.Draft
                };
                await ApplyAsync(r, RowDto(item));
                r.OriginalQuantity = r.Quantity;
                _db.PublicityRequests.Add(r);
                await _db.SaveChangesAsync();
                await AddEvent(r, PublicityStage.IndentCreation, "Created", name, dto.Remarks);
                if (dto.Submit) await Submit(r, name, dto.Remarks);
                await _db.SaveChangesAsync();
                created.Add(new PubBulkCreatedDto { Id = r.Id, IndentNo = r.IndentNo, ProductId = item.ProductId });
            }
            return Ok(created);
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
            var error = await ReviewOneAsync(r, dto.Action, dto.Quantity, dto.Remarks);
            if (error != null) return BadRequest(new { Success = false, Message = error });
            await _db.SaveChangesAsync();
            return Ok(new { Success = true, r.Stage, r.Status });
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
            foreach (var r in items.Where(r => allowed.Contains(r.Id)))
                if (await ReviewOneAsync(r, "Approve", null, dto.Remarks) is null) done++;

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
            return Ok(new { Success = true, Approved = done });
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
                if (r.Status != PublicityStatus.ExceptionRaised) return BadRequest(new { Success = false, Message = "A stock transfer resolves a raised exception only." });
                var source = dto.SourceRequestId is > 0 ? await LoadAsync(dto.SourceRequestId.Value, tracking: true) : null;
                if (source is null || source.Id == r.Id) return BadRequest(new { Success = false, Message = "Select the source request that holds excess stock." });
                var ss = await SummaryAsync(source);
                var remaining = ss.Excess - ss.TransferredOut;
                if (dto.Quantity > remaining) return BadRequest(new { Success = false, Message = $"Only {remaining} Nos of excess remain at the source HQ." });
                d.Quantity = dto.Quantity;
                d.DispatchNo = await NextNoAsync("TRF");
                d.Status = PublicityDispatchStatus.TransferRequested;
                d.SourceHqId = source.HeadquarterId;
                d.SourceRequestId = source.Id;
                _db.PublicityDispatches.Add(d);
                await _db.SaveChangesAsync();
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
                // Dispatch Quantity is never taken from the client for these two kinds - it is always
                // the authoritative server-side figure for whichever workflow is dispatching: the
                // request's own final-approved Quantity (set by the last authorized reviewer to touch
                // it, never DGM - MD CS's approve-only stage) for an Original dispatch, or the still-
                // outstanding shortage (SummaryAsync, itself computed only from persisted dispatch/ack
                // rows) for a ReInitiation - this is what actually prevents a client-supplied quantity
                // from ever being saved, not just the frontend's readonly attribute.
                if (dto.Kind == PublicityDispatchKind.Original)
                {
                    if (r.Status != PublicityStatus.FinalApproved) return BadRequest(new { Success = false, Message = "The request is not final approved yet." });
                    d.Quantity = r.Quantity;
                }
                else
                {
                    if (r.Status != PublicityStatus.ExceptionRaised) return BadRequest(new { Success = false, Message = "Re-initiation resolves a raised exception only." });
                    d.Quantity = (await SummaryAsync(r)).Outstanding;
                }
                if (d.Quantity <= 0) return BadRequest(new { Success = false, Message = "There is no quantity to dispatch for this request." });
                if (string.IsNullOrWhiteSpace(dto.VendorName) || string.IsNullOrWhiteSpace(dto.Transporter) || dto.DispatchDate is null)
                    return BadRequest(new { Success = false, Message = "Vendor, transporter and dispatch date are required." });
                // The Delivery Address dropdown is the only source of the destination for these two
                // kinds now - dto.DestinationAddress (free text) is never trusted here; the AddressId
                // is re-validated server-side (exists, active) and the dispatch's own DestinationAddress
                // is composed from the saved record's CURRENT fields at this exact moment, so it becomes
                // an immutable snapshot - a later edit to the address book can never retroactively change
                // what this already-created dispatch says.
                if (dto.DeliveryAddressId is not > 0) return BadRequest(new { Success = false, Message = "Select a delivery address." });
                var addr = await _db.PublicityDeliveryAddresses.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == dto.DeliveryAddressId && a.IsActive);
                if (addr is null) return BadRequest(new { Success = false, Message = "Select a valid, active delivery address." });
                d.DeliveryAddressId = addr.Id;
                d.DestinationAddress = await ComposeAddressTextAsync(addr);
                d.DispatchNo = await NextNoAsync("DSP");
                d.Status = PublicityDispatchStatus.Dispatched;
                d.VendorName = dto.VendorName!.Trim();
                d.Transporter = dto.Transporter!.Trim();
                d.TrackingNo = Clean(dto.TrackingNo);
                d.VehicleNo = Clean(dto.VehicleNo);
                d.DispatchDate = dto.DispatchDate;
                d.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;
                await SetResponsibleAsync(d, dto.ResponsibleUserId);
                _db.PublicityDispatches.Add(d);
                await _db.SaveChangesAsync();
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

        // A small, shared, Publicity-specific address book (regional offices etc.) - gated by the
        // exact same IsDispatcherAsync() check CreateDispatch itself already uses, so nobody who
        // couldn't create a dispatch can read or add addresses either; no separate/broader address
        // permission exists. There is deliberately no update/delete endpoint (the spec explicitly asks
        // not to introduce address-edit/delete permissions beyond what's needed) - IsActive exists so
        // a bad entry can be deactivated directly in the database if that's ever needed, without an API
        // surface for it.
        [HttpGet("delivery-addresses")]
        public async Task<IActionResult> DeliveryAddresses()
        {
            if (!await IsDispatcherAsync()) return Forbid();
            var rows = await _db.PublicityDeliveryAddresses.AsNoTracking().Where(a => a.IsActive)
                .OrderBy(a => a.AddressLabel).Take(500).ToListAsync();
            var stateIds = rows.Select(a => a.StateId).Distinct().ToList();
            var districtIds = rows.Where(a => a.DistrictId.HasValue).Select(a => a.DistrictId!.Value).Distinct().ToList();
            var states = await _db.States.AsNoTracking().Where(s => stateIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.StateName);
            var districts = await _db.Districts.AsNoTracking().Where(d => districtIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.DistrictName);
            return Ok(rows.Select(a => new PubDeliveryAddressDto
            {
                Id = a.Id, AddressLabel = a.AddressLabel, RecipientName = a.RecipientName, ContactPerson = a.ContactPerson,
                MobileNumber = a.MobileNumber, AddressLine1 = a.AddressLine1, AddressLine2 = a.AddressLine2, Landmark = a.Landmark,
                StateId = a.StateId, StateName = states.GetValueOrDefault(a.StateId),
                DistrictId = a.DistrictId, DistrictName = a.DistrictId is int did ? districts.GetValueOrDefault(did) : null,
                City = a.City, PinCode = a.PinCode
            }).ToList());
        }

        [HttpPost("delivery-addresses")]
        public async Task<IActionResult> CreateDeliveryAddress([FromBody] PubDeliveryAddressCreateDto dto)
        {
            if (!await IsDispatcherAsync()) return Forbid();
            string? Req(string? v, string label, int max) =>
                string.IsNullOrWhiteSpace(v) ? $"Enter the {label}." : v.Trim().Length > max ? $"{label} is too long." : null;
            var error = Req(dto.AddressLabel, "address name / label", 150)
                ?? Req(dto.RecipientName, "recipient / office name", 200)
                ?? Req(dto.AddressLine1, "address line 1", 300)
                ?? Req(dto.City, "district / city", 150)
                ?? (dto.ContactPerson is { Length: > 150 } ? "Contact person is too long." : null)
                ?? (dto.AddressLine2 is { Length: > 300 } ? "Address line 2 is too long." : null)
                ?? (dto.Landmark is { Length: > 150 } ? "Landmark is too long." : null)
                ?? (!Regex.IsMatch(dto.MobileNumber?.Trim() ?? "", @"^[6-9]\d{9}$") ? "Enter a valid 10-digit Indian mobile number." : null)
                ?? (!Regex.IsMatch(dto.PinCode?.Trim() ?? "", @"^\d{6}$") ? "Enter a valid 6-digit PIN code." : null)
                ?? (!await _db.States.AnyAsync(s => s.Id == dto.StateId && s.IsActive) ? "Select the state." : null)
                ?? (dto.DistrictId is > 0 && !await _db.Districts.AnyAsync(d => d.Id == dto.DistrictId && d.StateId == dto.StateId && d.IsActive)
                    ? "Select a district that belongs to the selected state." : null);
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
            if (dto.ReceivedQty < 0) return BadRequest(new { Success = false, Message = "Enter the received quantity." });
            if (dto.IsDamaged && (dto.DamagedQty <= 0 || dto.DamagedQty > dto.ReceivedQty))
                return BadRequest(new { Success = false, Message = "Damaged quantity must be between 1 and the received quantity." });

            var ack = d.Acknowledgement ?? new PublicityAcknowledgement { DispatchId = d.Id, RequestId = r.Id };
            if (d.Acknowledgement is null) _db.PublicityAcknowledgements.Add(ack);
            ack.ReceivedQty = dto.ReceivedQty;
            ack.IsDamaged = dto.IsDamaged;
            ack.DamagedQty = dto.IsDamaged ? dto.DamagedQty : 0;
            ack.Remarks = Clean(dto.Remarks);
            ack.UpdatedAt = DateTime.UtcNow;
            // Acknowledged By is never taken from the client - CanAcknowledgeAsync above already
            // proved the CALLER is the one and only authorized party for this dispatch (the assigned
            // Responsible Person, or the Transfer-kind destination rule), so the acknowledger's
            // identity can only ever be the authenticated user, never a dto-supplied id.
            ack.AcknowledgedByUserId = Uid();
            ack.AcknowledgedByName = await MyNameAsync();

            if (dto.Submit)
            {
                ack.IsDraft = false;
                ack.SubmittedAt = DateTime.UtcNow;
                d.Status = PublicityDispatchStatus.Acknowledged;
                d.DeliveredAt ??= DateTime.UtcNow;
                var shortage = Math.Max(0, d.Quantity - ack.ReceivedQty);
                var type = ExceptionType(shortage, Math.Max(0, ack.ReceivedQty - d.Quantity), ack.DamagedQty);
                var name = await MyNameAsync();
                await AddEvent(r, PublicityStage.Acknowledgement, type == "None" ? "Acknowledged" : "Exception Raised", name,
                    ack.Remarks ?? $"Received {ack.ReceivedQty} of {d.Quantity} Nos.", d.Id);
                if (type != "None" && type != "Excess")
                {
                    var parts = new List<string>();
                    if (shortage > 0) parts.Add("Shortage");
                    if (ack.DamagedQty > 0) parts.Add("Damaged");
                    await NotifyAsync(PublicityNotificationType.Exception, r, d.Id, null, AppRole.MDAdmin, null,
                        $"Exception Raised - {string.Join(" + ", parts)}",
                        $"{r.IndentNo} reported {string.Join(" and ", parts).ToLowerInvariant()} material ({(shortage > 0 ? $"{shortage} Nos short" : "")}{(shortage > 0 && ack.DamagedQty > 0 ? ", " : "")}{(ack.DamagedQty > 0 ? $"{ack.DamagedQty} Nos damaged" : "")}). Review required.", ack.Remarks);
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
        public async Task<IActionResult> Upload(int id, [FromQuery] PublicityFileKind kind, [FromQuery] int? dispatchId, [FromForm] List<IFormFile> files)
        {
            var r = await LoadAsync(id);
            if (r is null || !await CanReadAsync(r)) return NotFound(new { Success = false, Message = "Request not found." });
            if (files is null || files.Count == 0) return BadRequest(new { Success = false, Message = "No file uploaded." });
            if (dispatchId.HasValue && !await _db.PublicityDispatches.AnyAsync(d => d.Id == dispatchId && (d.RequestId == id || d.SourceRequestId == id)))
                return BadRequest(new { Success = false, Message = "Dispatch does not belong to this request." });
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
                    RequestId = id, DispatchId = dispatchId, Kind = kind, FileName = Path.GetFileName(f.FileName),
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
            var d = n.DispatchId is int did ? await _db.PublicityDispatches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == did) : null;
            if (d is not null)
            {
                var dest = await LoadAsync(d.RequestId);
                var source = d.SourceRequestId is int sid ? await LoadAsync(sid) : null;
                var names = await NamesAsync(Array.Empty<int>(), Array.Empty<int>(), new[] { dest!.HeadquarterId, source?.HeadquarterId ?? 0 });
                dto.DestinationIndentNo = dest.IndentNo;
                dto.DestinationHq = names.Hqs.GetValueOrDefault(dest.HeadquarterId);
                dto.TransferQty = d.Quantity;
                dto.Status = DispatchStatusLabel(d.Status, d.Kind);
                dto.Reason = d.Remarks ?? $"Shortage resolution for {dto.DestinationHq}";
                var pm = await ProductMaterialPartsAsync(dest);
                dto.Product = pm.Product;
                dto.Material = pm.Material;
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
            return Ok(dto);
        }

        [HttpPost("notifications/{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var ids = await (await MyNotificationsAsync()).Where(x => id == 0 || x.Id == id).Select(x => x.Id).ToListAsync();
            await _db.PublicityNotifications.Where(x => ids.Contains(x.Id) && x.ReadAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReadAt, DateTime.UtcNow));
            return Ok(new { Success = true });
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

            (PublicityStage, PublicityStatus) next;
            if (incoming.Count == 0) next = (PublicityStage.Dispatch, PublicityStatus.FinalApproved);
            else if (open.Any(d => d.Status < PublicityDispatchStatus.Dispatched)) next = (PublicityStage.InterHqTransfer, PublicityStatus.TransferInProgress);
            else if (open.Any(d => d.Status == PublicityDispatchStatus.Delivered)) next = (PublicityStage.Acknowledgement, PublicityStatus.AcknowledgementPending);
            else if (open.Any(d => d.Kind == PublicityDispatchKind.Transfer)) next = (PublicityStage.InterHqTransfer, PublicityStatus.TransferInProgress);
            else if (open.Any(d => d.Kind == PublicityDispatchKind.ReInitiation)) next = (PublicityStage.ReInitiation, PublicityStatus.ReInitiationInProgress);
            else if (open.Count > 0) next = (PublicityStage.Delivery, PublicityStatus.InDispatch);
            else if (s.Outstanding > 0) next = (PublicityStage.ExceptionHandling, PublicityStatus.ExceptionRaised);
            else if (outgoingOpen) next = (PublicityStage.TransferCreation, PublicityStatus.TransferOrderReceived);
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
            s.Usable = s.Received - s.Damaged;
            s.Outstanding = Math.Max(0, s.Requested - s.Usable);
            s.Excess = Math.Max(0, s.Usable - s.Requested);
            s.ExceptionType = ExceptionType(s.Shortage, acks.Sum(d => Math.Max(0, d.Acknowledgement!.ReceivedQty - d.Quantity)), s.Damaged);
            s.ExceptionStatus = s.ExceptionType == "None" ? "None"
                : s.ExceptionType == "Excess" ? "Reported"
                : acks.Count == 0 ? "None" : s.Outstanding > 0 ? "Raised" : "Resolved";
            s.MaterialCondition = acks.Count == 0 ? "-"
                : s.Damaged == 0 && s.Shortage == 0 ? "Good"
                : s.Outstanding > 0 ? $"Partially received - {s.Damaged} Nos damaged, {s.Shortage} Nos shortage"
                : "Completed with replacement";
            return s;
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
            var productIds = items.Select(r => r.ProductId).Distinct().ToList();
            var products = await _db.Products.AsNoTracking().Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);
            var materials = await _db.PublicityMaterialTypes.AsNoTracking().ToDictionaryAsync(m => m.Id, m => m.Name);
            var mine = await MyStageAsync();
            // Who actually approved the RM stage, per request - from the existing per-request audit
            // trail (PublicityEvent), never from the Region's CURRENT RM assignment (see PubRowDto.
            // RmApprovedByName's own doc comment for why that would be wrong for a request an earlier,
            // since-reassigned RM approved).
            var requestIds = items.Select(r => r.Id).ToList();
            // A request made by a chain role (e.g. RMDO) and later approved by the RM has a distinct
            // Stage=RmReview "Approved" event - that's the normal case. But a request an RM created for
            // THEMSELVES skips RmReview entirely (Submit()'s creatorIndex logic), so no such event ever
            // exists for it; the RM's own "Submitted" action (logged at Stage=IndentCreation, the stage it
            // was submitted FROM) is the only recorded act of theirs and functionally is their approval.
            var rmApprovedByName = await _db.PublicityEvents.AsNoTracking()
                .Where(e => requestIds.Contains(e.RequestId) &&
                    ((e.Stage == PublicityStage.RmReview && e.Action == "Approved") ||
                     (e.Stage == PublicityStage.IndentCreation && e.Action == "Submitted" && e.ActorRole == "RM")))
                .GroupBy(e => e.RequestId)
                .Select(g => new { RequestId = g.Key, Name = g.OrderByDescending(e => e.At).ThenByDescending(e => e.Id).First().ActorName })
                .ToDictionaryAsync(x => x.RequestId, x => x.Name);
            return items.Select(r => new PubRowDto
            {
                Id = r.Id, IndentNo = r.IndentNo, Quantity = r.Quantity, OriginalQuantity = r.OriginalQuantity,
                Product = products.GetValueOrDefault(r.ProductId, ""), Material = materials.GetValueOrDefault(r.MaterialTypeId, ""),
                Hq = names.Hqs.GetValueOrDefault(r.HeadquarterId, ""), State = names.States.GetValueOrDefault(r.StateId, ""),
                Region = names.Regions.GetValueOrDefault(r.RegionId, ""),
                Stage = r.Stage, Status = r.Status, CreatedAt = r.CreatedAt,
                AwaitingMe = IsPendingAt(r.Status, r.Stage, mine),
                StatusNote = r.Status == PublicityStatus.Returned ? $"Return by {StageLabel(r.Stage).Replace(" Review", "")}" : null,
                CreatedByName = r.CreatedByName,
                RmApprovedByName = rmApprovedByName.GetValueOrDefault(r.Id),
                SmdReviewedAt = r.SmdReviewedAt,
                SmdReviewedByName = r.SmdReviewedByName
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
            // The courier proof for an outgoing transfer is uploaded from THIS source request's own
            // page (api/requests/{sourceId}/files), so it is stored with RequestId == r.Id - excluding
            // f.RequestId == r.Id here (as this query previously did) silently hid the proof the source
            // RMDO had just submitted, leaving Proof permanently null on their own tracking page and the
            // "Create Transfer" action visible even after submission.
            var transferFiles = await _db.PublicityFiles.AsNoTracking().Where(f => f.DispatchId != null &&
                outgoing.Select(o => o.Id).Contains(f.DispatchId.Value)).ToListAsync();
            var requestedRole = r.RequestedByUserId is null ? null
                : RoleLabel(await _db.Users.AsNoTracking().Where(u => u.Id == r.RequestedByUserId).Select(u => (AppRole?)u.Role).FirstOrDefaultAsync());

            var canReview = r.Status is PublicityStatus.PendingApproval or PublicityStatus.PendingFinalApproval && await IsMyTurnAsync(r.Stage);
            var isDispatcher = await IsDispatcherAsync();
            var dto = new PubDetailDto
            {
                Id = r.Id, IndentNo = r.IndentNo, Stage = r.Stage, Status = r.Status, CreatedAt = r.CreatedAt,
                MaterialTypeId = r.MaterialTypeId, Material = pm.Material, ProductId = r.ProductId, Product = pm.Product,
                Quantity = r.Quantity, RequestedByUserId = r.RequestedByUserId, RequestedByName = r.RequestedByName ?? r.CreatedByName,
                RequestedByRole = requestedRole ?? "RMDO",
                StateId = r.StateId, RegionId = r.RegionId, HeadquarterId = r.HeadquarterId,
                State = names.States.GetValueOrDefault(r.StateId, ""), Region = names.Regions.GetValueOrDefault(r.RegionId, ""),
                Hq = names.Hqs.GetValueOrDefault(r.HeadquarterId, ""), Remarks = r.Remarks,
                CanEdit = CanEdit(r),
                CanReview = canReview,
                CanDispatch = isDispatcher && r.Status == PublicityStatus.FinalApproved,
                CanResolve = isDispatcher && r.Status == PublicityStatus.ExceptionRaised,
                SmdReviewedAt = r.SmdReviewedAt,
                SmdReviewedByName = r.SmdReviewedByName,
                // An outgoing transfer's proof file has RequestId == r.Id (uploaded from this very
                // request's own page) AND its DispatchId is in the outgoing set, so it legitimately
                // matches both `files` and `transferFiles` above - dedupe by Id so it is listed once.
                Files = files.Concat(transferFiles).DistinctBy(f => f.Id).Select(ToFile).ToList(),
                Summary = await SummaryAsync(r)
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
            if (dto.CanResolve)
            {
                var candidates = await _db.PublicityRequests.AsNoTracking().Where(x => !x.IsDeleted && x.Id != r.Id &&
                    x.ProductId == r.ProductId && x.MaterialTypeId == r.MaterialTypeId &&
                    (x.Status == PublicityStatus.Completed || x.Status == PublicityStatus.TransferOrderReceived)).Take(50).ToListAsync();
                var srcNames = await NamesAsync(candidates.Select(c => c.StateId), candidates.Select(c => c.RegionId), candidates.Select(c => c.HeadquarterId));
                foreach (var c in candidates)
                {
                    var s = await SummaryAsync(c);
                    var remaining = s.Excess - s.TransferredOut;
                    if (remaining > 0)
                        dto.ExcessSources.Add(new PubExcessDto
                        {
                            RequestId = c.Id, IndentNo = c.IndentNo, Hq = srcNames.Hqs.GetValueOrDefault(c.HeadquarterId, ""),
                            StateId = c.StateId, State = srcNames.States.GetValueOrDefault(c.StateId, ""),
                            RegionId = c.RegionId, Region = srcNames.Regions.GetValueOrDefault(c.RegionId, ""),
                            HqId = c.HeadquarterId,
                            ReportedExcess = s.Excess, Remaining = remaining, LastUpdated = c.UpdatedAt
                        });
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
            var acked = a is { IsDraft: false };
            var ackPill = !acked ? "Pending" : dto.Ack!.ExceptionType == "None" ? "Completed" : "Exception Raised";
            var ackState = !acked ? 0 : dto.Ack!.ExceptionType is "None" or "Excess" ? 2 : 3;
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
            var done = r.Status == PublicityStatus.Completed;
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
                n.TargetRole == role && n.TargetHqId == hqId && n.Title == title && n.ReadAt == null);
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
            var q = _db.PublicityRequests.Include(r => r.Events).Where(r => r.Id == id && !r.IsDeleted);
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

        private async Task<(string Product, string Material)> ProductMaterialPartsAsync(PublicityRequest r) =>
            (await _db.Products.AsNoTracking().Where(p => p.Id == r.ProductId).Select(p => p.Name).FirstOrDefaultAsync() ?? "",
             await _db.PublicityMaterialTypes.AsNoTracking().Where(m => m.Id == r.MaterialTypeId).Select(m => m.Name).FirstOrDefaultAsync() ?? "");

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
            Id = f.Id, DispatchId = f.DispatchId, Kind = f.Kind, FileName = f.FileName, Path = f.Path,
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
