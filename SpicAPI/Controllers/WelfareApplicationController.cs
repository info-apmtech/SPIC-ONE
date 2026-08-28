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
    public class WelfareApplicationController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public WelfareApplicationController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpPost("submit")]
        public Task<IActionResult> SubmitApplication(
            [FromForm] WelfareApplicationSubmitDto dto,
            [FromForm] List<IFormFile> files,
            [FromForm] List<string> documentTypes)
            => SaveApplication(dto, files, documentTypes, isDraft: false);

        [HttpPost("draft")]
        public Task<IActionResult> SaveDraftApplication(
            [FromForm] WelfareApplicationSubmitDto dto,
            [FromForm] List<IFormFile> files,
            [FromForm] List<string> documentTypes)
            => SaveApplication(dto, files, documentTypes, isDraft: true);

        private async Task<IActionResult> SaveApplication(
            WelfareApplicationSubmitDto dto,
            List<IFormFile>? files,
            List<string>? documentTypes,
            bool isDraft)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var dealer = await _db.DealerRegistrations
                .FirstOrDefaultAsync(d => d.UserTableId == userId);
            if (dealer == null)
                return NotFound(new { Message = "Dealer not found." });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            var schemeEnum = ParseSchemeName(dto.SchemeName);
            if (schemeEnum == null)
                return BadRequest(new { Message = $"Invalid scheme name: {dto.SchemeName}" });

            // Backend eligibility validation for Sub Dealer and Approved Employee (final submit only; drafts skip this check)
            if (!isDraft && !string.IsNullOrEmpty(dto.BeneficiaryGroup))
            {
                var eligibilityError = await ValidateGroupEligibilityAsync(dealer.Id, dto.BeneficiaryGroup);
                if (eligibilityError != null)
                    return BadRequest(eligibilityError);
            }

            // Backend mandatory-document checks (final submit only; drafts skip these checks)
            if (!isDraft)
            {
                var meritError = ValidateRequiredMeritDocuments(dto, documentTypes);
                if (meritError != null)
                    return meritError;

                var deathReliefError = ValidateRequiredDeathReliefAffidavit(dto, documentTypes);
                if (deathReliefError != null)
                    return deathReliefError;
            }

            var applicationNumber = GenerateApplicationNumber(schemeEnum.Value);

            var app = new WelfareApplication
            {
                DealerId = dealer.Id,
                SchemeName = schemeEnum.Value,
                ApplicationNumber = applicationNumber,
                ApplicationDate = DateTime.Now,
                Status = isDraft ? WelfareApplicationStatus.Draft : WelfareApplicationStatus.Submitted,

                // Dealer snapshot
                DealerCode = dealer.SPICCode ?? dealer.DealerCode,
                DealerName = user.Name,
                DealershipNature = dealer.BusinessEntityType,
                MobileNumber = dealer.OfficialContactNumber,
                Region = dealer.Region > 0
                    ? (_db.Regions.FirstOrDefault(r => r.Id == dealer.Region)?.RegionName)
                    : null,
                District = dealer.DistrictId.HasValue
                    ? (_db.Districts.FirstOrDefault(d => d.Id == dealer.DistrictId.Value)?.DistrictName)
                    : null,

                // Audit
                CreatedBy = userId,
                CreatedAt = DateTime.Now,
                UpdatedBy = userId,
                UpdatedAt = DateTime.Now,
            };

            ApplyFormFields(app, dto);

            _db.WelfareApplications.Add(app);
            await _db.SaveChangesAsync();

            // Store uploaded files
            await SaveUploadedFilesAsync(app, files, documentTypes, userId);

            // Audit trail
            _db.WelfareApplicationActionLogs.Add(new WelfareApplicationActionLog
            {
                WelfareApplicationId = app.Id,
                ActorLevel = null,
                Action = isDraft ? "DraftSaved" : "Submitted",
                Remarks = null,
                ActorName = user.Name ?? user.UserName,
                CreatedBy = userId,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Ok(new
            {
                ApplicationId = app.Id,
                ApplicationNumber = app.ApplicationNumber,
                Status = app.Status.ToString(),
                Message = isDraft ? "Draft saved successfully." : "Application submitted successfully."
            });
        }

        // =====================================================================
        //  RESUBMIT - dealer corrects a returned application WITHOUT creating a
        //  new one. The SAME application id/number is kept, documents are
        //  updated and the workflow restarts from Pending MO.
        // =====================================================================

        [HttpPost("resubmit/{id:int}")]
        public async Task<IActionResult> ResubmitApplication(
            int id,
            [FromForm] WelfareApplicationSubmitDto dto,
            [FromForm] List<IFormFile> files,
            [FromForm] List<string> documentTypes,
            [FromForm] string? removedDocumentIds)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var dealer = await _db.DealerRegistrations
                .FirstOrDefaultAsync(d => d.UserTableId == userId);
            if (dealer == null)
                return NotFound(new { Message = "Dealer not found." });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            var app = await _db.WelfareApplications
                .Include(a => a.Documents)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (app == null || app.DealerId != dealer.Id)
                return NotFound(new { Message = "Application not found." });

            // Only applications returned to the dealer for correction may be resubmitted.
            // This prevents bypassing the MO/RM/SM/AVP approval chain.
            if (app.Status != WelfareApplicationStatus.ReturnedToDealer)
            {
                return Conflict(new
                {
                    Message = "Only applications that were rejected by the MO and returned to you for correction can be resubmitted.",
                    CurrentStatus = (int)app.Status,
                    CurrentStatusDisplay = SDWAWelfareApplicationController.GetStatusDisplayName(app.Status)
                });
            }

            var schemeEnum = ParseSchemeName(dto.SchemeName);
            if (schemeEnum == null)
                return BadRequest(new { Message = $"Invalid scheme name: {dto.SchemeName}" });

            // The scheme of an existing application cannot be changed during resubmission.
            if (schemeEnum.Value != app.SchemeName)
                return BadRequest(new { Message = "The welfare scheme of an existing application cannot be changed." });

            // Same backend eligibility validation as a final submit
            if (!string.IsNullOrEmpty(dto.BeneficiaryGroup))
            {
                var eligibilityError = await ValidateGroupEligibilityAsync(dealer.Id, dto.BeneficiaryGroup);
                if (eligibilityError != null)
                    return BadRequest(eligibilityError);
            }

            // Backend mandatory-document checks. Previously stored documents that the dealer keeps count, documents being removed/replaced do not.
            var removedIds = ParseRemovedDocumentIds(removedDocumentIds);
            var meritError = ValidateRequiredMeritDocuments(dto, documentTypes, app.Documents, removedIds);
            if (meritError != null)
                return meritError;

            var deathReliefError = ValidateRequiredDeathReliefAffidavit(dto, documentTypes, app.Documents, removedIds);
            if (deathReliefError != null)
                return deathReliefError;

            // Update the corrected details on the SAME application record
            ApplyFormFields(app, dto);
            app.UpdatedBy = userId;
            app.UpdatedAt = DateTime.Now;

            // Restart the approval workflow from Pending MO
            app.Status = WelfareApplicationStatus.Submitted;
            app.ResubmissionCount++;
            app.LastResubmittedAt = DateTime.Now;
            app.LastResubmittedBy = userId;

            // Remove documents the dealer replaced / deleted (ids sent comma-separated)
            if (removedIds.Count > 0)
            {
                foreach (var doc in app.Documents.Where(d => removedIds.Contains(d.Id)).ToList())
                {
                    TryDeleteDocumentFile(doc.FilePath);
                    _db.WelfareApplicationDocuments.Remove(doc);
                }
            }

            await _db.SaveChangesAsync();

            // Store newly uploaded / replacement files
            await SaveUploadedFilesAsync(app, files, documentTypes, userId);

            // Audit trail
            _db.WelfareApplicationActionLogs.Add(new WelfareApplicationActionLog
            {
                WelfareApplicationId = app.Id,
                ActorLevel = null,
                Action = "Resubmitted",
                Remarks = $"Resubmission #{app.ResubmissionCount} by dealer after rejection.",
                ActorName = user.Name ?? user.UserName,
                CreatedBy = userId,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Ok(new
            {
                ApplicationId = app.Id,
                ApplicationNumber = app.ApplicationNumber,
                Status = app.Status.ToString(),
                Message = "Application resubmitted successfully. It has been sent back to the MO for review."
            });
        }

        private static WelfareSchemeType? ParseSchemeName(string name)
        {
            return name?.ToLowerInvariant() switch
            {
                "wedding" => WelfareSchemeType.Wedding,
                "sathabhishekam" => WelfareSchemeType.Sathabhishekam,
                "grahapravesam" => WelfareSchemeType.Grahapravesam,
                "educational assistance" => WelfareSchemeType.EducationalAssistance,
                "medical assistance" => WelfareSchemeType.MedicalAssistance,
                "death relief" => WelfareSchemeType.DeathRelief,
                "merit award" => WelfareSchemeType.MeritAward,
                "distinction award" => WelfareSchemeType.DistinctionAward,
                _ => null,
            };
        }

        // =====================================================================
        //  Shared helpers (submit + resubmit)
        // =====================================================================

        /// <summary>
        /// Backend eligibility gate for Sub Dealer / Approved Employee groups.
        /// Returns a BadRequest payload when the dealer is not eligible, otherwise null.
        /// </summary>
        private async Task<object?> ValidateGroupEligibilityAsync(int dealerId, string beneficiaryGroup)
        {
            var salesHistory = await CalculateDealerSalesAverage(dealerId);

            if (beneficiaryGroup == "Sub Dealer" && salesHistory < 50000)
            {
                return new
                {
                    Message = "You do not have sufficient sales quantity to apply for a Sub Dealer. A minimum average annual sales of 50,000 MT over the last 3 years is required.",
                    ActualAverage = salesHistory,
                    RequiredAverage = 50000
                };
            }

            if (beneficiaryGroup == "Approved Employee" && salesHistory < 5000)
            {
                return new
                {
                    Message = "You do not have sufficient sales quantity to apply for an Approved Employee. A minimum average annual sales of 5,000 MT over the last 3 years is required.",
                    ActualAverage = salesHistory,
                    RequiredAverage = 5000
                };
            }

            return null;
        }

        /// <summary>
        /// Merit Award requires the marks list of the selected examination only:
        /// "10th" -> "Marks list of 10th standard", "12th" -> "Marks list of +2 examinations".
        /// The document for the other examination is optional. Mirrors the frontend
        /// conditional requirement.
        /// </summary>
        private static IActionResult? ValidateRequiredMeritDocuments(
            WelfareApplicationSubmitDto dto,
            List<string>? documentTypes,
            IEnumerable<WelfareApplicationDocument>? existingDocuments = null,
            IEnumerable<int>? removedIds = null)
        {
            if (!string.Equals(dto.SchemeName, "Merit Award", StringComparison.OrdinalIgnoreCase))
                return null;

            string? requiredDocument = null;
            if (string.Equals(dto.ExaminationAppeared, "10th", StringComparison.OrdinalIgnoreCase))
                requiredDocument = "Marks list of 10th standard";
            else if (string.Equals(dto.ExaminationAppeared, "12th", StringComparison.OrdinalIgnoreCase))
                requiredDocument = "Marks list of +2 examinations";

            if (requiredDocument == null)
                return null;

            var uploaded = documentTypes?.Any(t => string.Equals(t, requiredDocument, StringComparison.OrdinalIgnoreCase)) ?? false;
            var kept = existingDocuments?.Any(d =>
                (removedIds == null || !removedIds.Contains(d.Id)) &&
                string.Equals(d.DocumentType, requiredDocument, StringComparison.OrdinalIgnoreCase)) ?? false;

            if (uploaded || kept)
                return null;

            return new BadRequestObjectResult(new
            {
                Message = $"{requiredDocument} is required when {dto.ExaminationAppeared} examination is selected."
            });
        }

        /// <summary>
        /// Death Relief requires the "Affidavit from other legal heirs" document only when
        /// the Legal Heir Relation is "Others". Mirrors the frontend conditional requirement.
        /// </summary>
        private static IActionResult? ValidateRequiredDeathReliefAffidavit(
            WelfareApplicationSubmitDto dto,
            List<string>? documentTypes,
            IEnumerable<WelfareApplicationDocument>? existingDocuments = null,
            IEnumerable<int>? removedIds = null)
        {
            if (!string.Equals(dto.SchemeName, "Death Relief", StringComparison.OrdinalIgnoreCase))
                return null;

            if (!string.Equals(dto.LegalHeirRelation, "Others", StringComparison.OrdinalIgnoreCase))
                return null;

            const string affidavit = "Affidavit from other legal heirs";

            var uploaded = documentTypes?.Any(t => string.Equals(t, affidavit, StringComparison.OrdinalIgnoreCase)) ?? false;
            var kept = existingDocuments?.Any(d =>
                (removedIds == null || !removedIds.Contains(d.Id)) &&
                string.Equals(d.DocumentType, affidavit, StringComparison.OrdinalIgnoreCase)) ?? false;

            if (uploaded || kept)
                return null;

            return new BadRequestObjectResult(new
            {
                Message = "Please upload the affidavit for Others selected."
            });
        }

        /// <summary>
        /// Applies the submitted form values onto an application entity.
        /// Used by BOTH the initial submit and the dealer resubmission so the
        /// corrected details always land on the SAME application record.
        /// Sales/quantity figures are only overwritten when provided, so data
        /// captured at first submission is never lost.
        /// </summary>
        private static void ApplyFormFields(WelfareApplication app, WelfareApplicationSubmitDto dto)
        {
            if (dto.QuantityLifted.HasValue)
                app.QuantityLifted = dto.QuantityLifted;

            // Beneficiary
            app.BeneficiaryName = dto.BeneficiaryName;
            app.BeneficiaryDateOfBirth = dto.BeneficiaryDateOfBirth;
            app.NomineeName = dto.NomineeName;
            app.NomineeRelationship = dto.NomineeRelationship;
            app.BeneficiaryNameAsInCheque = dto.BeneficiaryNameAsInCheque;
            app.LeafOrBankPassbook = dto.LeafOrBankPassbook;

            // Beneficiary Group
            app.BeneficiaryGroup = dto.BeneficiaryGroup;
            app.SubDealerId = dto.SubDealerId;
            app.SubDealerName = dto.SubDealerName;
            app.EmployeeId = dto.EmployeeId;
            app.EmployeeName = dto.EmployeeName;

            if (dto.AverageQuantityLifted3Years.HasValue)
                app.AverageQuantityLifted3Years = dto.AverageQuantityLifted3Years;
            if (dto.LastYearQuantityLifted.HasValue)
                app.LastYearQuantityLifted = dto.LastYearQuantityLifted;

            // Scheme-specific: Wedding
            app.MarriageDate = dto.EventDate;

            // Scheme-specific: Grahapravesam / Sathabhishekam
            app.EventDate = dto.EventDate;
            app.OwnershipType = dto.OwnershipType;
            app.EventVenue = dto.EventVenue;

            // Scheme-specific: Educational
            app.Course = dto.Course;
            app.EduYear = dto.EduYear;
            app.CollegeName = dto.CollegeName;
            app.TotalNumberOfCourses = dto.TotalNumberOfCourses;
            app.IsFirstApplication = dto.IsFirstApplication;

            // Scheme-specific: Medical
            app.MedicalTreatmentType = dto.MedicalTreatmentType;

            // Scheme-specific: Death Relief
            app.DateOfDeath = dto.DateOfDeath;
            app.LegalHeirName = dto.LegalHeirName;
            app.DeathCause = dto.DeathCause;

            // Scheme-specific: Merit Award
            app.MeritCandidateName = dto.MeritCandidateName;
            app.MeritFatherName = dto.MeritFatherName;
            app.ExaminationAppeared = dto.ExaminationAppeared;
            app.BoardName = dto.BoardName;
            app.MaximumMarks = dto.MaximumMarks;
            app.MarksObtained = dto.MarksObtained;
            app.MeritPercentage = dto.MeritPercentage;

            // Scheme-specific: Distinction Award
            app.DistinctionCandidateName = dto.DistinctionCandidateName;
            app.DistinctionFatherName = dto.DistinctionFatherName;
            app.ProfessionalCourseName = dto.ProfessionalCourseName;
            app.CourseCompletionYear = dto.CourseCompletionYear;
            app.UniversityName = dto.UniversityName;
            app.DistinctionMaximumMarks = dto.DistinctionMaximumMarks;
            app.DistinctionMarksObtained = dto.DistinctionMarksObtained;
            app.DistinctionAggregatePercentage = dto.DistinctionAggregatePercentage;
            app.HasArrears = dto.HasArrears;
            app.IsWholesaleDealerEmployee = dto.IsWholesaleDealerEmployee;

            // Declaration
            app.IsDeclarationConfirmed = dto.IsDeclarationConfirmed;
        }

        /// <summary>Stores uploaded files on disk and creates document rows.</summary>
        private async Task SaveUploadedFilesAsync(WelfareApplication app, List<IFormFile>? files, List<string>? documentTypes, string userId)
        {
            if (files == null || files.Count == 0 || documentTypes == null || documentTypes.Count != files.Count)
                return;

            var uploadDir = Path.Combine(_env.ContentRootPath, "Uploads", "Welfare", app.Id.ToString());
            Directory.CreateDirectory(uploadDir);

            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var docType = documentTypes[i];

                if (file.Length == 0) continue;

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var safeFileName = $"{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadDir, safeFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var doc = new WelfareApplicationDocument
                {
                    WelfareApplicationId = app.Id,
                    DocumentType = docType,
                    DocumentName = docType,
                    FileName = file.FileName,
                    FilePath = Path.Combine("Welfare", app.Id.ToString(), safeFileName),
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    UploadedBy = userId,
                    UploadedAt = DateTime.Now,
                };

                _db.WelfareApplicationDocuments.Add(doc);
            }

            await _db.SaveChangesAsync();
        }

        private static List<int> ParseRemovedDocumentIds(string? removedDocumentIds)
        {
            if (string.IsNullOrWhiteSpace(removedDocumentIds))
                return new List<int>();

            return removedDocumentIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => int.TryParse(part, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }

        private void TryDeleteDocumentFile(string? filePath)
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

        private static string GenerateApplicationNumber(WelfareSchemeType scheme)
        {
            var prefix = scheme switch
            {
                WelfareSchemeType.Wedding => "WD",
                WelfareSchemeType.Sathabhishekam => "SH",
                WelfareSchemeType.Grahapravesam => "GH",
                WelfareSchemeType.EducationalAssistance => "EA",
                WelfareSchemeType.MedicalAssistance => "MA",
                WelfareSchemeType.DeathRelief => "DR",
                WelfareSchemeType.MeritAward => "MT",
                WelfareSchemeType.DistinctionAward => "DA",
                _ => "XX",
            };
            var year = DateTime.Now.Year;
            var seq = DateTime.Now.Ticks % 10000;
            return $"SDWA-{prefix}-{year}-{seq:D5}";
        }

        private async Task<decimal> CalculateDealerSalesAverage(int dealerId)
        {
            var today = DateTime.Today;
            int currentFyStartYear = today.Month >= 4 ? today.Year : today.Year - 1;

            var wantedStartYears = new[]
            {
                currentFyStartYear - 3,
                currentFyStartYear - 2,
                currentFyStartYear - 1
            };

            var completedFYs = await _db.FinancialYears
                .AsNoTracking()
                .Where(fy => wantedStartYears.Contains(fy.StartDate.Year))
                .OrderBy(fy => fy.StartDate)
                .ToListAsync();

            if (completedFYs.Count < 3)
            {
                var fallback = await _db.FinancialYears
                    .AsNoTracking()
                    .Where(fy => fy.EndDate < today)
                    .OrderByDescending(fy => fy.EndDate)
                    .Take(3)
                    .OrderBy(fy => fy.StartDate)
                    .ToListAsync();

                if (fallback.Count >= 3)
                    completedFYs = fallback;
            }

            var fyIds = completedFYs.Select(fy => fy.Id).ToList();

            var totalQuantity = await _db.DealerCreditLimitSalesData
                .AsNoTracking()
                .Where(x => x.CustomerId == dealerId && fyIds.Contains(x.FinancialYearId))
                .SumAsync(x => x.Quantity);

            int yearCount = Math.Max(completedFYs.Count, 1);
            return totalQuantity / yearCount;
        }
    }
}
