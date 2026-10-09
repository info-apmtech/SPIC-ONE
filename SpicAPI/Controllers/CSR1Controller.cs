using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using System.Security.Claims;
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
                // AUDIT
                // =====================================================

                var currentUserId = CurrentUserId;
                var resolvedUserId = (!string.IsNullOrWhiteSpace(currentUserId) && currentUserId != "System" && currentUserId.Contains("-"))
                    ? currentUserId
                    : (!string.IsNullOrWhiteSpace(model.CreatedBy) && model.CreatedBy.Contains("-") ? model.CreatedBy : currentUserId);

                var currentDate =
                    DateTime.Now;


                model.CreatedAt =
                    currentDate;

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
        public async Task<IActionResult> GetCSR1List()
        {
            try
            {
                await NormalizeCSR1UserIdsAsync();

                var csr1Records = await _csr1Repo
                    .GetAll()
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();

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

                var result = csr1Records
                    .Select(csr =>
                    {
                        var program = programMasters
                            .FirstOrDefault(x => x.Id == csr.ProgramId);

                        var programType = programTypes
                            .FirstOrDefault(x => x.Id == csr.ProgramTypeId);

                        return new CSR1ListDto
                        {
                            Id = csr.Id,

                            ProgramTypeId = csr.ProgramTypeId,
                            ProgramType = programType?.Name ?? "",

                            ProgramId = csr.ProgramId,
                            ProgramName = program?.Name ?? "",

                            NumberOfPrograms = csr.NumberOfPrograms,
                            Budget = csr.Budget,

                            HeadquarterId = csr.HeadquarterId,
                            LocationId = csr.LocationId,
                            ResponsiblePersonId = csr.ResponsiblePersonId,

                            Status = csr.Status,
                            Remarks = csr.Remarks,
                            CreatedAt = csr.CreatedAt,
                            IsActive = csr.IsActive,

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
    }
}