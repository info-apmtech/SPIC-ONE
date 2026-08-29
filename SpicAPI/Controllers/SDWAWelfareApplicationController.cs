using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SDWAWelfareApplicationController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SDWAWelfareApplicationController> _logger;

        public SDWAWelfareApplicationController(AppDbContext db, IWebHostEnvironment env, ILogger<SDWAWelfareApplicationController> logger)
        {
            _db = db;
            _env = env;
            _logger = logger;
        }

        // =====================================================================
        //  My Applications - summary list for the logged-in dealer
        // =====================================================================

        [HttpGet("my-applications")]
        public async Task<ActionResult<List<WelfareApplicationSummaryDto>>> GetMyApplications()
        {
            var dealer = await GetDealerAsync();
            if (dealer == null)
                return NotFound(new { Message = "Dealer profile not found for the logged-in user." });

            var applications = await _db.WelfareApplications
                .AsNoTracking()
                .Where(a => a.DealerId == dealer.Id)
                .OrderByDescending(a => a.UpdatedAt)
                .Select(a => new WelfareApplicationSummaryDto
                {
                    Id = a.Id,
                    ApplicationNumber = a.ApplicationNumber,
                    SchemeType = (int)a.SchemeName,
                    SchemeName = GetSchemeDisplayName(a.SchemeName),
                    BeneficiaryName = a.BeneficiaryName,
                    BeneficiaryGroup = a.BeneficiaryGroup,
                    ApplicationDate = a.ApplicationDate,
                    Status = (int)a.Status,
                    StatusDisplay = GetStatusDisplayName(a.Status),
                    LastUpdatedAt = a.UpdatedAt,
                    DocumentCount = a.Documents.Count(),
                    IsFirstApplication = a.IsFirstApplication
                })
                .ToListAsync();

            // Attach rejection details (stage + officer + reason) for rejected / returned-for-correction applications.
            var rejectedAppIds = applications
                .Where(x => x.Status == (int)WelfareApplicationStatus.Rejected || x.Status == (int)WelfareApplicationStatus.ReturnedToDealer)
                .Select(x => x.Id)
                .ToList();
            if (rejectedAppIds.Count > 0)
            {
                var rejections = await _db.WelfareApplicationApprovals
                    .AsNoTracking()
                    .Where(ap => rejectedAppIds.Contains(ap.WelfareApplicationId) && ap.ApprovalStatus == WelfareApprovalStatus.Rejected)
                    .OrderByDescending(ap => ap.ApprovedAt)
                    .ToListAsync();

                foreach (var app in applications.Where(x => x.Status == (int)WelfareApplicationStatus.Rejected || x.Status == (int)WelfareApplicationStatus.ReturnedToDealer))
                {
                    var rejection = rejections.FirstOrDefault(r => r.WelfareApplicationId == app.Id);
                    if (rejection == null) continue;

                    var level = WelfareSchemeApprovalController.GetStageDisplay(rejection.ApprovalLevel);
                    app.RejectedByLevel = level;
                    app.RejectedByName = rejection.ApprovedBy;
                    app.RejectedAt = rejection.ApprovedAt;
                    app.RejectionReason = rejection.Remarks;

                    // Reverse rejection flow: MO returned it to the dealer for correction/resubmission
                    if (app.Status == (int)WelfareApplicationStatus.ReturnedToDealer)
                    {
                        app.CanResubmit = true;
                        app.StatusDisplay = $"Rejected by {level} – Resubmission Required";
                    }
                }
            }

            return Ok(applications);
        }

        // =====================================================================
        //  My Applications - previous Educational Assistance applications for
        //  the logged-in dealer (used by the renewal dropdown)
        // =====================================================================

        [HttpGet("my-educational-applications")]
        public async Task<ActionResult<List<EducationalApplicationSummaryDto>>> GetMyEducationalApplications()
        {
            var dealer = await GetDealerAsync();
            if (dealer == null)
                return NotFound(new { Message = "Dealer profile not found for the logged-in user." });

            var applications = await _db.WelfareApplications
                .AsNoTracking()
                .Where(a => a.DealerId == dealer.Id
                         && a.SchemeName == WelfareSchemeType.EducationalAssistance
                         && a.Status != WelfareApplicationStatus.Draft)
                .OrderByDescending(a => a.UpdatedAt)
                .Select(a => new EducationalApplicationSummaryDto
                {
                    Id = a.Id,
                    ApplicationNumber = a.ApplicationNumber,
                    Course = a.Course,
                    EduYear = a.EduYear,
                    CollegeName = a.CollegeName,
                    TotalNumberOfCourses = a.TotalNumberOfCourses,
                    IsFirstApplication = a.IsFirstApplication,
                    Status = (int)a.Status,
                    ApplicationDate = a.ApplicationDate,
                    BeneficiaryName = a.BeneficiaryName
                })
                .ToListAsync();

            return Ok(applications);
        }

        // =====================================================================
        //  My Applications - full detail for one application (ownership enforced)
        // =====================================================================

        [HttpGet("my-application/{id:int}")]
        public async Task<ActionResult<WelfareApplicationDetailDto>> GetMyApplication(int id)
        {
            var dealer = await GetDealerAsync();
            if (dealer == null)
                return NotFound(new { Message = "Dealer profile not found for the logged-in user." });

            var application = await _db.WelfareApplications
                .AsNoTracking()
                .Include(a => a.Documents)
                .Include(a => a.Approvals)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
                return NotFound(new { Message = "Application not found." });

            if (application.DealerId != dealer.Id)
                return NotFound(new { Message = "Application not found." });

            var detail = new WelfareApplicationDetailDto
            {
                Id = application.Id,
                ApplicationNumber = application.ApplicationNumber,
                SchemeType = (int)application.SchemeName,
                SchemeName = GetSchemeDisplayName(application.SchemeName),
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
                SubDealerId = application.SubDealerId,
                SubDealerName = application.SubDealerName,
                EmployeeId = application.EmployeeId,
                EmployeeName = application.EmployeeName,

                AverageQuantityLifted3Years = application.AverageQuantityLifted3Years,
                LastYearQuantityLifted = application.LastYearQuantityLifted,

                IsDeclarationConfirmed = application.IsDeclarationConfirmed,

                CanResubmit = application.Status == WelfareApplicationStatus.ReturnedToDealer,
                ResubmissionCount = application.ResubmissionCount,
                LastResubmittedAt = application.LastResubmittedAt,

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
                        ApprovalLevel = GetApprovalLevelDisplayName(ap.ApprovalLevel),
                        ApprovalStatus = ap.ApprovalStatus.ToString(),
                        Remarks = ap.Remarks,
                        Recommendation = ap.Recommendation,
                        Comment = ap.Comment,
                        ApprovedBy = ap.ApprovedBy,
                        ApprovedAt = ap.ApprovedAt
                    })
                    .ToList()
            };

            return Ok(detail);
        }

        // =====================================================================
        //  My Applications - acknowledgment PDF download (ownership enforced)
        // =====================================================================

        [HttpGet("my-application/{id:int}/pdf")]
        public async Task<IActionResult> GetMyApplicationPdf(int id)
        {
            var dealer = await GetDealerAsync();
            if (dealer == null)
                return NotFound(new { Message = "Dealer profile not found for the logged-in user." });

            var application = await _db.WelfareApplications
                .AsNoTracking()
                .Include(a => a.Documents)
                .Include(a => a.Approvals)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null || application.DealerId != dealer.Id)
                return NotFound(new { Message = "Application not found." });

            try
            {
                var bytes = Services.WelfareApplicationPdfBuilder.Build(application);
                var fileName = string.IsNullOrWhiteSpace(application.ApplicationNumber)
                    ? $"WelfareApplication-{application.Id}.pdf"
                    : $"{application.ApplicationNumber}.pdf";

                return File(bytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate welfare application PDF for application {ApplicationId}.", id);
                return StatusCode(500, new { Message = "Failed to generate the PDF. Please try again." });
            }
        }

        // =====================================================================
        //  Document view / download (ownership enforced)
        // =====================================================================

        [HttpGet("document/{documentId:int}")]
        public async Task<IActionResult> GetDocument(int documentId, [FromQuery] string? disposition)
        {
            var dealer = await GetDealerAsync();
            if (dealer == null)
                return NotFound(new { Message = "Dealer profile not found for the logged-in user." });

            var document = await _db.WelfareApplicationDocuments
                .AsNoTracking()
                .Include(d => d.WelfareApplication)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null || document.WelfareApplication == null || document.WelfareApplication.DealerId != dealer.Id)
                return NotFound(new { Message = "Document not found." });

            if (string.IsNullOrWhiteSpace(document.FilePath))
                return NotFound(new { Message = "Document file path is missing." });

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

            return PhysicalFile(fullPath, contentType,
                download ? fileName : null);
        }

        // =====================================================================
        //  Helpers
        // =====================================================================

        private async Task<DealerRegistration?> GetDealerAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return null;

            return await _db.DealerRegistrations
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserTableId == userId);
        }

        internal static string GetSchemeDisplayName(WelfareSchemeType scheme) => scheme switch
        {
            WelfareSchemeType.MedicalAssistance => "Medical Assistance",
            WelfareSchemeType.Wedding => "Wedding Gift",
            WelfareSchemeType.Grahapravesam => "Grahapravesam",
            WelfareSchemeType.EducationalAssistance => "Educational Assistance",
            WelfareSchemeType.Sathabhishekam => "Sathabhishekam",
            WelfareSchemeType.DeathRelief => "Death Relief",
            WelfareSchemeType.MeritAward => "Merit Award",
            WelfareSchemeType.DistinctionAward => "Distinction Award",
            _ => scheme.ToString()
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
            WelfareApplicationStatus.ReturnedToDealer => "Rejected – Resubmission Required",
            _ => status.ToString()
        };

        private static string GetApprovalLevelDisplayName(AppRole role) => role switch
        {
            AppRole.MO => "Marketing Officer (MO)",
            AppRole.RM => "Regional Manager (RM)",
            AppRole.SMM => "Senior Manager (SM)",
            AppRole.AVP => "AVP",
            _ => role.ToString()
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
