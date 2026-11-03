using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using System.Security.Claims;
using static SPIC.Core.Entities.EmployeeRegistration;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DemoDocumentationController : ControllerBase
    {
        private readonly AppDbContext _db;
        public DemoDocumentationController(AppDbContext db) => _db = db;

        // Returns one row per FINAL approved CSR-2, not Level1/Level2 approvals.
        // This endpoint must never infer completed field visits/treatments from CSR-2 approval.
        [HttpGet("approved-csr2")]
        public async Task<IActionResult> GetApprovedCSR2()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            // Only the active login assigned as CSR1.ResponsiblePersonId may access demos.
            if (!await _db.Set<Employeelogin>().AsNoTracking()
                    .AnyAsync(x => x.IsActive && x.UserId == userId, HttpContext.RequestAborted))
                return Forbid();

            // All final approved CSR-2 reports joined with their approved, active CSR-1.
            var query =
                from csr2 in _db.Set<CSR2>().AsNoTracking()
                join csr1 in _db.Set<CSR1>().AsNoTracking() on csr2.CSR1Id equals csr1.Id
                join program in _db.Set<ProgramMaster>().AsNoTracking() on csr1.ProgramId equals program.Id
                where csr2.Status == CSR2Status.Approved &&
                      csr1.Status == CSR1Status.Approved && csr1.IsActive
                select new { csr2, csr1, program };

            // Enforce ownership in SQL; do not permit Admin/State/HQ scope bypass here.
            query = query.Where(x => x.csr1.ResponsiblePersonId == userId);

            var rows = await query.OrderByDescending(x => x.csr2.Id)
                .Select(x => new
                {
                    Id = x.csr2.Id,
                    CSR1Id = x.csr1.Id,
                    CSR2Status = x.csr2.Status.ToString(),
                    CSR2CompletedDate = x.csr2.ApprovedAt,
                    x.csr1.SanctionNo,
                    ProgramName = x.program.Name,
                    ProgramType = x.program.ProgramType.Name,
                    x.csr1.LocationId,
                    x.csr2.ActualLocation
                }).ToListAsync(HttpContext.RequestAborted);

            if (rows.Count == 0) return Ok(Array.Empty<object>());

            var ids = rows.Select(x => x.Id).ToArray();
            var focus1 = await _db.Set<CSR2Products1>().AsNoTracking()
                .Where(x => ids.Contains(x.CSR2Id) && x.IsActive)
                .Select(x => new { x.CSR2Id, x.CropId, x.ProductId })
                .ToListAsync(HttpContext.RequestAborted);
            var focus2 = await _db.Set<CSR2Products2>().AsNoTracking()
                .Where(x => ids.Contains(x.CSR2Id) && x.IsActive)
                .Select(x => new { x.CSR2Id, x.CropId, x.ProductId })
                .ToListAsync(HttpContext.RequestAborted);
            var focus3 = await _db.Set<CSR2Products3>().AsNoTracking()
                .Where(x => ids.Contains(x.CSR2Id) && x.IsActive)
                .Select(x => new { x.CSR2Id, x.CropId, x.ProductId })
                .ToListAsync(HttpContext.RequestAborted);

            var byReport = focus1.Select(x => (x.CSR2Id, x.CropId, x.ProductId))
                .Concat(focus2.Select(x => (x.CSR2Id, x.CropId, x.ProductId)))
                .Concat(focus3.Select(x => (x.CSR2Id, x.CropId, x.ProductId)))
                .GroupBy(x => x.CSR2Id)
                .ToDictionary(x => x.Key, x => new
                {
                    Crops = x.SelectMany(y => y.CropId ?? Array.Empty<int>()).Where(id => id > 0).Distinct().ToArray(),
                    Products = x.SelectMany(y => y.ProductId ?? Array.Empty<int>()).Where(id => id > 0).Distinct().ToArray()
                });

            // Resolve distinct saved focus IDs through the authoritative master tables.
            // Only the IDs used on the visible CSR-2 records are queried.
            var cropIds = byReport.Values.SelectMany(x => x.Crops).Distinct().ToArray();
            var productIds = byReport.Values.SelectMany(x => x.Products).Distinct().ToArray();

            var cropNames = await _db.Set<Crop>().AsNoTracking()
                .Where(x => cropIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name, HttpContext.RequestAborted);

            var productNames = await _db.Set<Product>().AsNoTracking()
                .Where(x => productIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name, HttpContext.RequestAborted);

            // Load documentation and treatment facts by the same CSR-2 ID used by this list.
            var documentationRows = await _db.Set<DemoDocumentation>().AsNoTracking()
                .Where(x => ids.Contains(x.CSR2Id))
                .Select(x => new { x.CSR2Id, x.Status, x.CropStageId, x.SowingDate, x.HarvestDate })
                .ToListAsync(HttpContext.RequestAborted);
            var documentationByCsr2 = documentationRows.GroupBy(x => x.CSR2Id)
                .ToDictionary(x => x.Key, x => x.First());
            var treatmentRows = await _db.Set<DemoTreatment>().AsNoTracking()
                .Where(x => ids.Contains(x.CSR2Id))
                .Select(x => new { x.CSR2Id, x.Status, x.VisitDate, x.TreatmentDate })
                .ToListAsync(HttpContext.RequestAborted);
            var treatmentsByCsr2 = treatmentRows.GroupBy(x => x.CSR2Id)
                .ToDictionary(x => x.Key, x => x.ToList());
            var stageIdsForList = documentationRows.Where(x => x.CropStageId.HasValue)
                .Select(x => x.CropStageId!.Value).Distinct().ToArray();
            var stageNamesForList = await _db.Set<CropStage>().AsNoTracking()
                .Where(x => stageIdsForList.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name, HttpContext.RequestAborted);

            // Unknown/deleted master IDs retain an explicit ID fallback.
            // Sowing/treatment milestones are NOT fields in CSR2; keep them empty.
            var result = rows.Select(row =>
            {
                byReport.TryGetValue(row.Id, out var focus);
                documentationByCsr2.TryGetValue(row.Id, out var savedDoc);
                treatmentsByCsr2.TryGetValue(row.Id, out var savedTreatments);
                var stageName = savedDoc?.CropStageId is int savedStageId &&
                    stageNamesForList.TryGetValue(savedStageId, out var stageLabel)
                    ? stageLabel : "Not Started";
                var completedCount = savedTreatments?.Count(t => t.Status == DemoTreatmentStatus.Completed) ?? 0;
                var lastTreatmentDate = savedTreatments?
                    .Select(t => t.TreatmentDate ?? t.VisitDate)
                    .Where(d => d.HasValue)
                    .OrderByDescending(d => d).FirstOrDefault();
                return new
                {
                    row.Id,
                    row.CSR1Id,
                    row.ProgramType,
                    row.CSR2Status,
                    row.CSR2CompletedDate,
                    SeasonNumber = row.SanctionNo ?? string.Empty, // UI column is Sanction Number
                    row.ProgramName,
                    Product = focus == null ? "" : string.Join(", ", focus.Products.Select(id => productNames.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name) ? name : $"Product #{id}")),
                    Crop = focus == null ? "" : string.Join(", ", focus.Crops.Select(id => cropNames.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name) ? name : $"Crop #{id}")),
                    District = "", // Resolve from your actual District/Location master when provided.
                    Location = row.ActualLocation ?? string.Empty,
                    CurrentStage = stageName,
                    SowingDate = savedDoc?.SowingDate,
                    HarvestDate = savedDoc?.HarvestDate,
                    TotalTreatments = savedTreatments?.Count ?? 0,
                    CompletedTreatments = completedCount,
                    LastTreatmentDate = lastTreatmentDate,
                    DocumentationStatus = savedDoc == null ? "Not Started" : savedDoc.Status.ToString(),
                    Status = savedDoc?.Status switch
                    {
                        DemoDocumentationStatus.Completed => "Completed",
                        DemoDocumentationStatus.Started => "In Progress",
                        _ => "Not Started"
                    }
                };
            }).ToList();

            return Ok(result);
        }

        [HttpGet("states")]
        public async Task<IActionResult> GetStates()
        {
            var result = await _db.Set<State>()
                .AsNoTracking()
                .OrderBy(x => x.StateName)
                .Select(x => new { x.Id, x.StateName })
                .ToListAsync(HttpContext.RequestAborted);
            return Ok(result);
        }

        [HttpGet("districts")]
        public async Task<IActionResult> GetDistricts([FromQuery] int stateId)
        {
            if (stateId <= 0) return BadRequest("Valid stateId is required.");

            var result = await _db.Set<District>()
                .AsNoTracking()
                .Where(x => x.StateId == stateId)
                .OrderBy(x => x.DistrictName)
                .Select(x => new { x.Id, x.DistrictName })
                .ToListAsync(HttpContext.RequestAborted);
            return Ok(result);
        }

        [HttpGet("crop-types")]
        public async Task<IActionResult> GetCropTypes([FromQuery] int cropId)
        {
            if (cropId <= 0)
                return BadRequest("Valid cropId is required.");

            bool cropExists = await _db.Set<Crop>().AsNoTracking()
                .AnyAsync(x => x.Id == cropId && x.IsActive, HttpContext.RequestAborted);
            if (!cropExists)
                return NotFound("Selected crop was not found or is inactive.");

            var result = await _db.Set<CropType>()
                .AsNoTracking()
                .Where(x => x.CropId == cropId && x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.CropId, x.Name })
                .ToListAsync(HttpContext.RequestAborted);
            return Ok(result);
        }

        [HttpGet("crop-stages")]
        public async Task<IActionResult> GetCropStages([FromQuery] int cropId)
        {
            if (cropId <= 0) return BadRequest("Valid cropId is required.");
            var cropExists = await _db.Set<Crop>().AsNoTracking()
                .AnyAsync(x => x.Id == cropId && x.IsActive, HttpContext.RequestAborted);
            if (!cropExists) return NotFound("Selected crop was not found or is inactive.");

            var stages = await _db.Set<CropStage>().AsNoTracking()
                .Where(x => x.CropId == cropId && x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.CropId, x.Name })
                .ToListAsync(HttpContext.RequestAborted);
            return Ok(stages);
        }

        public sealed class SaveDocumentationRequest
        {
            public int CSR2Id { get; set; }
            public string FarmerName { get; set; } = "";
            public string MobileNumber { get; set; } = "";
            public string? Village { get; set; }
            public string? FarmerAddress { get; set; }
            public int StateId { get; set; }
            public int DistrictId { get; set; }
            public string PinCode { get; set; } = "";
            public int CropId { get; set; }
            public int CropTypeId { get; set; }
            public DateTime? SowingDate { get; set; }
            public int? CropStageId { get; set; }
            public DateTime? HarvestDate { get; set; }
            public string? Remarks { get; set; }
        }

        // Server-authoritative eligibility for every read/write documentation request.
        private async Task<bool> CanAccessApprovedCsr2Async(int csr2Id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (csr2Id <= 0 || string.IsNullOrWhiteSpace(userId)) return false;
            return await (
                from c2 in _db.Set<CSR2>().AsNoTracking()
                join c1 in _db.Set<CSR1>().AsNoTracking() on c2.CSR1Id equals c1.Id
                where c2.Id == csr2Id && c2.Status == CSR2Status.Approved &&
                      c1.Status == CSR1Status.Approved && c1.IsActive &&
                      c1.ResponsiblePersonId == userId &&
                      _db.Set<Employeelogin>().Any(x => x.IsActive && x.UserId == userId)
                select c2.Id
            ).AnyAsync(HttpContext.RequestAborted);
        }

        [HttpGet("documentation-details")]
        public async Task<IActionResult> GetDocumentationDetails([FromQuery] int csr2Id)
        {
            if (csr2Id <= 0) return BadRequest("Invalid csr2Id.");
            if (!await CanAccessApprovedCsr2Async(csr2Id)) return Forbid();
            var doc = await _db.Set<DemoDocumentation>().AsNoTracking()
                .Where(x => x.CSR2Id == csr2Id)
                .Select(x => new
                {
                    x.Id,
                    x.CSR2Id,
                    x.FarmerName,
                    x.MobileNumber,
                    x.Village,
                    x.FarmerAddress,
                    x.StateId,
                    x.DistrictId,
                    x.PinCode,
                    x.CropId,
                    x.CropTypeId,
                    x.SowingDate,
                    x.CropStageId,
                    x.HarvestDate,
                    x.Remarks,
                    x.Status
                }).FirstOrDefaultAsync(HttpContext.RequestAborted);
            if (doc == null) return NotFound();
            var stateName = await _db.Set<State>().AsNoTracking()
                .Where(x => x.Id == doc.StateId).Select(x => x.StateName)
                .FirstOrDefaultAsync(HttpContext.RequestAborted);
            var districtName = await _db.Set<District>().AsNoTracking()
                .Where(x => x.Id == doc.DistrictId).Select(x => x.DistrictName)
                .FirstOrDefaultAsync(HttpContext.RequestAborted);
            var cropName = await _db.Set<Crop>().AsNoTracking()
                .Where(x => x.Id == doc.CropId).Select(x => x.Name)
                .FirstOrDefaultAsync(HttpContext.RequestAborted);
            return Ok(new
            {
                doc.Id,
                doc.CSR2Id,
                doc.FarmerName,
                doc.Village,
                doc.FarmerAddress,
                doc.StateId,
                doc.DistrictId,
                doc.PinCode,
                doc.CropId,
                doc.CropTypeId,
                doc.SowingDate,
                doc.CropStageId,
                doc.HarvestDate,
                doc.Remarks,
                State = stateName,
                District = districtName,
                Crop = cropName,
                MobileNumber = doc.MobileNumber,
                Status = doc.Status.ToString()
            });
        }

        [HttpPost("save-draft")]
        public Task<IActionResult> SaveDraft([FromBody] SaveDocumentationRequest request) => SaveDocumentationAsync(request, false);

        [HttpPost("start")]
        public Task<IActionResult> StartDocumentation([FromBody] SaveDocumentationRequest request) => SaveDocumentationAsync(request, true);

        private async Task<IActionResult> SaveDocumentationAsync(SaveDocumentationRequest request, bool start)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
            if (request.CSR2Id <= 0 || !await CanAccessApprovedCsr2Async(request.CSR2Id)) return Forbid();
            if (string.IsNullOrWhiteSpace(request.FarmerName) || request.FarmerName.Length > 200 ||
                string.IsNullOrWhiteSpace(request.MobileNumber) || request.MobileNumber.Length != 10 || !request.MobileNumber.All(char.IsDigit) ||
                string.IsNullOrWhiteSpace(request.PinCode) || request.PinCode.Length != 6 || !request.PinCode.All(char.IsDigit) ||
                request.StateId <= 0 || request.DistrictId <= 0 || request.CropId <= 0 || request.CropTypeId <= 0 ||
                request.CropStageId is null or <= 0)
                return BadRequest("Farmer, 10-digit mobile, 6-digit PIN and valid master IDs are required.");
            if (request.Village?.Length > 200 || request.FarmerAddress?.Length > 1000 ||
                request.Remarks?.Length > 2000 ||
                (request.SowingDate.HasValue && request.HarvestDate.HasValue && request.HarvestDate < request.SowingDate))
                return BadRequest("Field length or harvest date is invalid.");

            if (!await _db.Set<State>().AnyAsync(x => x.Id == request.StateId, HttpContext.RequestAborted) ||
                !await _db.Set<District>().AnyAsync(x => x.Id == request.DistrictId && x.StateId == request.StateId, HttpContext.RequestAborted) ||
                !await _db.Set<Crop>().AnyAsync(x => x.Id == request.CropId && x.IsActive, HttpContext.RequestAborted) ||
                !await _db.Set<CropType>().AnyAsync(x => x.Id == request.CropTypeId && x.CropId == request.CropId && x.IsActive, HttpContext.RequestAborted) ||
                !await _db.Set<CropStage>().AnyAsync(x => x.Id == request.CropStageId && x.CropId == request.CropId && x.IsActive, HttpContext.RequestAborted))
                return BadRequest("State, District, Crop, Crop Type or Crop Stage selection is invalid.");

            // Selected crop must be in one of the finally approved CSR-2 focus groups.
            var groups1 = await _db.Set<CSR2Products1>().AsNoTracking().Where(x => x.CSR2Id == request.CSR2Id && x.IsActive).Select(x => x.CropId).ToListAsync(HttpContext.RequestAborted);
            var groups2 = await _db.Set<CSR2Products2>().AsNoTracking().Where(x => x.CSR2Id == request.CSR2Id && x.IsActive).Select(x => x.CropId).ToListAsync(HttpContext.RequestAborted);
            var groups3 = await _db.Set<CSR2Products3>().AsNoTracking().Where(x => x.CSR2Id == request.CSR2Id && x.IsActive).Select(x => x.CropId).ToListAsync(HttpContext.RequestAborted);
            if (!groups1.Concat(groups2).Concat(groups3).Any(ids => ids != null && ids.Contains(request.CropId)))
                return BadRequest("Crop is not in this approved CSR-2 focus list.");

            var doc = await _db.Set<DemoDocumentation>().SingleOrDefaultAsync(x => x.CSR2Id == request.CSR2Id, HttpContext.RequestAborted);
            if (doc?.Status == DemoDocumentationStatus.Completed)
                return Conflict("Completed documentation is read-only.");
            if (doc != null && doc.Status == DemoDocumentationStatus.Started && !start)
                return Conflict("Documentation already started. Draft updates are not allowed.");
            if (doc == null)
            {
                doc = new DemoDocumentation { CSR2Id = request.CSR2Id, CreatedAt = DateTime.UtcNow, CreatedBy = userId };
                _db.Set<DemoDocumentation>().Add(doc);
            }
            else { doc.UpdatedAt = DateTime.UtcNow; doc.UpdatedBy = userId; }
            doc.FarmerName = request.FarmerName.Trim();
            doc.MobileNumber = request.MobileNumber;
            doc.Village = request.Village?.Trim();
            doc.FarmerAddress = request.FarmerAddress?.Trim();
            doc.StateId = request.StateId;
            doc.DistrictId = request.DistrictId;
            doc.PinCode = request.PinCode;
            doc.CropId = request.CropId;
            doc.CropTypeId = request.CropTypeId;
            doc.SowingDate = request.SowingDate;
            doc.CropStageId = request.CropStageId;
            doc.HarvestDate = request.HarvestDate;
            doc.Remarks = request.Remarks?.Trim();
            if (start && doc.Status != DemoDocumentationStatus.Started)
            {
                doc.Status = DemoDocumentationStatus.Started;
                doc.StartedAt = DateTime.UtcNow;
                doc.StartedBy = userId;
            }
            await _db.SaveChangesAsync(HttpContext.RequestAborted);
            return Ok(new { doc.Id, doc.CSR2Id, Status = doc.Status.ToString() });
        }

        // Treatment endpoints integrated into DemoDocumentationController.
        public sealed class SaveRequest
        {
            public int CSR2Id { get; set; }
            public int TreatmentNumber { get; set; } = 1;
            public DateTime? VisitDate { get; set; }
            public string? FieldOfficerUserId { get; set; }
            public int? CropStageId { get; set; }
            public string? PlantCondition { get; set; }
            public int? ProductId { get; set; }
            public string? Dosage { get; set; }
            public string? ApplicationMethod { get; set; }
            public DateTime? TreatmentDate { get; set; }
            public string? ApplicationStage { get; set; }
            public string? TreatmentRemarks { get; set; }
            public string? PhysicalParameters { get; set; }
            public string? YieldParameters { get; set; }
            public string? ObservationNotes { get; set; }
            public bool HasProblem { get; set; }
            public string? ProblemCategory { get; set; }
            public string? ProblemDescription { get; set; }
            public string? Severity { get; set; }
            public string? Recommendation { get; set; }
            public string? ActionTaken { get; set; }
            public List<string> QuickObservations { get; set; } = new();
        }

        private string? TreatmentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // CSR-1 ResponsiblePersonId owns both treatment writes and reads.
        // Completed documentation remains readable; writes require Started status.
        private async Task<DemoDocumentation?> AuthorizedDocumentation(int csr2Id, bool requireStarted = false)
        {
            if (!await CanAccessApprovedCsr2Async(csr2Id)) return null;
            var query = _db.Set<DemoDocumentation>().Where(x => x.CSR2Id == csr2Id);
            if (requireStarted)
                query = query.Where(x => x.Status == DemoDocumentationStatus.Started);
            else
                query = query.Where(x => x.Status == DemoDocumentationStatus.Started ||
                                         x.Status == DemoDocumentationStatus.Completed);
            return await query.SingleOrDefaultAsync(HttpContext.RequestAborted);
        }

        [HttpGet("treatment/lookups")]
        public async Task<IActionResult> Lookups([FromQuery] int csr2Id)
        {
            var doc = await AuthorizedDocumentation(csr2Id);
            if (doc == null) return Forbid();
            var stages = await _db.Set<CropStage>().AsNoTracking()
                .Where(x => x.CropId == doc.CropId && x.IsActive)
                .OrderBy(x => x.Name).Select(x => new { x.Id, x.Name })
                .ToListAsync(HttpContext.RequestAborted);
            var p1 = await _db.Set<CSR2Products1>().AsNoTracking().Where(x => x.CSR2Id == csr2Id && x.IsActive).Select(x => x.ProductId).ToListAsync(HttpContext.RequestAborted);
            var p2 = await _db.Set<CSR2Products2>().AsNoTracking().Where(x => x.CSR2Id == csr2Id && x.IsActive).Select(x => x.ProductId).ToListAsync(HttpContext.RequestAborted);
            var p3 = await _db.Set<CSR2Products3>().AsNoTracking().Where(x => x.CSR2Id == csr2Id && x.IsActive).Select(x => x.ProductId).ToListAsync(HttpContext.RequestAborted);
            var ids = p1.Concat(p2).Concat(p3).Where(x => x != null).SelectMany(x => x!).Distinct().ToArray();
            var products = await _db.Set<Product>().AsNoTracking().Where(x => ids.Contains(x.Id) && x.IsActive)
                .OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync(HttpContext.RequestAborted);
            // Logins provide the real GUID/string UserId saved with the treatment.
            // EmployeeInformation provides the human-readable officer name.
            // Resolve the employee/login relationship using known alternate property names
            // because the EmployeeInformation entity differs between SPIC branches.
            var employeeRecords = await _db.Set<EmployeeInformation>()
                .AsNoTracking().ToListAsync(HttpContext.RequestAborted);
            var activeLogins = await _db.Set<Employeelogin>()
                .AsNoTracking().Where(x => x.IsActive)
                .ToListAsync(HttpContext.RequestAborted);

            static string? EmployeeField(object record, params string[] names)
            {
                foreach (var name in names)
                {
                    var prop = record.GetType().GetProperties().FirstOrDefault(p =>
                        p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                    var value = prop?.GetValue(record)?.ToString();
                    if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
                }
                return null;
            }
            static string? OfficerName(object record)
            {
                var fullName = EmployeeField(record, "EmployeeName", "FullName", "Name", "EmployeeFullName");
                if (!string.IsNullOrWhiteSpace(fullName)) return fullName;
                var first = EmployeeField(record, "FirstName", "First_Name", "EmpFirstName", "EmployeeFirstName");
                var last = EmployeeField(record, "LastName", "Last_Name", "SurName", "EmployeeLastName");
                var combined = string.Join(" ", new[] { first, last }.Where(x => !string.IsNullOrWhiteSpace(x)));
                return string.IsNullOrWhiteSpace(combined)
                    ? EmployeeField(record, "UserName", "EmployeeCode") : combined;
            }
            var officerRows = employeeRecords.Select(employee => new
            {
                EmployeeId = EmployeeField(employee, "Id", "EmployeeInformationId", "EmployeeId"),
                UserId = EmployeeField(employee, "UserId", "EmployeeUserId", "EmployeeLoginId"),
                Name = OfficerName(employee)
            }).Where(x => !string.IsNullOrWhiteSpace(x.Name)).ToList();

            var officers = activeLogins.Select(login =>
            {
                var employeeId = EmployeeField(login, "EmployeeInformationId", "EmployeeId", "EmployeeInfoId");
                var employee = officerRows.FirstOrDefault(x =>
                    (!string.IsNullOrWhiteSpace(employeeId) && x.EmployeeId == employeeId) ||
                    (!string.IsNullOrWhiteSpace(x.UserId) && x.UserId == login.UserId));
                return new { Id = login.UserId, Name = employee?.Name };
            }).Where(x => !string.IsNullOrWhiteSpace(x.Id) && !string.IsNullOrWhiteSpace(x.Name))
              .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
              .Select(g => new { Id = g.Key, Name = g.First().Name! })
              .OrderBy(x => x.Name).ToList();
            var assignedOfficer = officers.Where(o => o.Id == TreatmentUserId).ToList();
            if (assignedOfficer.Count == 0)
                assignedOfficer.Add(new { Id = TreatmentUserId!, Name = "Assigned Responsible Person" });
            return Ok(new
            {
                stages,
                products,
                officers = assignedOfficer,
                defaultFieldOfficerUserId = TreatmentUserId
            });
        }


        // Treatment Journey: records are read from persisted treatment tables.
        // Uses the same creator-only authorization currently used by treatment/details.
        [HttpGet("treatment/journey")]
        public async Task<IActionResult> GetTreatmentJourney([FromQuery] int csr2Id)
        {
            var doc = await AuthorizedDocumentation(csr2Id);
            if (doc == null) return Forbid();

            var rows = await _db.Set<DemoTreatment>().AsNoTracking()
                .Where(x => x.DemoDocumentationId == doc.Id)
                .OrderBy(x => x.TreatmentNumber)
                .Select(x => new
                {
                    x.Id,
                    x.TreatmentNumber,
                    x.Status,
                    x.VisitDate,
                    x.TreatmentDate,
                    x.FieldOfficerUserId,
                    x.CropStageId,
                    x.ProductId,
                    x.PlantCondition,
                    x.Dosage,
                    x.ApplicationMethod,
                    x.ApplicationStage,
                    x.TreatmentRemarks,
                    x.PhysicalParameters,
                    x.YieldParameters,
                    x.ObservationNotes,
                    x.HasProblem,
                    x.ProblemCategory,
                    x.ProblemDescription,
                    x.Severity,
                    x.Recommendation,
                    x.ActionTaken,
                    QuickObservations = x.Observations.Select(o => o.Observation).ToList(),
                    Photos = x.Photos.Select(p => p.PhotoType).ToList()
                }).ToListAsync(HttpContext.RequestAborted);

            var stageIds = rows.Where(x => x.CropStageId.HasValue).Select(x => x.CropStageId!.Value).Distinct().ToArray();
            var productIds = rows.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).Distinct().ToArray();
            var stages = await _db.Set<CropStage>().AsNoTracking().Where(x => stageIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name, HttpContext.RequestAborted);
            var products = await _db.Set<Product>().AsNoTracking().Where(x => productIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name, HttpContext.RequestAborted);

            return Ok(rows.Select(x => new
            {
                x.Id,
                x.TreatmentNumber,
                Status = x.Status.ToString(),
                x.VisitDate,
                x.TreatmentDate,
                FieldOfficerName = x.FieldOfficerUserId, // Show ID until EmployeeInformation name mapping is verified.
                CropStageName = x.CropStageId.HasValue && stages.TryGetValue(x.CropStageId.Value, out var stage) ? stage : (string?)null,
                ProductName = x.ProductId.HasValue && products.TryGetValue(x.ProductId.Value, out var product) ? product : (string?)null,
                x.PlantCondition,
                x.Dosage,
                x.ApplicationMethod,
                x.ApplicationStage,
                x.TreatmentRemarks,
                x.PhysicalParameters,
                x.YieldParameters,
                x.ObservationNotes,
                x.HasProblem,
                x.ProblemCategory,
                x.ProblemDescription,
                x.Severity,
                x.Recommendation,
                x.ActionTaken,
                x.QuickObservations,
                ControlPhotoCount = x.Photos.Count(p => p == "Control"),
                TreatedPhotoCount = x.Photos.Count(p => p == "Treated")
            }));
        }

        [HttpGet("treatment/details")]
        public async Task<IActionResult> Details([FromQuery] int csr2Id, [FromQuery] int treatmentNumber = 1)
        {
            if (await AuthorizedDocumentation(csr2Id) == null) return Forbid();
            var row = await _db.Set<DemoTreatment>().AsNoTracking()
                .Where(x => x.DemoDocumentation!.CSR2Id == csr2Id && x.TreatmentNumber == treatmentNumber)
                .Select(x => new {
                    x.Id,
                    x.Status,
                    x.VisitDate,
                    x.FieldOfficerUserId,
                    x.CropStageId,
                    x.PlantCondition,
                    x.ProductId,
                    x.Dosage,
                    x.ApplicationMethod,
                    x.TreatmentDate,
                    x.ApplicationStage,
                    x.TreatmentRemarks,
                    x.PhysicalParameters,
                    x.YieldParameters,
                    x.ObservationNotes,
                    x.HasProblem,
                    x.ProblemCategory,
                    x.ProblemDescription,
                    x.Severity,
                    x.Recommendation,
                    x.ActionTaken,
                    QuickObservations = x.Observations.Select(y => y.Observation).ToList(),
                    Photos = x.Photos.Select(p => new { p.Id, p.PhotoType, p.FileName }).ToList()
                })
                .FirstOrDefaultAsync(HttpContext.RequestAborted);
            return row == null ? NotFound() : Ok(row);
        }

        [HttpPost("treatment/save")]
        public Task<IActionResult> Save([FromBody] SaveRequest request) => SaveInternal(request, false);

        [HttpPost("treatment/complete")]
        public Task<IActionResult> Complete([FromBody] SaveRequest request) => SaveInternal(request, true);

        private async Task<IActionResult> SaveInternal(SaveRequest req, bool complete)
        {
            var doc = await AuthorizedDocumentation(req.CSR2Id, requireStarted: true);
            if (doc == null) return Forbid();
            if (req.TreatmentNumber <= 0) return BadRequest("Invalid treatment number.");
            if (!string.IsNullOrWhiteSpace(req.FieldOfficerUserId) &&
                req.FieldOfficerUserId != TreatmentUserId)
                return Forbid();
            if (req.CropStageId.HasValue && !await _db.Set<CropStage>().AnyAsync(x => x.Id == req.CropStageId && x.CropId == doc.CropId && x.IsActive, HttpContext.RequestAborted))
                return BadRequest("Crop stage does not belong to the selected crop.");
            if (req.ProductId.HasValue && !await _db.Set<Product>().AnyAsync(x => x.Id == req.ProductId && x.IsActive, HttpContext.RequestAborted))
                return BadRequest("Invalid product.");
            if (req.VisitDate.HasValue && req.TreatmentDate.HasValue && req.TreatmentDate < req.VisitDate)
                return BadRequest("Treatment date cannot precede visit date.");
            if (complete && (!req.VisitDate.HasValue || !req.TreatmentDate.HasValue || !req.CropStageId.HasValue || !req.ProductId.HasValue))
                return BadRequest("Visit date, treatment date, crop stage and product are required to complete.");

            var obs = req.QuickObservations.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (obs.Any(x => x.Length > 200)) return BadRequest("Quick observation exceeds 200 characters.");
            var t = await _db.Set<DemoTreatment>().Include(x => x.Observations)
                .SingleOrDefaultAsync(x => x.DemoDocumentationId == doc.Id && x.TreatmentNumber == req.TreatmentNumber, HttpContext.RequestAborted);
            if (t?.Status == DemoTreatmentStatus.Completed) return Conflict("Completed treatment cannot be edited.");
            if (t == null)
            {
                t = new DemoTreatment { CSR2Id = doc.CSR2Id, DemoDocumentationId = doc.Id, TreatmentNumber = req.TreatmentNumber, CreatedBy = TreatmentUserId };
                _db.Set<DemoTreatment>().Add(t);
            }
            else { t.UpdatedAt = DateTime.UtcNow; t.UpdatedBy = TreatmentUserId; }
            // Repair the CSR2Id on an existing treatment whenever it is saved.
            t.CSR2Id = doc.CSR2Id;
            t.VisitDate = req.VisitDate; t.FieldOfficerUserId = TreatmentUserId;
            t.CropStageId = req.CropStageId; t.PlantCondition = req.PlantCondition;
            t.ProductId = req.ProductId; t.Dosage = req.Dosage; t.ApplicationMethod = req.ApplicationMethod;
            t.TreatmentDate = req.TreatmentDate; t.ApplicationStage = req.ApplicationStage;
            t.TreatmentRemarks = req.TreatmentRemarks; t.PhysicalParameters = req.PhysicalParameters;
            t.YieldParameters = req.YieldParameters; t.ObservationNotes = req.ObservationNotes;
            t.HasProblem = req.HasProblem;
            t.ProblemCategory = req.HasProblem ? req.ProblemCategory : null;
            t.ProblemDescription = req.HasProblem ? req.ProblemDescription : null;
            t.Severity = req.HasProblem ? req.Severity : null;
            t.Recommendation = req.Recommendation; t.ActionTaken = req.ActionTaken;
            _db.Set<DemoTreatmentObservation>().RemoveRange(t.Observations);
            foreach (var value in obs) t.Observations.Add(new DemoTreatmentObservation { Observation = value });
            if (complete)
            {
                t.Status = DemoTreatmentStatus.Completed;
                t.CompletedAt = DateTime.UtcNow;
                t.CompletedBy = TreatmentUserId;
                // Completing a problem-free treatment also closes its documentation.
                // A visit with a recorded problem completes ONLY the treatment.
                if (!req.HasProblem)
                {
                    doc.Status = DemoDocumentationStatus.Completed;
                    doc.UpdatedAt = DateTime.UtcNow;
                    doc.UpdatedBy = TreatmentUserId;
                }
            }
            await _db.SaveChangesAsync(HttpContext.RequestAborted);
            return Ok(new { t.Id, Status = t.Status.ToString() });
        }

        [HttpPost("treatment/photos")]
        [RequestSizeLimit(220_000_000)]
        public async Task<IActionResult> Photos([FromForm] int csr2Id, [FromForm] int treatmentNumber, [FromForm] string photoType, [FromForm] List<IFormFile> files)
        {
            var doc = await AuthorizedDocumentation(csr2Id, requireStarted: true);
            if (doc == null) return Forbid();
            if (photoType != "Control" && photoType != "Treated") return BadRequest("Invalid photo type.");
            var treatment = await _db.Set<DemoTreatment>().SingleOrDefaultAsync(x => x.DemoDocumentationId == doc.Id && x.TreatmentNumber == treatmentNumber, HttpContext.RequestAborted);
            if (treatment == null) return BadRequest("Save treatment draft before uploading photos.");
            if (treatment.Status == DemoTreatmentStatus.Completed) return Conflict("Treatment is completed.");
            if (files.Count == 0 || files.Count > 20) return BadRequest("Upload 1-20 images.");
            foreach (var file in files)
            {
                if (file.Length <= 0 || file.Length > 10 * 1024 * 1024 || (file.ContentType != "image/jpeg" && file.ContentType != "image/png"))
                    return BadRequest("Only JPG/PNG images up to 10 MB each are permitted.");
            }
            foreach (var file in files)
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream, HttpContext.RequestAborted);
                _db.Set<DemoTreatmentPhoto>().Add(new DemoTreatmentPhoto
                {
                    DemoTreatmentId = treatment.Id,
                    PhotoType = photoType,
                    FileName = Path.GetFileName(file.FileName),
                    ContentType = file.ContentType,
                    Data = stream.ToArray()
                });
            }
            await _db.SaveChangesAsync(HttpContext.RequestAborted);
            return Ok();
        }

        [HttpGet("treatment/photo/{id:int}")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var photo = await _db.Set<DemoTreatmentPhoto>().AsNoTracking()
                .Where(x => x.Id == id).Select(x => new { x.Data, x.ContentType, CSR2Id = x.DemoTreatment!.DemoDocumentation!.CSR2Id })
                .FirstOrDefaultAsync(HttpContext.RequestAborted);
            if (photo == null) return NotFound();
            if (await AuthorizedDocumentation(photo.CSR2Id) == null) return Forbid();
            return File(photo.Data, photo.ContentType);
        }

        [HttpDelete("treatment/photo/{id:int}")]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            var photo = await _db.Set<DemoTreatmentPhoto>().Include(x => x.DemoTreatment).ThenInclude(x => x!.DemoDocumentation)
                .SingleOrDefaultAsync(x => x.Id == id, HttpContext.RequestAborted);
            if (photo == null) return NotFound();
            if (await AuthorizedDocumentation(photo.DemoTreatment!.DemoDocumentation!.CSR2Id, requireStarted: true) == null) return Forbid();
            if (photo.DemoTreatment.Status == DemoTreatmentStatus.Completed) return Conflict("Treatment is completed.");
            _db.Set<DemoTreatmentPhoto>().Remove(photo);
            await _db.SaveChangesAsync(HttpContext.RequestAborted);
            return Ok();
        }
    }
}
