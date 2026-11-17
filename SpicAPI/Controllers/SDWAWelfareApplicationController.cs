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
        private const long MaxImageAttachmentBytes = 10L * 1024 * 1024;
        private const long MaxPdfAttachmentBytes = 25L * 1024 * 1024;
        private const int MaxPdfAttachmentPages = 20;
        private const int PdfAttachmentRenderDpi = 120;
        private static readonly string[] SupportedAttachmentExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };

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

                ChequeNumber = application.ChequeNumber,
                ChequeAmount = application.ChequeAmount,
                ChequeImagePath = application.ChequeImagePath,

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
                var attachedDocuments = await LoadAttachedDocumentsAsync(application);
                var bytes = Services.WelfareApplicationPdfBuilder.Build(application, attachedDocuments);
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

        private async Task<List<Services.WelfareApplicationPdfDocument>> LoadAttachedDocumentsAsync(WelfareApplication application)
        {
            var documents = new List<Services.WelfareApplicationPdfDocument>();
            var uploadsRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "Uploads"));
            var applicationId = application.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var applicationRoot = Path.GetFullPath(Path.Combine(uploadsRoot, "Welfare", applicationId));
            var expectedRelativePrefix = $"Welfare/{applicationId}/";

            foreach (var document in application.Documents.OrderBy(d => d.UploadedAt).ThenBy(d => d.Id))
            {
                if (string.IsNullOrWhiteSpace(document.FilePath))
                    continue;

                var relativePath = document.FilePath.Replace('\\', '/').Trim();
                if (relativePath.Length == 0 || Path.IsPathRooted(relativePath) ||
                    !relativePath.StartsWith(expectedRelativePrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var fullPath = Path.GetFullPath(Path.Combine(uploadsRoot, relativePath));
                if (!fullPath.StartsWith(applicationRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    continue;

                var extension = Path.GetExtension(fullPath);
                if (!SupportedAttachmentExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                    continue;

                var isPdf = extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
                var maxBytes = isPdf ? MaxPdfAttachmentBytes : MaxImageAttachmentBytes;
                var fileInfo = new FileInfo(fullPath);
                if (!fileInfo.Exists || fileInfo.Length == 0 || fileInfo.Length > maxBytes)
                    continue;

                var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                if (!HasMatchingSignature(extension, bytes))
                    continue;

                var pages = isPdf
                    ? RenderPdfPages(bytes, document.Id)
                    : new List<byte[]> { bytes };

                if (pages.Count == 0)
                    continue;

                documents.Add(new Services.WelfareApplicationPdfDocument(BuildDocumentLabel(document), pages));
            }

            return documents;
        }

        private IReadOnlyList<byte[]> RenderPdfPages(byte[] pdfBytes, int documentId)
        {
            int pageCount;
            try
            {
                pageCount = PDFtoImage.Conversion.GetPageCount(pdfBytes, null);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                    _logger.LogWarning(ex, "Could not read welfare document {DocumentId} as a PDF attachment.", documentId);
                return Array.Empty<byte[]>();
            }

            if (pageCount <= 0 || pageCount > MaxPdfAttachmentPages)
            {
                if (_logger != null)
                    _logger.LogWarning("Welfare document {DocumentId} has unsupported PDF page count {PageCount}.", documentId, pageCount);
                return Array.Empty<byte[]>();
            }

            var pages = new List<byte[]>(pageCount);
            for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                try
                {
                    using var stream = new MemoryStream();
                    PDFtoImage.Conversion.SavePng(
                        stream,
                        pdfBytes,
                        (Index)pageIndex,
                        null,
                        new PDFtoImage.RenderOptions { Dpi = PdfAttachmentRenderDpi });

                    if (stream.Length > 0)
                        pages.Add(stream.ToArray());
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                        _logger.LogWarning(ex, "Could not render page {PageNumber} of welfare document {DocumentId} for PDF attachment.", pageIndex + 1, documentId);
                }
            }

            return pages;
        }

        private static bool HasMatchingSignature(string extension, byte[] bytes) => extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => IsJpeg(bytes),
            ".png" => IsPng(bytes),
            ".webp" => IsWebp(bytes),
            ".pdf" => IsPdf(bytes),
            _ => false
        };

        private static bool IsJpeg(byte[] bytes) =>
            bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;

        private static bool IsPng(byte[] bytes) =>
            bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A;

        private static bool IsWebp(byte[] bytes) =>
            bytes.Length >= 12 && bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
            bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P';

        private static bool IsPdf(byte[] bytes) =>
            bytes.Length >= 5 && bytes[0] == (byte)'%' && bytes[1] == (byte)'P' && bytes[2] == (byte)'D' &&
            bytes[3] == (byte)'F' && bytes[4] == (byte)'-';

        private static string BuildDocumentLabel(WelfareApplicationDocument document)
        {
            var documentName = string.IsNullOrWhiteSpace(document.DocumentName) ? document.FileName : document.DocumentName;
            var category = document.DocumentType;

            if (!string.IsNullOrWhiteSpace(documentName) && !string.IsNullOrWhiteSpace(category) &&
                !string.Equals(documentName, category, StringComparison.OrdinalIgnoreCase))
                return $"{documentName} ({category})";

            if (!string.IsNullOrWhiteSpace(documentName))
                return documentName;

            if (!string.IsNullOrWhiteSpace(category))
                return category;

            return "Document";
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
            WelfareApplicationStatus.AVPReview => "Pending SDWA",
            WelfareApplicationStatus.SDWAAdminReview => "Pending SDWA Admin",
            WelfareApplicationStatus.Approved => "Approved",
            WelfareApplicationStatus.Rejected => "Rejected",
            WelfareApplicationStatus.Cancelled => "Cancelled",
            WelfareApplicationStatus.ReturnedToDealer => "Rejected – Resubmission Required",
            _ => status.ToString()
        };

        // AppRole.AVP / AppRole.RMD are internal storage markers reused for the
        // SDWA / SDWA Admin stages (see WelfareSchemeApprovalController) - there is
        // no AppRole.SDWAAdmin.
        private static string GetApprovalLevelDisplayName(AppRole role) => role switch
        {
            AppRole.MO => "Marketing Officer (MO)",
            AppRole.RM => "Regional Manager (RM)",
            AppRole.SMM => "Senior Manager (SM)",
            AppRole.AVP => "SDWA",
            AppRole.RMD => "SDWA Admin",
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
