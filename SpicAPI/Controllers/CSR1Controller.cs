using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using System.Security.Claims;
using System.Xml;
using static SPIC.Core.Entities.EmployeeRegistration;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CSR1Controller : ControllerBase
    {
        private readonly IGenericRepository<CSR1> _csr1Repo;
        private readonly IGenericRepository<ProgramMaster> _programMasterRepo;
        private readonly IGenericRepository<ProgramType> _programTypeRepo;
        private readonly IGenericRepository<CSR1Products1> _csr1Products1Repo;
        private readonly IGenericRepository<CSR1Products2> _csr1Products2Repo;
        private readonly IGenericRepository<CSR1Products3> _csr1Products3Repo;

        private readonly IGenericRepository<Crop> _cropRepo;
        private readonly IGenericRepository<Product> _productRepo;

        private readonly IGenericRepository<Headquarter> _headquarterRepo;
        private readonly IGenericRepository<Zone> _zoneRepo;

        private readonly IGenericRepository<EmployeeInformation> _employeeRepo;
        private readonly AppDbContext _db;

        public CSR1Controller(
            IGenericRepository<CSR1> csr1Repo,
            IGenericRepository<ProgramMaster> programMasterRepo,
            IGenericRepository<ProgramType> programTypeRepo,
            IGenericRepository<CSR1Products1> csr1Products1Repo,
            IGenericRepository<CSR1Products2> csrProducts2Repo,
            IGenericRepository<CSR1Products3> csrProducts3Repo,
            IGenericRepository<Crop> cropRepo,
            IGenericRepository<Product> productRepo,
            IGenericRepository<Headquarter> headquarterRepo,
            IGenericRepository<Zone> zoneRepo,
            IGenericRepository<EmployeeInformation> employeeRepo,
            AppDbContext db)
        {
            _csr1Repo = csr1Repo;
            _programMasterRepo = programMasterRepo;
            _programTypeRepo = programTypeRepo;

            _csr1Products1Repo = csr1Products1Repo;
            _csr1Products2Repo = csrProducts2Repo;
            _csr1Products3Repo = csrProducts3Repo;

            _cropRepo = cropRepo;
            _productRepo = productRepo;
            _headquarterRepo = headquarterRepo;
            _zoneRepo = zoneRepo;
            _employeeRepo = employeeRepo;
            _db = db;
        }

        private string CurrentUserId =>
            User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User?.FindFirst("sub")?.Value
            ?? User?.FindFirst("id")?.Value
            ?? User?.Identity?.Name
            ?? "System";

        private string CurrentUser => CurrentUserId;

        private async Task NormalizeCSR1UserIdsAsync()
        {
            try
            {
                var csr1s = await _db.CSR1
                    .Where(c => (!string.IsNullOrEmpty(c.CreatedBy) && !c.CreatedBy.Contains("-"))
                             || (!string.IsNullOrEmpty(c.UpdatedBy) && !c.UpdatedBy.Contains("-")))
                    .ToListAsync();

                if (csr1s.Count > 0)
                {
                    var users = await _db.Users.AsNoTracking().ToListAsync();
                    var changed = false;

                    foreach (var c in csr1s)
                    {
                        if (!string.IsNullOrEmpty(c.CreatedBy) && !c.CreatedBy.Contains("-"))
                        {
                            var user = users.FirstOrDefault(u =>
                                string.Equals(u.UserName, c.CreatedBy, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(u.Name, c.CreatedBy, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(u.Email, c.CreatedBy, StringComparison.OrdinalIgnoreCase));
                            if (user != null)
                            {
                                c.CreatedBy = user.Id;
                                changed = true;
                            }
                        }

                        if (!string.IsNullOrEmpty(c.UpdatedBy) && !c.UpdatedBy.Contains("-"))
                        {
                            var user = users.FirstOrDefault(u =>
                                string.Equals(u.UserName, c.UpdatedBy, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(u.Name, c.UpdatedBy, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(u.Email, c.UpdatedBy, StringComparison.OrdinalIgnoreCase));
                            if (user != null)
                            {
                                c.UpdatedBy = user.Id;
                                changed = true;
                            }
                        }
                    }

                    if (changed)
                    {
                        await _db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CSR1] NormalizeCSR1UserIds error: {ex.Message}");
            }
        }

        private static string ResolveCSR1Scope(string? creatorRole, int? headquarterId)
        {
            if (!string.IsNullOrEmpty(creatorRole))
            {
                if (string.Equals(creatorRole, AppRole.SMD.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(creatorRole, AppRole.SMM.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return "State";
                }

                if (string.Equals(creatorRole, AppRole.MDO.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(creatorRole, AppRole.JMDO.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(creatorRole, AppRole.MO.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(creatorRole, AppRole.RM.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(creatorRole, AppRole.RMD.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return "HQ";
                }
            }

            // Fallback: If HeadquarterId is present and > 0, it's HQ scope; otherwise State scope.
            return (headquarterId.HasValue && headquarterId.Value > 0) ? "HQ" : "State";
        }

        private async Task<(string Scope, string? CreatorRole)> ResolveCSR1ScopeAsync(CSR1 csr1)
        {
            string? creatorRole = null;
            if (!string.IsNullOrEmpty(csr1.CreatedBy))
            {
                var creator = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == csr1.CreatedBy);
                if (creator != null)
                {
                    creatorRole = creator.Role.ToString();
                }
            }

            return (ResolveCSR1Scope(creatorRole, csr1.HeadquarterId), creatorRole);
        }


        // ============================================================
        // CREATE CSR1
        // POST: api/CSR1
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> SaveCSR1(
    [FromBody] CSR1SaveRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                if (request == null || request.CSR1 == null)
                {
                    return BadRequest(new
                    {
                        message = "Invalid CSR1 data."
                    });
                }

                var model = request.CSR1;


                // =====================================================
                // VALIDATE PROGRAM
                // =====================================================

                if (model.ProgramId <= 0)
                {
                    return BadRequest(new
                    {
                        message = "ProgramId is required."
                    });
                }


                var programMaster = await _programMasterRepo
                    .GetWhere(x => x.Id == model.ProgramId)
                    .FirstOrDefaultAsync();


                if (programMaster == null)
                {
                    return BadRequest(new
                    {
                        message = "Selected Program not found."
                    });
                }


                model.ProgramTypeId =
                    programMaster.ProgramTypeId;


                if (model.ProgramTypeId <= 0)
                {
                    return BadRequest(new
                    {
                        message =
                            "ProgramTypeId not configured for selected Program."
                    });
                }


                // =====================================================
                // VALIDATE CROP / PRODUCT GROUP 1
                // =====================================================

                if (request.Crop1Id > 0 &&
                    (request.Product1Ids == null ||
                     !request.Product1Ids.Any(x => x > 0)))
                {
                    return BadRequest(new
                    {
                        message =
                            "Please select at least one product for Focus Crop 1."
                    });
                }


                // =====================================================
                // VALIDATE CROP / PRODUCT GROUP 2
                // =====================================================

                if (request.Crop2Id > 0 &&
                    (request.Product2Ids == null ||
                     !request.Product2Ids.Any(x => x > 0)))
                {
                    return BadRequest(new
                    {
                        message =
                            "Please select at least one product for Focus Crop 2."
                    });
                }


                // =====================================================
                // VALIDATE CROP / PRODUCT GROUP 3
                // =====================================================

                if (request.Crop3Id > 0 &&
                    (request.Product3Ids == null ||
                     !request.Product3Ids.Any(x => x > 0)))
                {
                    return BadRequest(new
                    {
                        message =
                            "Please select at least one product for Focus Crop 3."
                    });
                }


                // =====================================================
                // VALIDATE EXPENSES AGAINST BUDGET
                // =====================================================

                var totalExpenses = model.PrintingAndStationery + model.PublicityMaterial +
                    model.StageArrangements + model.ServiceChargesLCA + model.TransportRent +
                    model.JeepRunningExpenses + model.Refreshments + model.Inputs +
                    model.Photography + model.Compliments + model.Others;

                if (model.Budget > 0 && totalExpenses > model.Budget)
                {
                    return BadRequest(new
                    {
                        message = $"Total Expense Breakdown (₹{totalExpenses:N2}) exceeds the allocated Budget (₹{model.Budget:N2}). Expenses must be within the allocated budget."
                    });
                }

                // =====================================================
                // AUDIT
                // =====================================================

                var currentUserId = CurrentUserId;
                var resolvedUserId = (!string.IsNullOrWhiteSpace(currentUserId) && currentUserId != "System" && currentUserId.Contains("-"))
                    ? currentUserId
                    : (!string.IsNullOrWhiteSpace(model.CreatedBy) && model.CreatedBy.Contains("-") ? model.CreatedBy : currentUserId);

                var currentDate =
                    DateTime.Now;


                model.CreatedAt =
                    (model.CreatedAt != default(DateTime))
                        ? model.CreatedAt
                        : currentDate;

                model.UpdatedAt =
                    currentDate;

                model.CreatedBy =
                    resolvedUserId;

                model.UpdatedBy =
                    resolvedUserId;

                model.IsActive =
                    true;

                model.Status =
                    "SubmittedForValidation";


                // =====================================================
                // SAVE CSR1
                // =====================================================

                var created =
                    await _csr1Repo.CreateAsync(model);


                if (created == null)
                {
                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new
                        {
                            message = "Unable to save CSR1."
                        });
                }


                int csr1Id =
                    created.Id;


                // =====================================================
                // SAVE GROUP 1
                // CSR1Products1
                // =====================================================

                if (request.Crop1Id > 0 &&
                    request.Product1Ids != null)
                {
                    foreach (var productId in request.Product1Ids
                                 .Where(x => x > 0)
                                 .Distinct())
                    {
                        var product =
                            new CSR1Products1
                            {
                                CSR1Id =
                                    csr1Id,

                                CropId =
                                    request.Crop1Id,

                                ProductId =
                                    productId,

                                IsActive =
                                    true
                            };


                        await _csr1Products1Repo
                            .CreateAsync(product);
                    }
                }


                // =====================================================
                // SAVE GROUP 2
                // CSRProducts2
                // =====================================================

                if (request.Crop2Id > 0 &&
                    request.Product2Ids != null)
                {
                    foreach (var productId in request.Product2Ids
                                 .Where(x => x > 0)
                                 .Distinct())
                    {
                        var product =
                            new CSR1Products2
                            {
                                CSR1Id =
                                    csr1Id,

                                CropId =
                                    request.Crop2Id,

                                ProductId =
                                    productId,

                                IsActive =
                                    true
                            };


                        await _csr1Products2Repo
                            .CreateAsync(product);
                    }
                }


                // =====================================================
                // SAVE GROUP 3
                // CSRProducts3
                // =====================================================

                if (request.Crop3Id > 0 &&
                    request.Product3Ids != null)
                {
                    foreach (var productId in request.Product3Ids
                                 .Where(x => x > 0)
                                 .Distinct())
                    {
                        var product =
                            new CSR1Products3
                            {
                                CSR1Id =
                                    csr1Id,

                                CropId =
                                    request.Crop3Id,

                                ProductId =
                                    productId,

                                IsActive =
                                    true
                            };


                        await _csr1Products3Repo
                            .CreateAsync(product);
                    }
                }


                // =====================================================
                // RESPONSE
                // =====================================================

                return Ok(new
                {
                    message =
                        "CSR1 submitted successfully for validation",

                    data = new
                    {
                        CSR1Id =
                            created.Id,

                        Crop1Id =
                            request.Crop1Id,

                        Product1Count =
                            request.Product1Ids?
                                .Where(x => x > 0)
                                .Distinct()
                                .Count() ?? 0,

                        Crop2Id =
                            request.Crop2Id,

                        Product2Count =
                            request.Product2Ids?
                                .Where(x => x > 0)
                                .Distinct()
                                .Count() ?? 0,

                        Crop3Id =
                            request.Crop3Id,

                        Product3Count =
                            request.Product3Ids?
                                .Where(x => x > 0)
                                .Distinct()
                                .Count() ?? 0
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message =
                            "Error while saving CSR1",

                        error =
                            ex.Message
                    });
            }
        }


        // ============================================================
        // GET ALL ACTIVE CSR1
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                await NormalizeCSR1UserIdsAsync();

                var items = await _csr1Repo
                    .GetAll()
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message =
                            "Error while loading CSR1 records",

                        error =
                            ex.Message
                    });
            }
        }


        // ============================================================
        // GET ALL INCLUDING INACTIVE
        // ============================================================

        [HttpGet("all")]
        public async Task<IActionResult> GetAllWithInactive()
        {
            try
            {
                await NormalizeCSR1UserIdsAsync();

                var items = await _csr1Repo
                    .GetAllWithInactive()
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message =
                            "Error while loading CSR1 records",

                        error =
                            ex.Message
                    });
            }
        }


        // ============================================================
        // GET BY ID
        // ============================================================

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                await NormalizeCSR1UserIdsAsync();

                var item =
                    await _csr1Repo.GetByIdAsync(id);


                if (item == null)
                {
                    return NotFound(new
                    {
                        message =
                            "CSR1 record not found"
                    });
                }


                var product1 =
                    await _csr1Products1Repo
                        .GetWhere(x =>
                            x.CSR1Id == id &&
                            x.IsActive)
                        .ToListAsync();


                var product2 =
                    await _csr1Products2Repo
                        .GetWhere(x =>
                            x.CSR1Id == id &&
                            x.IsActive)
                        .ToListAsync();


                var product3 =
                    await _csr1Products3Repo
                        .GetWhere(x =>
                            x.CSR1Id == id &&
                            x.IsActive)
                        .ToListAsync();


                return Ok(new
                {
                    csr1 =
                        item,

                    product1Ids =
                        product1.Select(x => x.ProductId)
                            .ToList(),

                    product2Ids =
                        product2.Select(x => x.ProductId)
                            .ToList(),

                    product3Ids =
                        product3.Select(x => x.ProductId)
                            .ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message =
                            "Error while loading CSR1",

                        error =
                            ex.Message
                    });
            }
        }


        // ============================================================
        // UPDATE CSR1 + PRODUCTS
        // PUT: api/CSR1/5
        // ============================================================

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
     int id,
     [FromBody] CSR1SaveRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                if (request == null || request.CSR1 == null)
                {
                    return BadRequest(new
                    {
                        message =
                            "Invalid CSR1 data."
                    });
                }


                var entity =
                    request.CSR1;


                var existing =
                    await _csr1Repo.GetByIdAsync(id);


                if (existing == null)
                {
                    return NotFound(new
                    {
                        message =
                            "CSR1 record not found"
                    });
                }


                // =====================================================
                // VALIDATE PROGRAM
                // =====================================================

                if (entity.ProgramId <= 0)
                {
                    return BadRequest(new
                    {
                        message =
                            "ProgramId is required."
                    });
                }


                var programMaster =
                    await _programMasterRepo
                        .GetWhere(x =>
                            x.Id == entity.ProgramId)
                        .FirstOrDefaultAsync();


                if (programMaster == null)
                {
                    return BadRequest(new
                    {
                        message =
                            "Selected Program not found."
                    });
                }


                entity.ProgramTypeId =
                    programMaster.ProgramTypeId;


                // =====================================================
                // UPDATE AUDIT
                // =====================================================

                entity.Id =
                    id;

                var currentUserId = CurrentUserId;
                var resolvedUserId = (!string.IsNullOrWhiteSpace(currentUserId) && currentUserId != "System" && currentUserId.Contains("-"))
                    ? currentUserId
                    : (!string.IsNullOrWhiteSpace(entity.UpdatedBy) && entity.UpdatedBy.Contains("-") ? entity.UpdatedBy : currentUserId);

                // Preserve CreatedBy, but heal if existing record saved username instead of ID
                var createdBy = existing.CreatedBy;
                if ((string.IsNullOrWhiteSpace(createdBy) || !createdBy.Contains("-")) 
                    && !string.IsNullOrWhiteSpace(entity.CreatedBy) && entity.CreatedBy.Contains("-"))
                {
                    createdBy = entity.CreatedBy;
                }

                entity.CreatedBy =
                    createdBy;

                entity.CreatedAt =
                    existing.CreatedAt;

                entity.UpdatedBy =
                    resolvedUserId;

                entity.UpdatedAt =
                    DateTime.Now;

                entity.IsActive =
                    existing.IsActive;


                // =====================================================
                // UPDATE CSR1
                // =====================================================

                var updated =
                    await _csr1Repo.PatchAsync(
                        id,
                        entity);


                if (updated == null)
                {
                    return NotFound(new
                    {
                        message =
                            "CSR1 record not found."
                    });
                }


                // =====================================================
                // DELETE OLD GROUP 1
                // =====================================================

                var existingProducts1 =
                    await _csr1Products1Repo
                        .GetWhere(x =>
                            x.CSR1Id == id)
                        .ToListAsync();


                foreach (var item in existingProducts1)
                {
                    await _csr1Products1Repo
                        .DeleteAsync(item.Id);
                }


                // =====================================================
                // DELETE OLD GROUP 2
                // =====================================================

                var existingProducts2 =
                    await _csr1Products2Repo
                        .GetWhere(x =>
                            x.CSR1Id == id)
                        .ToListAsync();


                foreach (var item in existingProducts2)
                {
                    await _csr1Products2Repo
                        .DeleteAsync(item.Id);
                }


                // =====================================================
                // DELETE OLD GROUP 3
                // =====================================================

                var existingProducts3 =
                    await _csr1Products3Repo
                        .GetWhere(x =>
                            x.CSR1Id == id)
                        .ToListAsync();


                foreach (var item in existingProducts3)
                {
                    await _csr1Products3Repo
                        .DeleteAsync(item.Id);
                }


                // =====================================================
                // RECREATE GROUP 1
                // =====================================================

                if (request.Crop1Id > 0 &&
                    request.Product1Ids != null)
                {
                    foreach (var productId in request.Product1Ids
                                 .Where(x => x > 0)
                                 .Distinct())
                    {
                        await _csr1Products1Repo
                            .CreateAsync(
                                new CSR1Products1
                                {
                                    CSR1Id =
                                        id,

                                    CropId =
                                        request.Crop1Id,

                                    ProductId =
                                        productId,

                                    IsActive =
                                        true
                                });
                    }
                }


                // =====================================================
                // RECREATE GROUP 2
                // =====================================================

                if (request.Crop2Id > 0 &&
                    request.Product2Ids != null)
                {
                    foreach (var productId in request.Product2Ids
                                 .Where(x => x > 0)
                                 .Distinct())
                    {
                        await _csr1Products2Repo
                            .CreateAsync(
                                new CSR1Products2
                                {
                                    CSR1Id =
                                        id,

                                    CropId =
                                        request.Crop2Id,

                                    ProductId =
                                        productId,

                                    IsActive =
                                        true
                                });
                    }
                }


                // =====================================================
                // RECREATE GROUP 3
                // =====================================================

                if (request.Crop3Id > 0 &&
                    request.Product3Ids != null)
                {
                    foreach (var productId in request.Product3Ids
                                 .Where(x => x > 0)
                                 .Distinct())
                    {
                        await _csr1Products3Repo
                            .CreateAsync(
                                new CSR1Products3
                                {
                                    CSR1Id =
                                        id,

                                    CropId =
                                        request.Crop3Id,

                                    ProductId =
                                        productId,

                                    IsActive =
                                        true
                                });
                    }
                }


                return Ok(new
                {
                    message =
                        "CSR1 updated successfully",

                    data =
                        updated
                });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message =
                            "Error while updating CSR1",

                        error =
                            ex.Message
                    });
            }
        }

        // ============================================================
        // DELETE
        // ============================================================

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                // Delete product group 1
                var products1 =
                    await _csr1Products1Repo
                        .GetWhere(x =>
                            x.CSR1Id == id)
                        .ToListAsync();

                foreach (var item in products1)
                {
                    await _csr1Products1Repo
                        .DeleteAsync(item.Id);
                }


                // Delete product group 2
                var products2 =
                    await _csr1Products2Repo
                        .GetWhere(x =>
                            x.CSR1Id == id)
                        .ToListAsync();

                foreach (var item in products2)
                {
                    await _csr1Products2Repo
                        .DeleteAsync(item.Id);
                }


                // Delete product group 3
                var products3 =
                    await _csr1Products3Repo
                        .GetWhere(x =>
                            x.CSR1Id == id)
                        .ToListAsync();

                foreach (var item in products3)
                {
                    await _csr1Products3Repo
                        .DeleteAsync(item.Id);
                }


                var deleted =
                    await _csr1Repo.DeleteAsync(id);


                if (!deleted)
                {
                    return NotFound(new
                    {
                        message =
                            "CSR1 record not found"
                    });
                }


                return Ok(new
                {
                    message =
                        "CSR1 deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message =
                            "Error while deleting CSR1",

                        error =
                            ex.Message
                    });
            }
        }


        [HttpGet("list")]
        public async Task<IActionResult> GetCSR1List([FromQuery] bool mine = false)
        {
            try
            {
                await NormalizeCSR1UserIdsAsync();

                var csr1Records = await _csr1Repo
                    .GetAll()
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();

                // mine=true: a JMDO only sees the CSR1 programs they're personally the
                // ResponsiblePersonId on (Tasks Allocation Step 4 / its MDO review counterpart) -
                // these are assigned directly on the CSR1 record itself, so there is nothing for
                // the JMDO or the reviewing MDO to separately "pick"; every other caller (CSR-1
                // Management, etc.) keeps seeing the full list via the default mine=false.
                if (mine)
                {
                    var myEmployeeId = await _db.Employeelogins
                        .AsNoTracking()
                        .Where(e => e.UserId == CurrentUserId)
                        .Select(e => (int?)e.EmployeeInformationID)
                        .FirstOrDefaultAsync();

                    csr1Records = myEmployeeId is > 0
                        ? csr1Records.Where(c => c.ResponsiblePersonId == myEmployeeId).ToList()
                        : new List<CSR1>();
                }

                var programMasters = await _programMasterRepo
                    .GetAll()
                    .ToListAsync();

                var programTypes = await _programTypeRepo
                    .GetAll()
                    .ToListAsync();

                var products1 = await _csr1Products1Repo
                    .GetAll()
                    .ToListAsync();

                var products2 = await _csr1Products2Repo
                    .GetAll()
                    .ToListAsync();

                var products3 = await _csr1Products3Repo
                    .GetAll()
                    .ToListAsync();

                var employeeLogins = await _db.Employeelogins.AsNoTracking().ToListAsync();
                var users = await _db.Users.AsNoTracking().ToListAsync();

                var result = csr1Records
                    .Select(csr =>
                    {
                        var program = programMasters
                            .FirstOrDefault(x => x.Id == csr.ProgramId);

                        var programType = programTypes
                            .FirstOrDefault(x => x.Id == csr.ProgramTypeId);

                        var empLogin = employeeLogins.FirstOrDefault(x => x.EmployeeInformationID == csr.ResponsiblePersonId);
                        var resolvedHqId = (csr.HeadquarterId ?? 0) > 0 ? csr.HeadquarterId : empLogin?.HeadquartersId;

                        var creator = !string.IsNullOrEmpty(csr.CreatedBy)
                            ? users.FirstOrDefault(u => u.Id == csr.CreatedBy)
                            : null;
                        var creatorRole = creator?.Role.ToString();
                        var scope = ResolveCSR1Scope(creatorRole, csr.HeadquarterId);
                        var createdByName = creator?.Name ?? creator?.UserName ?? creator?.Email ?? csr.CreatedBy;

                        return new CSR1ListDto
                        {
                            Id = csr.Id,
                            SanctionNo = csr.SanctionNo,
                            ProgramTypeId = csr.ProgramTypeId,
                            ProgramType = programType?.Name ?? "",

                            ProgramId = csr.ProgramId,
                            ProgramName = program?.Name ?? "",

                            NumberOfPrograms = csr.NumberOfPrograms,
                            Budget = csr.Budget,

                            HeadquarterId = resolvedHqId,
                            LocationId = csr.LocationId,
                            ResponsiblePersonId = csr.ResponsiblePersonId,

                            Status = csr.Status,
                            Scope = scope,
                            CreatorRole = creatorRole,
                            CreatedBy = createdByName,
                            CreatedById = csr.CreatedBy,
                            Remarks = csr.Remarks,
                            CreatedAt = csr.CreatedAt,
                            IsActive = csr.IsActive,
                            CSR1Date = csr.CSR1Date,
                            Products1 = products1
                                .Where(x => x.CSR1Id == csr.Id)
                                .Select(x => new CSR1ProductDto
                                {
                                    Id = x.Id,
                                    CropId = x.CropId,
                                    ProductId = x.ProductId
                                })
                                .ToList(),

                            Products2 = products2
                                .Where(x => x.CSR1Id == csr.Id)
                                .Select(x => new CSR1ProductDto
                                {
                                    Id = x.Id,
                                    CropId = x.CropId,
                                    ProductId = x.ProductId
                                })
                                .ToList(),

                            Products3 = products3
                                .Where(x => x.CSR1Id == csr.Id)
                                .Select(x => new CSR1ProductDto
                                {
                                    Id = x.Id,
                                    CropId = x.CropId,
                                    ProductId = x.ProductId
                                })
                                .ToList()
                        };
                    })
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = "Error while loading CSR1 list",
                        error = ex.Message
                    });
            }
        }


        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetCSR1Details(int id)
        {
            try
            {
                await NormalizeCSR1UserIdsAsync();

                // =====================================================
                // CSR1
                // =====================================================

                var csr1 =
                    await _csr1Repo.GetByIdAsync(id);

                if (csr1 == null)
                {
                    return NotFound(new
                    {
                        message = "CSR1 record not found."
                    });
                }


                // =====================================================
                // PROGRAM
                // =====================================================

                var program =
                    await _programMasterRepo
                        .GetWhere(x => x.Id == csr1.ProgramId)
                        .FirstOrDefaultAsync();


                var programType =
                    await _programTypeRepo
                        .GetWhere(x => x.Id == csr1.ProgramTypeId)
                        .FirstOrDefaultAsync();


                // =====================================================
                // LOCATION / EMPLOYEE
                // =====================================================

                var headquarter =
                    csr1.HeadquarterId.HasValue
                        ? await _headquarterRepo
                            .GetWhere(x =>
                                x.Id == csr1.HeadquarterId.Value)
                            .FirstOrDefaultAsync()
                        : null;


                var zone =
                    csr1.LocationId.HasValue
                        ? await _zoneRepo
                            .GetWhere(x =>
                                x.Id == csr1.LocationId.Value)
                            .FirstOrDefaultAsync()
                        : null;


                var employee =
                    csr1.ResponsiblePersonId.HasValue
                        ? await _employeeRepo
                            .GetWhere(x =>
                                x.Id == csr1.ResponsiblePersonId.Value)
                            .FirstOrDefaultAsync()
                        : null;


                // =====================================================
                // MASTER DATA
                // =====================================================

                var crops =
                    await _cropRepo
                        .GetAll()
                        .ToListAsync();


                var products =
                    await _productRepo
                        .GetAll()
                        .ToListAsync();


                // =====================================================
                // PRODUCT TABLE 1
                // =====================================================

                var group1 =
                    await _csr1Products1Repo
                        .GetWhere(x =>
                            x.CSR1Id == id &&
                            x.IsActive)
                        .ToListAsync();


                // =====================================================
                // PRODUCT TABLE 2
                // =====================================================

                var group2 =
                    await _csr1Products2Repo
                        .GetWhere(x =>
                            x.CSR1Id == id &&
                            x.IsActive)
                        .ToListAsync();


                // =====================================================
                // PRODUCT TABLE 3
                // =====================================================

                var group3 =
                    await _csr1Products3Repo
                        .GetWhere(x =>
                            x.CSR1Id == id &&
                            x.IsActive)
                        .ToListAsync();


                // =====================================================
                // RESPONSE
                // =====================================================

                var createdByUser = !string.IsNullOrEmpty(csr1.CreatedBy)
                    ? await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == csr1.CreatedBy)
                    : null;
                var createdByName = createdByUser?.Name ?? createdByUser?.UserName ?? csr1.CreatedBy;

                var updatedByUser = !string.IsNullOrEmpty(csr1.UpdatedBy)
                    ? await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == csr1.UpdatedBy)
                    : null;
                var updatedByName = updatedByUser?.Name ?? updatedByUser?.UserName ?? csr1.UpdatedBy;

                var (scope, creatorRole) = await ResolveCSR1ScopeAsync(csr1);

                var result =
                    new CSR1DetailsDto
                    {
                        Id =
                            csr1.Id,

                        ProgramTypeId =
                            csr1.ProgramTypeId,

                        ProgramType =
                            programType?.Name ?? "-",

                        ProgramId =
                            csr1.ProgramId,

                        ProgramName =
                            program?.Name ?? "-",

                        NumberOfPrograms =
                            csr1.NumberOfPrograms,

                        Budget =
                            csr1.Budget,


                        HeadquarterId =
                            csr1.HeadquarterId,

                        HeadquarterName =
                            headquarter?.HeadquarterName ?? "-",


                        LocationId =
                            csr1.LocationId,

                        LocationName =
                            zone?.ZoneName ?? "-",


                        ResponsiblePersonId =
                            csr1.ResponsiblePersonId,

                        ResponsiblePersonName =
                            employee?.Name ?? "-",


                        PrintingAndStationery =
                            csr1.PrintingAndStationery,

                        PublicityMaterial =
                            csr1.PublicityMaterial,

                        StageArrangements =
                            csr1.StageArrangements,

                        ServiceChargesLCA =
                            csr1.ServiceChargesLCA,

                        TransportRent =
                            csr1.TransportRent,

                        JeepRunningExpenses =
                            csr1.JeepRunningExpenses,

                        Refreshments =
                            csr1.Refreshments,

                        Inputs =
                            csr1.Inputs,

                        Photography =
                            csr1.Photography,

                        Compliments =
                            csr1.Compliments,

                        Others =
                            csr1.Others,


                        Status =
                            csr1.Status,

                        Scope =
                            scope,

                        CreatorRole =
                            creatorRole,

                        Remarks =
                            csr1.Remarks,

                        CreatedBy =
                            createdByName,

                        CreatedById =
                            csr1.CreatedBy,

                        CreatedAt =
                            csr1.CreatedAt,

                        UpdatedBy =
                            updatedByName,

                        UpdatedById =
                            csr1.UpdatedBy,

                        UpdatedAt =
                            csr1.UpdatedAt,
                        CSR1Date = csr1.CSR1Date,
                        SanctionNo = csr1.SanctionNo,


                        Focus1 =
                            group1
                                .Select(x =>
                                    new CSR1FocusDetailsDto
                                    {
                                        CropId =
                                            x.CropId,

                                        CropName =
                                            crops.FirstOrDefault(c =>
                                                c.Id == x.CropId)?.Name ?? "-",

                                        ProductId =
                                            x.ProductId,

                                        ProductName =
                                            products.FirstOrDefault(p =>
                                                p.Id == x.ProductId)?.Name ?? "-"
                                    })
                                .ToList(),


                        Focus2 =
                            group2
                                .Select(x =>
                                    new CSR1FocusDetailsDto
                                    {
                                        CropId =
                                            x.CropId,

                                        CropName =
                                            crops.FirstOrDefault(c =>
                                                c.Id == x.CropId)?.Name ?? "-",

                                        ProductId =
                                            x.ProductId,

                                        ProductName =
                                            products.FirstOrDefault(p =>
                                                p.Id == x.ProductId)?.Name ?? "-"
                                    })
                                .ToList(),


                        Focus3 =
                            group3
                                .Select(x =>
                                    new CSR1FocusDetailsDto
                                    {
                                        CropId =
                                            x.CropId,

                                        CropName =
                                            crops.FirstOrDefault(c =>
                                                c.Id == x.CropId)?.Name ?? "-",

                                        ProductId =
                                            x.ProductId,

                                        ProductName =
                                            products.FirstOrDefault(p =>
                                                p.Id == x.ProductId)?.Name ?? "-"
                                    })
                                .ToList()
                    };


                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message =
                            "Error while loading CSR1 details.",

                        error =
                            ex.Message
                    });
            }
        }

        // ============================================================
        // VALIDATE CSR1
        // POST / PUT: api/CSR1/{id}/validate
        // HQ Scope: MO validates (MDO applies, MO validates, RM approves)
        // State Scope: SMDO (SMD) validates (SMDO applies, SMDO validates, SMM approves)
        // ============================================================

        [HttpPost("{id:int}/validate")]
        [HttpPut("{id:int}/validate")]
        public async Task<IActionResult> ValidateCSR1(int id)
        {
            try
            {
                var csr1 = await _csr1Repo.GetByIdAsync(id);
                if (csr1 == null)
                {
                    return NotFound(new { message = $"CSR-1 #{id} not found." });
                }

                if (string.Equals(csr1.Status, "Validated", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "CSR-1 is already validated." });
                }

                if (string.Equals(csr1.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "CSR-1 is already approved." });
                }

                var currentRole = User.FindFirst(ClaimTypes.Role)?.Value?.Trim() ?? "";
                bool isAdmin = currentRole == AppRole.Admin.ToString() ||
                               currentRole == AppRole.CorporateAdmin.ToString() ||
                               currentRole == AppRole.Director.ToString() ||
                               currentRole == AppRole.AVP.ToString();

                var (scope, creatorRole) = await ResolveCSR1ScopeAsync(csr1);

                if (!isAdmin)
                {
                    if (scope == "HQ")
                    {
                        // HQ scope: only MO can validate (MDO applies, MO validates, RM approves)
                        if (!string.Equals(currentRole, AppRole.MO.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            return StatusCode(StatusCodes.Status403Forbidden, new
                            {
                                message = "Only Marketing Officers (MO) can validate Headquarters-level CSR-1."
                            });
                        }
                    }
                    else // State scope
                    {
                        // State scope: only SMDO (SMD) can validate (SMDO applies, SMDO validates, SMM approves)
                        if (!string.Equals(currentRole, AppRole.SMD.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            return StatusCode(StatusCodes.Status403Forbidden, new
                            {
                                message = "Only State Marketing Development Officers (SMDO) can validate State-level CSR-1."
                            });
                        }
                    }
                }

                csr1.Status = "Validated";
                csr1.UpdatedBy = CurrentUserId;
                csr1.UpdatedAt = DateTime.Now;
                csr1.ValidateBy = CurrentUserId;
                csr1.ValidateAt = DateTime.Now;

                await _csr1Repo.PatchAsync(id, csr1);

                return Ok(new
                {
                    message = "CSR-1 validated successfully.",
                    data = csr1
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error while validating CSR-1",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // APPROVE CSR1
        // POST / PUT: api/CSR1/{id}/approve
        // HQ Scope: RM / RMD approves
        // State Scope: SMM approves
        // ============================================================


        [HttpPost("{id:int}/approve")]
        [HttpPut("{id:int}/approve")]
        public async Task<IActionResult> ApproveCSR1(int id)
        {
            try
            {
                var csr1 = await _csr1Repo.GetByIdAsync(id);

                if (csr1 == null)
                {
                    return NotFound(new
                    {
                        message = $"CSR-1 #{id} not found."
                    });
                }

                if (string.Equals(
                    csr1.Status,
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        message = "CSR-1 is already approved."
                    });
                }

                if (string.Equals(csr1.Status, "Rejected",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(csr1.Status, "Draft",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        message = "This CSR-1 cannot be approved in its current status."
                    });
                }

                // --------------------------------------------------
                // 1. Identify current user's roles.
                // --------------------------------------------------

                var roles = User.FindAll(ClaimTypes.Role)
                    .Select(x => x.Value.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                bool HasRole(params string[] names) =>
                    names.Any(roles.Contains);

                bool isAdmin = HasRole(
                    AppRole.Admin.ToString(),
                    AppRole.CorporateAdmin.ToString(),
                    AppRole.Director.ToString(),
                    AppRole.AVP.ToString());

                // --------------------------------------------------
                // 2. Determine workflow using the original
                //    application submission date.
                //
                //    IMPORTANT: CreatedAt is used here only because
                //    the supplied entity has no SubmittedAt.
                //    Replace it with the actual submission timestamp
                //    if drafts can be created before submission.
                // --------------------------------------------------

                var applicationDateIst = csr1.CreatedAt;

                // Assumes CreatedAt was stored in India local time.
                // Do not convert this value again unless your DB
                // stores UTC timestamps.

                bool requiresThreeLevelApproval =
                    applicationDateIst.Day >= 27;

                // --------------------------------------------------
                // 3. Resolve existing application scope.
                // --------------------------------------------------

                var (scope, creatorRole) =
                    await ResolveCSR1ScopeAsync(csr1);

                bool isHQScope = string.Equals(
                    scope,
                    "HQ",
                    StringComparison.OrdinalIgnoreCase);

                bool isStateScope = string.Equals(
                    scope,
                    "State",
                    StringComparison.OrdinalIgnoreCase);

                if (!isHQScope && !isStateScope)
                {
                    return BadRequest(new
                    {
                        message = "Unable to determine CSR-1 approval scope."
                    });
                }

                // --------------------------------------------------
                // 4. Existing single-level approval (Day 1-26).
                //    Preserve current approval behaviour.
                // --------------------------------------------------

                if (!requiresThreeLevelApproval)
                {
                    if (!isAdmin &&
                        !string.Equals(
                            csr1.Status,
                            "Validated",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return BadRequest(new
                        {
                            message = "CSR-1 must be validated before approval."
                        });
                    }

                    if (!isAdmin)
                    {
                        if (isHQScope &&
                            !HasRole(
                                AppRole.RM.ToString(),
                                AppRole.RMD.ToString()))
                        {
                            return StatusCode(403, new
                            {
                                message = "Only RM/RMD can approve HQ-level CSR-1."
                            });
                        }

                        if (isStateScope &&
                            !HasRole(AppRole.SMM.ToString()))
                        {
                            return StatusCode(403, new
                            {
                                message = "Only SMM can approve State-level CSR-1."
                            });
                        }
                    }

                    if (string.IsNullOrWhiteSpace(csr1.SanctionNo))
                    {
                        csr1.SanctionNo =
                            await GenerateSanctionForCSR1Async(
                                csr1,
                                scope);
                    }

                    csr1.Status = "Approved";
                    csr1.ApprovedBy = CurrentUserId;
                    csr1.ApprovedAt = DateTime.UtcNow;

                    csr1.UpdatedBy = CurrentUserId;
                    csr1.UpdatedAt = DateTime.UtcNow;

                    await _csr1Repo.PatchAsync(id, csr1);

                    return Ok(new
                    {
                        message = "CSR-1 approved successfully.",
                        approvalLevel = 1,
                        isFinalApproval = true,
                        sanctionNo = csr1.SanctionNo,
                        data = csr1
                    });
                }

                // --------------------------------------------------
                // 5. Three-level approval (Day 27 onwards).
                //    Validation remains mandatory.
                // --------------------------------------------------

                if (!string.Equals(
                    csr1.Status,
                    "Validated",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        message = "CSR-1 must be validated before approval."
                    });
                }

                var now = DateTime.UtcNow;

                bool level1Completed =
                    !string.IsNullOrWhiteSpace(csr1.Level1ApprovedBy) &&
                    csr1.Level1ApprovedAt.HasValue;

                bool level2Completed =
                    !string.IsNullOrWhiteSpace(csr1.Level2ApprovedBy) &&
                    csr1.Level2ApprovedAt.HasValue;

                bool level3Completed =
                    !string.IsNullOrWhiteSpace(csr1.Level3ApprovedBy) &&
                    csr1.Level3ApprovedAt.HasValue;

                if (level3Completed)
                {
                    return BadRequest(new
                    {
                        message = "All three approval levels are already completed."
                    });
                }

                // --------------------------------------------------
                // LEVEL 1: SMM / RMM
                // --------------------------------------------------

                if (!level1Completed)
                {
                    if (level2Completed || level3Completed)
                    {
                        return BadRequest(new
                        {
                            message = "Invalid CSR-1 approval sequence."
                        });
                    }

                    if (isHQScope)
                    {
                        // RMM is the new designation; RM/RMD
                        // are retained for existing role compatibility.
                        if (!HasRole("RMM",
                            AppRole.RM.ToString(),
                            AppRole.RMD.ToString()))
                        {
                            return StatusCode(403, new
                            {
                                message = "Only RMM can perform Level 1 approval for HQ scope."
                            });
                        }
                    }
                    else
                    {
                        if (!HasRole(AppRole.SMM.ToString()))
                        {
                            return StatusCode(403, new
                            {
                                message = "Only SMM can perform Level 1 approval for State scope."
                            });
                        }
                    }

                    csr1.Level1ApprovedBy = CurrentUserId;
                    csr1.Level1ApprovedAt = now;

                    // Do not mark overall approved.
                    // Do not generate sanction number.
                    csr1.UpdatedBy = CurrentUserId;
                    csr1.UpdatedAt = now;

                    await _csr1Repo.PatchAsync(id, csr1);

                    return Ok(new
                    {
                        message = "Level 1 approved successfully. Forwarded to MDAdmin.",
                        approvalLevel = 1,
                        nextApprover = "MDAdmin",
                        isFinalApproval = false,
                        sanctionNo = (string?)null,
                        data = csr1
                    });
                }

                // --------------------------------------------------
                // LEVEL 2: MDAdmin
                // --------------------------------------------------

                if (!level2Completed)
                {
                    if (level3Completed)
                    {
                        return BadRequest(new
                        {
                            message = "Invalid CSR-1 approval sequence."
                        });
                    }

                    if (!HasRole("MDAdmin"))
                    {
                        return StatusCode(403, new
                        {
                            message = "Only MDAdmin can perform Level 2 approval."
                        });
                    }

                    csr1.Level2ApprovedBy = CurrentUserId;
                    csr1.Level2ApprovedAt = now;

                    // Still not finally approved.
                    // No sanction number.
                    csr1.UpdatedBy = CurrentUserId;
                    csr1.UpdatedAt = now;

                    await _csr1Repo.PatchAsync(id, csr1);

                    return Ok(new
                    {
                        message = "Level 2 approved successfully. Forwarded to CSMD-HOD.",
                        approvalLevel = 2,
                        nextApprover = "CSMD-HOD",
                        isFinalApproval = false,
                        sanctionNo = (string?)null,
                        data = csr1
                    });
                }

                // --------------------------------------------------
                // LEVEL 3: CSMD-HOD
                // Final approval and sanction number.
                // --------------------------------------------------

                if (!HasRole("CSMD-HOD"))
                {
                    return StatusCode(403, new
                    {
                        message = "Only CSMD-HOD can perform Level 3 approval."
                    });
                }

                // Both prior approvals must be complete.
                if (!level1Completed || !level2Completed)
                {
                    return BadRequest(new
                    {
                        message = "Level 1 and Level 2 approvals must be completed first."
                    });
                }

                if (string.IsNullOrWhiteSpace(csr1.SanctionNo))
                {
                    csr1.SanctionNo =
                        await GenerateSanctionForCSR1Async(
                            csr1,
                            scope);
                }

                csr1.Level3ApprovedBy = CurrentUserId;
                csr1.Level3ApprovedAt = now;

                // Overall final approval.
                csr1.ApprovedBy = CurrentUserId;
                csr1.ApprovedAt = now;
                csr1.Status = "Approved";

                csr1.UpdatedBy = CurrentUserId;
                csr1.UpdatedAt = now;

                await _csr1Repo.PatchAsync(id, csr1);

                return Ok(new
                {
                    message = "Level 3 approved successfully. Sanction number generated.",
                    approvalLevel = 3,
                    isFinalApproval = true,
                    sanctionNo = csr1.SanctionNo,
                    data = csr1
                });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = "Error while approving CSR-1",
                        error = ex.Message
                    });
            }
        }

        private async Task<(int StateId, int? RegionId)>
            ResolveCSR1SanctionLocationAsync(CSR1 csr1)
        {
            if (csr1 == null)
                throw new ArgumentNullException(nameof(csr1));

            if (!csr1.HeadquarterId.HasValue ||
                csr1.HeadquarterId.Value <= 0)
            {
                throw new InvalidOperationException(
                    $"CSR-1 #{csr1.Id} has no HeadquarterId. " +
                    "State-level applications without a headquarters " +
                    "require a separate state lookup.");
            }

            // Existing Headquarter repository
            var headquarter = await _headquarterRepo
                .GetWhere(x =>
                    x.Id == csr1.HeadquarterId.Value)
                .Include(x => x.Region)
                .FirstOrDefaultAsync();

            if (headquarter == null)
            {
                throw new InvalidOperationException(
                    $"Headquarter #{csr1.HeadquarterId.Value} not found.");
            }

            if (headquarter.RegionId <= 0)
            {
                throw new InvalidOperationException(
                    "Headquarter has no valid RegionId.");
            }

            if (headquarter.Region == null ||
                headquarter.Region.StateId <= 0)
            {
                throw new InvalidOperationException(
                    "Region or StateId could not be resolved.");
            }

            return (
                headquarter.Region.StateId,
                headquarter.RegionId
            );
        }


        private async Task<string> GenerateSanctionForCSR1Async(
            CSR1 csr1,
            string scope)
        {
            // Resolve StateId and RegionId from the actual
            // application hierarchy before generating.
            var (stateId, regionId) =
                await ResolveCSR1SanctionLocationAsync(csr1);

            bool isRegionalApproval = string.Equals(
                scope,
                "HQ",
                StringComparison.OrdinalIgnoreCase);

            if (stateId <= 0)
                throw new InvalidOperationException(
                    "Unable to determine CSR-1 State.");

            if (isRegionalApproval &&
                (!regionId.HasValue || regionId.Value <= 0))
                throw new InvalidOperationException(
                    "Unable to determine CSR-1 Region.");

            return await GenerateCSR1SanctionNoAsync(
                stateId,
                regionId,
                isRegionalApproval,
                csr1.CSR1Date);
        }


        private async Task<string> GenerateCSR1SanctionNoAsync(
            int stateId,
            int? regionId,
            bool isRegionalApproval,
            DateTime programDate)
        {
            if (stateId <= 0)
            {
                throw new InvalidOperationException(
                    "Valid StateId is required.");
            }

            // --------------------------------------------
            // FINANCIAL YEAR
            // Example: 2026-2027 => FY27
            // --------------------------------------------

            int fyEndYear = programDate.Month >= 4
                ? programDate.Year + 1
                : programDate.Year;

            string financialYear = $"FY{fyEndYear % 100:D2}";

            // Month: 01, 02 ... 12
            string month = programDate.ToString("MM");

            // --------------------------------------------
            // REGION SANCTION NUMBER
            // FY27/TN/MDUI/11/0546
            // --------------------------------------------

            if (isRegionalApproval)
            {
                if (!regionId.HasValue || regionId.Value <= 0)
                {
                    throw new InvalidOperationException(
                        "RegionId is required for regional sanction.");
                }

                var sequence = await _db
                    .SanctionNumberSequences
                    .FirstOrDefaultAsync(x =>
                        x.StateId == stateId &&
                        x.RegionId == regionId.Value);

                if (sequence == null)
                {
                    throw new InvalidOperationException(
                        "Regional sanction number sequence not found.");
                }

                if (string.IsNullOrWhiteSpace(sequence.StateSAPCode) ||
                    string.IsNullOrWhiteSpace(sequence.RegionSAPCode))
                {
                    throw new InvalidOperationException(
                        "State or Region SAP Code is missing.");
                }

                // Stored number assumed to be LAST issued.
                int nextNumber = checked(sequence.SanctionNumber + 1);

                string sanctionNo =
                    $"{financialYear}/" +
                    $"{sequence.StateSAPCode.Trim()}/" +
                    $"{sequence.RegionSAPCode.Trim()}/" +
                    $"{month}/" +
                    $"{nextNumber:D4}";

                sequence.SanctionNumber = nextNumber;
                sequence.UpdatedBy = CurrentUserId;
                sequence.UpdatedAt = DateTime.UtcNow;

                // SaveChanges is handled by the caller's
                // approval transaction.
                return sanctionNo;
            }

            // --------------------------------------------
            // STATE SANCTION NUMBER
            // FY27/TN/11/0005
            // --------------------------------------------

            var stateSequence = await _db
                .SanctionNumberSequences
                .FirstOrDefaultAsync(x =>
                    x.StateId == stateId &&
                    x.RegionId == null);

            if (stateSequence == null)
            {
                throw new InvalidOperationException(
                    "State sanction number sequence not found.");
            }

            if (string.IsNullOrWhiteSpace(stateSequence.StateSAPCode))
            {
                throw new InvalidOperationException(
                    "State SAP Code is missing.");
            }

            // Stored number assumed to be LAST issued.
            int nextStateNumber =
                checked(stateSequence.SanctionNumberState + 1);

            string stateSanctionNo =
                $"{financialYear}/" +
                $"{stateSequence.StateSAPCode.Trim()}/" +
                $"{month}/" +
                $"{nextStateNumber:D4}";

            stateSequence.SanctionNumberState = nextStateNumber;
            stateSequence.UpdatedBy = CurrentUserId;
            stateSequence.UpdatedAt = DateTime.UtcNow;

            return stateSanctionNo;
        }

        // ============================================================
        // REJECT CSR1
        // POST / PUT: api/CSR1/{id}/reject
        // HQ Scope: MO or RM/RMD can reject with remarks
        // State Scope: SMD or SMM can reject with remarks
        // ============================================================

        [HttpPost("{id:int}/reject")]
        [HttpPut("{id:int}/reject")]
        public async Task<IActionResult> RejectCSR1(int id, [FromBody] CSR1RejectRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Remarks))
                {
                    return BadRequest(new { message = "Rejection reason / remarks is required." });
                }

                var csr1 = await _csr1Repo.GetByIdAsync(id);
                if (csr1 == null)
                {
                    return NotFound(new { message = $"CSR-1 #{id} not found." });
                }

                if (string.Equals(csr1.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "Approved CSR-1 cannot be rejected." });
                }

                var currentRole = User.FindFirst(ClaimTypes.Role)?.Value?.Trim() ?? "";
                bool isAdmin = currentRole == AppRole.Admin.ToString() ||
                               currentRole == AppRole.CorporateAdmin.ToString() ||
                               currentRole == AppRole.Director.ToString() ||
                               currentRole == AppRole.AVP.ToString();

                var (scope, creatorRole) = await ResolveCSR1ScopeAsync(csr1);

                if (!isAdmin)
                {
                    if (scope == "HQ")
                    {
                        bool canRejectHQ = string.Equals(currentRole, AppRole.MO.ToString(), StringComparison.OrdinalIgnoreCase) ||
                                           string.Equals(currentRole, AppRole.RM.ToString(), StringComparison.OrdinalIgnoreCase) ||
                                           string.Equals(currentRole, AppRole.RMD.ToString(), StringComparison.OrdinalIgnoreCase);

                        if (!canRejectHQ)
                        {
                            return StatusCode(StatusCodes.Status403Forbidden, new
                            {
                                message = "You are not authorized to reject this Headquarters-level CSR-1."
                            });
                        }
                    }
                    else // State scope
                    {
                        bool canRejectState = string.Equals(currentRole, AppRole.SMD.ToString(), StringComparison.OrdinalIgnoreCase) ||
                                              string.Equals(currentRole, AppRole.SMM.ToString(), StringComparison.OrdinalIgnoreCase);

                        if (!canRejectState)
                        {
                            return StatusCode(StatusCodes.Status403Forbidden, new
                            {
                                message = "You are not authorized to reject this State-level CSR-1."
                            });
                        }
                    }
                }

                csr1.Status = "Rejected";
                csr1.Remarks = request.Remarks.Trim();
                csr1.UpdatedBy = CurrentUserId;
                csr1.UpdatedAt = DateTime.Now;

                await _csr1Repo.PatchAsync(id, csr1);

                return Ok(new
                {
                    message = "CSR-1 rejected successfully.",
                    data = csr1
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error while rejecting CSR-1",
                    error = ex.Message
                });
            }
        }
    }
}