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
                .Where(x => x.IsActive && x.Status == "Approved");
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
                    x.CSR1Status,
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
                csr1.Status,
                Expenses = new
                {
                    csr1.PrintingAndStationery,
                    csr1.PublicityMaterial,
                    csr1.StageArrangements,
                    csr1.ServiceChargesLCA,
                    csr1.TransportRent,
                    csr1.JeepRunningExpenses,
                    csr1.Refreshments,
                    csr1.Inputs,
                    csr1.Photography,
                    csr1.Compliments,
                    csr1.Others
                },
                Focus1 = focus1,
                Focus2 = focus2,
                Focus3 = focus3
            });
        }

        [HttpGet("all")]
        public async Task<IActionResult> All()
        {
            var people = await _db.Set<EmployeeInformation>().AsNoTracking()
                .Select(e => new { e.Id, e.Name, e.OfficialPhoneNumber, e.PersonalPhoneNumber })
                .ToListAsync();
            var logins = await _db.Set<Employeelogin>().AsNoTracking()
                .Where(l => l.IsActive)
                .Select(l => new { l.EmployeeInformationID, l.Role }).ToListAsync();
            var rolesByEmployee = logins.GroupBy(l => l.EmployeeInformationID)
                .ToDictionary(g => g.Key,
                    g => g.Select(x => x.Role.ToString()).Distinct().OrderBy(role => role).ToList());
            var result = people.Select(e => new {
                e.Id,
                e.Name,
                e.OfficialPhoneNumber,
                e.PersonalPhoneNumber,
                Roles = rolesByEmployee.TryGetValue(e.Id, out var roles) ? roles : new List<string>()
            }).OrderBy(e => e.Name).ToList();
            return Ok(result);
        }

        // CSR-2 persistence endpoints (merged from previous package)
        private static readonly string[] CategoryKeys = {
        "PrintingAndStationery", "PublicityMaterial", "StageArrangements", "ServiceChargesLCA",
        "TransportRent", "JeepRunningExpenses", "Refreshments", "Inputs", "Photography", "Compliments", "Others"
    };
        private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "unknown";
        private string UploadRoot => Path.Combine(_env.ContentRootPath, "App_Data", "csr2-private");

        // IMPORTANT: Replace this with the SAME scope/claims authorization policy as your
        // CSR1 details endpoint. Being logged in alone MUST NOT grant access to every CSR1.
        private Task<CSR1?> ApprovedCsr1Async(int csr1Id) =>
            VisibleApprovedCSR1().FirstOrDefaultAsync(x => x.Id == csr1Id);

        private static decimal PlannedAmount(CSR1 c, string key) => key switch
        {
            "PrintingAndStationery" => c.PrintingAndStationery,
            "PublicityMaterial" => c.PublicityMaterial,
            "StageArrangements" => c.StageArrangements,
            "ServiceChargesLCA" => c.ServiceChargesLCA,
            "TransportRent" => c.TransportRent,
            "JeepRunningExpenses" => c.JeepRunningExpenses,
            "Refreshments" => c.Refreshments,
            "Inputs" => c.Inputs,
            "Photography" => c.Photography,
            "Compliments" => c.Compliments,
            "Others" => c.Others,
            _ => 0m
        };

        [HttpGet("by-csr1/{csr1Id:int}")]
        public async Task<IActionResult> GetByCsr1(int csr1Id)
        {
            if (await ApprovedCsr1Async(csr1Id) == null) return NotFound("Approved CSR1 not available or access denied.");
            var report = await _db.Set<CSR2>().AsNoTracking()
                .Include(x => x.Participants).Include(x => x.Employees).Include(x => x.Activities)
                .Include(x => x.Media).Include(x => x.Expenses).ThenInclude(x => x.Proofs)
                .AsSplitQuery() // Avoid a large Cartesian product across child collections.
                .FirstOrDefaultAsync(x => x.CSR1Id == csr1Id, HttpContext.RequestAborted);
            if (report == null) return NotFound();
            return Ok(new CSR2DetailsDto
            {
                Id = report.Id,
                CSR1Id = report.CSR1Id,
                Status = report.Status,
                ActualEventDate = report.ActualEventDate,
                StartTime = report.StartTime,
                EndTime = report.EndTime,
                EventCoordinatorEmployeeId = report.EventCoordinatorEmployeeId,
                ActualLocation = report.ActualLocation,
                ExpectedParticipantsCount = report.ExpectedParticipantsCount,
                IsOrganized = report.IsOrganized,
                Participants = report.Participants.Select(x => new CSR2ParticipantDto { Name = x.Name, PhoneNumber = x.PhoneNumber, Role = x.Role }).ToList(),
                Employees = report.Employees.Select(x => new CSR2EmployeeDto { EmployeeInformationId = x.EmployeeInformationId, EmployeeName = x.EmployeeName, PhoneNumber = x.PhoneNumber, RoleName = x.RoleName }).ToList(),
                Activities = report.Activities.OrderBy(x => x.SortOrder).Select(x => new CSR2ActivityDto { ActivityName = x.ActivityName, StartTime = x.StartTime, EndTime = x.EndTime, ParticipantsCount = x.ParticipantsCount, Description = x.Description, Remarks = x.Remarks, SortOrder = x.SortOrder }).ToList(),
                Expenses = report.Expenses.Select(x => new CSR2ExpenseDto { CategoryKey = x.CategoryKey, CategoryName = x.CategoryName, PlannedAmount = x.PlannedAmount, ActualAmount = x.ActualAmount, ExpenseDate = x.ExpenseDate, ProofId = x.Proofs.Select(p => (int?)p.Id).FirstOrDefault(), ProofFileName = x.Proofs.Select(p => p.OriginalFileName).FirstOrDefault() }).ToList(),
                Media = report.Media.Select(x => new CSR2MediaDto { Id = x.Id, MediaType = x.MediaType, OriginalFileName = x.OriginalFileName, Caption = x.Caption, UseInDigitalLibrary = x.UseInDigitalLibrary, FileSizeBytes = x.FileSizeBytes }).ToList()
            });
        }

        [HttpPost("save-draft")]
        public async Task<IActionResult> SaveDraft([FromBody] CSR2SaveDto dto)
        {
            var csr1 = await ApprovedCsr1Async(dto.CSR1Id);
            if (csr1 == null) return NotFound("Approved CSR1 not available or access denied.");
            if (dto.EndTime.HasValue && dto.StartTime.HasValue && dto.EndTime <= dto.StartTime)
                return BadRequest("Event end time must be later than start time.");
            if (dto.ExpectedParticipantsCount < 0 || dto.Expenses.Any(x => x.ActualAmount < 0) || dto.Activities.Any(x => x.ParticipantsCount < 0 || x.EndTime <= x.StartTime))
                return BadRequest("Invalid amounts, counts or activity times.");
            if (dto.Expenses.Select(x => x.CategoryKey).Distinct().Count() != dto.Expenses.Count || dto.Expenses.Any(x => !CategoryKeys.Contains(x.CategoryKey)))
                return BadRequest("Invalid or duplicate expense category.");
            if (dto.Participants.Any(x => string.IsNullOrWhiteSpace(x.Name) || string.IsNullOrWhiteSpace(x.Role)) ||
                dto.Employees.Any(x => x.EmployeeInformationId <= 0 || string.IsNullOrWhiteSpace(x.RoleName)) ||
                dto.Activities.Any(x => string.IsNullOrWhiteSpace(x.ActivityName) || string.IsNullOrWhiteSpace(x.Description)))
                return BadRequest("Required participant, employee or activity data is missing.");
            var report = await _db.Set<CSR2>().Include(x => x.Participants).Include(x => x.Employees)
                .Include(x => x.Activities).Include(x => x.Expenses).AsSplitQuery()
                .FirstOrDefaultAsync(x => x.CSR1Id == dto.CSR1Id, HttpContext.RequestAborted);
            if (report != null && dto.Id.HasValue && dto.Id != report.Id) return Conflict("Report ID mismatch.");
            if (report != null && report.Status != "Draft") return Conflict("Submitted reports are read-only.");
            if (report == null)
            {
                report = new CSR2 { CSR1Id = dto.CSR1Id, CreatedBy = Actor, CreatedAt = DateTime.UtcNow };
                _db.Set<CSR2>().Add(report);
            }
            report.ActualEventDate = dto.ActualEventDate;
            report.StartTime = dto.StartTime; report.EndTime = dto.EndTime;
            report.EventCoordinatorEmployeeId = dto.EventCoordinatorEmployeeId;
            report.ActualLocation = dto.ActualLocation?.Trim();
            report.ExpectedParticipantsCount = dto.ExpectedParticipantsCount;
            report.IsOrganized = dto.IsOrganized;
            report.ApprovedBudgetSnapshot = csr1.Budget;
            report.UpdatedAt = DateTime.UtcNow; report.UpdatedBy = Actor;
            // Reconcile non-file children in a transaction; media/proofs are managed independently.
            await using var tx = await _db.Database.BeginTransactionAsync();
            _db.Set<CSR2Participant>().RemoveRange(report.Participants);
            _db.Set<CSR2Employee>().RemoveRange(report.Employees);
            _db.Set<CSR2Activity>().RemoveRange(report.Activities);
            report.Participants = dto.Participants.Select(x => new CSR2Participant { Name = x.Name.Trim(), PhoneNumber = x.PhoneNumber, Role = x.Role.Trim() }).ToList();
            report.Employees = dto.Employees.GroupBy(x => x.EmployeeInformationId).Select(g => g.First()).Select(x => new CSR2Employee { EmployeeInformationId = x.EmployeeInformationId, EmployeeName = x.EmployeeName, PhoneNumber = x.PhoneNumber, RoleName = x.RoleName }).ToList();
            report.Activities = dto.Activities.Select((x, i) => new CSR2Activity { ActivityName = x.ActivityName.Trim(), StartTime = x.StartTime, EndTime = x.EndTime, ParticipantsCount = x.ParticipantsCount, Description = x.Description.Trim(), Remarks = x.Remarks, SortOrder = i + 1 }).ToList();
            // Do not delete/recreate expense rows: uploaded proofs are attached to expense IDs.
            foreach (var item in dto.Expenses)
            {
                var expense = report.Expenses.FirstOrDefault(x => x.CategoryKey == item.CategoryKey);
                if (expense == null) { expense = new CSR2Expense { CategoryKey = item.CategoryKey }; report.Expenses.Add(expense); }
                expense.CategoryName = item.CategoryName;
                expense.PlannedAmount = PlannedAmount(csr1, item.CategoryKey); // never trust client planned amount
                expense.ActualAmount = item.ActualAmount; expense.ExpenseDate = item.ExpenseDate;
            }
            await _db.SaveChangesAsync(); await tx.CommitAsync();
            return Ok(new { id = report.Id, report.CSR1Id, report.Status });
        }

        [HttpPost("{id:int}/submit")]
        public async Task<IActionResult> Submit(int id)
        {
            var report = await _db.Set<CSR2>().Include(x => x.Expenses).FirstOrDefaultAsync(x => x.Id == id);
            if (report == null || await ApprovedCsr1Async(report.CSR1Id) == null) return NotFound();
            if (report.Status != "Draft") return Conflict("Already submitted.");
            if (!report.ActualEventDate.HasValue || !report.StartTime.HasValue || !report.EndTime.HasValue ||
                report.EndTime <= report.StartTime || report.EventCoordinatorEmployeeId is null or <= 0 ||
                string.IsNullOrWhiteSpace(report.ActualLocation)) return BadRequest("Complete actual event details.");
            if (report.Expenses.Any(x => x.ActualAmount > 0 && !x.ExpenseDate.HasValue)) return BadRequest("Expense dates are required for amounts entered.");
            report.Status = "Submitted"; report.SubmittedAt = DateTime.UtcNow; report.SubmittedBy = Actor;
            await _db.SaveChangesAsync(); return Ok(new { report.Id, report.Status });
        }

        [HttpPost("{id:int}/media")]
        [RequestSizeLimit(220L * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 220L * 1024 * 1024)]
        public async Task<IActionResult> UploadMedia(int id, [FromForm] IFormFile file, [FromForm] string mediaType,
            [FromForm] string? caption, [FromForm] bool useInDigitalLibrary = true)
        {
            var report = await _db.Set<CSR2>().FirstOrDefaultAsync(x => x.Id == id);
            if (report == null || await ApprovedCsr1Async(report.CSR1Id) == null) return NotFound();
            if (report.Status != "Draft") return Conflict("Only draft reports accept uploads.");
            var isPhoto = mediaType == "Photo"; var isVideo = mediaType == "Video";
            if (!isPhoto && !isVideo) return BadRequest("MediaType must be Photo or Video.");
            var max = isPhoto ? 20L * 1024 * 1024 : 200L * 1024 * 1024;
            var extensions = isPhoto ? new[] { ".jpg", ".jpeg", ".png" } : new[] { ".mp4", ".mov" };
            if (file == null || file.Length <= 0 || file.Length > max || !extensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant())) return BadRequest("Invalid file type or size.");
            var stored = await StoreFileAsync(id, file, extensions);
            var media = new CSR2Media { CSR2Id = id, MediaType = mediaType, OriginalFileName = Path.GetFileName(file.FileName), ContentType = stored.ContentType, FileSizeBytes = file.Length, StorageKey = stored.StorageKey, Caption = caption, UseInDigitalLibrary = useInDigitalLibrary, UploadedBy = Actor };
            try { _db.Set<CSR2Media>().Add(media); await _db.SaveChangesAsync(); }
            catch { System.IO.File.Delete(stored.Path); throw; }
            return Ok(new { media.Id, media.MediaType, media.OriginalFileName });
        }

        [HttpPost("{id:int}/expenses/{categoryKey}/proof")]
        [RequestSizeLimit(12L * 1024 * 1024)]
        public async Task<IActionResult> UploadExpenseProof(int id, string categoryKey, [FromForm] IFormFile file)
        {
            var report = await _db.Set<CSR2>().FirstOrDefaultAsync(x => x.Id == id);
            if (report == null || await ApprovedCsr1Async(report.CSR1Id) == null) return NotFound();
            if (report.Status != "Draft") return Conflict("Only draft reports accept uploads.");
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
            if (report.Status != "Draft") return Conflict();
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
            if (report.Status != "Draft") return Conflict();
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
            if (report.Status != "Draft") return Conflict();
            media.Caption = patch.Caption; media.UseInDigitalLibrary = patch.UseInDigitalLibrary;
            await _db.SaveChangesAsync(); return NoContent();
        }

        private async Task<(string StorageKey, string Path, string ContentType)> StoreFileAsync(int reportId, IFormFile file, string[] allowedExtensions)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext)) throw new InvalidDataException("File extension rejected.");
            // Content-Type supplied by browser is not trusted for security; use allowed extension mapping.
            var type = ext switch { ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".pdf" => "application/pdf", ".mov" => "video/quicktime", _ => "video/mp4" };
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

    }
}
