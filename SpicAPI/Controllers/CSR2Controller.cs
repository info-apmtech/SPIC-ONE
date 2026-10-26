using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using SPIC.Core.DTOs;
using System.Security.Claims;
using static SPIC.Core.Entities.EmployeeRegistration;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CSR2Controller : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        public CSR2Controller(AppDbContext db, IWebHostEnvironment env) { _db = db; _env = env; }

        // Keep the approved CSR-1 visibility rule consistent for list, read and persistence.
        // Adapt this rule to existing state/region/HQ authorization before production.
        private IQueryable<CSR1> VisibleApprovedCSR1()
        {
            var query = _db.Set<CSR1>().AsNoTracking()
                .Where(x => x.IsActive && x.Status == CSR1Status.Approved);
            if (!User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                query = query.Where(x => x.CreatedBy == userId);
            }
            return query;
        }

        [HttpGet("assigned-programs")]
        public async Task<IActionResult> GetAssignedPrograms()
        {
            var records = await VisibleApprovedCSR1()
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    CSR1Id = x.Id,
                    x.ProgramId,
                    x.BudgetProgramId,
                    x.ProgramMonth,
                    x.ProgramYear,
                    x.NumberOfPrograms,
                    ApprovedBudget = x.Budget,
                    SanctionNumber = x.SanctionNo,
                    ProgramDate = x.CSR1Date,
                    x.LocationId,
                    x.HeadquarterId,
                    x.ResponsiblePersonId,
                    CSR1Status = x.Status
                }).ToListAsync();
            var ids = records.Select(x => x.ProgramId).Distinct().ToList();
            var masters = await _db.Set<ProgramMaster>()
                .AsNoTracking().Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, ProgramName = x.Name, ProgramType = x.ProgramType.Name })
                .ToDictionaryAsync(x => x.Id);
            return Ok(records.Select(x =>
            {
                masters.TryGetValue(x.ProgramId, out var master);
                return new
                {
                    x.CSR1Id,
                    x.ProgramId,
                    x.BudgetProgramId,
                    x.ProgramMonth,
                    x.ProgramYear,
                    x.NumberOfPrograms,
                    x.ApprovedBudget,
                    x.SanctionNumber,
                    x.ProgramDate,
                    x.LocationId,
                    x.HeadquarterId,
                    x.ResponsiblePersonId,
                    CSR1Status = x.CSR1Status.ToString(),
                    ProgramName = master?.ProgramName ?? "",
                    ProgramType = master?.ProgramType ?? ""
                };
            }));
        }

        [HttpGet("assigned-programs/{csr1Id:int}")]
        public async Task<IActionResult> GetProgramDetails(int csr1Id)
        {
            var csr1 = await VisibleApprovedCSR1().FirstOrDefaultAsync(x => x.Id == csr1Id);
            if (csr1 == null) return NotFound(new { message = "Approved CSR1 record not found." });

            // Rehydrate the 11 legacy expense properties from CSR1Expenses - EF no longer
            // populates them (they're [NotMapped]; the real data lives in CSR1Expenses now).
            var csr1ExpenseRows = await _db.Set<CSR1Expenses>().AsNoTracking()
                .Where(x => x.CSR1Id == csr1Id && x.IsActive).ToListAsync();
            SPIC.Core.Entities.CSR1ExpenseMapper.ApplyExpenses(csr1, csr1ExpenseRows);
            var master = await _db.Set<ProgramMaster>().AsNoTracking()
                .Where(x => x.Id == csr1.ProgramId)
                .Select(x => new { ProgramName = x.Name, ProgramType = x.ProgramType.Name })
                .FirstOrDefaultAsync();
            var focus1 = await _db.Set<CSR1Products1>().AsNoTracking()
                .Where(x => x.CSR1Id == csr1Id && x.IsActive)
                .Select(x => new { x.CropId, x.ProductId }).ToListAsync();
            var focus2 = await _db.Set<CSR1Products2>().AsNoTracking()
                .Where(x => x.CSR1Id == csr1Id && x.IsActive)
                .Select(x => new { x.CropId, x.ProductId }).ToListAsync();
            var focus3 = await _db.Set<CSR1Products3>().AsNoTracking()
                .Where(x => x.CSR1Id == csr1Id && x.IsActive)
                .Select(x => new { x.CropId, x.ProductId }).ToListAsync();
            // Show every active expense master in CSR-2.
            // If CSR-1 has a matching CSR1Expenses row, use its amount; otherwise planned amount = 0.
            var plannedExpenses = await _db.Set<ExpenseInformation>()
                .AsNoTracking()
                .Where(masterExpense => masterExpense.IsActive)
                .OrderBy(masterExpense => masterExpense.Id)
                .Select(masterExpense => new
                {
                    ExpenseId = masterExpense.Id,
                    ExpenseName = masterExpense.Name,
                    ExpenseAmount = _db.Set<CSR1Expenses>()
                        .Where(e =>
                            e.CSR1Id == csr1Id &&
                            e.IsActive &&
                            e.ExpenseId == masterExpense.Id)
                        .Sum(e => (decimal?)e.ExpenseAmount) ?? 0m
                })
                .ToListAsync();

            return Ok(new
            {
                CSR1Id = csr1.Id,
                csr1.ProgramId,
                ProgramName = master?.ProgramName ?? "",
                ProgramType = master?.ProgramType ?? "",
                csr1.BudgetProgramId,
                csr1.ProgramMonth,
                csr1.ProgramYear,
                csr1.NumberOfPrograms,
                csr1.Budget,
                csr1.SanctionNo,
                csr1.CSR1Date,
                csr1.HeadquarterId,
                csr1.LocationId,
                csr1.ResponsiblePersonId,
                Status = csr1.Status.ToString(),
                Expenses = plannedExpenses,
                Focus1 = focus1,
                Focus2 = focus2,
                Focus3 = focus3
            });
        }

        [HttpGet("participant-mobile-search")]
        public async Task<IActionResult> SearchParticipantMobile([FromQuery] string? term)
        {
            var search = new string((term ?? string.Empty)
                .Where(char.IsDigit)
                .Take(15)
                .ToArray());

            if (search.Length < 3)
                return Ok(new List<ParticipantMobileLookupResult>());

            var farmers = await _db.Set<SasFarmer>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted &&
                            x.Mobile != null &&
                            x.Mobile.Contains(search))
                .OrderBy(x => x.Mobile)
                .Take(20)
                .Select(x => new ParticipantMobileLookupResult
                {
                    SourceId = x.Id,
                    Mobile = x.Mobile,
                    Name = x.Name ?? string.Empty,
                    Role = "Farmer",
                    Source = "SasFarmers"
                })
                .ToListAsync(HttpContext.RequestAborted);

            var dealerRows = await _db.Set<DealerRegistration>()
                .AsNoTracking()
                .Where(x =>
                    (x.OfficialContactNumber != null &&
                     x.OfficialContactNumber.Contains(search)) ||
                    (x.AlternativeNumber != null &&
                     x.AlternativeNumber.Contains(search)))
                .OrderBy(x => x.FirmName)
                .Take(30)
                .Select(x => new
                {
                    x.Id,
                    x.FirmName,
                    x.OfficialContactNumber,
                    x.AlternativeNumber
                })
                .ToListAsync(HttpContext.RequestAborted);

            var dealers = dealerRows
                .SelectMany(x => new[]
                {
                    new ParticipantMobileLookupResult
                    {
                        SourceId = x.Id,
                        Mobile = x.OfficialContactNumber ?? string.Empty,
                        Name = x.FirmName ?? string.Empty,
                        Role = "Dealer",
                        Source = "DealerRegistrations"
                    },
                    new ParticipantMobileLookupResult
                    {
                        SourceId = x.Id,
                        Mobile = x.AlternativeNumber ?? string.Empty,
                        Name = x.FirmName ?? string.Empty,
                        Role = "Dealer",
                        Source = "DealerRegistrations"
                    }
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Mobile) &&
                            x.Mobile.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var result = farmers
                .Concat(dealers)
                .GroupBy(x => new
                {
                    x.SourceId,
                    Mobile = x.Mobile.Trim(),
                    x.Role
                })
                .Select(g => g.First())
                .OrderBy(x => x.Mobile.StartsWith(search, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(x => x.Mobile)
                .ThenBy(x => x.Role)
                .Take(20)
                .ToList();

            return Ok(result);
        }

        [HttpGet("all")]
        public async Task<IActionResult> All()
        {
            var people = await _db.Set<EmployeeInformation>()
                .AsNoTracking()
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.OfficialPhoneNumber,
                    e.PersonalPhoneNumber
                })
                .ToListAsync();

            // Get Employeelogin + UserName from Users table
            var logins = await (
                from login in _db.Set<Employeelogin>().AsNoTracking()
                join user in _db.Users.AsNoTracking()
                    on login.UserId equals user.Id
                where login.IsActive
                select new
                {
                    login.EmployeeInformationID,
                    login.UserId,
                    user.UserName,
                    login.Role
                }
            ).ToListAsync();

            var loginByEmployee = logins
                .GroupBy(x => x.EmployeeInformationID)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        UserId = g
                            .Select(x => x.UserId)
                            .FirstOrDefault(x =>
                                !string.IsNullOrWhiteSpace(x)),

                        UserName = g
                            .Select(x => x.UserName)
                            .FirstOrDefault(x =>
                                !string.IsNullOrWhiteSpace(x)),

                        Roles = g
                            .Select(x => x.Role.ToString())
                            .Distinct()
                            .OrderBy(role => role)
                            .ToList()
                    });

            var result = people
                .Select(e =>
                {
                    loginByEmployee.TryGetValue(
                        e.Id,
                        out var login);

                    return new
                    {
                        e.Id,
                        e.Name,
                        e.OfficialPhoneNumber,
                        e.PersonalPhoneNumber,

                        // Employeelogins.UserId
                        UserId = login?.UserId,

                        // Users.UserName
                        UserName = login?.UserName ?? "",

                        Roles = login?.Roles
                            ?? new List<string>()
                    };
                })
                .OrderBy(e => e.Name)
                .ToList();

            return Ok(result);
        }

        // CSR-2 persistence endpoints (merged from previous package)
        // Expense categories are dynamic and are validated against CSR1Expenses/ExpenseInformations.
        private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "unknown";
        private string UploadRoot => Path.Combine(_env.ContentRootPath, "App_Data", "csr2-private");

        // IMPORTANT: Replace this with the SAME scope/claims authorization policy as your
        // CSR1 details endpoint. Being logged in alone MUST NOT grant access to every CSR1.
        private Task<CSR1?> ApprovedCsr1Async(int csr1Id) =>
            VisibleApprovedCSR1().FirstOrDefaultAsync(x => x.Id == csr1Id);

        [HttpGet("by-csr1/{csr1Id:int}")]
        public async Task<IActionResult> GetByCsr1(int csr1Id)
        {
            if (await ApprovedCsr1Async(csr1Id) == null)
                return NotFound("Approved CSR1 not available or access denied.");

            var report = await _db.Set<CSR2>()
                .AsNoTracking()
                .Include(x => x.Participants)
                .Include(x => x.Employees)
                .Include(x => x.Media)
                .Include(x => x.FocusProducts)
                .Include(x => x.Expenses)
                    .ThenInclude(x => x.Proofs)
                .AsSplitQuery()
                .FirstOrDefaultAsync(
                    x => x.CSR1Id == csr1Id,
                    HttpContext.RequestAborted);

            if (report == null)
                return NotFound();

            return Ok(new CSR2DetailsDto
            {
                Id = report.Id,
                CSR1Id = report.CSR1Id,
                Status = report.Status.ToString(),

                ActualEventDate = report.ActualEventDate,
                StartTime = report.StartTime,
                EndTime = report.EndTime,

                EventCoordinatorEmployeeId =
                    report.EventCoordinatorEmployeeId,

                ActualLocation = report.ActualLocation,
                ExpectedParticipantsCount = report.ExpectedParticipantsCount,

                Participants = report.Participants
                    .Select(x => new CSR2ParticipantDto
                    {
                        Name = x.Name,
                        PhoneNumber = x.PhoneNumber,
                        Role = x.Role
                    })
                    .ToList(),

                Employees = report.Employees
                    .Select(x => new CSR2EmployeeDto
                    {
                        EmployeeInformationId = x.EmployeeInformationId,
                        EmployeeName = x.EmployeeName,
                        PhoneNumber = x.PhoneNumber,
                        RoleName = x.RoleName
                    })
                    .ToList(),

                FocusProducts = report.FocusProducts
                    .Select(x => new CSR2FocusProductDto
                    {
                        GroupNumber = x.GroupNumber,
                        CropId = x.CropId,
                        ProductId = x.ProductId
                    })
                    .ToList(),

                Expenses = report.Expenses
                    .Select(x => new CSR2ExpenseDto
                    {
                        CategoryKey = x.CategoryKey,
                        CategoryName = x.CategoryName,
                        ActualAmount = x.ActualAmount,
                        ExpenseDate = x.ExpenseDate,
                        BillNo = x.BillNo,

                        ProofId = x.Proofs
                            .Select(p => (int?)p.Id)
                            .FirstOrDefault(),

                        ProofFileName = x.Proofs
                            .Select(p => p.OriginalFileName)
                            .FirstOrDefault()
                    })
                    .ToList(),

                Media = report.Media
                    .Select(x => new CSR2MediaDto
                    {
                        Id = x.Id,
                        MediaType = x.MediaType,
                        OriginalFileName = x.OriginalFileName,
                        Caption = x.Caption,
                        FileSizeBytes = x.FileSizeBytes
                    })
                    .ToList()
            });
        }

        [HttpPost("save-draft")]
        public async Task<IActionResult> SaveDraft([FromBody] CSR2SaveDto dto)
        {
            var csr1 = await ApprovedCsr1Async(dto.CSR1Id);

            if (csr1 == null)
                return NotFound("Approved CSR1 not available or access denied.");

            if (dto.EndTime.HasValue &&
                dto.StartTime.HasValue &&
                dto.EndTime <= dto.StartTime)
            {
                return BadRequest("Event end time must be later than start time.");
            }

            if (string.IsNullOrWhiteSpace(dto.EventCoordinatorEmployeeId))
                return BadRequest("Event Coordinator is required.");

            if (dto.ExpectedParticipantsCount < 0 ||
                dto.Expenses.Any(x => x.ActualAmount < 0))
            {
                return BadRequest("Invalid expense amounts or participant counts.");
            }

            var focusGroups = dto.FocusProducts
                .GroupBy(x => x.GroupNumber)
                .ToList();

            if (dto.FocusProducts.Any(x =>
                    x.GroupNumber is < 1 or > 3 ||
                    x.CropId <= 0 ||
                    x.ProductId <= 0) ||
                dto.FocusProducts
                    .GroupBy(x => new { x.GroupNumber, x.CropId, x.ProductId })
                    .Any(g => g.Count() > 1) ||
                focusGroups.Any(g => g.Select(x => x.CropId).Distinct().Count() > 3) ||
                focusGroups.Any(g => g.Select(x => x.ProductId).Distinct().Count() > 3) ||
                !focusGroups.Any(g => g.Key == 1 &&
                    g.Select(x => x.CropId).Distinct().Any() &&
                    g.Select(x => x.ProductId).Distinct().Any()))
            {
                return BadRequest(
                    "Invalid focus selections. Group 1 requires at least one crop and one product; maximum three crops and three products per group.");
            }

            // CSR-2 expense rows are driven by the complete active ExpenseInformations master,
            // not only by CSR1Expenses rows that have a non-zero planned amount.
            var csr1ExpenseMasters = await _db.Set<ExpenseInformation>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Id)
                .Select(x => new
                {
                    ExpenseId = x.Id,
                    ExpenseName = x.Name
                })
                .ToListAsync(HttpContext.RequestAborted);

            var allowedExpenseByKey = csr1ExpenseMasters
                .GroupBy(x => x.ExpenseId)
                .ToDictionary(
                    g => g.Key.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    g => g.First().ExpenseName,
                    StringComparer.OrdinalIgnoreCase);

            if (dto.Expenses
                    .Select(x => x.CategoryKey?.Trim() ?? "")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() != dto.Expenses.Count ||
                dto.Expenses.Any(x =>
                    string.IsNullOrWhiteSpace(x.CategoryKey) ||
                    !allowedExpenseByKey.ContainsKey(x.CategoryKey.Trim())))
            {
                return BadRequest("Invalid or duplicate expense category. Expense rows must come from the active ExpenseInformations master.");
            }

            if (dto.Participants.Any(x =>
                    string.IsNullOrWhiteSpace(x.Name) ||
                    string.IsNullOrWhiteSpace(x.Role)) ||
                dto.Employees.Any(x =>
                    string.IsNullOrWhiteSpace(x.EmployeeInformationId) ||
                    string.IsNullOrWhiteSpace(x.RoleName)))
            {
                return BadRequest(
                    "Required participant or employee data is missing.");
            }

            var report = await _db.Set<CSR2>()
                .Include(x => x.Participants)
                .Include(x => x.Employees)
                .Include(x => x.Expenses)
                .Include(x => x.FocusProducts)
                .AsSplitQuery()
                .FirstOrDefaultAsync(
                    x => x.CSR1Id == dto.CSR1Id,
                    HttpContext.RequestAborted);

            if (report != null &&
                dto.Id.HasValue &&
                dto.Id != report.Id)
            {
                return Conflict("Report ID mismatch.");
            }

            if (report != null &&
                report.Status != CSR2Status.Draft)
            {
                return Conflict("Submitted reports are read-only.");
            }

            if (report == null)
            {
                report = new CSR2
                {
                    CSR1Id = dto.CSR1Id,
                    CreatedBy = Actor,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Set<CSR2>().Add(report);
            }

            report.ActualEventDate = csr1.CSR1Date;

            report.StartTime = dto.StartTime;

            report.EndTime = dto.EndTime;

            report.EventCoordinatorEmployeeId =
                dto.EventCoordinatorEmployeeId?.Trim();

            report.ActualLocation =
                dto.ActualLocation?.Trim();

            report.ExpectedParticipantsCount =
                dto.ExpectedParticipantsCount;

            report.UpdatedAt =
                DateTime.UtcNow;

            report.UpdatedBy =
                Actor;

            await using var tx =
                await _db.Database.BeginTransactionAsync();

            _db.Set<CSR2Participant>()
                .RemoveRange(report.Participants);

            _db.Set<CSR2Employee>()
                .RemoveRange(report.Employees);

            _db.Set<CSR2FocusProduct>()
                .RemoveRange(report.FocusProducts);

            report.Participants =
                dto.Participants
                    .Select(x => new CSR2Participant
                    {
                        Name = x.Name.Trim(),
                        PhoneNumber = x.PhoneNumber,
                        Role = x.Role.Trim()
                    })
                    .ToList();

            report.FocusProducts =
                dto.FocusProducts
                    .Select(x => new CSR2FocusProduct
                    {
                        GroupNumber = x.GroupNumber,
                        CropId = x.CropId,
                        ProductId = x.ProductId
                    })
                    .ToList();

            report.Employees =
                dto.Employees
                    .GroupBy(x => x.EmployeeInformationId)
                    .Select(g => g.First())
                    .Select(x => new CSR2Employee
                    {
                        EmployeeInformationId =
                            x.EmployeeInformationId,

                        EmployeeName =
                            x.EmployeeName,

                        PhoneNumber =
                            x.PhoneNumber,

                        RoleName =
                            x.RoleName
                    })
                    .ToList();

            foreach (var item in dto.Expenses)
            {
                var normalizedCategoryKey = item.CategoryKey.Trim();
                var expense =
                    report.Expenses.FirstOrDefault(
                        x => string.Equals(
                            x.CategoryKey,
                            normalizedCategoryKey,
                            StringComparison.OrdinalIgnoreCase));

                if (expense == null)
                {
                    expense = new CSR2Expense
                    {
                        CategoryKey = item.CategoryKey
                    };

                    report.Expenses.Add(expense);
                }

                expense.CategoryKey = normalizedCategoryKey;

                // Category name is authoritative from ExpenseInformations, not from the client.
                expense.CategoryName =
                    allowedExpenseByKey[normalizedCategoryKey];


                expense.ActualAmount =
                    item.ActualAmount;

                expense.ExpenseDate =
                    item.ExpenseDate;

                expense.BillNo =
                    item.BillNo?.Trim();
            }

            await _db.SaveChangesAsync();

            await tx.CommitAsync();

            return Ok(new
            {
                id = report.Id,
                report.CSR1Id,
                Status = report.Status.ToString()
            });
        }

        [HttpPost("{id:int}/submit")]
        public async Task<IActionResult> Submit(int id)
        {
            var report = await _db.Set<CSR2>()
                .Include(x => x.Expenses)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (report == null ||
                await ApprovedCsr1Async(report.CSR1Id) == null)
            {
                return NotFound();
            }

            if (report.Status != CSR2Status.Draft)
                return Conflict("Already submitted.");

            if (!report.ActualEventDate.HasValue ||
                !report.StartTime.HasValue ||
                !report.EndTime.HasValue ||
                report.EndTime <= report.StartTime ||
                string.IsNullOrWhiteSpace(report.EventCoordinatorEmployeeId) ||
                string.IsNullOrWhiteSpace(report.ActualLocation))
            {
                return BadRequest("Complete actual event details.");
            }

            if (report.Expenses.Any(x =>
                    x.ActualAmount > 0 &&
                    !x.ExpenseDate.HasValue))
            {
                return BadRequest(
                    "Expense dates are required for amounts entered.");
            }

            report.Status = CSR2Status.Submitted;
            report.SubmittedAt = DateTime.UtcNow;
            report.SubmittedBy = Actor;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                report.Id,
                Status = report.Status.ToString()
            });
        }

        [HttpPost("{id:int}/media")]
        [RequestSizeLimit(720L * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 720L * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadMedia(int id, [FromForm] CSR2UploadMediaDto form)
        {
            var file = form.File;
            var mediaType = form.MediaType?.Trim();
            var caption = form.Caption;

            var report = await _db.Set<CSR2>().FirstOrDefaultAsync(x => x.Id == id);
            if (report == null || await ApprovedCsr1Async(report.CSR1Id) == null) return NotFound();
            if (report.Status != CSR2Status.Draft) return Conflict("Only draft reports accept uploads.");

            var isPhoto = string.Equals(mediaType, "Photo", StringComparison.OrdinalIgnoreCase);
            var isVideo = string.Equals(mediaType, "Video", StringComparison.OrdinalIgnoreCase);
            var isDocument = string.Equals(mediaType, "Document", StringComparison.OrdinalIgnoreCase);

            if (!isPhoto && !isVideo && !isDocument)
                return BadRequest("MediaType must be Photo, Video or Document.");

            var max = isPhoto
                ? 20L * 1024 * 1024
                : isVideo
                    ? 700L * 1024 * 1024
                    : 25L * 1024 * 1024;

            var extensions = isPhoto
                ? new[] { ".jpg", ".jpeg", ".png" }
                : isVideo
                    ? new[] { ".mp4", ".mov" }
                    : new[] { ".pdf", ".doc", ".docx" };

            if (file == null ||
                file.Length <= 0 ||
                file.Length > max ||
                !extensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
            {
                return BadRequest("Invalid file type or size.");
            }

            var stored = await StoreFileAsync(id, file, extensions);

            var media = new CSR2Media
            {
                CSR2Id = id,
                MediaType = isPhoto ? "Photo" : isVideo ? "Video" : "Document",
                OriginalFileName = Path.GetFileName(file.FileName),
                ContentType = stored.ContentType,
                FileSizeBytes = file.Length,
                StorageKey = stored.StorageKey,
                Caption = caption,
                UploadedBy = Actor
            };

            try
            {
                _db.Set<CSR2Media>().Add(media);
                await _db.SaveChangesAsync();
            }
            catch
            {
                System.IO.File.Delete(stored.Path);
                throw;
            }

            return Ok(new { media.Id, media.MediaType, media.OriginalFileName });
        }

        [HttpPost("{id:int}/expenses/{categoryKey}/proof")]
        [RequestSizeLimit(12L * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadExpenseProof(int id, string categoryKey, IFormFile file)
        {
            var report = await _db.Set<CSR2>().FirstOrDefaultAsync(x => x.Id == id);
            if (report == null || await ApprovedCsr1Async(report.CSR1Id) == null) return NotFound();
            if (report.Status != CSR2Status.Draft) return Conflict("Only draft reports accept uploads.");
            var expense = await _db.Set<CSR2Expense>().Include(x => x.Proofs).FirstOrDefaultAsync(x => x.CSR2Id == id && x.CategoryKey == categoryKey);
            if (expense == null) return NotFound("Save the expense row first.");
            var extensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            if (file == null || file.Length <= 0 || file.Length > 10L * 1024 * 1024 || !extensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant())) return BadRequest("Invalid proof type or size.");
            var stored = await StoreFileAsync(id, file, extensions);
            var proof = new CSR2ExpenseProof { CSR2ExpenseId = expense.Id, OriginalFileName = Path.GetFileName(file.FileName), ContentType = stored.ContentType, FileSizeBytes = file.Length, StorageKey = stored.StorageKey };
            try { _db.Set<CSR2ExpenseProof>().Add(proof); await _db.SaveChangesAsync(); }
            catch { System.IO.File.Delete(stored.Path); throw; }
            return Ok(new { proof.Id, proof.OriginalFileName });
        }

        [HttpGet("media/{mediaId:int}/file")]
        public async Task<IActionResult> ReadMedia(int mediaId)
        {
            var media = await _db.Set<CSR2Media>().FirstOrDefaultAsync(x => x.Id == mediaId);
            if (media == null || await ApprovedCsr1Async((await _db.Set<CSR2>().Where(x => x.Id == media.CSR2Id).Select(x => x.CSR1Id).FirstOrDefaultAsync())) == null) return NotFound();
            var path = ResolveStoragePath(media.StorageKey);
            if (!System.IO.File.Exists(path)) return NotFound();
            return PhysicalFile(path, media.ContentType, enableRangeProcessing: true);
        }
        [HttpGet("proof/{proofId:int}/file")]
        public async Task<IActionResult> ReadProof(int proofId)
        {
            var proof = await _db.Set<CSR2ExpenseProof>().Include(x => x.CSR2Expense).FirstOrDefaultAsync(x => x.Id == proofId);
            if (proof?.CSR2Expense == null) return NotFound();
            var report = await _db.Set<CSR2>().FirstOrDefaultAsync(x => x.Id == proof.CSR2Expense.CSR2Id);
            if (report == null || await ApprovedCsr1Async(report.CSR1Id) == null) return NotFound();
            var path = ResolveStoragePath(proof.StorageKey);
            if (!System.IO.File.Exists(path)) return NotFound();
            return PhysicalFile(path, proof.ContentType, enableRangeProcessing: true);
        }
        [HttpDelete("media/{mediaId:int}")]
        public async Task<IActionResult> DeleteMedia(int mediaId)
        {
            var media = await _db.Set<CSR2Media>().FirstOrDefaultAsync(x => x.Id == mediaId);
            if (media == null) return NotFound();
            var report = await _db.Set<CSR2>().FirstOrDefaultAsync(x => x.Id == media.CSR2Id);
            if (report == null || await ApprovedCsr1Async(report.CSR1Id) == null) return NotFound();
            if (report.Status != CSR2Status.Draft) return Conflict();
            _db.Remove(media); await _db.SaveChangesAsync();
            System.IO.File.Delete(ResolveStoragePath(media.StorageKey)); return NoContent();
        }
        [HttpDelete("proof/{proofId:int}")]
        public async Task<IActionResult> DeleteProof(int proofId)
        {
            var proof = await _db.Set<CSR2ExpenseProof>().Include(x => x.CSR2Expense).FirstOrDefaultAsync(x => x.Id == proofId);
            if (proof?.CSR2Expense == null) return NotFound();
            var report = await _db.Set<CSR2>().FirstOrDefaultAsync(x => x.Id == proof.CSR2Expense.CSR2Id);
            if (report == null || await ApprovedCsr1Async(report.CSR1Id) == null) return NotFound();
            if (report.Status != CSR2Status.Draft) return Conflict();
            _db.Remove(proof); await _db.SaveChangesAsync();
            System.IO.File.Delete(ResolveStoragePath(proof.StorageKey)); return NoContent();
        }
        [HttpPatch("media/{mediaId:int}")]
        public async Task<IActionResult> UpdateMedia(int mediaId, [FromBody] CSR2MediaDto patch)
        {
            var media = await _db.Set<CSR2Media>().FirstOrDefaultAsync(x => x.Id == mediaId);
            if (media == null) return NotFound();
            var report = await _db.Set<CSR2>().FirstOrDefaultAsync(x => x.Id == media.CSR2Id);
            if (report == null || await ApprovedCsr1Async(report.CSR1Id) == null) return NotFound();
            if (report.Status != CSR2Status.Draft) return Conflict();
            media.Caption = patch.Caption;
            await _db.SaveChangesAsync(); return NoContent();
        }

        private async Task<(string StorageKey, string Path, string ContentType)> StoreFileAsync(int reportId, IFormFile file, string[] allowedExtensions)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext)) throw new InvalidDataException("File extension rejected.");
            // Content-Type supplied by browser is not trusted for security; use allowed extension mapping.
            var type = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".mov" => "video/quicktime",
                _ => "video/mp4"
            };
            var key = $"{reportId}/{Guid.NewGuid():N}{ext}";
            var path = ResolveStoragePath(key); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await file.CopyToAsync(target, HttpContext.RequestAborted);
            return (key, path, type);
        }
        private string ResolveStoragePath(string storageKey)
        {
            // Storage keys originate only from server-generated GUID values.
            var root = Path.GetFullPath(UploadRoot) + Path.DirectorySeparatorChar;
            var path = Path.GetFullPath(Path.Combine(root, storageKey.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(root, StringComparison.Ordinal)) throw new InvalidOperationException("Invalid storage key.");
            return path;
        }

        [HttpGet("csr1-display/{csr1Id:int}")]
        public async Task<IActionResult> GetCSR1DisplayDetails(int csr1Id)
        {
            // CSR1 is the authoritative source for all three values shown in CSR2:
            // ApprovedBy, ApprovedAt and ResponsiblePersonId.
            // Read these values directly from CSR1.
            // Do not apply CreatedBy ownership here; the CSR-2 page may be opened
            // by an authorized reviewer/employee who did not create the CSR-1.
            var csr1 = await _db.Set<CSR1>()
                .AsNoTracking()
                .Where(x =>
                    x.Id == csr1Id &&
                    x.IsActive &&
                    x.Status == CSR1Status.Approved)
                .Select(x => new
                {
                    x.Id,
                    x.ApprovedBy,
                    x.ApprovedAt,
                    x.ResponsiblePersonId
                })
                .FirstOrDefaultAsync();

            if (csr1 == null)
            {
                return NotFound(new
                {
                    message = $"Approved CSR-1 #{csr1Id} not found or access denied."
                });
            }

            // Return all active ExpenseInformations for CSR-2 display.
            // Matching CSR1Expenses values are filled in; missing rows are returned as 0.
            var plannedExpenses = await _db.Set<ExpenseInformation>()
                .AsNoTracking()
                .Where(masterExpense => masterExpense.IsActive)
                .OrderBy(masterExpense => masterExpense.Id)
                .Select(masterExpense => new
                {
                    ExpenseId = masterExpense.Id,
                    ExpenseName = masterExpense.Name,
                    ExpenseAmount = _db.Set<CSR1Expenses>()
                        .Where(expense =>
                            expense.CSR1Id == csr1Id &&
                            expense.IsActive &&
                            expense.ExpenseId == masterExpense.Id)
                        .Sum(expense => (decimal?)expense.ExpenseAmount) ?? 0m
                })
                .ToListAsync(HttpContext.RequestAborted);

            string? approvedByName = null;
            string? responsiblePersonName = null;

            // CSR1.ApprovedBy stores the application UserId.
            // Resolve it through Employeelogin -> EmployeeInformation.Name.
            if (!string.IsNullOrWhiteSpace(csr1.ApprovedBy))
            {
                approvedByName = await (
                    from login in _db.Set<Employeelogin>().AsNoTracking()
                    join employee in _db.Set<EmployeeInformation>().AsNoTracking()
                        on login.EmployeeInformationID equals employee.Id
                    where login.UserId == csr1.ApprovedBy
                    select employee.Name
                ).FirstOrDefaultAsync();
            }

            // CSR1.ResponsiblePersonId stores Employeelogins.UserId.
            // Resolve UserId -> EmployeeInformation so CSR-2 receives the name
            // and the numeric employee id required by its dropdown.
            int? responsiblePersonEmployeeId = null;

            if (!string.IsNullOrWhiteSpace(csr1.ResponsiblePersonId))
            {
                var responsible = await (
                    from login in _db.Set<Employeelogin>().AsNoTracking()
                    join employee in _db.Set<EmployeeInformation>().AsNoTracking()
                        on login.EmployeeInformationID equals employee.Id
                    where login.UserId == csr1.ResponsiblePersonId
                    select new
                    {
                        EmployeeId = employee.Id,
                        employee.Name
                    }
                ).FirstOrDefaultAsync();

                responsiblePersonEmployeeId = responsible?.EmployeeId;
                responsiblePersonName = responsible?.Name;
            }

            return Ok(new
            {
                csr1Id = csr1.Id,
                approvedById = csr1.ApprovedBy,
                approvedByName = approvedByName ?? "",
                approvedAt = csr1.ApprovedAt,
                responsiblePersonId = csr1.ResponsiblePersonId,
                responsiblePersonEmployeeId = responsiblePersonEmployeeId?.ToString(),
                responsiblePersonName = responsiblePersonName ?? "",
                expenses = plannedExpenses
            });
        }
    }

    public class ParticipantMobileLookupResult
    {
        public int SourceId { get; set; }
        public string Mobile { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }

    public class CSR2UploadMediaDto
    {
        public IFormFile File { get; set; } = null!;
        public string MediaType { get; set; } = string.Empty;
        public string? Caption { get; set; }
    }


}

