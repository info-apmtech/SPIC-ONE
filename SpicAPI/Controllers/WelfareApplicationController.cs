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
                var salesHistory = await CalculateDealerSalesAverage(dealer.Id);

                if (dto.BeneficiaryGroup == "Sub Dealer" && salesHistory < 50000)
                {
                    return BadRequest(new
                    {
                        Message = "You do not have sufficient sales quantity to apply for a Sub Dealer. A minimum average annual sales of 50,000 MT over the last 3 years is required.",
                        ActualAverage = salesHistory,
                        RequiredAverage = 50000
                    });
                }

                if (dto.BeneficiaryGroup == "Approved Employee" && salesHistory < 5000)
                {
                    return BadRequest(new
                    {
                        Message = "You do not have sufficient sales quantity to apply for an Approved Employee. A minimum average annual sales of 5,000 MT over the last 3 years is required.",
                        ActualAverage = salesHistory,
                        RequiredAverage = 5000
                    });
                }
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
                QuantityLifted = dto.QuantityLifted,

                // Beneficiary
                BeneficiaryName = dto.BeneficiaryName,
                Relationship = dto.Relationship,
                BeneficiaryDateOfBirth = dto.BeneficiaryDateOfBirth,
                NomineeName = dto.NomineeName,
                NomineeRelationship = dto.NomineeRelationship,
                BeneficiaryNameAsInCheque = dto.BeneficiaryNameAsInCheque,
                LeafOrBankPassbook = dto.LeafOrBankPassbook,

                // Beneficiary Group
                BeneficiaryGroup = dto.BeneficiaryGroup,
                SubDealerId = dto.SubDealerId,
                SubDealerName = dto.SubDealerName,
                EmployeeId = dto.EmployeeId,
                EmployeeName = dto.EmployeeName,
                AverageQuantityLifted3Years = dto.AverageQuantityLifted3Years,
                LastYearQuantityLifted = dto.LastYearQuantityLifted,

                // Scheme-specific: Wedding
                MarriageDate = dto.EventDate,

                // Scheme-specific: Grahapravesam / Sathabhishekam
                EventDate = dto.EventDate,
                OwnershipType = dto.OwnershipType,
                EventVenue = dto.EventVenue,

                // Scheme-specific: Educational
                Course = dto.Course,
                EduYear = dto.EduYear,
                CollegeName = dto.CollegeName,
                TotalNumberOfCourses = dto.TotalNumberOfCourses,
                IsFirstApplication = dto.IsFirstApplication,

                // Scheme-specific: Medical
                MedicalTreatmentType = dto.MedicalTreatmentType,

                // Scheme-specific: Death Relief
                DateOfDeath = dto.DateOfDeath,
                LegalHeirName = dto.LegalHeirName,
                DeathCause = dto.DeathCause,

                // Scheme-specific: Merit Award
                MeritCandidateName = dto.MeritCandidateName,
                MeritFatherName = dto.MeritFatherName,
                ExaminationAppeared = dto.ExaminationAppeared,
                BoardName = dto.BoardName,
                MaximumMarks = dto.MaximumMarks,
                MarksObtained = dto.MarksObtained,
                MeritPercentage = dto.MeritPercentage,

                // Scheme-specific: Distinction Award
                DistinctionCandidateName = dto.DistinctionCandidateName,
                DistinctionFatherName = dto.DistinctionFatherName,
                ProfessionalCourseName = dto.ProfessionalCourseName,
                CourseCompletionYear = dto.CourseCompletionYear,
                UniversityName = dto.UniversityName,
                DistinctionMaximumMarks = dto.DistinctionMaximumMarks,
                DistinctionMarksObtained = dto.DistinctionMarksObtained,
                DistinctionAggregatePercentage = dto.DistinctionAggregatePercentage,
                HasArrears = dto.HasArrears,
                IsWholesaleDealerEmployee = dto.IsWholesaleDealerEmployee,

                // Declaration
                IsDeclarationConfirmed = dto.IsDeclarationConfirmed,

                // Audit
                CreatedBy = userId,
                CreatedAt = DateTime.Now,
                UpdatedBy = userId,
                UpdatedAt = DateTime.Now,
            };

            _db.WelfareApplications.Add(app);
            await _db.SaveChangesAsync();

            // Store uploaded files
            if (files != null && files.Count > 0 && documentTypes != null && documentTypes.Count == files.Count)
            {
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

            return Ok(new
            {
                ApplicationId = app.Id,
                ApplicationNumber = app.ApplicationNumber,
                Status = app.Status.ToString(),
                Message = isDraft ? "Draft saved successfully." : "Application submitted successfully."
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
