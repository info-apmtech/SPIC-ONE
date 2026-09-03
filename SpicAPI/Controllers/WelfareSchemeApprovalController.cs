using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using System.Linq.Expressions;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// Welfare Scheme Approval workflow: MO -> RM -> SM -> SDWA -> SDWA Admin (final approval).
    /// MO/RM/SM are resolved from the actor's AppRole (unchanged). The SDWA and SDWA
    /// Admin stages are each resolved from the actor's existing Designation.RoleAccess
    /// granting the existing PagePermission.SDWA / PagePermission.SDWAAdmin page
    /// permissions, NOT from AppRole and NOT from the Designation's name - any AppRole
    /// whose Designation's RoleAccess includes an "SDWA.*" / "SDWAAdmin.*" token acts
    /// as that approver. There is deliberately NO AppRole.SDWAAdmin - SDWA Admin is a
    /// Designation/permission concept only. SDWA Admin can only ever act once an
    /// application has already cleared the SDWA stage (see NextStage) - it is never skippable.
    /// Each stage can only act on applications currently pending at its own stage;
    /// the stage is enforced server-side (frontend hiding alone is not trusted).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WelfareSchemeApprovalController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<WelfareSchemeApprovalController> _logger;

        public WelfareSchemeApprovalController(AppDbContext db, IWebHostEnvironment env, ILogger<WelfareSchemeApprovalController> logger)
        {
            _db = db;
            _env = env;
            _logger = logger;
        }

        // =====================================================================
        //  Workflow definition: role -> statuses actionable by that role
        // =====================================================================

        // MO/RM/SM stages only - resolved from the actor's AppRole. The SDWA and
        // SDWA Admin stages are NOT here: they are resolved from the actor's
        // Designation.RoleAccess permissions (see GetDesignationApproverFlagsAsync /
        // GetActorStatuses / ResolveActingRole below), so they work no matter what
        // AppRole the SDWA/SDWA-Admin-permissioned user holds.
        private static readonly IReadOnlyDictionary<AppRole, WelfareApplicationStatus[]> StageStatuses =
            new Dictionary<AppRole, WelfareApplicationStatus[]>
            {
                [AppRole.MO]  = new[] { WelfareApplicationStatus.Submitted, WelfareApplicationStatus.MOReview }, // Pending MO
                [AppRole.RM]  = new[] { WelfareApplicationStatus.RMReview },                                     // Pending RM
                [AppRole.SMM] = new[] { WelfareApplicationStatus.SMReview },                                     // Pending SM
            };

        // Stage TRANSITION definitions (not authorization - purely "what status comes
        // next/previous for this internal stage marker"). AppRole.AVP / AppRole.RMD are
        // reused here PURELY as internal storage/transition markers for the SDWA / SDWA
        // Admin stages (WelfareApplicationApproval.ApprovalLevel is AppRole-typed, and
        // there is deliberately no AppRole.SDWAAdmin - SDWA Admin is a Designation/
        // permission concept only, never an AppRole). Neither AppRole.AVP nor
        // AppRole.RMD is ever used to GRANT access by itself anywhere in this file -
        // access is decided exclusively by ResolveActingRole below, which only ever
        // returns these markers after the actor's Designation permission has already
        // been checked (GetDesignationApproverFlagsAsync). AppRole.RMD's real-world
        // meaning (Regional Manager Deputy, used elsewhere in the app e.g. Logistics/
        // Dashboard) is otherwise completely unrelated and untouched by this reuse.
        private static readonly IReadOnlyDictionary<AppRole, WelfareApplicationStatus> NextStage =
            new Dictionary<AppRole, WelfareApplicationStatus>
            {
                [AppRole.MO]  = WelfareApplicationStatus.RMReview,
                [AppRole.RM]  = WelfareApplicationStatus.SMReview,
                [AppRole.SMM] = WelfareApplicationStatus.AVPReview,
                [AppRole.AVP] = WelfareApplicationStatus.SDWAAdminReview, // SDWA approves -> Pending SDWA Admin
                [AppRole.RMD] = WelfareApplicationStatus.Approved         // SDWA Admin approves -> Approved (final)
            };

        // Reverse rejection flow: on rejection the application goes back ONE stage
        // (SDWA Admin -> SDWA, SDWA -> SM, SM -> RM, RM -> MO); a MO rejection returns
        // it to the dealer for correction/resubmission instead of permanently closing it.
        private static readonly IReadOnlyDictionary<AppRole, WelfareApplicationStatus> PreviousStage =
            new Dictionary<AppRole, WelfareApplicationStatus>
            {
                [AppRole.RMD] = WelfareApplicationStatus.AVPReview, // SDWA Admin rejects -> back to Pending SDWA
                [AppRole.AVP] = WelfareApplicationStatus.SMReview,
                [AppRole.SMM] = WelfareApplicationStatus.RMReview,
                [AppRole.RM]  = WelfareApplicationStatus.MOReview,
                [AppRole.MO]  = WelfareApplicationStatus.ReturnedToDealer
            };

        // AppRole-based approver roles (MO/RM/SM). The SDWA and SDWA Admin stages are
        // granted separately and exclusively via their Designation permissions - see IsApprover().
        internal static readonly AppRole[] ApproverRoles =
            { AppRole.MO, AppRole.RM, AppRole.SMM };

        private static readonly AppRole[] ViewerRoles =
            { AppRole.Admin, AppRole.CorporateAdmin, AppRole.Director };

        private static readonly WelfareApplicationStatus[] AllPendingStages =
            { WelfareApplicationStatus.Submitted, WelfareApplicationStatus.MOReview, WelfareApplicationStatus.RMReview, WelfareApplicationStatus.SMReview, WelfareApplicationStatus.AVPReview, WelfareApplicationStatus.SDWAAdminReview };

        // =====================================================================
        //  Permission-based approver resolution (SDWA / SDWA Admin - no AppRole involved)
        // =====================================================================

        // Bundles the two Designation-permission-based approver flags for one actor.
        // A user can hold either, both, or neither, independent of their AppRole and
        // independent of each other (e.g. SDWA without SDWA Admin, or both at once).
        private readonly record struct DesignationApproverFlags(bool IsSdwaApprover, bool IsSdwaAdminApprover)
        {
            public bool Any => IsSdwaApprover || IsSdwaAdminApprover;
        }

        // Resolves whether the given user's Designation.RoleAccess grants the given
        // PagePermission, following UserInfo.DesignationId -> Designation.RoleAccess
        // (exactly like AuthenticationController.Login() already resolves RoleAccess)
        // via the SAME parsing rules LoginState.CanAccess/Can use client-side
        // (RoleAccessPermissions). NOT based on AppRole, Designation NAME, UserId,
        // EmployeeId or DesignationId comparisons - purely "does this user's existing
        // RoleAccess grant this permission". Shared by both the SDWA and SDWA Admin checks.
        private async Task<bool> UserHasDesignationPermissionAsync(UserInfo user, PagePermission permission)
        {
            if (!user.DesignationId.HasValue || user.DesignationId.Value <= 0)
                return false;

            var designation = await _db.Designations
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == user.DesignationId.Value && d.IsActive);

            return designation != null && RoleAccessPermissions.HasPage(designation.RoleAccess, permission);
        }

        private async Task<DesignationApproverFlags> GetDesignationApproverFlagsAsync(UserInfo user) =>
            new DesignationApproverFlags(
                await UserHasDesignationPermissionAsync(user, PagePermission.SDWA),
                await UserHasDesignationPermissionAsync(user, PagePermission.SDWAAdmin));

        // Is this actor an approver at all - by AppRole (MO/RM/SM) OR by SDWA/SDWA Admin Designation?
        private static bool IsApprover(AppRole? role, DesignationApproverFlags flags) =>
            (role.HasValue && ApproverRoles.Contains(role.Value)) || flags.Any;

        // All statuses this actor may act on right now: their AppRole's stage
        // (if MO/RM/SM) UNION the SDWA stage (if their Designation grants SDWA) UNION
        // the SDWA Admin stage (if their Designation grants SDWA Admin). A user can
        // hold any combination at once (e.g. AppRole=MO plus SDWA Admin permission).
        private static WelfareApplicationStatus[] GetActorStatuses(AppRole? role, DesignationApproverFlags flags)
        {
            var statuses = new List<WelfareApplicationStatus>();
            if (role.HasValue && StageStatuses.TryGetValue(role.Value, out var roleStatuses))
                statuses.AddRange(roleStatuses);
            if (flags.IsSdwaApprover)
                statuses.Add(WelfareApplicationStatus.AVPReview);
            if (flags.IsSdwaAdminApprover)
                statuses.Add(WelfareApplicationStatus.SDWAAdminReview);
            return statuses.Distinct().ToArray();
        }

        // Resolves which capacity the actor is acting IN for one specific
        // application, matched against that application's CURRENT status.
        // Returns the AppRole to use for stage transition/display/territory
        // purposes (AppRole.AVP / AppRole.RMD are internal stage markers - see the
        // NextStage/PreviousStage comment above), or null if this actor is not
        // authorized to act on the application in its current state. SDWA Admin can
        // only ever be resolved when the application is ALREADY at the SDWA Admin
        // stage - it structurally cannot be reached before the SDWA stage has
        // approved it (see NextStage), so SDWA is never skipped.
        private static AppRole? ResolveActingRole(AppRole? role, DesignationApproverFlags flags, WelfareApplicationStatus status)
        {
            if (role.HasValue && StageStatuses.TryGetValue(role.Value, out var roleStatuses) && roleStatuses.Contains(status))
                return role.Value;
            if (flags.IsSdwaApprover && status == WelfareApplicationStatus.AVPReview)
                return AppRole.AVP;
            if (flags.IsSdwaAdminApprover && status == WelfareApplicationStatus.SDWAAdminReview)
                return AppRole.RMD;
            return null;
        }

        // =====================================================================
        //  Approval queue - list + stats + tabs for the logged-in approver
        // =====================================================================

        [HttpGet("applications")]
        public async Task<ActionResult<WelfareApprovalPageDto>> GetApplications([FromQuery] string? tab)
        {
            var (role, user) = await GetActorAsync();
            if (user == null)
                return Unauthorized();

            var flags = await GetDesignationApproverFlagsAsync(user);
            bool isApprover = IsApprover(role, flags);
            bool isViewer = role.HasValue && ViewerRoles.Contains(role.Value);
            if (!isApprover && !isViewer)
                return StatusCode(403, new { Message = "You are not authorized to access welfare scheme approvals." });

            var activeTab = NormalizeTab(tab);

            // Geographic visibility: MO -> own HQ, RM -> own Region, SMM -> own State.
            // The SDWA / SDWA Admin final approvers / Admin / CorporateAdmin / Director
            // see every application.
            Expression<Func<DealerRegistration, bool>> dealerScope = flags.Any
                ? (dealer => true)
                : BuildDealerGeoScope(role);

            var applications = await _db.WelfareApplications
                .AsNoTracking()
                .Include(a => a.Approvals)
                .Where(a => a.Status != WelfareApplicationStatus.Draft && a.Status != WelfareApplicationStatus.Cancelled)
                .Where(a => _db.DealerRegistrations.Where(dealerScope).Any(d => d.Id == a.DealerId))
                .OrderByDescending(a => a.ApplicationDate)
                .ToListAsync();

            var page = new WelfareApprovalPageDto
            {
                ActiveTab = activeTab,
                Stats = BuildStats(applications, role, flags),
                Tabs = BuildTabs(applications, role, flags),
                Applications = applications
                    .Where(a => MatchesTab(a, activeTab, role, flags))
                .OrderByDescending(a => a.ApplicationDate)
                .Select(a => MapToListRow(a, activeTab, role, flags))
                .ToList()
            };

            return Ok(page);
        }

        // =====================================================================
        //  Application detail for the approving officer
        // =====================================================================

        [HttpGet("applications/{id:int}")]
        public async Task<ActionResult<WelfareApplicationDetailDto>> GetApplication(int id)
        {
            var (role, user) = await GetActorAsync();
            if (user == null)
                return Unauthorized();

            var flags = await GetDesignationApproverFlagsAsync(user);
            if (!IsApprover(role, flags) && !(role.HasValue && ViewerRoles.Contains(role.Value)))
                return StatusCode(403, new { Message = "You are not authorized to view welfare scheme approvals." });

            var application = await _db.WelfareApplications
                .AsNoTracking()
                .Include(a => a.Documents)
                .Include(a => a.Approvals)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null || application.Status == WelfareApplicationStatus.Draft)
                return NotFound(new { Message = "Application not found." });

            // Same territory rule as the queue: officers may only open applications
            // belonging to dealers inside their own HQ / Region / State. The SDWA /
            // SDWA Admin final approvers see every territory (same as the old AVP behavior).
            if (!flags.Any && !await IsDealerWithinScopeAsync(application.DealerId, role))
                return StatusCode(403, new { Message = "This application belongs to another territory." });

            return Ok(MapToDetail(application));
        }

        // =====================================================================
        //  Document download for approving officers
        // =====================================================================

        [HttpGet("document/{documentId:int}")]
        public async Task<IActionResult> GetDocument(int documentId, [FromQuery] string? disposition)
        {
            var (role, user) = await GetActorAsync();
            var flags = user != null ? await GetDesignationApproverFlagsAsync(user) : default;
            if (!IsApprover(role, flags) && !(role.HasValue && ViewerRoles.Contains(role.Value)))
                return StatusCode(403, new { Message = "You are not authorized to download this document." });

            var document = await _db.WelfareApplicationDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null || string.IsNullOrWhiteSpace(document.FilePath))
                return NotFound(new { Message = "Document not found." });

            var uploadsRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "Uploads"));
            var fullPath = Path.GetFullPath(Path.Combine(uploadsRoot, document.FilePath.Replace('\\', '/')));

            if (!fullPath.StartsWith(uploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                !System.IO.File.Exists(fullPath))
                return NotFound(new { Message = "File not found on server." });

            var contentType = string.IsNullOrWhiteSpace(document.ContentType)
                ? GetContentType(fullPath)
                : document.ContentType;

            var download = string.Equals(disposition, "attachment", StringComparison.OrdinalIgnoreCase);
            var fileName = string.IsNullOrWhiteSpace(document.FileName)
                ? Path.GetFileName(fullPath)
                : document.FileName;

            return PhysicalFile(fullPath, contentType, download ? fileName : null);
        }

        // =====================================================================
        //  MO document management - delete an incorrect uploaded document
        //  (removes the DB row and the physical file). MO role only.
        // =====================================================================

        [HttpDelete("document/{documentId:int}")]
        public async Task<IActionResult> DeleteDocument(int documentId)
        {
            var (role, user) = await GetActorAsync();
            if (user == null)
                return Unauthorized();

            if (!role.HasValue || role.Value != AppRole.MO)
                return StatusCode(403, new { Message = "Only the MO can delete documents." });

            var document = await _db.WelfareApplicationDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
                return NotFound(new { Message = "Document not found." });

            // Only allow deletion of documents belonging to an application the MO
            // is allowed to access (territory rule).
            var application = await _db.WelfareApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == document.WelfareApplicationId);

            if (application == null || !await IsDealerWithinScopeAsync(application.DealerId, role))
                return StatusCode(403, new { Message = "This document belongs to an application outside your territory." });

            DeleteDocumentFile(document.FilePath);
            _db.WelfareApplicationDocuments.Remove(document);

            _db.WelfareApplicationActionLogs.Add(new WelfareApplicationActionLog
            {
                WelfareApplicationId = application.Id,
                ActorLevel = role.Value,
                Action = "DocumentDeleted",
                Remarks = $"MO deleted document '{document.DocumentName ?? document.FileName}'.",
                ActorName = BuildActorName(user, role.Value),
                CreatedBy = user.Id,
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();

            return Ok(new { Success = true, Message = "Document deleted successfully." });
        }

        // =====================================================================
        //  MO document REPLACE - replace the file of an EXISTING document row
        //  with a corrected file. MO role only.
        //
        //  This updates ONLY the targeted document record (identified by
        //  documentId): its physical file is swapped, the DB row's file
        //  metadata is updated in place, and the old file is deleted. The
        //  document's Id, WelfareApplicationId, DocumentType and DocumentName
        //  are preserved, so no duplicate document is created and no other
        //  document/application is affected.
        // =====================================================================

        [HttpPost("document/{documentId:int}/replace")]
        public async Task<IActionResult> ReplaceDocument(int documentId, [FromForm] IFormFile file, [FromForm] string documentType)
        {
            var (role, user) = await GetActorAsync();
            if (user == null)
                return Unauthorized();

            if (!role.HasValue || role.Value != AppRole.MO)
                return StatusCode(403, new { Message = "Only the MO can replace documents." });

            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "A replacement file is required." });

            var document = await _db.WelfareApplicationDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
                return NotFound(new { Message = "Document not found." });

            // Only allow replacement of a document belonging to an application the
            // MO is allowed to access (territory rule).
            var application = await _db.WelfareApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == document.WelfareApplicationId);

            if (application == null)
                return NotFound(new { Message = "Application not found." });

            if (!await IsDealerWithinScopeAsync(application.DealerId, role))
                return StatusCode(403, new { Message = "This document belongs to an application outside your territory." });

            // Save the new file into the same application's upload folder.
            var oldFilePath = document.FilePath;
            var uploadDir = Path.Combine(_env.ContentRootPath, "Uploads", "Welfare", application.Id.ToString());
            System.IO.Directory.CreateDirectory(uploadDir);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var safeFileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadDir, safeFileName);

            if (!Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(uploadDir) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { Message = "Invalid file path." });

            using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Update the EXISTING document row in place (no new record).
            var previousName = document.DocumentName ?? document.FileName;
            document.FileName = file.FileName;
            document.FilePath = Path.Combine("Welfare", application.Id.ToString(), safeFileName);
            document.ContentType = file.ContentType;
            document.FileSize = file.Length;
            document.UploadedBy = user.Id;
            document.UploadedAt = DateTime.Now;

            _db.WelfareApplicationActionLogs.Add(new WelfareApplicationActionLog
            {
                WelfareApplicationId = application.Id,
                ActorLevel = role.Value,
                Action = "DocumentReplaced",
                Remarks = $"MO replaced document '{previousName}' with '{file.FileName}'.",
                ActorName = BuildActorName(user, role.Value),
                CreatedBy = user.Id,
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();

            // Best-effort removal of the old physical file (the DB row now points
            // to the new file, so the old one is no longer referenced).
            DeleteDocumentFile(oldFilePath);

            return Ok(new { Success = true, Message = "Document replaced successfully." });
        }

        // =====================================================================
        //  MO document upload - add a corrected/replacement document.
        //  MO role only. Reuses the same storage pattern as the dealer flow.
        // =====================================================================

        [HttpPost("applications/{id:int}/documents")]
        public async Task<IActionResult> UploadDocument(int id, [FromForm] List<IFormFile> files, [FromForm] List<string> documentTypes)
        {
            var (role, user) = await GetActorAsync();
            if (user == null)
                return Unauthorized();

            if (!role.HasValue || role.Value != AppRole.MO)
                return StatusCode(403, new { Message = "Only the MO can upload documents." });

            if (files == null || files.Count == 0 || documentTypes == null || documentTypes.Count != files.Count)
                return BadRequest(new { Message = "A file and a document type are required." });

            var application = await _db.WelfareApplications
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
                return NotFound(new { Message = "Application not found." });

            if (!await IsDealerWithinScopeAsync(application.DealerId, role))
                return StatusCode(403, new { Message = "This application is outside your territory." });

            var uploadDir = Path.Combine(_env.ContentRootPath, "Uploads", "Welfare", id.ToString());
            System.IO.Directory.CreateDirectory(uploadDir);

            var actorName = BuildActorName(user, role.Value);
            var uploadedCount = 0;

            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var docType = documentTypes[i];
                if (file.Length == 0) continue;

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var safeFileName = $"{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadDir, safeFileName);
                if (Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(uploadDir) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
                else
                {
                    continue;
                }

                _db.WelfareApplicationDocuments.Add(new WelfareApplicationDocument
                {
                    WelfareApplicationId = application.Id,
                    DocumentType = docType,
                    DocumentName = docType,
                    FileName = file.FileName,
                    FilePath = Path.Combine("Welfare", id.ToString(), safeFileName),
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    UploadedBy = user.Id,
                    UploadedAt = DateTime.Now
                });

                uploadedCount++;
            }

            if (uploadedCount == 0)
                return BadRequest(new { Message = "No files were uploaded." });

            _db.WelfareApplicationActionLogs.Add(new WelfareApplicationActionLog
            {
                WelfareApplicationId = application.Id,
                ActorLevel = role.Value,
                Action = "DocumentUploaded",
                Remarks = $"MO uploaded {uploadedCount} document(s).",
                ActorName = actorName,
                CreatedBy = user.Id,
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();

            return Ok(new { Success = true, Message = $"{uploadedCount} document(s) uploaded successfully." });
        }

        // =====================================================================
        //  APPROVE - only the role owning the current stage may approve
        // =====================================================================

        [HttpPost("applications/{id:int}/approve")]
        public async Task<ActionResult<WelfareApprovalActionResponse>> Approve(int id, [FromBody] WelfareApprovalActionRequest? request)
        {
            return await ProcessAction(id, request, isApproval: true);
        }

        // =====================================================================
        //  REJECT - only the role owning the current stage may reject
        // =====================================================================

        [HttpPost("applications/{id:int}/reject")]
        public async Task<ActionResult<WelfareApprovalActionResponse>> Reject(int id, [FromBody] WelfareApprovalActionRequest? request)
        {
            return await ProcessAction(id, request, isApproval: false);
        }

        // =====================================================================
        //  Shared approve/reject pipeline with strict stage enforcement
        // =====================================================================

        private async Task<ActionResult<WelfareApprovalActionResponse>> ProcessAction(int id, WelfareApprovalActionRequest? request, bool isApproval)
        {
            var (role, user) = await GetActorAsync();
            if (user == null)
                return Unauthorized();

            var flags = await GetDesignationApproverFlagsAsync(user);
            if (!IsApprover(role, flags))
                return StatusCode(403, new { Message = "You are not authorized to approve or reject welfare scheme applications." });

            var application = await _db.WelfareApplications
                .Include(a => a.Approvals)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
                return NotFound(new { Message = "Application not found." });

            // Resolve WHICH capacity this actor is using for THIS application, matched
            // against its current status - not a blanket "role == X" check. A user can
            // be, for example, AppRole=MO with a separate SDWA Admin permission: they
            // act as MO on MO-stage applications and as the SDWA Admin final approver on
            // SDWA-Admin-stage applications, resolved purely from the application's status.
            var actingRole = ResolveActingRole(role, flags, application.Status);
            if (actingRole == null)
            {
                var actual = GetStatusDisplayName(application.Status);
                return Conflict(new
                {
                    Message = $"This application is currently '{actual}'. You are not authorized to process it at this stage.",
                    CurrentStatus = (int)application.Status,
                    CurrentStatusDisplay = actual
                });
            }

            // Territory guard: an officer must never be able to approve or reject an
            // application outside their own HQ / Region / State, even when calling the
            // endpoint directly. The SDWA final approver is global (same as old AVP).
            if (!await IsDealerWithinScopeAsync(application.DealerId, actingRole))
                return StatusCode(403, new { Message = "This application belongs to another territory." });

            var remarks = request?.Remarks?.Trim();

            if (!isApproval && string.IsNullOrWhiteSpace(remarks))
                return BadRequest(new { Message = "Remarks are mandatory when rejecting an application." });

            // MO approval must carry an explicit recommendation and a comment.
            // Gated on the RESOLVED acting capacity for this application, not the
            // actor's raw AppRole - an MO-role user acting as SDWA final approver on a
            // final-stage application must not be forced through the MO comment whitelist.
            var recommendation = request?.Recommendation?.Trim();
            var comment = request?.Comment?.Trim();
            if (isApproval && actingRole == AppRole.MO)
            {
                if (!string.Equals(recommendation, "Verified", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(recommendation, "Rejected", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { Message = "A valid recommendation (Verified / Rejected) is mandatory when approving at the MO stage." });

                if (!IsValidMoComment(comment))
                    return BadRequest(new { Message = "A valid comment is mandatory when approving at the MO stage." });
            }

            var actorName = BuildActorName(user, actingRole.Value);
            var now = DateTime.Now;

            // Record the approval step against the existing per-level approval entity.
            var step = application.Approvals.FirstOrDefault(x => x.ApprovalLevel == actingRole.Value);
            if (step == null)
            {
                step = new WelfareApplicationApproval
                {
                    WelfareApplicationId = application.Id,
                    ApprovalLevel = actingRole.Value,
                    CreatedBy = actorName,
                    CreatedAt = now
                };
                application.Approvals.Add(step);
                _db.WelfareApplicationApprovals.Add(step);
            }

            step.ApprovalStatus = isApproval ? WelfareApprovalStatus.Approved : WelfareApprovalStatus.Rejected;
            step.ApprovedBy = actorName;
            step.ApprovedAt = now;
            step.Remarks = isApproval
                ? remarks
                : string.IsNullOrWhiteSpace(request?.Reason)
                    ? remarks
                    : $"{request!.Reason!.Trim()}: {remarks}";
            // Recommendation/Comment captured at approval; cleared on reject so a
            // stale value from an earlier approve cycle never lingers.
            step.Recommendation = isApproval ? recommendation : null;
            step.Comment = isApproval ? comment : null;
            step.UpdatedBy = actorName;
            step.UpdatedAt = now;

            if (isApproval)
            {
                application.Status = NextStage[actingRole.Value];   // MO->RM, RM->SM, SM->SDWA, SDWA->SDWA Admin, SDWA Admin->Approved
            }
            else
            {
                // Reverse rejection flow: return to the previous stage instead of stopping the workflow.
                // SDWA Admin reject -> SDWA, SDWA reject -> SM, SM reject -> RM, RM reject -> MO, MO reject -> Dealer (resubmission required).
                application.Status = PreviousStage[actingRole.Value];
            }

            application.UpdatedBy = actorName;
            application.UpdatedAt = now;

            // Immutable audit entry so the complete approval/rejection/resubmission history is preserved
            _db.WelfareApplicationActionLogs.Add(new WelfareApplicationActionLog
            {
                WelfareApplicationId = application.Id,
                ActorLevel = actingRole.Value,
                Action = isApproval ? "Approved" : "Rejected",
                Remarks = step.Remarks,
                ActorName = actorName,
                CreatedBy = actorName,
                CreatedAt = now
            });

            await _db.SaveChangesAsync();

            var responseMessage = isApproval
                ? $"Application approved and moved to '{GetStatusDisplayName(application.Status)}'."
                : application.Status == WelfareApplicationStatus.ReturnedToDealer
                    ? "Application rejected and returned to the dealer for correction and resubmission."
                    : $"Application rejected by {GetStageDisplay(actingRole.Value)} and returned to the {GetStageDisplayFromStatus(application.Status)} stage.";

            _logger.LogInformation(
                "Welfare application {ApplicationId} {Action} at {Stage} by {Actor}. New status: {Status}.",
                id, isApproval ? "approved" : "rejected", GetStageDisplay(actingRole.Value), actorName, application.Status);

            return Ok(new WelfareApprovalActionResponse
            {
                Success = true,
                Message = responseMessage,
                ApplicationId = application.Id,
                Status = (int)application.Status,
                StatusDisplay = GetStatusDisplayName(application.Status)
            });
        }

        // =====================================================================
        //  Helpers
        // =====================================================================

        private async Task<(AppRole?, UserInfo?)> GetActorAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return (null, null);

            AppRole? role = null;
            var roleClaim = User.FindFirstValue(ClaimTypes.Role);
            if (!string.IsNullOrEmpty(roleClaim) && Enum.TryParse<AppRole>(roleClaim, out var parsed))
                role = parsed;

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            return (role, user);
        }

        // -------------------------------------------------------------------
        //  Geographic visibility — mirrors the dealer registration flow:
        //    MO  -> only dealers of the officer's own HQ
        //    RM  -> only dealers of the officer's own Region
        //    SMM -> only dealers of the officer's own State
        //    AVP / Admin / CorporateAdmin / Director -> everything
        //  The approver's geography comes from the JWT claims minted at login
        //  (spic:hq_id / spic:region_id / spic:state_id). A geo-scoped role
        //  whose token carries no usable claim is restricted to NOTHING rather
        //  than silently seeing every territory's applications.
        // -------------------------------------------------------------------

        private static readonly AppRole[] GeoScopedApproverRoles = { AppRole.MO, AppRole.RM, AppRole.SMM };

        private static bool IsGeoScoped(AppRole? role) =>
            role.HasValue && GeoScopedApproverRoles.Contains(role.Value);

        private (int HqId, int RegionId, int StateId) GetActorGeoIds() =>
        (
            int.TryParse(User.FindFirst("spic:hq_id")?.Value, out var hqId) ? hqId : 0,
            int.TryParse(User.FindFirst("spic:region_id")?.Value, out var regionId) ? regionId : 0,
            int.TryParse(User.FindFirst("spic:state_id")?.Value, out var stateId) ? stateId : 0
        );

        // EF-translatable predicate over DealerRegistration used to scope the
        // approval queue (composed as a correlated EXISTS on WelfareApplication.DealerId).
        private Expression<Func<DealerRegistration, bool>> BuildDealerGeoScope(AppRole? role)
        {
            if (!IsGeoScoped(role))
                return dealer => true;

            var (hqId, regionId, stateId) = GetActorGeoIds();

            if (role == AppRole.MO && hqId > 0)
            {
                var hq = hqId;
                return dealer => dealer.HQ == hq;
            }

            if (role == AppRole.RM && regionId > 0)
            {
                var region = regionId;
                return dealer => dealer.Region == region;
            }

            if (role == AppRole.SMM && stateId > 0)
            {
                var state = stateId;
                return dealer => dealer.StateId == state;
            }

            return dealer => false;
        }

        // Single-application variant used by the detail and approve/reject endpoints.
        private Task<bool> IsDealerWithinScopeAsync(int dealerId, AppRole? role)
        {
            var scope = BuildDealerGeoScope(role);
            return _db.DealerRegistrations.AsNoTracking().Where(scope).AnyAsync(d => d.Id == dealerId);
        }

        // The MO comment must be one of the fixed verification verdicts defined
        // in the MO approval UI. Single source for the allowed set.
        private static readonly string[] MoCommentOptions =
        {
            "Information verified and eligible for approval",
            "Documents incomplete or not valid",
            "Information mismatch found",
            "Eligibility criteria not met at verification stage"
        };

        private static bool IsValidMoComment(string? comment) =>
            !string.IsNullOrWhiteSpace(comment) &&
            MoCommentOptions.Contains(comment, StringComparer.OrdinalIgnoreCase);

        private static string BuildActorName(UserInfo user, AppRole role)
            => $"{(string.IsNullOrWhiteSpace(user.Name) ? user.UserName : user.Name)} ({GetStageDisplay(role)})";

        // AppRole.AVP / AppRole.RMD are internal storage markers for the SDWA / SDWA
        // Admin stages (see NextStage/PreviousStage comment above and ResolveActingRole)
        // - they display as "SDWA" / "SDWA Admin" here, regardless of whether the
        // acting user's actual AppRole happens to match. There is no AppRole.SDWAAdmin.
        internal static string GetStageDisplay(AppRole role) => role switch
        {
            AppRole.MO => "MO",
            AppRole.RM => "RM",
            AppRole.SMM => "SM",
            AppRole.AVP => "SDWA",
            AppRole.RMD => "SDWA Admin",
            _ => role.ToString()
        };

        internal static string GetStatusDisplayName(WelfareApplicationStatus status) => status switch
        {
            WelfareApplicationStatus.Draft => "Draft",
            WelfareApplicationStatus.Submitted => "Pending MO",
            WelfareApplicationStatus.MOReview => "Pending MO",
            WelfareApplicationStatus.RMReview => "Pending RM",
            WelfareApplicationStatus.SMReview => "Pending SM",
            WelfareApplicationStatus.AVPReview => "Pending SDWA",
            WelfareApplicationStatus.SDWAAdminReview => "Pending SDWA Admin",
            WelfareApplicationStatus.Approved => "Approved",
            WelfareApplicationStatus.Rejected => "Rejected",
            WelfareApplicationStatus.Cancelled => "Cancelled",
            WelfareApplicationStatus.ReturnedToDealer => "Returned to Dealer",
            _ => status.ToString()
        };

        // Stage name for the status an application was returned to by the reverse rejection flow
        private static string GetStageDisplayFromStatus(WelfareApplicationStatus status) => status switch
        {
            WelfareApplicationStatus.AVPReview => "SDWA",
            WelfareApplicationStatus.SMReview => "SM",
            WelfareApplicationStatus.RMReview => "RM",
            WelfareApplicationStatus.MOReview or WelfareApplicationStatus.Submitted => "MO",
            _ => "-"
        };

        private static string NormalizeTab(string? tab)
        {
            var value = (tab ?? "pending").Trim().ToLowerInvariant();
            return value switch
            {
                "approvedbyme" => "approvedbyme",
                "validatedmo" => "validatedmo",
                "recommendedrm" => "recommendedrm",
                "recommendedsm" => "recommendedsm",
                "rejected" => "rejected",
                "completed" => "completed",
                _ => "pending"
            };
        }

        private static bool MatchesTab(WelfareApplication app, string tab, AppRole? role, DesignationApproverFlags flags)
        {
            // For read-only viewers (Admin/Director), "pending" shows everything still in the flow.
            if (tab == "pending")
                return IsApprover(role, flags)
                    ? GetActorStatuses(role, flags).Contains(app.Status)
                    : AllPendingStages.Contains(app.Status);

            return tab switch
            {
                // Applications this approver has already cleared stay visible to them
                // (labelled "Approved by <stage>"); apps back pending at their own stage
                // after a reverse rejection are excluded so they remain actionable only.
                "approvedbyme" => ClearedByApprover(app, role, flags),
                "validatedmo" => app.Status is WelfareApplicationStatus.RMReview or WelfareApplicationStatus.SMReview or WelfareApplicationStatus.AVPReview or WelfareApplicationStatus.SDWAAdminReview,
                "recommendedrm" => app.Status is WelfareApplicationStatus.SMReview or WelfareApplicationStatus.AVPReview or WelfareApplicationStatus.SDWAAdminReview,
                "recommendedsm" => app.Status is WelfareApplicationStatus.AVPReview or WelfareApplicationStatus.SDWAAdminReview,
                "rejected" => app.Status == WelfareApplicationStatus.Rejected,
                "completed" => app.Status == WelfareApplicationStatus.Approved,
                _ => false
            };
        }

        private static bool ClearedByApprover(WelfareApplication app, AppRole? role, DesignationApproverFlags flags)
        {
            bool clearedByRole = role.HasValue &&
                ApproverRoles.Contains(role.Value) &&
                !StageStatuses[role.Value].Contains(app.Status) &&
                app.Approvals.Any(ap => ap.ApprovalLevel == role.Value && ap.ApprovalStatus == WelfareApprovalStatus.Approved);

            bool clearedAsSdwa = flags.IsSdwaApprover &&
                app.Status != WelfareApplicationStatus.AVPReview &&
                app.Approvals.Any(ap => ap.ApprovalLevel == AppRole.AVP && ap.ApprovalStatus == WelfareApprovalStatus.Approved);

            bool clearedAsSdwaAdmin = flags.IsSdwaAdminApprover &&
                app.Status != WelfareApplicationStatus.SDWAAdminReview &&
                app.Approvals.Any(ap => ap.ApprovalLevel == AppRole.RMD && ap.ApprovalStatus == WelfareApprovalStatus.Approved);

            return clearedByRole || clearedAsSdwa || clearedAsSdwaAdmin;
        }

        private static WelfareApprovalStatsDto BuildStats(List<WelfareApplication> apps, AppRole? role, DesignationApproverFlags flags)
        {
            Func<WelfareApplicationStatus[], int> countStages = stages => apps.Count(a => stages.Contains(a.Status));

            return new WelfareApprovalStatsDto
            {
                TotalApplications = apps.Count,
                PendingMyStage = IsApprover(role, flags)
                    ? countStages(GetActorStatuses(role, flags))
                    : countStages(AllPendingStages),
                ValidatedByMO = countStages(new[] { WelfareApplicationStatus.RMReview, WelfareApplicationStatus.SMReview, WelfareApplicationStatus.AVPReview, WelfareApplicationStatus.SDWAAdminReview }),
                Rejected = apps.Count(a => a.Status == WelfareApplicationStatus.Rejected),
                Completed = apps.Count(a => a.Status == WelfareApplicationStatus.Approved)
            };
        }

        private static List<WelfareApprovalTabDto> BuildTabs(List<WelfareApplication> apps, AppRole? role, DesignationApproverFlags flags)
        {
            var tabs = new List<WelfareApprovalTabDto>();

            var pendingCount = IsApprover(role, flags)
                ? apps.Count(a => GetActorStatuses(role, flags).Contains(a.Status))
                : apps.Count(a => AllPendingStages.Contains(a.Status));

            tabs.Add(new WelfareApprovalTabDto { Key = "pending", Label = "Pending", Count = pendingCount });
            if (IsApprover(role, flags))
            {
                // SDWA Admin identity takes precedence over SDWA, which takes precedence
                // over role, in the label when present - most specific/senior capacity wins;
                // MO/RM/SM (and SDWA) still act normally on their own stages regardless.
                var label = flags.IsSdwaAdminApprover ? "SDWA Admin" : flags.IsSdwaApprover ? "SDWA" : GetStageDisplay(role!.Value);
                tabs.Add(new WelfareApprovalTabDto
                {
                    Key = "approvedbyme",
                    Label = $"Approved by {label}",
                    Count = apps.Count(a => ClearedByApprover(a, role, flags))
                });
            }
            tabs.Add(new WelfareApprovalTabDto
            {
                Key = "validatedmo",
                Label = "Validated by MO",
                Count = apps.Count(a => a.Status is WelfareApplicationStatus.RMReview or WelfareApplicationStatus.SMReview or WelfareApplicationStatus.AVPReview or WelfareApplicationStatus.SDWAAdminReview)
            });
            tabs.Add(new WelfareApprovalTabDto
            {
                Key = "recommendedrm",
                Label = "Recommended by RM",
                Count = apps.Count(a => a.Status is WelfareApplicationStatus.SMReview or WelfareApplicationStatus.AVPReview or WelfareApplicationStatus.SDWAAdminReview)
            });
            tabs.Add(new WelfareApprovalTabDto
            {
                Key = "recommendedsm",
                Label = "Approved by SM",
                Count = apps.Count(a => a.Status is WelfareApplicationStatus.AVPReview or WelfareApplicationStatus.SDWAAdminReview)
            });
            tabs.Add(new WelfareApprovalTabDto { Key = "rejected", Label = "Rejected", Count = apps.Count(a => a.Status == WelfareApplicationStatus.Rejected) });
            tabs.Add(new WelfareApprovalTabDto { Key = "completed", Label = "Completed", Count = apps.Count(a => a.Status == WelfareApplicationStatus.Approved) });

            return tabs;
        }

        private static WelfareApprovalApplicationDto MapToListRow(WelfareApplication a, string tab, AppRole? role, DesignationApproverFlags flags)
        {
            var stageLabel = tab != "approvedbyme"
                ? null
                : flags.IsSdwaAdminApprover ? "SDWA Admin" : flags.IsSdwaApprover ? "SDWA" : (role.HasValue ? GetStageDisplay(role.Value) : null);

            return new WelfareApprovalApplicationDto
            {
                Id = a.Id,
                ApplicationNumber = a.ApplicationNumber,
                Status = (int)a.Status,
                StatusDisplay = stageLabel != null ? $"Approved by {stageLabel}" : GetStatusDisplayName(a.Status),
                DealerCode = a.DealerCode,
                DealerName = a.DealerName,
                Region = a.Region,
                District = a.District,
                SchemeType = (int)a.SchemeName,
                SchemeName = SDWAWelfareApplicationController.GetSchemeDisplayName(a.SchemeName),
                ApplicationDate = a.ApplicationDate,
                BeneficiaryName = a.BeneficiaryName,
                BeneficiaryGroup = a.BeneficiaryGroup,
                Approvals = a.Approvals
                    .OrderBy(ap => ap.CreatedAt)
                    .Select(ap => new WelfareApprovalStepDto
                    {
                        ApprovalLevel = GetStageDisplay(ap.ApprovalLevel),
                        ApprovalStatus = ap.ApprovalStatus.ToString(),
                        Remarks = ap.Remarks,
                        Recommendation = ap.Recommendation,
                        Comment = ap.Comment,
                        ApprovedBy = ap.ApprovedBy,
                        ApprovedAt = ap.ApprovedAt
                    })
                    .ToList()
            };
        }

        private static WelfareApplicationDetailDto MapToDetail(WelfareApplication application) => new()
        {
            Id = application.Id,
            ApplicationNumber = application.ApplicationNumber,
            SchemeType = (int)application.SchemeName,
            SchemeName = SDWAWelfareApplicationController.GetSchemeDisplayName(application.SchemeName),
            ApplicationDate = application.ApplicationDate,
            Status = (int)application.Status,
            StatusDisplay = GetStatusDisplayName(application.Status),

            DealerId = application.DealerId,
            DealerCode = application.DealerCode,
            DealerName = application.DealerName,
            DealershipNature = application.DealershipNature,
            MobileNumber = application.MobileNumber,
            Region = application.Region,
            District = application.District,
            QuantityLifted = application.QuantityLifted,

            BeneficiaryName = application.BeneficiaryName,
            BeneficiaryDateOfBirth = application.BeneficiaryDateOfBirth,
            NomineeName = application.NomineeName,
            NomineeRelationship = application.NomineeRelationship,
            BeneficiaryNameAsInCheque = application.BeneficiaryNameAsInCheque,
            LeafOrBankPassbook = application.LeafOrBankPassbook,

            MarriageDate = application.MarriageDate,
            EventDate = application.EventDate,
            OwnershipType = application.OwnershipType,
            EventVenue = application.EventVenue,

            Course = application.Course,
            EduYear = application.EduYear,
            CollegeName = application.CollegeName,
            TotalNumberOfCourses = application.TotalNumberOfCourses,
            IsFirstApplication = application.IsFirstApplication,

            MedicalTreatmentType = application.MedicalTreatmentType,

            DateOfDeath = application.DateOfDeath,
            LegalHeirName = application.LegalHeirName,
            DeathCause = application.DeathCause,

            MeritCandidateName = application.MeritCandidateName,
            MeritFatherName = application.MeritFatherName,
            ExaminationAppeared = application.ExaminationAppeared,
            BoardName = application.BoardName,
            MaximumMarks = application.MaximumMarks,
            MarksObtained = application.MarksObtained,
            MeritPercentage = application.MeritPercentage,

            DistinctionCandidateName = application.DistinctionCandidateName,
            DistinctionFatherName = application.DistinctionFatherName,
            ProfessionalCourseName = application.ProfessionalCourseName,
            CourseCompletionYear = application.CourseCompletionYear,
            UniversityName = application.UniversityName,
            DistinctionMaximumMarks = application.DistinctionMaximumMarks,
            DistinctionMarksObtained = application.DistinctionMarksObtained,
            DistinctionAggregatePercentage = application.DistinctionAggregatePercentage,
            HasArrears = application.HasArrears,
            IsWholesaleDealerEmployee = application.IsWholesaleDealerEmployee,

            BeneficiaryGroup = application.BeneficiaryGroup,
            SubDealerName = application.SubDealerName,
            EmployeeName = application.EmployeeName,

            AverageQuantityLifted3Years = application.AverageQuantityLifted3Years,
            LastYearQuantityLifted = application.LastYearQuantityLifted,

            IsDeclarationConfirmed = application.IsDeclarationConfirmed,

            Documents = application.Documents
                .OrderBy(d => d.UploadedAt)
                .Select(d => new WelfareApplicationDocumentDto
                {
                    Id = d.Id,
                    DocumentType = d.DocumentType,
                    DocumentName = d.DocumentName,
                    FileName = d.FileName,
                    ContentType = d.ContentType,
                    FileSize = d.FileSize,
                    IsVerified = d.IsVerified,
                    UploadedAt = d.UploadedAt
                })
                .ToList(),

            Approvals = application.Approvals
                .OrderBy(ap => ap.CreatedAt)
                .Select(ap => new WelfareApprovalStepDto
                {
                    ApprovalLevel = GetStageDisplay(ap.ApprovalLevel),
                    ApprovalStatus = ap.ApprovalStatus.ToString(),
                    Remarks = ap.Remarks,
                    Recommendation = ap.Recommendation,
                    Comment = ap.Comment,
                    ApprovedBy = ap.ApprovedBy,
                    ApprovedAt = ap.ApprovedAt
                })
                .ToList()
        };

        private static string GetContentType(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }

        private void DeleteDocumentFile(string? filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return;

                var uploadsRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "Uploads"));
                var fullPath = Path.GetFullPath(Path.Combine(uploadsRoot, filePath.Replace('\\', '/')));

                if (fullPath.StartsWith(uploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                    System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch
            {
                // Physical cleanup is best-effort; the DB row is removed regardless.
            }
        }
    }
}
