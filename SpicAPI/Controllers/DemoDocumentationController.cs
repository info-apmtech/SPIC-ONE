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

            var roles = (await _db.Set<Employeelogin>().AsNoTracking()
                    .Where(x => x.IsActive && x.UserId == userId)
                    .Select(x => x.Role)
                    .ToListAsync(HttpContext.RequestAborted))
                .Select(x => x.ToString())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var claim in User.FindAll(ClaimTypes.Role)) roles.Add(claim.Value);
            bool Has(params string[] names) => names.Any(roles.Contains);

            var employee = await _db.Set<Employeelogin>().AsNoTracking()
                .Where(x => x.IsActive && x.UserId == userId)
                .Select(x => new { x.StateId, x.RegionId, x.HeadquartersId })
                .FirstOrDefaultAsync(HttpContext.RequestAborted);

            // All final approved CSR-2 reports joined with their approved, active CSR-1.
            var query =
                from csr2 in _db.Set<CSR2>().AsNoTracking()
                join csr1 in _db.Set<CSR1>().AsNoTracking() on csr2.CSR1Id equals csr1.Id
                join program in _db.Set<ProgramMaster>().AsNoTracking() on csr1.ProgramId equals program.Id
                where csr2.Status == CSR2Status.Approved &&
                      csr1.Status == CSR1Status.Approved && csr1.IsActive
                select new { csr2, csr1, program };

            // Same ownership + organizational-scope approach used by CSR2Controller.
            // Never trust a State/HQ ID submitted by the browser to establish authorization.
            if (!Has("Admin", "SuperAdmin", "MDADMIN", "MDHOD", "CSMDHOD", "CSMD-HOD", "RMDO"))
            {
                if (employee == null)
                {
                    query = query.Where(x => x.csr1.ResponsiblePersonId == userId);
                }
                else if (Has("SMD", "SMDO", "SMM") && employee.StateId > 0)
                {
                    int stateId = employee.StateId;
                    query = query.Where(x => x.csr1.ResponsiblePersonId == userId ||
                        _db.Set<Employeelogin>().Any(login => login.IsActive &&
                            login.UserId == x.csr1.CreatedBy && login.StateId == stateId));
                }
                else if (Has("MDO", "JMDO", "MO", "RM", "RMM") && employee.HeadquartersId > 0)
                {
                    int hqId = employee.HeadquartersId;
                    query = query.Where(x => x.csr1.ResponsiblePersonId == userId ||
                        _db.Set<Employeelogin>().Any(login => login.IsActive &&
                            login.UserId == x.csr1.CreatedBy && login.HeadquartersId == hqId));
                }
                else
                {
                    query = query.Where(x => x.csr1.ResponsiblePersonId == userId);
                }
            }

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

            // Unknown/deleted master IDs retain an explicit ID fallback.
            // Sowing/treatment milestones are NOT fields in CSR2; keep them empty.
            var result = rows.Select(row =>
            {
                byReport.TryGetValue(row.Id, out var focus);
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
                    CurrentStage = "Not Started",
                    SowingDate = (DateTime?)null,
                    HarvestDate = (DateTime?)null,
                    TotalTreatments = 0,
                    CompletedTreatments = 0,
                    LastTreatmentDate = (DateTime?)null,
                    DocumentationStatus = "Pending",
                    Status = "Not Started"
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

        // Matches GetApprovedCSR2 user/role/geographic scope; never trusts browser ownership.
        private async Task<bool> CanAccessApprovedCsr2Async(int csr2Id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return false;
            var roles = (await _db.Set<Employeelogin>().AsNoTracking()
                .Where(x => x.IsActive && x.UserId == userId).Select(x => x.Role)
                .ToListAsync(HttpContext.RequestAborted))
                .Select(x => x.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var claim in User.FindAll(ClaimTypes.Role)) roles.Add(claim.Value);
            bool Has(params string[] names) => names.Any(roles.Contains);
            var employee = await _db.Set<Employeelogin>().AsNoTracking()
                .Where(x => x.IsActive && x.UserId == userId)
                .Select(x => new { x.StateId, x.HeadquartersId })
                .FirstOrDefaultAsync(HttpContext.RequestAborted);
            var query = from c2 in _db.Set<CSR2>().AsNoTracking()
                        join c1 in _db.Set<CSR1>().AsNoTracking() on c2.CSR1Id equals c1.Id
                        where c2.Id == csr2Id && c2.Status == CSR2Status.Approved &&
                              c1.Status == CSR1Status.Approved && c1.IsActive
                        select c1;
            if (!Has("Admin", "SuperAdmin", "MDADMIN", "MDHOD", "CSMDHOD", "CSMD-HOD", "RMDO"))
            {
                if (employee != null && Has("SMD", "SMDO", "SMM") && employee.StateId > 0)
                {
                    int stateId = employee.StateId;
                    query = query.Where(c1 => c1.ResponsiblePersonId == userId ||
                        _db.Set<Employeelogin>().Any(login => login.IsActive &&
                            login.UserId == c1.CreatedBy && login.StateId == stateId));
                }
                else if (employee != null && Has("MDO", "JMDO", "MO", "RM", "RMM") && employee.HeadquartersId > 0)
                {
                    int hqId = employee.HeadquartersId;
                    query = query.Where(c1 => c1.ResponsiblePersonId == userId ||
                        _db.Set<Employeelogin>().Any(login => login.IsActive &&
                            login.UserId == c1.CreatedBy && login.HeadquartersId == hqId));
                }
                else query = query.Where(c1 => c1.ResponsiblePersonId == userId);
            }
            return await query.AnyAsync(HttpContext.RequestAborted);
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
    }
}
