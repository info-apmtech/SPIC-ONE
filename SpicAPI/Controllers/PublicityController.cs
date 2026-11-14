using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
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
            var region = regionId ?? ClaimInt("spic:region_id");
            var peopleQuery = from u in _db.Users.AsNoTracking()
                              join e in _db.Employeelogins.AsNoTracking() on u.Id equals e.UserId
                              where e.IsActive && (region <= 0 || e.RegionId == region)
                              select new { u.Id, u.Name, u.UserName, u.Role };
            var people = (await peopleQuery.Take(500).ToListAsync())
                .Select(p => new PubPerson { UserId = p.Id, Name = p.Name ?? p.UserName ?? p.Id, Role = RoleLabel(p.Role) })
                .GroupBy(p => p.UserId).Select(g => g.First()).OrderBy(p => p.Name).ToList();
            var me = await MyNameAsync();
            if (people.All(p => p.UserId != Uid()))
                people.Insert(0, new PubPerson { UserId = Uid(), Name = me, Role = RoleLabel(MyRole()) });

            var couriers = await _db.SasCouriers.AsNoTracking().Where(c => c.IsActive).Select(c => c.Name).ToListAsync();
            var vendors = await _db.PublicityDispatches.AsNoTracking().Where(d => d.VendorName != null)
                .Select(d => d.VendorName!).Distinct().Take(100).ToListAsync();

            return Ok(new PubLookupsDto
            {
                Materials = await _db.PublicityMaterialTypes.AsNoTracking().Where(m => m.IsActive)
                    .OrderBy(m => m.SortOrder).ThenBy(m => m.Name).Select(m => new PubOption { Id = m.Id, Name = m.Name }).ToListAsync(),
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
            var scoped = (await ScopedAsync()).Where(r => r.Status != PublicityStatus.Draft &&
                r.Status != PublicityStatus.Rejected && r.Status != PublicityStatus.Returned);
            var pending = await scoped.Where(r => r.Stage == mine &&
                (r.Status == PublicityStatus.PendingApproval || r.Status == PublicityStatus.PendingFinalApproval)).ToListAsync();
            var below = await scoped.Where(r => r.Stage < mine && r.Stage >= PublicityStage.RmReview)
                .Select(r => new { r.StateId, r.RegionId }).ToListAsync();
            var rows = await RowsAsync(pending);
            var names = await NamesAsync(pending.Select(r => r.StateId), pending.Select(r => r.RegionId), Array.Empty<int>());

            var groups = pending.Select((r, i) => (r, row: rows[i]))
                .GroupBy(x => (x.r.StateId, x.r.RegionId))
                .Select(g => new PubRegionDto
                {
                    State = names.States.GetValueOrDefault(g.Key.StateId, ""),
                    Region = names.Regions.GetValueOrDefault(g.Key.RegionId, ""),
                    Hqs = g.Select(x => x.r.HeadquarterId).Distinct().Count(),
                    Quantity = g.Sum(x => x.r.Quantity),
                    Pending = g.Count(),
                    Total = g.Count() + below.Count(b => b.StateId == g.Key.StateId && b.RegionId == g.Key.RegionId),
                    LastUpdated = g.Max(x => x.r.UpdatedAt ?? x.r.SubmittedAt ?? x.r.CreatedAt),
                    Rows = g.Select(x => x.row).ToList()
                }).OrderBy(g => g.State).ThenBy(g => g.Region).ToList();

            var scope = await ScopeLabelAsync();
            var nextIndex = Array.FindIndex(ChainStages, s => s == mine) + 1;
            var lastSubmitted = pending.Select(r => r.SubmittedAt ?? r.UpdatedAt ?? r.CreatedAt).DefaultIfEmpty(DateTime.Now).Max();
            var submissionId = pending.Count > 0
                ? $"SUB-{lastSubmitted.Year}-{(pending.FirstOrDefault()?.Id ?? 12):D4}"
                : $"SUB-{DateTime.Now.Year}-0012";

            return Ok(new PubConsolidationDto
            {
                Scope = scope,
                StageLabel = StageLabel(mine.Value),
                NextLabel = nextIndex < ChainStages.Length ? StageLabel(ChainStages[nextIndex]).Replace(" Review", "") : "Dispatch",
                SubmissionId = submissionId,
                SubmittedAt = lastSubmitted,
                Regions = groups.Count,
                Hqs = pending.Select(r => r.HeadquarterId).Distinct().Count(),
                Requests = pending.Count,
                Quantity = pending.Sum(r => r.Quantity),
                ReadyRegions = groups.Count(g => g.Pending == g.Total),
                Groups = groups
            });
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
            var isChainRole = MyRole() is AppRole.RMD or AppRole.MDO or AppRole.RM or AppRole.SMD or AppRole.SMM;
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
            var error = await ValidateAsync(dto);
            if (error != null) return BadRequest(new { Success = false, Message = error });
            var before = new { r.MaterialTypeId, r.ProductId, r.Quantity, r.HeadquarterId, r.RequestedByUserId, r.Remarks };
            await ApplyAsync(r, dto);
            r.UpdatedAt = DateTime.UtcNow;
            if (approverEdit)
            {
                var changes = new List<string>();
                if (before.Quantity != r.Quantity) changes.Add($"Quantity {before.Quantity} -> {r.Quantity}");
                if (before.MaterialTypeId != r.MaterialTypeId) changes.Add("Material changed");
                if (before.ProductId != r.ProductId) changes.Add("Product changed");
                if (before.HeadquarterId != r.HeadquarterId) changes.Add("Location changed");
                if (before.RequestedByUserId != r.RequestedByUserId) changes.Add("Requested by changed");
                if (before.Remarks != r.Remarks) changes.Add("Remarks updated");
                if (changes.Count > 0) await AddEvent(r, r.Stage, "Edited", await MyNameAsync(), string.Join("; ", changes) + ".");
            }
            else
            {
                r.OriginalQuantity = r.Quantity;
                if (dto.Submit) await Submit(r, await MyNameAsync(), dto.Remarks);
            }
            await _db.SaveChangesAsync();
            return Ok(new { r.Id, r.IndentNo });
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

        [HttpPost("requests/review-bulk")]
        public async Task<IActionResult> ReviewBulk([FromBody] PubBulkReviewDto dto)
        {
            var scoped = await ScopedAsync();
            var ids = dto.Ids.Distinct().ToList();
            var items = await _db.PublicityRequests.Where(r => ids.Contains(r.Id) && !r.IsDeleted).ToListAsync();
            var allowed = (await scoped.Where(r => ids.Contains(r.Id)).Select(r => r.Id).ToListAsync()).ToHashSet();
            var done = 0;
            foreach (var r in items.Where(r => allowed.Contains(r.Id)))
                if (await ReviewOneAsync(r, "Approve", null, dto.Remarks) is null) done++;
            await _db.SaveChangesAsync();
            return Ok(new { Success = true, Approved = done });
        }

        private async Task<string?> ReviewOneAsync(PublicityRequest r, string action, int? qty, string? remarks)
        {
            if (r.Status is not (PublicityStatus.PendingApproval or PublicityStatus.PendingFinalApproval))
                return "This request is not waiting for approval.";
            if (!await IsMyTurnAsync(r.Stage)) return "This request is not waiting for your approval.";
            var name = await MyNameAsync();
            // Rewritten from a string switch to if/else-if on purpose: a multi-case string switch
            // statement is lowered by Roslyn into a ReadOnlySpan-based jump table, which cannot be
            // held across an 'await' inside a case body (CS - "ReadOnlySpan<string> cannot be
            // preserved across await or yield boundary").
            var normalized = action?.Trim().ToLowerInvariant();
            if (normalized == "edit")
            {
                if (r.Stage == PublicityStage.DgmMdCsReview) return "DGM - MD CS is approve-only; the quantity cannot be edited at this stage.";
                if (qty is not > 0) return "Enter a valid quantity.";
                var note = $"Quantity changed from {r.Quantity} to {qty}." + (string.IsNullOrWhiteSpace(remarks) ? "" : " " + remarks.Trim());
                r.Quantity = qty.Value;
                await AddEvent(r, r.Stage, "Edited", name, note);
            }
            else if (normalized == "return")
            {
                if (string.IsNullOrWhiteSpace(remarks)) return "Enter the reason for returning the request.";
                await AddEvent(r, r.Stage, "Returned", name, remarks);
                r.Status = PublicityStatus.Returned;
                Notify(PublicityNotificationType.Approval, r, null, r.CreatedByUserId, null, null,
                    $"Request {r.IndentNo} returned", $"{StageLabel(r.Stage)}: {remarks!.Trim()}", "Edit and resubmit the request.");
            }
            else if (normalized == "reject")
            {
                if (string.IsNullOrWhiteSpace(remarks)) return "Enter the reason for rejecting the request.";
                await AddEvent(r, r.Stage, "Rejected", name, remarks);
                r.Status = PublicityStatus.Rejected;
            }
            else
            {
                // DGM - MD CS approves only: unlike every earlier stage, a quantity sent along with
                // the approve call is never applied here, so this stage can't smuggle an edit through.
                if (qty is > 0 && qty != r.Quantity && r.Stage != PublicityStage.DgmMdCsReview)
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
                    Notify(PublicityNotificationType.Approval, r, null, r.CreatedByUserId, null, null,
                        $"Request {r.IndentNo} final approved", "Your publicity material request is approved and ready for dispatch.", null);
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
            if (dto.Quantity <= 0) return BadRequest(new { Success = false, Message = "Enter the dispatch quantity." });
            var name = await MyNameAsync();
            var d = new PublicityDispatch
            {
                RequestId = r.Id,
                Kind = dto.Kind,
                Quantity = dto.Quantity,
                DestinationAddress = Clean(dto.DestinationAddress),
                Remarks = Clean(dto.Remarks),
                CreatedByUserId = Uid(),
                CreatedByName = name
            };

            if (dto.Kind == PublicityDispatchKind.Transfer)
            {
                if (r.Status != PublicityStatus.ExceptionRaised) return BadRequest(new { Success = false, Message = "A stock transfer resolves a raised exception only." });
                var source = dto.SourceRequestId is > 0 ? await LoadAsync(dto.SourceRequestId.Value, tracking: true) : null;
                if (source is null || source.Id == r.Id) return BadRequest(new { Success = false, Message = "Select the source request that holds excess stock." });
                var ss = await SummaryAsync(source);
                var remaining = ss.Excess - ss.TransferredOut;
                if (dto.Quantity > remaining) return BadRequest(new { Success = false, Message = $"Only {remaining} Nos of excess remain at the source HQ." });
                d.DispatchNo = await NextNoAsync("TRF");
                d.Status = PublicityDispatchStatus.TransferRequested;
                d.SourceHqId = source.HeadquarterId;
                d.SourceRequestId = source.Id;
                _db.PublicityDispatches.Add(d);
                await _db.SaveChangesAsync();
                await AddEvent(r, PublicityStage.InterHqTransfer, "Transfer Requested", name, dto.Remarks, d.Id);
                await AddEvent(source, PublicityStage.TransferCreation, "Transfer Order Received", name, $"{d.Quantity} Nos to {r.IndentNo}", d.Id);
                var hqs = await NamesAsync(Array.Empty<int>(), Array.Empty<int>(), new[] { r.HeadquarterId });
                Notify(PublicityNotificationType.StockTransfer, source, d.Id, source.CreatedByUserId, AppRole.RMD, source.HeadquarterId,
                    "Stock Transfer Order Received",
                    $"{d.Quantity} Nos {await ProductMaterialAsync(r)} to be dispatched to {hqs.Hqs.GetValueOrDefault(r.HeadquarterId, "the destination HQ")}",
                    $"From your available excess stock (Request ID: {source.IndentNo})");
                await RecomputeAsync(source);
            }
            else
            {
                if (dto.Kind == PublicityDispatchKind.Original && r.Status != PublicityStatus.FinalApproved)
                    return BadRequest(new { Success = false, Message = "The request is not final approved yet." });
                if (dto.Kind == PublicityDispatchKind.ReInitiation && r.Status != PublicityStatus.ExceptionRaised)
                    return BadRequest(new { Success = false, Message = "Re-initiation resolves a raised exception only." });
                if (string.IsNullOrWhiteSpace(dto.VendorName) || string.IsNullOrWhiteSpace(dto.Transporter) || dto.DispatchDate is null)
                    return BadRequest(new { Success = false, Message = "Vendor, transporter and dispatch date are required." });
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
                Notify(PublicityNotificationType.Dispatch, r, d.Id, r.CreatedByUserId, null, null,
                    $"Material dispatched ({d.DispatchNo})", $"{d.Quantity} Nos dispatched via {d.Transporter}.", d.TrackingNo is null ? null : $"Tracking No: {d.TrackingNo}");
            }

            await RecomputeAsync(r);
            await _db.SaveChangesAsync();
            return Ok(new { d.Id, d.DispatchNo });
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
                Notify(PublicityNotificationType.Dispatch, r, d.Id, r.CreatedByUserId, null, null,
                    $"Material delivered ({d.DispatchNo})", "Please submit the receipt acknowledgement.", null);
            await RecomputeAsync(r);
            if (d.SourceRequestId is int sid && await LoadAsync(sid, tracking: true) is { } source) await RecomputeAsync(source);
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
            if (string.IsNullOrWhiteSpace(dto.Transporter) || string.IsNullOrWhiteSpace(dto.TrackingNo) || dto.DispatchDate is null)
                return BadRequest(new { Success = false, Message = "Courier, tracking number and dispatch date are required." });
            if (!await _db.PublicityFiles.AnyAsync(f => f.DispatchId == d.Id && f.Kind == PublicityFileKind.CourierProof))
                return BadRequest(new { Success = false, Message = "Upload the courier / dispatch proof first." });

            var name = await MyNameAsync();
            d.Transporter = dto.Transporter.Trim();
            d.TrackingNo = dto.TrackingNo.Trim();
            d.DispatchDate = dto.DispatchDate;
            d.VehicleNo = Clean(dto.VehicleNo);
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
            Notify(PublicityNotificationType.StockTransfer, r, d.Id, null, AppRole.MDAdmin, null,
                $"Dispatch proof submitted ({d.DispatchNo})", $"{name} uploaded the courier proof for {d.Quantity} Nos.", "Review and confirm the transfer.");
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
            var by = string.IsNullOrWhiteSpace(dto.AcknowledgedByUserId) ? Uid() : dto.AcknowledgedByUserId!;
            ack.AcknowledgedByUserId = by;
            ack.AcknowledgedByName = await _db.Users.AsNoTracking().Where(u => u.Id == by).Select(u => u.Name ?? u.UserName).FirstOrDefaultAsync() ?? await MyNameAsync();

            if (dto.Submit)
            {
                ack.IsDraft = false;
                ack.SubmittedAt = DateTime.UtcNow;
                d.Status = PublicityDispatchStatus.Acknowledged;
                d.DeliveredAt ??= DateTime.UtcNow;
                var type = ExceptionType(Math.Max(0, d.Quantity - ack.ReceivedQty), Math.Max(0, ack.ReceivedQty - d.Quantity), ack.DamagedQty);
                var name = await MyNameAsync();
                await AddEvent(r, PublicityStage.Acknowledgement, type == "None" ? "Acknowledged" : "Exception Raised", name,
                    ack.Remarks ?? $"Received {ack.ReceivedQty} of {d.Quantity} Nos.", d.Id);
                if (type != "None" && type != "Excess")
                    Notify(PublicityNotificationType.Exception, r, d.Id, null, AppRole.MDAdmin, null,
                        $"Exception raised ({type})", $"{r.IndentNo}: received {ack.ReceivedQty} of {d.Quantity} Nos, {ack.DamagedQty} damaged.", ack.Remarks);
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
            var d = n.DispatchId is int did ? await _db.PublicityDispatches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == did) : null;
            if (d is not null)
            {
                var dest = await LoadAsync(d.RequestId);
                var source = d.SourceRequestId is int sid ? await LoadAsync(sid) : null;
                var names = await NamesAsync(Array.Empty<int>(), Array.Empty<int>(), new[] { dest!.HeadquarterId, source?.HeadquarterId ?? 0 });
                dto.DestinationIndentNo = dest.IndentNo;
                dto.DestinationHq = names.Hqs.GetValueOrDefault(dest.HeadquarterId);
                dto.TransferQty = d.Quantity;
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
            return items.Select(r => new PubRowDto
            {
                Id = r.Id, IndentNo = r.IndentNo, Quantity = r.Quantity, OriginalQuantity = r.OriginalQuantity,
                Product = products.GetValueOrDefault(r.ProductId, ""), Material = materials.GetValueOrDefault(r.MaterialTypeId, ""),
                Hq = names.Hqs.GetValueOrDefault(r.HeadquarterId, ""), State = names.States.GetValueOrDefault(r.StateId, ""),
                Region = names.Regions.GetValueOrDefault(r.RegionId, ""),
                Stage = r.Stage, Status = r.Status, CreatedAt = r.CreatedAt,
                AwaitingMe = IsPendingAt(r.Status, r.Stage, mine),
                StatusNote = r.Status == PublicityStatus.Returned ? $"Return by {StageLabel(r.Stage).Replace(" Review", "")}" : null
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
                outgoing.Select(o => o.Id).Contains(f.DispatchId.Value) && f.RequestId != r.Id).ToListAsync();
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
                Files = files.Concat(transferFiles).Select(ToFile).ToList(),
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
                var hqNames = await NamesAsync(Array.Empty<int>(), Array.Empty<int>(), candidates.Select(c => c.HeadquarterId));
                foreach (var c in candidates)
                {
                    var s = await SummaryAsync(c);
                    var remaining = s.Excess - s.TransferredOut;
                    if (remaining > 0)
                        dto.ExcessSources.Add(new PubExcessDto { RequestId = c.Id, IndentNo = c.IndentNo, Hq = hqNames.Hqs.GetValueOrDefault(c.HeadquarterId, ""), Remaining = remaining });
                }
            }
            return dto;
        }

        private async Task<PubDispatchDto> DispatchDtoAsync(PublicityDispatch d, PublicityRequest dest, List<PublicityFile> files, string destHq)
        {
            var source = d.SourceRequestId is int sid ? await LoadAsync(sid) : null;
            var sourceHq = source is null ? null
                : (await NamesAsync(Array.Empty<int>(), Array.Empty<int>(), new[] { source.HeadquarterId })).Hqs.GetValueOrDefault(source.HeadquarterId);
            var a = d.Acknowledgement;
            var dto = new PubDispatchDto
            {
                Id = d.Id, DispatchNo = d.DispatchNo, RequestId = d.RequestId, RequestIndentNo = dest.IndentNo, Kind = d.Kind, Status = d.Status,
                SourceName = d.Kind == PublicityDispatchKind.Transfer ? sourceHq ?? "Source HQ" : d.VendorName ?? "Vendor",
                SourceHqId = d.SourceHqId, SourceRequestId = d.SourceRequestId, SourceIndentNo = source?.IndentNo,
                DestinationHq = destHq, DestinationAddress = d.DestinationAddress, Quantity = d.Quantity,
                Transporter = d.Transporter, TrackingNo = d.TrackingNo, VehicleNo = d.VehicleNo,
                ResponsibleName = d.ResponsibleName, ResponsibleRole = d.ResponsibleRole,
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
            }
            else
            {
                dto.Steps = Steps(
                    ("Dispatched", d.Transporter, d.CreatedAt, true),
                    ("In Transit", d.CurrentLocation ?? (d.ExpectedDeliveryDate is { } e ? $"Expected Delivery {e:dd MMM yyyy}" : null), d.InTransitAt, d.Status >= PublicityDispatchStatus.InTransit),
                    ("Delivered", d.CurrentLocation, d.DeliveredAt, d.Status >= PublicityDispatchStatus.Delivered),
                    ("Acknowledged", ackPill, a?.SubmittedAt, acked));
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
            var sequence = new[] { "Indent Creation", "Submitted to RM", "RM Review", "SMD Review", "SMM Review", "ADMIN - MD CS Review", "DGM - MD CS Review", "Note Approval", "Dispatch", "Acknowledgement" };
            var current = r.Status is PublicityStatus.PendingApproval or PublicityStatus.PendingFinalApproval ? StageLabel(r.Stage)
                : r.Status == PublicityStatus.FinalApproved ? "Note Approval"
                : r.Status is PublicityStatus.InDispatch or PublicityStatus.AcknowledgementPending ? "Acknowledgement" : null;
            // the stage waiting now always gets its own "Pending Review" row (it may already have
            // older rows, e.g. RM Review after a return and resubmit); then the stages not reached yet
            if (current != null) list.Add(new PubHistoryDto { Stage = current, Action = "Pending Review", State = 1 });
            foreach (var label in sequence.Where(l => l != current && list.All(h => h.Stage != l)))
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
            if (IsSuper() || r.CreatedByUserId == Uid() || r.RequestedByUserId == Uid()) return true;
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

        private async Task<string?> ValidateAsync(PubUpsertDto dto)
        {
            if (dto.Quantity <= 0) return "Enter the required quantity.";
            if (!await _db.PublicityMaterialTypes.AnyAsync(m => m.Id == dto.MaterialTypeId)) return "Select the type of publicity material.";
            if (!await _db.Products.AnyAsync(p => p.Id == dto.ProductId)) return "Select the product.";
            if (!await _db.Headquarters.AnyAsync(h => h.Id == dto.HeadquarterId && h.RegionId == dto.RegionId)) return "Select the headquarter.";
            if (!await _db.Regions.AnyAsync(x => x.Id == dto.RegionId && x.StateId == dto.StateId)) return "Select the region.";
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
                Notify(PublicityNotificationType.Approval, r, null, r.CreatedByUserId, null, null,
                    $"Request {r.IndentNo} final approved", "Your publicity material request is approved and ready for dispatch.", null);
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

        private void Notify(PublicityNotificationType type, PublicityRequest r, int? dispatchId, string? userId, AppRole? role, int? hqId,
            string title, string message, string? sub) =>
            _db.PublicityNotifications.Add(new PublicityNotification
            {
                Type = type, RequestId = r.Id, DispatchId = dispatchId, TargetUserId = userId, TargetRole = role,
                TargetHqId = hqId, Title = title, Message = message, SubText = sub
            });

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
