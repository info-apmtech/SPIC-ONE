using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// SDWA Welfare Scheme Approval workflow (strict MO -> RM -> SM -> AVP).
    /// Each role can only act on applications currently pending at its own stage;
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

        private static readonly IReadOnlyDictionary<AppRole, WelfareApplicationStatus[]> StageStatuses =
            new Dictionary<AppRole, WelfareApplicationStatus[]>
            {
                [AppRole.MO]  = new[] { WelfareApplicationStatus.Submitted, WelfareApplicationStatus.MOReview }, // Pending MO
                [AppRole.RM]  = new[] { WelfareApplicationStatus.RMReview },                                     // Pending RM
                [AppRole.SMM] = new[] { WelfareApplicationStatus.SMReview },                                     // Pending SM
                [AppRole.AVP] = new[] { WelfareApplicationStatus.AVPReview }                                     // Pending AVP
            };

        private static readonly IReadOnlyDictionary<AppRole, WelfareApplicationStatus> NextStage =
            new Dictionary<AppRole, WelfareApplicationStatus>
            {
                [AppRole.MO]  = WelfareApplicationStatus.RMReview,
                [AppRole.RM]  = WelfareApplicationStatus.SMReview,
                [AppRole.SMM] = WelfareApplicationStatus.AVPReview,
                [AppRole.AVP] = WelfareApplicationStatus.Approved
            };

        // Reverse rejection flow: on rejection the application goes back ONE stage
        // (AVP -> SM, SM -> RM, RM -> MO); a MO rejection returns it to the dealer
        // for correction/resubmission instead of permanently closing it.
        private static readonly IReadOnlyDictionary<AppRole, WelfareApplicationStatus> PreviousStage =
            new Dictionary<AppRole, WelfareApplicationStatus>
            {
                [AppRole.AVP] = WelfareApplicationStatus.SMReview,
                [AppRole.SMM] = WelfareApplicationStatus.RMReview,
                [AppRole.RM]  = WelfareApplicationStatus.MOReview,
                [AppRole.MO]  = WelfareApplicationStatus.ReturnedToDealer
            };

        internal static readonly AppRole[] ApproverRoles =
            { AppRole.MO, AppRole.RM, AppRole.SMM, AppRole.AVP };

        private static readonly AppRole[] ViewerRoles =
            { AppRole.Admin, AppRole.CorporateAdmin, AppRole.Director };

        private static readonly WelfareApplicationStatus[] AllPendingStages =
            { WelfareApplicationStatus.Submitted, WelfareApplicationStatus.MOReview, WelfareApplicationStatus.RMReview, WelfareApplicationStatus.SMReview, WelfareApplicationStatus.AVPReview };

        // =====================================================================
        //  Approval queue - list + stats + tabs for the logged-in approver
        // =====================================================================

        [HttpGet("applications")]
        public async Task<ActionResult<WelfareApprovalPageDto>> GetApplications([FromQuery] string? tab)
        {
            var (role, user) = await GetActorAsync();
            if (user == null)
                return Unauthorized();

            bool isApprover = role.HasValue && ApproverRoles.Contains(role.Value);
            bool isViewer = role.HasValue && ViewerRoles.Contains(role.Value);
            if (!isApprover && !isViewer)
                return StatusCode(403, new { Message = "You are not authorized to access welfare scheme approvals." });

            var activeTab = NormalizeTab(tab);

            var applications = await _db.WelfareApplications
                .AsNoTracking()
                .Include(a => a.Approvals)
                .Where(a => a.Status != WelfareApplicationStatus.Draft && a.Status != WelfareApplicationStatus.Cancelled)
                .OrderByDescending(a => a.ApplicationDate)
                .ToListAsync();

            var page = new WelfareApprovalPageDto
            {
                ActiveTab = activeTab,
                Stats = BuildStats(applications, role),
                Tabs = BuildTabs(applications, role),
                Applications = applications
                    .Where(a => MatchesTab(a, activeTab, role))
                    .OrderByDescending(a => a.ApplicationDate)
                    .Select(MapToListRow)
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

            if (!(role.HasValue && ApproverRoles.Contains(role.Value)) && !(role.HasValue && ViewerRoles.Contains(role.Value)))
                return StatusCode(403, new { Message = "You are not authorized to view welfare scheme approvals." });

            var application = await _db.WelfareApplications
                .AsNoTracking()
                .Include(a => a.Documents)
                .Include(a => a.Approvals)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null || application.Status == WelfareApplicationStatus.Draft)
                return NotFound(new { Message = "Application not found." });

            return Ok(MapToDetail(application));
        }

        // =====================================================================
        //  Document download for approving officers
        // =====================================================================

        [HttpGet("document/{documentId:int}")]
        public async Task<IActionResult> GetDocument(int documentId, [FromQuery] string? disposition)
        {
            var (role, _) = await GetActorAsync();
            if (!(role.HasValue && ApproverRoles.Contains(role.Value)) && !(role.HasValue && ViewerRoles.Contains(role.Value)))
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

            if (!role.HasValue || !ApproverRoles.Contains(role.Value))
                return StatusCode(403, new { Message = "You are not authorized to approve or reject welfare scheme applications." });

            var remarks = request?.Remarks?.Trim();

            if (!isApproval && string.IsNullOrWhiteSpace(remarks))
                return BadRequest(new { Message = "Remarks are mandatory when rejecting an application." });

            var application = await _db.WelfareApplications
                .Include(a => a.Approvals)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
                return NotFound(new { Message = "Application not found." });

            // Strict stage check: the application must currently be pending at THIS role's stage.
            var stageStatuses = StageStatuses[role.Value];
            if (!stageStatuses.Contains(application.Status))
            {
                var expected = GetStageDisplay(role.Value);
                var actual = GetStatusDisplayName(application.Status);
                return Conflict(new
                {
                    Message = $"This application is currently '{actual}'. Only the {expected} stage can process it.",
                    CurrentStatus = (int)application.Status,
                    CurrentStatusDisplay = actual
                });
            }

            var actorName = BuildActorName(user, role.Value);
            var now = DateTime.Now;

            // Record the approval step against the existing per-level approval entity.
            var step = application.Approvals.FirstOrDefault(x => x.ApprovalLevel == role.Value);
            if (step == null)
            {
                step = new WelfareApplicationApproval
                {
                    WelfareApplicationId = application.Id,
                    ApprovalLevel = role.Value,
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
            step.UpdatedBy = actorName;
            step.UpdatedAt = now;

            if (isApproval)
            {
                application.Status = NextStage[role.Value];   // MO->RM, RM->SM, SM->AVP, AVP->Approved
            }
            else
            {
                // Reverse rejection flow: return to the previous stage instead of stopping the workflow.
                // AVP reject -> SM, SM reject -> RM, RM reject -> MO, MO reject -> Dealer (resubmission required).
                application.Status = PreviousStage[role.Value];
            }

            application.UpdatedBy = actorName;
            application.UpdatedAt = now;

            // Immutable audit entry so the complete approval/rejection/resubmission history is preserved
            _db.WelfareApplicationActionLogs.Add(new WelfareApplicationActionLog
            {
                WelfareApplicationId = application.Id,
                ActorLevel = role.Value,
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
                    : $"Application rejected by {GetStageDisplay(role.Value)} and returned to the {GetStageDisplayFromStatus(application.Status)} stage.";

            _logger.LogInformation(
                "Welfare application {ApplicationId} {Action} at {Stage} by {Actor}. New status: {Status}.",
                id, isApproval ? "approved" : "rejected", GetStageDisplay(role.Value), actorName, application.Status);

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

        private static string BuildActorName(UserInfo user, AppRole role)
            => $"{(string.IsNullOrWhiteSpace(user.Name) ? user.UserName : user.Name)} ({GetStageDisplay(role)})";

        internal static string GetStageDisplay(AppRole role) => role switch
        {
            AppRole.MO => "MO",
            AppRole.RM => "RM",
            AppRole.SMM => "SM",
            AppRole.AVP => "AVP",
            _ => role.ToString()
        };

        internal static string GetStatusDisplayName(WelfareApplicationStatus status) => status switch
        {
            WelfareApplicationStatus.Draft => "Draft",
            WelfareApplicationStatus.Submitted => "Pending MO",
            WelfareApplicationStatus.MOReview => "Pending MO",
            WelfareApplicationStatus.RMReview => "Pending RM",
            WelfareApplicationStatus.SMReview => "Pending SM",
            WelfareApplicationStatus.AVPReview => "Pending AVP",
            WelfareApplicationStatus.Approved => "Approved",
            WelfareApplicationStatus.Rejected => "Rejected",
            WelfareApplicationStatus.Cancelled => "Cancelled",
            WelfareApplicationStatus.ReturnedToDealer => "Returned to Dealer",
            _ => status.ToString()
        };

        // Stage name for the status an application was returned to by the reverse rejection flow
        private static string GetStageDisplayFromStatus(WelfareApplicationStatus status) => status switch
        {
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
                "validatedmo" => "validatedmo",
                "recommendedrm" => "recommendedrm",
                "recommendedsm" => "recommendedsm",
                "rejected" => "rejected",
                "completed" => "completed",
                _ => "pending"
            };
        }

        private static bool MatchesTab(WelfareApplication app, string tab, AppRole? role)
        {
            // For read-only viewers (Admin/Director), "pending" shows everything still in the flow.
            if (tab == "pending")
                return role.HasValue && ApproverRoles.Contains(role.Value)
                    ? StageStatuses[role.Value].Contains(app.Status)
                    : AllPendingStages.Contains(app.Status);

            return tab switch
            {
                "validatedmo" => app.Status is WelfareApplicationStatus.RMReview or WelfareApplicationStatus.SMReview or WelfareApplicationStatus.AVPReview,
                "recommendedrm" => app.Status is WelfareApplicationStatus.SMReview or WelfareApplicationStatus.AVPReview,
                "recommendedsm" => app.Status == WelfareApplicationStatus.AVPReview,
                "rejected" => app.Status == WelfareApplicationStatus.Rejected,
                "completed" => app.Status == WelfareApplicationStatus.Approved,
                _ => false
            };
        }

        private static WelfareApprovalStatsDto BuildStats(List<WelfareApplication> apps, AppRole? role)
        {
            Func<WelfareApplicationStatus[], int> countStages = stages => apps.Count(a => stages.Contains(a.Status));

            return new WelfareApprovalStatsDto
            {
                TotalApplications = apps.Count,
                PendingMyStage = role.HasValue && ApproverRoles.Contains(role.Value)
                    ? countStages(StageStatuses[role.Value])
                    : countStages(AllPendingStages),
                ValidatedByMO = countStages(new[] { WelfareApplicationStatus.RMReview, WelfareApplicationStatus.SMReview, WelfareApplicationStatus.AVPReview }),
                Rejected = apps.Count(a => a.Status == WelfareApplicationStatus.Rejected),
                Completed = apps.Count(a => a.Status == WelfareApplicationStatus.Approved)
            };
        }

        private static List<WelfareApprovalTabDto> BuildTabs(List<WelfareApplication> apps, AppRole? role)
        {
            var tabs = new List<WelfareApprovalTabDto>();

            var pendingCount = role.HasValue && ApproverRoles.Contains(role.Value)
                ? apps.Count(a => StageStatuses[role.Value].Contains(a.Status))
                : apps.Count(a => AllPendingStages.Contains(a.Status));

            tabs.Add(new WelfareApprovalTabDto { Key = "pending", Label = "Pending", Count = pendingCount });
            tabs.Add(new WelfareApprovalTabDto
            {
                Key = "validatedmo",
                Label = "Validated by MO",
                Count = apps.Count(a => a.Status is WelfareApplicationStatus.RMReview or WelfareApplicationStatus.SMReview or WelfareApplicationStatus.AVPReview)
            });
            tabs.Add(new WelfareApprovalTabDto
            {
                Key = "recommendedrm",
                Label = "Recommended by RM",
                Count = apps.Count(a => a.Status is WelfareApplicationStatus.SMReview or WelfareApplicationStatus.AVPReview)
            });
            tabs.Add(new WelfareApprovalTabDto
            {
                Key = "recommendedsm",
                Label = "Approved by SM",
                Count = apps.Count(a => a.Status == WelfareApplicationStatus.AVPReview)
            });
            tabs.Add(new WelfareApprovalTabDto { Key = "rejected", Label = "Rejected", Count = apps.Count(a => a.Status == WelfareApplicationStatus.Rejected) });
            tabs.Add(new WelfareApprovalTabDto { Key = "completed", Label = "Completed", Count = apps.Count(a => a.Status == WelfareApplicationStatus.Approved) });

            return tabs;
        }

        private static WelfareApprovalApplicationDto MapToListRow(WelfareApplication a) => new()
        {
            Id = a.Id,
            ApplicationNumber = a.ApplicationNumber,
            Status = (int)a.Status,
            StatusDisplay = GetStatusDisplayName(a.Status),
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
                    ApprovedBy = ap.ApprovedBy,
                    ApprovedAt = ap.ApprovedAt
                })
                .ToList()
        };

        private static WelfareApplicationDetailDto MapToDetail(WelfareApplication application) => new()
        {
            Id = application.Id,
            ApplicationNumber = application.ApplicationNumber,
            SchemeType = (int)application.SchemeName,
            SchemeName = SDWAWelfareApplicationController.GetSchemeDisplayName(application.SchemeName),
            ApplicationDate = application.ApplicationDate,
            Status = (int)application.Status,
            StatusDisplay = GetStatusDisplayName(application.Status),

            DealerCode = application.DealerCode,
            DealerName = application.DealerName,
            DealershipNature = application.DealershipNature,
            MobileNumber = application.MobileNumber,
            Region = application.Region,
            District = application.District,
            QuantityLifted = application.QuantityLifted,

            BeneficiaryName = application.BeneficiaryName,
            Relationship = application.Relationship,
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
    }
}
