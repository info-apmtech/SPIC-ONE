using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using SpicAPI.Services;
using System.Security.Claims;
using System.Xml;
using static SPIC.Core.Entities.EmployeeRegistration;
using System.Linq;

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

        // ============================================================
        // EXPENSE INFORMATION MASTER
        // GET: api/CSR1/expense-informations
        // Keeps ExpenseInformation untouched; CSR-1 reads the master directly.
        // ============================================================
        [HttpGet("expense-informations")]
        public async Task<IActionResult> GetExpenseInformations()
        {
            var expenses = await _db.Set<ExpenseInformation>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.IsActive
                })
                .ToListAsync();

            return Ok(expenses);
        }

        private static List<int> CleanIds(IEnumerable<int>? ids) =>
            (ids ?? Enumerable.Empty<int>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();

        private async Task<string?> ResolveResponsiblePersonUserIdAsync(
    string? responsiblePersonId)
        {
            if (string.IsNullOrWhiteSpace(responsiblePersonId))
                return null;

            var value = responsiblePersonId.Trim();

            // If already an Employeelogins.UserId, keep it.
            var existingUserId = await _db.Employeelogins
                .AsNoTracking()
                .Where(x => x.UserId == value)
                .Select(x => x.UserId)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(existingUserId))
                return existingUserId;

            // Old/UI value = EmployeeInformation.Id
            if (!int.TryParse(value, out var employeeInformationId) ||
                employeeInformationId <= 0)
            {
                return null;
            }

            // Convert EmployeeInformation.Id -> Employeelogins.UserId
            return await _db.Employeelogins
                .AsNoTracking()
                .Where(x =>
                    x.EmployeeInformationID == employeeInformationId)
                .Select(x => x.UserId)
                .FirstOrDefaultAsync();
        }
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
                if (string.Equals(creatorRole, "SMDO", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(creatorRole, AppRole.SMD.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(creatorRole, AppRole.SMM.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return "State";
                }

                if (string.Equals(creatorRole, "RMDO", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(creatorRole, AppRole.MDO.ToString(), StringComparison.OrdinalIgnoreCase) ||
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
[FromBody] CSR1SaveRequestWithExpenses request)
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
                // RESOLVE RESPONSIBLE PERSON
                // EmployeeInformation.Id -> Employeelogins.UserId
                // =====================================================

                if (string.IsNullOrWhiteSpace(model.ResponsiblePersonId))
                {
                    return BadRequest(new
                    {
                        message = "Responsible Person is required."
                    });
                }

                var responsiblePersonUserId =
                    await ResolveResponsiblePersonUserIdAsync(
                        model.ResponsiblePersonId);

                if (string.IsNullOrWhiteSpace(responsiblePersonUserId))
                {
                    return BadRequest(new
                    {
                        message =
                            "Selected Responsible Person does not have a valid Employee Login."
                    });
                }

                // Save Employeelogins.UserId, NOT EmployeeInformation.Id
                model.ResponsiblePersonId =
                    responsiblePersonUserId;

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


                if (programMaster.ProgramTypeId <= 0)
                {
                    return BadRequest(new
                    {
                        message =
                            "ProgramTypeId not configured for selected Program."
                    });
                }


                // =====================================================
                // VALIDATE MULTI CROP / PRODUCT GROUPS
                // =====================================================

                var crop1Ids = CleanIds(request.Crop1Ids);
                var crop2Ids = CleanIds(request.Crop2Ids);
                var crop3Ids = CleanIds(request.Crop3Ids);

                var product1Ids = CleanIds(request.Product1Ids);
                var product2Ids = CleanIds(request.Product2Ids);
                var product3Ids = CleanIds(request.Product3Ids);

                if (crop1Ids.Count == 0)
                    return BadRequest(new { message = "Please select at least one Focus Crop 1." });

                if (product1Ids.Count == 0)
                    return BadRequest(new { message = "Please select at least one product for Focus 1." });

                if (crop1Ids.Count > 3 || crop2Ids.Count > 3 || crop3Ids.Count > 3)
                    return BadRequest(new { message = "Maximum 3 crops are allowed per Focus section." });

                if (product1Ids.Count > 3 || product2Ids.Count > 3 || product3Ids.Count > 3)
                    return BadRequest(new { message = "Maximum 3 products are allowed per Focus section." });

                if ((crop2Ids.Count > 0 && product2Ids.Count == 0) ||
                    (crop2Ids.Count == 0 && product2Ids.Count > 0))
                    return BadRequest(new { message = "Focus 2 requires both crop and product selections." });

                if ((crop3Ids.Count > 0 && product3Ids.Count == 0) ||
                    (crop3Ids.Count == 0 && product3Ids.Count > 0))
                    return BadRequest(new { message = "Focus 3 requires both crop and product selections." });


                // =====================================================
                // VALIDATE EXPENSES AGAINST BUDGET
                // =====================================================

                var requestedExpenses = request.Expenses ?? new List<CSR1ExpenseSaveDto>();

                // Negative amounts are invalid. Zero amounts are allowed in the request,
                // but are intentionally not persisted in CSR1Expenses.
                if (requestedExpenses.Any(x => x.ExpenseId <= 0 || x.ExpenseAmount < 0))
                    return BadRequest(new { message = "Invalid expense id or amount." });

                var expenseRows = requestedExpenses
                    .Where(x => x.ExpenseAmount > 0)
                    .ToList();

                if (expenseRows.GroupBy(x => x.ExpenseId).Any(g => g.Count() > 1))
                    return BadRequest(new { message = "Duplicate expense entries are not allowed." });

                var requestedExpenseIds = expenseRows.Select(x => x.ExpenseId).Distinct().ToList();
                var validExpenseIds = await _db.Set<ExpenseInformation>()
                    .AsNoTracking()
                    .Where(x => x.IsActive && requestedExpenseIds.Contains(x.Id))
                    .Select(x => x.Id)
                    .ToListAsync();

                if (requestedExpenseIds.Any(id => !validExpenseIds.Contains(id)))
                    return BadRequest(new { message = "One or more selected expenses are invalid or inactive." });

                var totalExpenses = expenseRows
    .Where(x => x.ExpenseAmount > 0)
    .Sum(x => x.ExpenseAmount);

                totalExpenses = decimal.Round(totalExpenses, 2);
                var budgetAmount = decimal.Round(model.Budget, 2);

                if (budgetAmount != totalExpenses)
                {
                    return BadRequest(new
                    {
                        message =
                            $"Budget amount and Total Expense must be equal. " +
                            $"Budget: ₹{budgetAmount:N2}, Total Expense: ₹{totalExpenses:N2}."
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
                    CSR1Status.SubmittedForValidation;

                // CSR-1 cutoff day comes from the active CSRExceedingDaySettings row.
                // The configured cutoff controls legacy vs three-level workflow.
                var csr1CutoffDay = await GetCSR1CutoffDayAsync();

                if (IsThreeLevelApplication(currentDate, csr1CutoffDay))
                {
                    var callerRoles = User.FindAll(ClaimTypes.Role)
                        .Select(x => x.Value.Trim())
                        .ToArray();

                    if (!callerRoles.Any(Csr1WorkflowRules.CanApplyThreeLevel))
                        return StatusCode(StatusCodes.Status403Forbidden, new
                        {
                            message = $"After the configured CSR-1 cutoff day ({csr1CutoffDay}), only MO, SMDO or RMDO can submit CSR-1."
                        });

                    // CSR1Date is treated as PROGRAM date (not application date).
                    var allowedMonth = AllowedProgramMonth(currentDate, csr1CutoffDay);
                    if (model.CSR1Date == default ||
                        model.CSR1Date.Year != allowedMonth.Year ||
                        model.CSR1Date.Month != allowedMonth.Month)
                        return BadRequest(new
                        {
                            message = $"After the configured CSR-1 cutoff day ({csr1CutoffDay}), only {allowedMonth:MMMM yyyy} programs can be submitted."
                        });

                    // Do not allow incoming CreatedAt to impersonate an old workflow.
                    model.CreatedAt = currentDate;
                    model.CreatedBy = CurrentUserId;
                    model.UpdatedBy = CurrentUserId;

                    // SubmittedForValidation remains mandatory; no automatic approval.
                    // On validation: Validated -> Level1Approved -> Level2Approved -> Approved.
                }


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
                // SAVE CSR1 EXPENSES
                // ExpenseId references ExpenseInformations.Id
                // =====================================================
                if (expenseRows.Count > 0)
                {
                    _db.Set<CSR1Expenses>().AddRange(
                        expenseRows.Select(x => new CSR1Expenses
                        {
                            CSR1Id = csr1Id,
                            ExpenseId = x.ExpenseId,
                            ExpenseAmount = x.ExpenseAmount,
                            IsActive = true
                        }));

                    await _db.SaveChangesAsync();
                }


                // =====================================================
                // SAVE EXPENSE BREAKDOWN - normalized CSR1Expenses rows
                // =====================================================

                _db.Set<CSR1Expenses>().AddRange(
                    SPIC.Core.Entities.CSR1ExpenseMapper.ToExpenseRows(model, csr1Id));
                await _db.SaveChangesAsync();

                // =====================================================
                // SAVE MULTI CROP / PRODUCT GROUPS - NEW DB SHAPE
                // One row per Focus section. CropId/ProductId are PostgreSQL int[].
                // =====================================================

                await _csr1Products1Repo.CreateAsync(new CSR1Products1
                {
                    CSR1Id = csr1Id,
                    CropId = crop1Ids.ToArray(),
                    ProductId = product1Ids.ToArray(),
                    IsActive = true
                });

                if (crop2Ids.Count > 0 && product2Ids.Count > 0)
                {
                    await _csr1Products2Repo.CreateAsync(new CSR1Products2
                    {
                        CSR1Id = csr1Id,
                        CropId = crop2Ids.ToArray(),
                        ProductId = product2Ids.ToArray(),
                        IsActive = true
                    });
                }

                if (crop3Ids.Count > 0 && product3Ids.Count > 0)
                {
                    await _csr1Products3Repo.CreateAsync(new CSR1Products3
                    {
                        CSR1Id = csr1Id,
                        CropId = crop3Ids.ToArray(),
                        ProductId = product3Ids.ToArray(),
                        IsActive = true
                    });
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

                        Crop1Ids = crop1Ids,
                        Product1Count = product1Ids.Count,

                        Crop2Ids = crop2Ids,
                        Product2Count = product2Ids.Count,

                        Crop3Ids = crop3Ids,
                        Product3Count = product3Ids.Count
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

                var expenses = await (
                    from e in _db.Set<CSR1Expenses>().AsNoTracking()
                    join master in _db.Set<ExpenseInformation>().AsNoTracking()
                        on e.ExpenseId equals master.Id
                    where e.CSR1Id == id && e.IsActive && e.ExpenseAmount > 0
                    orderby master.Id
                    select new
                    {
                        ExpenseId = master.Id,
                        ExpenseName = master.Name,
                        e.ExpenseAmount
                    })
                    .ToListAsync();


                return Ok(new
                {
                    csr1 =
                        item,

                    crop1Ids = product1.SelectMany(x => x.CropId ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToList(),
                    product1Ids = product1.SelectMany(x => x.ProductId ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToList(),

                    crop2Ids = product2.SelectMany(x => x.CropId ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToList(),
                    product2Ids = product2.SelectMany(x => x.ProductId ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToList(),

                    crop3Ids = product3.SelectMany(x => x.CropId ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToList(),
                    product3Ids = product3.SelectMany(x => x.ProductId ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToList(),
                    expenses
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
            [FromBody] CSR1SaveRequestWithExpenses request)
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

                var entity = request.CSR1;

                // =====================================================
                // RESOLVE RESPONSIBLE PERSON
                // EmployeeInformation.Id -> Employeelogins.UserId
                // =====================================================

                if (string.IsNullOrWhiteSpace(entity.ResponsiblePersonId))
                {
                    return BadRequest(new
                    {
                        message = "Responsible Person is required."
                    });
                }

                var responsiblePersonUserId =
                    await ResolveResponsiblePersonUserIdAsync(
                        entity.ResponsiblePersonId);

                if (string.IsNullOrWhiteSpace(responsiblePersonUserId))
                {
                    return BadRequest(new
                    {
                        message =
                            "Selected Responsible Person does not have a valid Employee Login."
                    });
                }

                entity.ResponsiblePersonId =
                    responsiblePersonUserId;

                var existing = await _csr1Repo.GetByIdAsync(id);

                if (existing == null)
                {
                    return NotFound(new
                    {
                        message = "CSR1 record not found"
                    });
                }

                // =====================================================
                // THREE-LEVEL CSR1 EDIT RESTRICTION
                // Cutoff day comes from CSRExceedingDaySettings.CSR1Day.
                // =====================================================

                var updateCutoffDay = await GetCSR1CutoffDayAsync();

                if (IsThreeLevelApplication(existing.CreatedAt, updateCutoffDay))
                {
                    if (existing.Status != CSR1Status.Draft)
                    {
                        return BadRequest(new
                        {
                            message = "Submitted three-level CSR-1 cannot be edited through this endpoint."
                        });
                    }
                }

                // =====================================================
                // VALIDATE PROGRAM
                // =====================================================

                if (entity.ProgramId <= 0)
                {
                    return BadRequest(new
                    {
                        message = "ProgramId is required."
                    });
                }

                var programMaster = await _programMasterRepo
                    .GetWhere(x => x.Id == entity.ProgramId)
                    .FirstOrDefaultAsync();

                if (programMaster == null)
                {
                    return BadRequest(new
                    {
                        message = "Selected Program not found."
                    });
                }

                if (programMaster.ProgramTypeId <= 0)
                {
                    return BadRequest(new
                    {
                        message = "ProgramTypeId not configured for selected Program."
                    });
                }

                var requestedUpdateExpenses = request.Expenses ?? new List<CSR1ExpenseSaveDto>();

                if (requestedUpdateExpenses.Any(x => x.ExpenseId <= 0 || x.ExpenseAmount < 0))
                    return BadRequest(new { message = "Invalid expense id or amount." });

                // Persist only positive amounts; zero-value rows are omitted.
                var updateExpenses = requestedUpdateExpenses
                    .Where(x => x.ExpenseAmount > 0)
                    .ToList();

                if (updateExpenses.GroupBy(x => x.ExpenseId).Any(g => g.Count() > 1))
                    return BadRequest(new { message = "Invalid or duplicate expense entries." });

                var updateExpenseIds = updateExpenses.Select(x => x.ExpenseId).Distinct().ToList();
                var validUpdateExpenseIds = await _db.Set<ExpenseInformation>()
                    .AsNoTracking()
                    .Where(x => x.IsActive && updateExpenseIds.Contains(x.Id))
                    .Select(x => x.Id)
                    .ToListAsync();

                if (updateExpenseIds.Any(id => !validUpdateExpenseIds.Contains(id)))
                    return BadRequest(new { message = "One or more selected expenses are invalid or inactive." });

                var updateExpenseTotal = updateExpenses.Sum(x => x.ExpenseAmount);
                if (entity.Budget > 0 && updateExpenseTotal > entity.Budget)
                    return BadRequest(new { message = $"Total Expense Breakdown (₹{updateExpenseTotal:N2}) exceeds the allocated Budget (₹{entity.Budget:N2})." });

                // =====================================================
                // UPDATE AUDIT
                // =====================================================

                entity.Id = id;

                var currentUserId = CurrentUserId;

                var resolvedUserId =
                    (!string.IsNullOrWhiteSpace(currentUserId) &&
                     currentUserId != "System" &&
                     currentUserId.Contains("-"))
                        ? currentUserId
                        : (!string.IsNullOrWhiteSpace(entity.UpdatedBy) &&
                           entity.UpdatedBy.Contains("-")
                            ? entity.UpdatedBy
                            : currentUserId);

                // Preserve CreatedBy, but heal if existing record
                // saved username instead of ID.
                var createdBy = existing.CreatedBy;

                if ((string.IsNullOrWhiteSpace(createdBy) ||
                     !createdBy.Contains("-")) &&
                    !string.IsNullOrWhiteSpace(entity.CreatedBy) &&
                    entity.CreatedBy.Contains("-"))
                {
                    createdBy = entity.CreatedBy;
                }

                entity.CreatedBy = createdBy;
                entity.CreatedAt = existing.CreatedAt;

                entity.UpdatedBy = resolvedUserId;
                entity.UpdatedAt = DateTime.Now;
                entity.IsActive = existing.IsActive;

                // =====================================================
                // UPDATE CSR1
                // =====================================================

                var updated = await _csr1Repo.PatchAsync(id, entity);

                if (updated == null)
                {
                    return NotFound(new
                    {
                        message = "CSR1 record not found."
                    });
                }

                //// =====================================================
                //// REPLACE EXPENSE BREAKDOWN - delete + recreate, same
                //// pattern as the Product groups below.
                //// =====================================================

                //var existingExpenses = await _db.Set<CSR1Expenses>()
                //    .Where(x => x.CSR1Id == id)
                //    .ToListAsync();
                //_db.Set<CSR1Expenses>().RemoveRange(existingExpenses);
                //_db.Set<CSR1Expenses>().AddRange(
                //    SPIC.Core.Entities.CSR1ExpenseMapper.ToExpenseRows(entity, id));
                //await _db.SaveChangesAsync();

                // =====================================================
                // DELETE OLD GROUP 1
                // =====================================================

                var existingProducts1 = await _csr1Products1Repo
                    .GetWhere(x => x.CSR1Id == id)
                    .ToListAsync();

                foreach (var item in existingProducts1)
                {
                    await _csr1Products1Repo.DeleteAsync(item.Id);
                }

                // =====================================================
                // DELETE OLD GROUP 2
                // =====================================================

                var existingProducts2 = await _csr1Products2Repo
                    .GetWhere(x => x.CSR1Id == id)
                    .ToListAsync();

                foreach (var item in existingProducts2)
                {
                    await _csr1Products2Repo.DeleteAsync(item.Id);
                }

                // =====================================================
                // DELETE OLD GROUP 3
                // =====================================================

                var existingProducts3 = await _csr1Products3Repo
                    .GetWhere(x => x.CSR1Id == id)
                    .ToListAsync();

                foreach (var item in existingProducts3)
                {
                    await _csr1Products3Repo.DeleteAsync(item.Id);
                }

                // =====================================================
                // RECREATE MULTI CROP / PRODUCT GROUPS
                // =====================================================

                var updateCrop1Ids = CleanIds(request.Crop1Ids);
                var updateCrop2Ids = CleanIds(request.Crop2Ids);
                var updateCrop3Ids = CleanIds(request.Crop3Ids);
                var updateProduct1Ids = CleanIds(request.Product1Ids);
                var updateProduct2Ids = CleanIds(request.Product2Ids);
                var updateProduct3Ids = CleanIds(request.Product3Ids);

                await _csr1Products1Repo.CreateAsync(new CSR1Products1
                {
                    CSR1Id = id,
                    CropId = updateCrop1Ids.ToArray(),
                    ProductId = updateProduct1Ids.ToArray(),
                    IsActive = true
                });

                if (updateCrop2Ids.Count > 0 && updateProduct2Ids.Count > 0)
                {
                    await _csr1Products2Repo.CreateAsync(new CSR1Products2
                    {
                        CSR1Id = id,
                        CropId = updateCrop2Ids.ToArray(),
                        ProductId = updateProduct2Ids.ToArray(),
                        IsActive = true
                    });
                }

                if (updateCrop3Ids.Count > 0 && updateProduct3Ids.Count > 0)
                {
                    await _csr1Products3Repo.CreateAsync(new CSR1Products3
                    {
                        CSR1Id = id,
                        CropId = updateCrop3Ids.ToArray(),
                        ProductId = updateProduct3Ids.ToArray(),
                        IsActive = true
                    });
                }

                var existingExpenses = await _db.Set<CSR1Expenses>()
                    .Where(x => x.CSR1Id == id)
                    .ToListAsync();
                _db.Set<CSR1Expenses>().RemoveRange(existingExpenses);
                _db.Set<CSR1Expenses>().AddRange(
                    updateExpenses.Select(x => new CSR1Expenses
                    {
                        CSR1Id = id,
                        ExpenseId = x.ExpenseId,
                        ExpenseAmount = x.ExpenseAmount,
                        IsActive = true
                    }));
                await _db.SaveChangesAsync();


                // =====================================================
                // RESPONSE
                // =====================================================

                return Ok(new
                {
                    message = "CSR1 updated successfully",
                    data = updated
                });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = "Error while updating CSR1",
                        error = ex.Message
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
                var csr1Expenses = await _db.Set<CSR1Expenses>()
                    .Where(x => x.CSR1Id == id)
                    .ToListAsync();
                if (csr1Expenses.Count > 0)
                {
                    _db.Set<CSR1Expenses>().RemoveRange(csr1Expenses);
                    await _db.SaveChangesAsync();
                }

                // Delete separate approval details first (FK to CSR1).
                var approvalDetails = await _db.Set<CSR1ApprovalDetails>()
                    .Where(x => x.CSR1Id == id)
                    .ToListAsync();
                if (approvalDetails.Count > 0)
                {
                    _db.Set<CSR1ApprovalDetails>().RemoveRange(approvalDetails);
                    await _db.SaveChangesAsync();
                }

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

                // =========================================================
                // mine=true
                // CSR1.ResponsiblePersonId stores Employeelogins.UserId.
                // Therefore compare it directly with the current UserId.
                // =========================================================
                if (mine)
                {
                    csr1Records = csr1Records
                        .Where(c =>
                            !string.IsNullOrWhiteSpace(c.ResponsiblePersonId) &&
                            string.Equals(
                                c.ResponsiblePersonId,
                                CurrentUserId,
                                StringComparison.OrdinalIgnoreCase))
                        .ToList();
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

                var employeeLogins = await _db.Employeelogins
                    .AsNoTracking()
                    .ToListAsync();

                var users = await _db.Users
                    .AsNoTracking()
                    .ToListAsync();

                var result = csr1Records
                    .Select(csr =>
                    {
                        var program = programMasters
                            .FirstOrDefault(x => x.Id == csr.ProgramId);

                        var programType = programTypes
                            .FirstOrDefault(x => x.Id == program?.ProgramTypeId);

                        // CSR1.ResponsiblePersonId stores Employeelogins.UserId.
                        // Resolve the employee/login metadata using that UserId.
                        var empLogin = !string.IsNullOrWhiteSpace(csr.ResponsiblePersonId)
                            ? employeeLogins.FirstOrDefault(x =>
                                string.Equals(
                                    x.UserId,
                                    csr.ResponsiblePersonId,
                                    StringComparison.OrdinalIgnoreCase))
                            : null;

                        var resolvedHqId =
                            (csr.HeadquarterId ?? 0) > 0
                                ? csr.HeadquarterId
                                : empLogin?.HeadquartersId;

                        var creator = !string.IsNullOrEmpty(csr.CreatedBy)
                            ? users.FirstOrDefault(u =>
                                u.Id == csr.CreatedBy)
                            : null;

                        var creatorRole =
                            creator?.Role.ToString();

                        var scope =
                            ResolveCSR1Scope(
                                creatorRole,
                                csr.HeadquarterId);

                        var createdByName =
                            creator?.Name
                            ?? creator?.UserName
                            ?? creator?.Email
                            ?? csr.CreatedBy;

                        return new CSR1ListDto
                        {
                            Id = csr.Id,

                            SanctionNo = csr.SanctionNo,

                            ProgramTypeId =
                                program?.ProgramTypeId ?? 0,

                            ProgramType =
                                programType?.Name ?? "",

                            ProgramId =
                                csr.ProgramId,

                            ProgramName =
                                program?.Name ?? "",

                            NumberOfPrograms =
                                csr.NumberOfPrograms,

                            Budget =
                                csr.Budget,

                            HeadquarterId =
                                resolvedHqId,

                            LocationId =
                                csr.LocationId,

                            // Now string
                            ResponsiblePersonId =
                                csr.ResponsiblePersonId,

                            Status =
                                csr.Status.ToString(),

                            Scope =
                                scope,

                            CreatorRole =
                                creatorRole,

                            CreatedBy =
                                createdByName,

                            CreatedById =
                                csr.CreatedBy,

                            Remarks =
                                csr.Remarks,

                            CreatedAt =
                                csr.CreatedAt,

                            IsActive =
                                csr.IsActive,

                            CSR1Date =
                                csr.CSR1Date,

                            Products1 = products1
                                .Where(x =>
                                    x.CSR1Id == csr.Id)
                                .SelectMany(x =>
                                    (x.CropId ?? Array.Empty<int>())
                                    .SelectMany(cropId =>
                                        (x.ProductId ?? Array.Empty<int>())
                                        .Select(productId =>
                                            new CSR1ProductDto
                                            {
                                                Id = x.Id,
                                                CropId = cropId,
                                                ProductId = productId
                                            })))
                                .ToList(),

                            Products2 = products2
                                .Where(x =>
                                    x.CSR1Id == csr.Id)
                                .SelectMany(x =>
                                    (x.CropId ?? Array.Empty<int>())
                                    .SelectMany(cropId =>
                                        (x.ProductId ?? Array.Empty<int>())
                                        .Select(productId =>
                                            new CSR1ProductDto
                                            {
                                                Id = x.Id,
                                                CropId = cropId,
                                                ProductId = productId
                                            })))
                                .ToList(),

                            Products3 = products3
                                .Where(x =>
                                    x.CSR1Id == csr.Id)
                                .SelectMany(x =>
                                    (x.CropId ?? Array.Empty<int>())
                                    .SelectMany(cropId =>
                                        (x.ProductId ?? Array.Empty<int>())
                                        .Select(productId =>
                                            new CSR1ProductDto
                                            {
                                                Id = x.Id,
                                                CropId = cropId,
                                                ProductId = productId
                                            })))
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
                    program != null
                        ? await _programTypeRepo
                            .GetWhere(x => x.Id == program.ProgramTypeId)
                            .FirstOrDefaultAsync()
                        : null;


                // =====================================================
                // LOCATION
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


                // =====================================================
                // RESPONSIBLE PERSON
                // CSR1.ResponsiblePersonId stores Employeelogins.UserId.
                // Resolve UserId -> EmployeeInformationID -> Employee name.
                // =====================================================

                EmployeeInformation? employee = null;

                if (!string.IsNullOrWhiteSpace(csr1.ResponsiblePersonId))
                {
                    var responsibleEmployeeId = await _db.Employeelogins
                        .AsNoTracking()
                        .Where(x => x.UserId == csr1.ResponsiblePersonId)
                        .Select(x => (int?)x.EmployeeInformationID)
                        .FirstOrDefaultAsync();

                    if (responsibleEmployeeId.HasValue)
                    {
                        employee = await _employeeRepo
                            .GetWhere(x => x.Id == responsibleEmployeeId.Value)
                            .FirstOrDefaultAsync();
                    }
                }


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
                // EXPENSE BREAKDOWN
                // Rehydrates csr1's 11 legacy [NotMapped] properties from
                // CSR1Expenses, so the DTO fields below keep reading them
                // the same way they always have.
                // =====================================================

                var csr1Expenses =
                    await _db.Set<CSR1Expenses>()
                        .Where(x => x.CSR1Id == id && x.IsActive)
                        .ToListAsync();
                SPIC.Core.Entities.CSR1ExpenseMapper.ApplyExpenses(csr1, csr1Expenses);


                // =====================================================
                // CREATED / UPDATED USER
                // =====================================================

                var createdByUser =
                    !string.IsNullOrEmpty(csr1.CreatedBy)
                        ? await _db.Users
                            .AsNoTracking()
                            .FirstOrDefaultAsync(u =>
                                u.Id == csr1.CreatedBy)
                        : null;

                var createdByName =
                    createdByUser?.Name
                    ?? createdByUser?.UserName
                    ?? csr1.CreatedBy;


                var updatedByUser =
                    !string.IsNullOrEmpty(csr1.UpdatedBy)
                        ? await _db.Users
                            .AsNoTracking()
                            .FirstOrDefaultAsync(u =>
                                u.Id == csr1.UpdatedBy)
                        : null;

                var updatedByName =
                    updatedByUser?.Name
                    ?? updatedByUser?.UserName
                    ?? csr1.UpdatedBy;


                var (scope, creatorRole) =
                    await ResolveCSR1ScopeAsync(csr1);


                // =====================================================
                // RESPONSE
                // =====================================================

                var result =
                    new CSR1DetailsDto
                    {
                        Id = csr1.Id,

                        ProgramTypeId =
                            program?.ProgramTypeId ?? 0,

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


                        Status =
                            csr1.Status.ToString(),

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

                        CSR1Date =
                            csr1.CSR1Date,

                        SanctionNo =
                            csr1.SanctionNo,


                        // =====================================================
                        // FOCUS GROUP 1
                        // =====================================================

                        Focus1 =
                            group1
                                .SelectMany(x =>
                                    (x.CropId ?? Array.Empty<int>())
                                        .SelectMany(cropId =>
                                            (x.ProductId ?? Array.Empty<int>())
                                                .Select(productId =>
                                                    new CSR1FocusDetailsDto
                                                    {
                                                        CropId = cropId,

                                                        CropName =
                                                            crops
                                                                .FirstOrDefault(c =>
                                                                    c.Id == cropId)
                                                                ?.Name ?? "-",

                                                        ProductId =
                                                            productId,

                                                        ProductName =
                                                            products
                                                                .FirstOrDefault(p =>
                                                                    p.Id == productId)
                                                                ?.Name ?? "-"
                                                    })))
                                .ToList(),


                        // =====================================================
                        // FOCUS GROUP 2
                        // =====================================================

                        Focus2 =
                            group2
                                .SelectMany(x =>
                                    (x.CropId ?? Array.Empty<int>())
                                        .SelectMany(cropId =>
                                            (x.ProductId ?? Array.Empty<int>())
                                                .Select(productId =>
                                                    new CSR1FocusDetailsDto
                                                    {
                                                        CropId = cropId,

                                                        CropName =
                                                            crops
                                                                .FirstOrDefault(c =>
                                                                    c.Id == cropId)
                                                                ?.Name ?? "-",

                                                        ProductId =
                                                            productId,

                                                        ProductName =
                                                            products
                                                                .FirstOrDefault(p =>
                                                                    p.Id == productId)
                                                                ?.Name ?? "-"
                                                    })))
                                .ToList(),


                        // =====================================================
                        // FOCUS GROUP 3
                        // =====================================================

                        Focus3 =
                            group3
                                .SelectMany(x =>
                                    (x.CropId ?? Array.Empty<int>())
                                        .SelectMany(cropId =>
                                            (x.ProductId ?? Array.Empty<int>())
                                                .Select(productId =>
                                                    new CSR1FocusDetailsDto
                                                    {
                                                        CropId = cropId,

                                                        CropName =
                                                            crops
                                                                .FirstOrDefault(c =>
                                                                    c.Id == cropId)
                                                                ?.Name ?? "-",

                                                        ProductId =
                                                            productId,

                                                        ProductName =
                                                            products
                                                                .FirstOrDefault(p =>
                                                                    p.Id == productId)
                                                                ?.Name ?? "-"
                                                    })))
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
        public async Task<IActionResult> ValidateCSR1(int id, [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] CSR1SaveRequestWithExpenses? request)
        {
            try
            {
                var csr1 = await _csr1Repo.GetByIdAsync(id);
                if (csr1 == null)
                {
                    return NotFound(new { message = $"CSR-1 #{id} not found." });
                }

                var validationCutoffDay = await GetCSR1CutoffDayAsync();
                var isThreeLevelApplication = IsThreeLevelApplication(csr1.CreatedAt, validationCutoffDay);

                if (isThreeLevelApplication &&
                    !new[] { CSR1Status.SubmittedForValidation, CSR1Status.Submitted, CSR1Status.Pending }
                        .Contains(csr1.Status))
                    return BadRequest(new { message = "This CSR-1 is not awaiting validation." });

                if (csr1.Status == CSR1Status.Validated)
                {
                    return BadRequest(new { message = "CSR-1 is already validated." });
                }

                if (csr1.Status == CSR1Status.Approved)
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
                        if (!string.Equals(currentRole, AppRole.SMD.ToString(), StringComparison.OrdinalIgnoreCase) &&
                            !(isThreeLevelApplication &&
                              string.Equals(currentRole, "SMDO", StringComparison.OrdinalIgnoreCase)))
                        {
                            return StatusCode(StatusCodes.Status403Forbidden, new
                            {
                                message = "Only State Marketing Development Officers (SMDO) can validate State-level CSR-1."
                            });
                        }
                    }
                }

                csr1.Status = CSR1Status.Validated;
                csr1.UpdatedBy = CurrentUserId;
                csr1.UpdatedAt = DateTime.Now;
                csr1.ValidateBy = CurrentUserId;
                csr1.ValidateAt = DateTime.Now;

                await SaveReviewAndWorkflowAsync(csr1, request);

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

        [HttpGet("{id:int}/review-edit")]
        public async Task<IActionResult> GetReviewEdit(int id)
        {
            // IMPORTANT: use the SAME per-record authorization/scope check as
            // details/{id}; do NOT expose editable records to unrelated users.
            var csr = await _db.Set<CSR1>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
            if (csr == null) return NotFound();
            // INSERT the existing record-level access check here BEFORE returning data.

            var reviewExpenses = await _db.Set<CSR1Expenses>().AsNoTracking()
                .Where(x => x.CSR1Id == id && x.IsActive)
                .ToListAsync();
            SPIC.Core.Entities.CSR1ExpenseMapper.ApplyExpenses(csr, reviewExpenses);

            var g1 = await _db.Set<CSR1Products1>().AsNoTracking()
                .Where(x => x.CSR1Id == id && x.IsActive)
                .Select(x => new { x.CropId, x.ProductId }).ToListAsync();
            var g2 = await _db.Set<CSR1Products2>().AsNoTracking()
                .Where(x => x.CSR1Id == id && x.IsActive)
                .Select(x => new { x.CropId, x.ProductId }).ToListAsync();
            var g3 = await _db.Set<CSR1Products3>().AsNoTracking()
                .Where(x => x.CSR1Id == id && x.IsActive)
                .Select(x => new { x.CropId, x.ProductId }).ToListAsync();

            var crop1Ids = g1.SelectMany(x => x.CropId ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToList();
            var product1Ids = g1.SelectMany(x => x.ProductId ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToList();
            var crop2Ids = g2.SelectMany(x => x.CropId ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToList();
            var product2Ids = g2.SelectMany(x => x.ProductId ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToList();
            var crop3Ids = g3.SelectMany(x => x.CropId ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToList();
            var product3Ids = g3.SelectMany(x => x.ProductId ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToList();

            var expenses = await _db.Set<CSR1Expenses>()
                .AsNoTracking()
                .Where(x => x.CSR1Id == id && x.IsActive && x.ExpenseId.HasValue && x.ExpenseAmount > 0)
                .OrderBy(x => x.ExpenseId)
                .Select(x => new CSR1ExpenseSaveDto
                {
                    ExpenseId = x.ExpenseId!.Value,
                    ExpenseAmount = x.ExpenseAmount
                })
                .ToListAsync();

            return Ok(new CSR1SaveRequestWithExpenses
            {
                CSR1 = csr,
                Crop1Ids = crop1Ids,
                Product1Ids = product1Ids,
                Crop2Ids = crop2Ids,
                Product2Ids = product2Ids,
                Crop3Ids = crop3Ids,
                Product3Ids = product3Ids,
                Expenses = expenses
            });
        }


        private async Task SaveReviewAndWorkflowAsync(CSR1 csr1, CSR1SaveRequestWithExpenses? request)
        {
            // This transaction includes all four tables plus status/approval fields.
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var persistent = await _db.Set<CSR1>().FindAsync(csr1.Id)
                    ?? throw new InvalidOperationException("CSR-1 not found during update.");

                if (request?.CSR1 is { } incoming)
                {
                    var requestedReviewExpenses = request.Expenses ?? new List<CSR1ExpenseSaveDto>();

                    if (requestedReviewExpenses.Any(x => x.ExpenseId <= 0 || x.ExpenseAmount < 0))
                        throw new ArgumentException("Invalid expense id or amount.");

                    // Ignore zero-value rows; CSR1Expenses stores only entered amounts.
                    var expenses = requestedReviewExpenses
                        .Where(x => x.ExpenseAmount > 0)
                        .ToList();

                    if (expenses.GroupBy(x => x.ExpenseId).Any(g => g.Count() > 1))
                        throw new ArgumentException("Invalid or duplicate expense entries.");

                    var expenseIds = expenses.Select(x => x.ExpenseId).Distinct().ToList();
                    var validExpenseIds = await _db.Set<ExpenseInformation>()
                        .AsNoTracking()
                        .Where(x => x.IsActive && expenseIds.Contains(x.Id))
                        .Select(x => x.Id)
                        .ToListAsync();

                    if (expenseIds.Any(id => !validExpenseIds.Contains(id)))
                        throw new ArgumentException("One or more selected expenses are invalid or inactive.");

                    decimal total = expenses.Sum(x => x.ExpenseAmount);
                    if (total <= 0 || incoming.Budget <= 0 ||
                        total > incoming.Budget || incoming.Budget > persistent.Budget)
                        throw new ArgumentException("Expense total must be > 0 and <= Budget; individual expenses cannot be negative.");
                    if (incoming.NumberOfPrograms <= 0)
                        throw new ArgumentException("Number of Programs is required.");
                    if (persistent.ProgramMonth.HasValue && persistent.ProgramYear.HasValue &&
                        (incoming.CSR1Date.Month != persistent.ProgramMonth ||
                         incoming.CSR1Date.Year != persistent.ProgramYear))
                        throw new ArgumentException("Program date must remain in its allocated month/year.");
                    var c1 = CleanIds(request.Crop1Ids);
                    var c2 = CleanIds(request.Crop2Ids);
                    var c3 = CleanIds(request.Crop3Ids);
                    var p1 = CleanIds(request.Product1Ids);
                    var p2 = CleanIds(request.Product2Ids);
                    var p3 = CleanIds(request.Product3Ids);
                    if (c1.Count == 0 || p1.Count == 0 ||
                        c1.Count > 3 || c2.Count > 3 || c3.Count > 3 ||
                        p1.Count > 3 || p2.Count > 3 || p3.Count > 3 ||
                        (c2.Count > 0 && p2.Count == 0) ||
                        (c3.Count > 0 && p3.Count == 0) ||
                        (c2.Count == 0 && p2.Count > 0) ||
                        (c3.Count == 0 && p3.Count > 0))
                        throw new ArgumentException("Invalid focus crops/products (maximum 3 crops and 3 products per Focus section).");

                    persistent.NumberOfPrograms = incoming.NumberOfPrograms;
                    // Allocated budget is immutable during review.
                    // persistent.Budget = incoming.Budget;
                    persistent.HeadquarterId = incoming.HeadquarterId;
                    persistent.LocationId = incoming.LocationId;
                    var responsiblePersonUserId =
     await ResolveResponsiblePersonUserIdAsync(
         incoming.ResponsiblePersonId);

                    if (string.IsNullOrWhiteSpace(responsiblePersonUserId))
                    {
                        throw new ArgumentException(
                            "Selected Responsible Person does not have a valid Employee Login.");
                    }

                    persistent.ResponsiblePersonId =
                        responsiblePersonUserId;
                    persistent.CSR1Date = incoming.CSR1Date;

                    // Replace normalized CSR-1 expense rows.
                    var oldExpenses = await _db.Set<CSR1Expenses>()
                        .Where(x => x.CSR1Id == persistent.Id)
                        .ToListAsync();
                    _db.Set<CSR1Expenses>().RemoveRange(oldExpenses);
                    _db.Set<CSR1Expenses>().AddRange(
                        expenses.Select(x => new CSR1Expenses
                        {
                            CSR1Id = persistent.Id,
                            ExpenseId = x.ExpenseId,
                            ExpenseAmount = x.ExpenseAmount,
                            IsActive = true
                        }));

                    // Only replace children if review payload was supplied.
                    var old1 = await _db.Set<CSR1Products1>().Where(x => x.CSR1Id == persistent.Id).ToListAsync();
                    var old2 = await _db.Set<CSR1Products2>().Where(x => x.CSR1Id == persistent.Id).ToListAsync();
                    var old3 = await _db.Set<CSR1Products3>().Where(x => x.CSR1Id == persistent.Id).ToListAsync();
                    _db.Set<CSR1Products1>().RemoveRange(old1);
                    _db.Set<CSR1Products2>().RemoveRange(old2);
                    _db.Set<CSR1Products3>().RemoveRange(old3);
                    _db.Set<CSR1Products1>().Add(new CSR1Products1
                    {
                        CSR1Id = persistent.Id,
                        CropId = c1.ToArray(),
                        ProductId = p1.ToArray(),
                        IsActive = true
                    });
                    if (c2.Count > 0 && p2.Count > 0)
                    {
                        _db.Set<CSR1Products2>().Add(new CSR1Products2
                        {
                            CSR1Id = persistent.Id,
                            CropId = c2.ToArray(),
                            ProductId = p2.ToArray(),
                            IsActive = true
                        });
                    }
                    if (c3.Count > 0 && p3.Count > 0)
                    {
                        _db.Set<CSR1Products3>().Add(new CSR1Products3
                        {
                            CSR1Id = persistent.Id,
                            CropId = c3.ToArray(),
                            ProductId = p3.ToArray(),
                            IsActive = true
                        });
                    }
                }

                // Copy ONLY server-authorized workflow results from the existing method.
                // Never take workflow fields from request.CSR1.
                persistent.Status = csr1.Status;
                persistent.UpdatedBy = csr1.UpdatedBy;
                persistent.UpdatedAt = csr1.UpdatedAt;
                persistent.ValidateBy = csr1.ValidateBy;
                persistent.ValidateAt = csr1.ValidateAt;
                persistent.ApprovedBy = csr1.ApprovedBy;
                persistent.ApprovedAt = csr1.ApprovedAt;
                persistent.SanctionNo = csr1.SanctionNo;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private async Task<CSR1ApprovalDetails> GetOrCreateApprovalDetailsAsync(int csr1Id)
        {
            var approval = await _db.Set<CSR1ApprovalDetails>()
                .FirstOrDefaultAsync(x => x.CSR1Id == csr1Id);

            if (approval != null)
                return approval;

            approval = new CSR1ApprovalDetails
            {
                CSR1Id = csr1Id,
                CreatedBy = CurrentUserId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Set<CSR1ApprovalDetails>().Add(approval);
            return approval;
        }


        [HttpPost("{id:int}/approve")]
        [HttpPut("{id:int}/approve")]
        public async Task<IActionResult> ApproveCSR1(
      int id,
      [FromBody(EmptyBodyBehavior =
        Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)]
    CSR1SaveRequestWithExpenses? request)
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

                if (csr1.Status == CSR1Status.Approved)
                {
                    return BadRequest(new
                    {
                        message = "CSR-1 is already approved."
                    });
                }

                if (csr1.Status == CSR1Status.Rejected ||
                    csr1.Status == CSR1Status.Draft)
                {
                    return BadRequest(new
                    {
                        message = "This CSR-1 cannot be approved in its current status."
                    });
                }

                // --------------------------------------------------
                // 1. CURRENT USER ROLES
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
                // 2. DETERMINE WORKFLOW
                // CSR1Day is read from the active CSRExceedingDaySettings row.
                // Day <= CSR1Day -> legacy; day > CSR1Day -> three-level.
                // --------------------------------------------------

                var applicationDateIst = csr1.CreatedAt;
                var approvalCutoffDay = await GetCSR1CutoffDayAsync();

                bool requiresThreeLevelApproval =
                    IsThreeLevelApplication(applicationDateIst, approvalCutoffDay);

                // --------------------------------------------------
                // 3. RESOLVE CSR-1 SCOPE
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

                // ==================================================
                // 4. LEGACY SINGLE-LEVEL APPROVAL
                // DAY 1 - 26
                // ==================================================

                if (!requiresThreeLevelApproval)
                {
                    if (!isAdmin &&
                        csr1.Status != CSR1Status.Validated)
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
                                message =
                                    "Only RM/RMD can approve HQ-level CSR-1."
                            });
                        }

                        if (isStateScope &&
                            !HasRole(AppRole.SMM.ToString()))
                        {
                            return StatusCode(403, new
                            {
                                message =
                                    "Only SMM can approve State-level CSR-1."
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

                    var now = DateTime.UtcNow;

                    csr1.Status = CSR1Status.Approved;
                    csr1.ApprovedBy = CurrentUserId;
                    csr1.ApprovedAt = now;

                    csr1.UpdatedBy = CurrentUserId;
                    csr1.UpdatedAt = now;

                    await SaveReviewAndWorkflowAsync(
                        csr1,
                        request);

                    return Ok(new
                    {
                        message = "CSR-1 approved successfully.",
                        approvalLevel = 1,
                        isFinalApproval = true,
                        sanctionNo = csr1.SanctionNo,
                        data = csr1
                    });
                }

                // ==================================================
                // 5. THREE LEVEL APPROVAL
                //
                // Validated
                //      ↓
                // Level1Approved - SMM / RMM
                //      ↓
                // Level2Approved - MDAdmin
                //      ↓
                // Approved       - MDHOD
                // ==================================================

                var next = Csr1WorkflowRules.NextStatus(
                    true,
                    csr1.Status.ToString(),
                    isStateScope
                        ? (HasRole("SMM")
                            ? "SMM"
                            : GetHighestNewRole(roles))
                        : (HasRole("RMM")
                            ? "RMM"
                            : GetHighestNewRole(roles)),
                    scope);

                if (next == null)
                {
                    return StatusCode(403, new
                    {
                        message =
                            "Not authorized for the current CSR-1 approval stage, or validation is pending."
                    });
                }

                var nowUtc = DateTime.UtcNow;

                // --------------------------------------------------
                // Load approval history
                // --------------------------------------------------

                var approvals = await _db
                    .Set<CSR1ApprovalDetails>()
                    .Where(x => x.CSR1Id == csr1.Id)
                    .ToListAsync();

                bool LevelExists(string level) =>
                    approvals.Any(x =>
                        string.Equals(
                            x.ApproverLevel,
                            level,
                            StringComparison.OrdinalIgnoreCase));

                // ==================================================
                // LEVEL 1
                // ==================================================

                if (next == "Level1Approved")
                {
                    if (LevelExists("Level1"))
                    {
                        return BadRequest(new
                        {
                            message = "Level 1 is already completed."
                        });
                    }

                    var approval = new CSR1ApprovalDetails
                    {
                        CSR1Id = csr1.Id,

                        ApprovedBy = CurrentUserId,
                        ApprovedAt = nowUtc,

                        ApproverLevel = "Level1",

                        CreatedBy = CurrentUserId,
                        CreatedAt = nowUtc
                    };

                    _db.Set<CSR1ApprovalDetails>()
                        .Add(approval);
                }

                // ==================================================
                // LEVEL 2
                // ==================================================

                else if (next == "Level2Approved")
                {
                    var level1Completed =
                        LevelExists("Level1");

                    var level2Completed =
                        LevelExists("Level2");

                    if (!level1Completed)
                    {
                        return BadRequest(new
                        {
                            message =
                                "Level 1 approval must be completed before Level 2."
                        });
                    }

                    if (level2Completed)
                    {
                        return BadRequest(new
                        {
                            message =
                                "Level 2 is already completed."
                        });
                    }

                    var approval = new CSR1ApprovalDetails
                    {
                        CSR1Id = csr1.Id,

                        ApprovedBy = CurrentUserId,
                        ApprovedAt = nowUtc,

                        ApproverLevel = "Level2",

                        CreatedBy = CurrentUserId,
                        CreatedAt = nowUtc
                    };

                    _db.Set<CSR1ApprovalDetails>()
                        .Add(approval);
                }

                // ==================================================
                // LEVEL 3 / FINAL APPROVAL
                // ==================================================

                else if (next == "Approved")
                {
                    var level1Completed =
                        LevelExists("Level1");

                    var level2Completed =
                        LevelExists("Level2");

                    var level3Completed =
                        LevelExists("Level3");

                    if (!level1Completed ||
                        !level2Completed)
                    {
                        return BadRequest(new
                        {
                            message =
                                "Complete Level 1 and Level 2 before final approval."
                        });
                    }

                    if (level3Completed)
                    {
                        return BadRequest(new
                        {
                            message =
                                "Final approval is already completed."
                        });
                    }

                    // Generate sanction only on final approval
                    if (string.IsNullOrWhiteSpace(csr1.SanctionNo))
                    {
                        csr1.SanctionNo =
                            await GenerateSanctionForCSR1Async(
                                csr1,
                                scope);
                    }

                    var approval = new CSR1ApprovalDetails
                    {
                        CSR1Id = csr1.Id,

                        ApprovedBy = CurrentUserId,
                        ApprovedAt = nowUtc,

                        ApproverLevel = "Level3",

                        CreatedBy = CurrentUserId,
                        CreatedAt = nowUtc
                    };

                    _db.Set<CSR1ApprovalDetails>()
                        .Add(approval);

                    // Keep final approval also in CSR1 master
                    csr1.ApprovedBy = CurrentUserId;
                    csr1.ApprovedAt = nowUtc;
                }
                else
                {
                    return BadRequest(new
                    {
                        message =
                            $"Unsupported CSR-1 approval workflow status: {next}."
                    });
                }

                // --------------------------------------------------
                // 6. CONVERT NEXT STATUS
                // --------------------------------------------------

                if (!Enum.TryParse<CSR1Status>(
                        next,
                        ignoreCase: true,
                        out var nextStatus) ||
                    !Enum.IsDefined(
                        typeof(CSR1Status),
                        nextStatus))
                {
                    return BadRequest(new
                    {
                        message =
                            $"Unknown CSR-1 workflow status: {next}."
                    });
                }

                csr1.Status = nextStatus;
                csr1.UpdatedBy = CurrentUserId;
                csr1.UpdatedAt = nowUtc;

                // --------------------------------------------------
                // 7. SAVE CSR-1 + APPROVAL HISTORY
                // --------------------------------------------------

                await SaveReviewAndWorkflowAsync(
                    csr1,
                    request);

                return Ok(new
                {
                    message =
                        next == "Approved"
                            ? "CSR-1 final approval completed."
                            : next == "Level1Approved"
                                ? "CSR-1 first approval completed."
                                : "CSR-1 second approval completed.",

                    approvalLevel =
                        next == "Level1Approved"
                            ? 1
                            : next == "Level2Approved"
                                ? 2
                                : 3,

                    isFinalApproval =
                        next == "Approved",

                    sanctionNo =
                        csr1.SanctionNo,

                    data = csr1
                });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message =
                            "Error while approving CSR-1",

                        error =
                            ex.Message
                    });
            }
        }
        // =====================================================
        // CSR1 - RESOLVE CURRENT USER ROLE FOR NEW WORKFLOW
        // =====================================================

        private static string GetHighestNewRole(HashSet<string> roles)
        {
            if (roles.Contains("MDHOD") || roles.Contains("CSMD-HOD"))
                return "MDHOD";

            if (roles.Contains("MDAdmin"))
                return "MDAdmin";

            if (roles.Contains("SMM"))
                return "SMM";

            if (roles.Contains("RMM"))
                return "RMM";

            if (roles.Contains("SMDO") || roles.Contains("SMD"))
                return "SMDO";

            if (roles.Contains("RMDO") || roles.Contains("RMD"))
                return "RMDO";

            if (roles.Contains("MO"))
                return "MO";

            return string.Empty;
        }

        // Supports GetHighestNewRole(User)
        private static string GetHighestNewRole(ClaimsPrincipal user)
        {
            var roles = user.FindAll(ClaimTypes.Role)
                .Select(x => x.Value?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return GetHighestNewRole(roles);
        }

        // Supports GetHighestNewRole()
        private string GetHighestNewRole()
        {
            return GetHighestNewRole(User);
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

            // Financial year: April 2026 to March 2027 => FY27.
            int fyEndYear = programDate.Month >= 4
                ? programDate.Year + 1
                : programDate.Year;

            string financialYear = $"FY{fyEndYear % 100:D2}";
            string month = programDate.ToString("MM");

            // Read State Code from the State master for both approval scopes.
            var state = await _db.States
                .AsNoTracking()
                .Where(x => x.Id == stateId)
                .Select(x => new { x.StateCode })
                .FirstOrDefaultAsync();

            if (state == null)
            {
                throw new InvalidOperationException(
                    "Selected State was not found in the State master.");
            }

            if (string.IsNullOrWhiteSpace(state.StateCode))
            {
                throw new InvalidOperationException(
                    "State Code is missing in the State master.");
            }

            string stateCode = state.StateCode.Trim();

            // Regional sanction number: FY27/TN/MDUI/11/0546.
            if (isRegionalApproval)
            {
                if (!regionId.HasValue || regionId.Value <= 0)
                {
                    throw new InvalidOperationException(
                        "RegionId is required for regional sanction.");
                }

                // Read Region Code from the Region master and verify its State.
                var region = await _db.Regions
                    .AsNoTracking()
                    .Where(x => x.Id == regionId.Value && x.StateId == stateId)
                    .Select(x => new { x.RegionCode })
                    .FirstOrDefaultAsync();

                if (region == null)
                {
                    throw new InvalidOperationException(
                        "Selected Region was not found or does not belong to the selected State.");
                }

                if (string.IsNullOrWhiteSpace(region.RegionCode))
                {
                    throw new InvalidOperationException(
                        "Region Code is missing in the Region master.");
                }

                string regionCode = region.RegionCode.Trim();

                // Keep this entity tracked: the caller saves the counter update.
                var sequence = await _db.SanctionNumberSequences
                    .FirstOrDefaultAsync(x =>
                        x.StateId == stateId &&
                        x.RegionId == regionId.Value);

                if (sequence == null)
                {
                    throw new InvalidOperationException(
                        "Regional sanction number sequence not found.");
                }

                // Stored number is LAST issued, as in the existing implementation.
                int nextNumber = checked(sequence.SanctionNumber + 1);

                string sanctionNo =
                    $"{financialYear}/" +
                    $"{stateCode}/" +
                    $"{regionCode}/" +
                    $"{month}/" +
                    $"{nextNumber:D4}";

                sequence.SanctionNumber = nextNumber;
                sequence.UpdatedBy = CurrentUserId;
                sequence.UpdatedAt = DateTime.UtcNow;

                // SaveChanges is handled by the caller's approval transaction.
                return sanctionNo;
            }

            // State sanction number: FY27/TN/11/0005.
            var stateSequence = await _db.SanctionNumberSequences
                .FirstOrDefaultAsync(x =>
                    x.StateId == stateId &&
                    x.RegionId == null);

            if (stateSequence == null)
            {
                throw new InvalidOperationException(
                    "State sanction number sequence not found.");
            }

            // Stored number is LAST issued, as in the existing implementation.
            int nextStateNumber = checked(stateSequence.SanctionNumberState + 1);

            string stateSanctionNo =
                $"{financialYear}/" +
                $"{stateCode}/" +
                $"{month}/" +
                $"{nextStateNumber:D4}";

            stateSequence.SanctionNumberState = nextStateNumber;
            stateSequence.UpdatedBy = CurrentUserId;
            stateSequence.UpdatedAt = DateTime.UtcNow;

            // SaveChanges is handled by the caller's approval transaction.
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

                if (csr1.Status == CSR1Status.Approved)
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

                csr1.Status = CSR1Status.Rejected;
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

        [HttpGet("{id:int}/workflow")]
        public async Task<IActionResult> GetWorkflow(int id)
        {
            var csr1 = await _csr1Repo.GetByIdAsync(id);

            if (csr1 == null)
                return NotFound();

            var workflowCutoffDay = await GetCSR1CutoffDayAsync();
            var three = IsThreeLevelApplication(csr1.CreatedAt, workflowCutoffDay);

            var (scope, _) = await ResolveCSR1ScopeAsync(csr1);

            var roles = User.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var currentApprovalRole = scope.Equals(
                "State",
                StringComparison.OrdinalIgnoreCase)
                    ? (roles.Contains("SMM")
                        ? "SMM"
                        : GetHighestNewRole(roles))
                    : (roles.Contains("RMM")
                        ? "RMM"
                        : GetHighestNewRole(roles));

            var next = three
                ? Csr1WorkflowRules.NextStatus(
                    true,
                    csr1.Status.ToString(),
                    currentApprovalRole,
                    scope)
                : null;

            var validated =
                csr1.Status == CSR1Status.Validated;

            var pending = new[]
            {
        CSR1Status.Submitted,
        CSR1Status.SubmittedForValidation,
        CSR1Status.Pending
    }.Contains(csr1.Status);

            var firstRole = scope.Equals(
                "State",
                StringComparison.OrdinalIgnoreCase)
                    ? "SMM"
                    : "RMM";

            // =========================================================
            // NEW APPROVAL DETAILS STRUCTURE
            // One row for each approval level
            // =========================================================

            var approvals = await _db.Set<CSR1ApprovalDetails>()
                .AsNoTracking()
                .Where(x => x.CSR1Id == id)
                .OrderBy(x => x.ApprovedAt)
                .ToListAsync();

            var level1Approval = approvals
                .FirstOrDefault(x =>
                    string.Equals(
                        x.ApproverLevel,
                        "Level1",
                        StringComparison.OrdinalIgnoreCase));

            var level2Approval = approvals
                .FirstOrDefault(x =>
                    string.Equals(
                        x.ApproverLevel,
                        "Level2",
                        StringComparison.OrdinalIgnoreCase));

            var level3Approval = approvals
                .FirstOrDefault(x =>
                    string.Equals(
                        x.ApproverLevel,
                        "Level3",
                        StringComparison.OrdinalIgnoreCase));

            // =========================================================
            // AWAITING LABEL
            // =========================================================

            var awaiting = !three
                ? ""
                : pending
                    ? (scope.Equals(
                        "State",
                        StringComparison.OrdinalIgnoreCase)
                            ? "Awaiting SMDO validation"
                            : "Awaiting MO validation")
                : validated
                    ? $"Awaiting {firstRole} approval"
                : csr1.Status == CSR1Status.Level1Approved
                    ? "Awaiting MDAdmin approval"
                : csr1.Status == CSR1Status.Level2Approved
                    ? "Awaiting MDHOD approval"
                : "";

            return Ok(new
            {
                isThreeLevelWorkflow = three,

                canValidate =
                    three &&
                    pending &&
                    (
                        scope.Equals(
                            "State",
                            StringComparison.OrdinalIgnoreCase)
                            ? roles.Contains("SMD") ||
                              roles.Contains("SMDO")
                            : roles.Contains("MO")
                    ),

                canApprove =
                    three &&
                    next != null,

                canReject = false,

                approvalLabel =
                    next == "Level1Approved"
                        ? "1st Approval"
                    : next == "Level2Approved"
                        ? "2nd Approval"
                    : next == "Approved"
                        ? "Final Approval"
                    : "Approve",

                awaitingLabel = awaiting,

                steps = three
                    ? new object[]
                    {
                new
                {
                    name = $"{firstRole} First Approval",
                    actor = level1Approval?.ApprovedBy,
                    at = level1Approval?.ApprovedAt,
                    completed =
                        level1Approval?.ApprovedAt.HasValue == true
                },

                new
                {
                    name = "MDAdmin Second Approval",
                    actor = level2Approval?.ApprovedBy,
                    at = level2Approval?.ApprovedAt,
                    completed =
                        level2Approval?.ApprovedAt.HasValue == true
                },

                new
                {
                    name = "MDHOD Final Approval",
                    actor = level3Approval?.ApprovedBy,
                    at = level3Approval?.ApprovedAt,
                    completed =
                        level3Approval?.ApprovedAt.HasValue == true
                }
                    }
                    : Array.Empty<object>()
            });
        }

        private async Task<(bool IsValid, decimal TotalExpense, string? ErrorMessage)> ValidateBudgetExpenseMatchAsync(
        CSR1 csr1, IEnumerable<CSR1ExpenseSaveDto>? requestExpenses = null, bool useRequestExpenses = false)
        {
            decimal totalExpense;

            if (useRequestExpenses)
            {
                var expenses = (requestExpenses ?? Enumerable.Empty<CSR1ExpenseSaveDto>())
                    .Where(x => x.ExpenseAmount > 0)
                    .ToList();

                totalExpense = expenses.Sum(x => x.ExpenseAmount);
            }
            else
            {
                totalExpense = await _db.Set<CSR1Expenses>()
                    .AsNoTracking()
                    .Where(x =>
                        x.CSR1Id == csr1.Id &&
                        x.IsActive &&
                        x.ExpenseAmount > 0)
                    .SumAsync(x => x.ExpenseAmount);
            }

            var budget = decimal.Round(csr1.Budget, 2);
            totalExpense = decimal.Round(totalExpense, 2);

            if (budget != totalExpense)
            {
                return (
                    false,
                    totalExpense,
                    $"Budget amount and Total Expense must be equal. " +
                    $"Budget: ₹{budget:N2}, Total Expense: ₹{totalExpense:N2}."
                );
            }

            return (true, totalExpense, null);
        }

        private static bool IsThreeLevelApplication(DateTime localDate, int csr1CutoffDay)
        {
            return localDate.Day > csr1CutoffDay;
        }

        private static DateTime AllowedProgramMonth(DateTime localDate, int csr1CutoffDay)
        {
            var monthOffset = localDate.Day <= csr1CutoffDay ? 1 : 2;

            return new DateTime(localDate.Year, localDate.Month, 1)
                .AddMonths(monthOffset);
        }

        private async Task<int> GetCSR1CutoffDayAsync()
        {
            var setting = await _db.Set<CSRExceedingDaySetting>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (setting == null)
            {
                throw new InvalidOperationException(
                    "Active CSR Exceeding Day Setting is not configured.");
            }

            if (setting.CSR1Day < 1 || setting.CSR1Day > 31)
            {
                throw new InvalidOperationException(
                    $"Invalid CSR1Day configured: {setting.CSR1Day}.");
            }

            return setting.CSR1Day;
        }

        [HttpGet("workflow-setting")]
        public async Task<IActionResult> GetWorkflowSetting()
        {
            try
            {
                var csr1Day = await GetCSR1CutoffDayAsync();

                return Ok(new
                {
                    CSR1Day = csr1Day
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}