using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit.Cryptography;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using static SPIC.Core.Entities.EmployeeRegistration;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BudgetController : ControllerBase
    {
        private readonly IGenericRepository<BudgetProgram> _budgetRepo;
        private readonly IGenericRepository<ProgramMaster> _programRepo;
        private readonly IGenericRepository<Headquarter> _headquarterRepo;
        private readonly IGenericRepository<Zone> _zoneRepo;
        private readonly IGenericRepository<EmployeeInformation> _employeeRepo;
        private readonly IGenericRepository<Crop> _cropRepo;
        private readonly IGenericRepository<Product> _productRepo;
        private readonly IGenericRepository<State> _stateRepo;
        private readonly IGenericRepository<AnnualBudgeting> _annualBudgetingRepo;
        private readonly AppDbContext _db;
        private string CurrentUser =>
    User.Identity?.Name ?? "System";
        public BudgetController(
    IGenericRepository<BudgetProgram> budgetRepo,
    IGenericRepository<ProgramMaster> programRepo,
    IGenericRepository<Headquarter> headquarterRepo,
    IGenericRepository<Zone> zoneRepo,
   IGenericRepository<EmployeeInformation> employeeRepo,
   IGenericRepository<Crop> cropRepo,
   IGenericRepository<Product> productRepo,
   IGenericRepository<State> stateRepo,
   IGenericRepository<AnnualBudgeting> annualBudgetingRepo,
   AppDbContext db)
        {
            _budgetRepo = budgetRepo;
            _programRepo = programRepo;
            _headquarterRepo = headquarterRepo;
            _zoneRepo = zoneRepo;
            _employeeRepo = employeeRepo;
            _cropRepo = cropRepo;
            _productRepo = productRepo;
            _stateRepo = stateRepo;
            _annualBudgetingRepo = annualBudgetingRepo;
            _db = db;
        }


        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> SaveBudget([FromBody] BudgetProgram model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);


            model.CreatedAt = DateTime.Now;
            model.CreatedBy = CurrentUser;
            model.UpdatedAt = DateTime.Now;


            var created = await _budgetRepo.CreateAsync(model);


            return Ok(new
            {
                message = "Budget created successfully",
                data = created
            });
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _budgetRepo
                .GetAll()
                .ToListAsync();

            return Ok(items);
        }


        [HttpGet("all")]
        public async Task<IActionResult> GetAllWithInactive()
        {
            var items = await _budgetRepo
                .GetAllWithInactive()
                .ToListAsync();

            return Ok(items);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _budgetRepo.GetByIdAsync(id);

            if (item == null)
                return NotFound();

            return Ok(item);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] BudgetProgram entity)
        {
           
            entity.UpdatedBy = CurrentUser;
            entity.UpdatedAt = DateTime.Now;

            var updated = await _budgetRepo
                .PatchAsync(id, entity);

            if (updated == null)
                return NotFound();

            return Ok(new
            {
                message = "Budget updated successfully",
                data = updated
            });
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _budgetRepo
                .DeleteAsync(id);

            if (!deleted)
                return NotFound();

            return Ok(new
            {
                message = "Budget deleted successfully"
            });
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var budgets = await _budgetRepo.GetAll().Include(x => x.Program).ToListAsync();

            return Ok(new
            {
                ApprovedBudget = budgets.Sum(x => x.TotalBudget),
                AllocatedBudget = budgets.Sum(x => x.April + x.May + x.June + x.July + x.August + x.September + x.October + x.November + x.December + x.January + x.February + x.March),
                ProgramCount = budgets.Count(),
                ProgramBudgets = budgets
            });
        }


        [HttpGet("programs")]
        public async Task<IActionResult> GetProgramWise()
        {
            var items = await _budgetRepo
                .GetAll()
                .Include(x => x.Program)
                .ThenInclude(x => x.ProgramType)
                .Where(x => x.Program != null)
                .Select(x => new ProgramWiseBudgetDto
                {
                    Id = x.Id,

                    ProgramType = x.Program!.ProgramType != null
                        ? x.Program.ProgramType.Name
                        : "",

                    ProgramName = x.Program.Name,

                    TotalBudget = x.TotalBudget,

                    AprilBudget = x.April,
                    MayBudget = x.May,
                    JuneBudget = x.June,
                    JulyBudget = x.July,
                    AugustBudget = x.August,
                    SeptemberBudget = x.September,
                    OctoberBudget = x.October,
                    NovemberBudget = x.November,
                    DecemberBudget = x.December,
                    JanuaryBudget = x.January,
                    FebruaryBudget = x.February,
                    MarchBudget = x.March

                })
                .ToListAsync();


            return Ok(items);
        }

        [HttpGet("program-master-list")]
        public async Task<IActionResult> GetProgramMasterList()
        {
            var programs = await _programRepo
                .GetAll()
                .Include(x => x.ProgramType)
                .Select(x => new
                {
                    Program = x,

                    Budget = _budgetRepo
                        .GetAll()
                        .Where(b => b.ProgramId == x.Id)
                        .OrderByDescending(b => b.UpdatedAt)
                        .FirstOrDefault()
                })
                .Select(x => new ProgramWiseBudgetDto
                {
                    ProgramId = x.Program.Id,
                    ProgramTypeId = x.Program.Id,
                    ProgramType = x.Program.ProgramType != null
                        ? x.Program.ProgramType.Name
                        : "",

                    ProgramName = x.Program.Name,


                    // =========================================
                    // Budget
                    // =========================================
                    BudgetAmount = x.Program.BudgetAmount,

                    IsChangeAmount = x.Program.BudgetAmount > 0
                        ? false
                        : true,

                    TotalBudget = x.Budget != null
                        ? x.Budget.TotalBudget
                        : x.Program.BudgetAmount,


                    // =========================================
                    // Monthly Counts
                    // =========================================
                    AprilCount = x.Budget != null
                        ? x.Budget.AprilCount
                        : 0,

                    MayCount = x.Budget != null
                        ? x.Budget.MayCount
                        : 0,

                    JuneCount = x.Budget != null
                        ? x.Budget.JuneCount
                        : 0,

                    JulyCount = x.Budget != null
                        ? x.Budget.JulyCount
                        : 0,

                    AugustCount = x.Budget != null
                        ? x.Budget.AugustCount
                        : 0,

                    SeptemberCount = x.Budget != null
                        ? x.Budget.SeptemberCount
                        : 0,

                    OctoberCount = x.Budget != null
                        ? x.Budget.OctoberCount
                        : 0,

                    NovemberCount = x.Budget != null
                        ? x.Budget.NovemberCount
                        : 0,

                    DecemberCount = x.Budget != null
                        ? x.Budget.DecemberCount
                        : 0,

                    JanuaryCount = x.Budget != null
                        ? x.Budget.JanuaryCount
                        : 0,

                    FebruaryCount = x.Budget != null
                        ? x.Budget.FebruaryCount
                        : 0,

                    MarchCount = x.Budget != null
                        ? x.Budget.MarchCount
                        : 0,


                    // =========================================
                    // Monthly Budget
                    // =========================================
                    AprilBudget = x.Budget != null
                        ? x.Budget.April
                        : 0,

                    MayBudget = x.Budget != null
                        ? x.Budget.May
                        : 0,

                    JuneBudget = x.Budget != null
                        ? x.Budget.June
                        : 0,

                    JulyBudget = x.Budget != null
                        ? x.Budget.July
                        : 0,

                    AugustBudget = x.Budget != null
                        ? x.Budget.August
                        : 0,

                    SeptemberBudget = x.Budget != null
                        ? x.Budget.September
                        : 0,

                    OctoberBudget = x.Budget != null
                        ? x.Budget.October
                        : 0,

                    NovemberBudget = x.Budget != null
                        ? x.Budget.November
                        : 0,

                    DecemberBudget = x.Budget != null
                        ? x.Budget.December
                        : 0,

                    JanuaryBudget = x.Budget != null
                        ? x.Budget.January
                        : 0,

                    FebruaryBudget = x.Budget != null
                        ? x.Budget.February
                        : 0,

                    MarchBudget = x.Budget != null
                        ? x.Budget.March
                        : 0
                })
                .ToListAsync();


            return Ok(programs);
        }

        [HttpPost("draft")]
        public async Task<IActionResult> SaveDraft(
    [FromBody] BudgetProgram model)
        {
          

            var existing = await _budgetRepo
                .GetAll()
                .FirstOrDefaultAsync(x =>
                    x.ProgramId == model.ProgramId &&
                    x.FinancialYear == model.FinancialYear &&
                    x.Status == "Draft"
                );


            if (existing != null)
            {
                existing.TotalBudget = model.TotalBudget;

                existing.AprilCount = model.AprilCount;
                existing.April = model.April;

                existing.MayCount = model.MayCount;
                existing.May = model.May;

                existing.UpdatedBy = CurrentUser;
                existing.UpdatedAt = DateTime.Now;


                await _budgetRepo.UpdateAsync(existing);
            }
            else
            {
                model.Status = "Draft";
                model.CreatedBy = CurrentUser;
                await _budgetRepo.CreateAsync(model);
            }


            return Ok(new
            {
                message = "Budget draft saved successfully"
            });
        }

        [HttpGet("drafts")]
        public async Task<IActionResult> GetDrafts()
        {
            var drafts = await _budgetRepo
                .GetAll()
                .Where(x => x.Status == "Draft")
                .ToListAsync();

            return Ok(drafts);
        }

        [HttpGet("submissions")]
        public async Task<IActionResult> GetBudgetSubmissions()
        {
            var data =
                await _budgetRepo
                .GetAll()
                .AsNoTracking()
                .Include(x => x.Program)
                .ThenInclude(x => x.ProgramType)
                .Select(x => new BudgetSubmissionDto
                {
                    Id = x.Id,

                    ProgramType =
                        x.Program != null &&
                        x.Program.ProgramType != null
                        ? x.Program.ProgramType.Name
                        : "",


                    ProgramName =
                        x.Program != null
                        ? x.Program.Name
                        : "",


                    TotalBudget =
                        x.TotalBudget,


                    SubmittedDate =
                        x.CreatedAt,


                    Status =
                        x.Status,


                    ValidationDue = null

                })
                .ToListAsync();


            return Ok(data);
        }
        [HttpGet("submission-summary")]
        public async Task<IActionResult> GetSubmissionSummary()
        {

            var result =
                await _budgetRepo
                .GetAll()
                .AsNoTracking()
                .GroupBy(x => 1)
                .Select(x => new
                {

                    Total =
                        x.Count(),


                    Approved =
                        x.Count(a => a.Status == "Approved"),


                    Pending =
                        x.Count(a => a.Status == "Pending"),


                    Rejected =
                        x.Count(a => a.Status == "Rejected"),


                    Draft =
                        x.Count(a => a.Status == "Draft")

                })
                .FirstOrDefaultAsync();


            return Ok(result);
        }

        [HttpGet("headquarters")]
        public async Task<IActionResult> GetHeadquarters()
        {
            var headquarters = await _headquarterRepo
                .GetAll()
                .Where(x => x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.HeadquarterName,
                    x.IsActive
                })
                .OrderBy(x => x.HeadquarterName)
                .ToListAsync();


            return Ok(headquarters);
        }

        [HttpGet("zones")]
        public async Task<IActionResult> GetZones()
        {
            var zones = await _zoneRepo
                .GetAll()
                .Where(x => x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.ZoneName,
                    x.IsActive
                })
                .OrderBy(x => x.ZoneName)
                .ToListAsync();


            return Ok(zones);
        }

        [HttpGet("employees")]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await _employeeRepo
                .GetAll()
                .Select(x => new
                {
                    x.Id,
                    x.Name
                })
                .OrderBy(x => x.Name)
                .ToListAsync();


            return Ok(employees);
        }

        [HttpGet("crops")]
        public async Task<IActionResult> GetCrops()
        {
            var crops = await _cropRepo
                .GetAll()
                .Where(x => x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.IsActive
                })
                .OrderBy(x => x.Name)
                .ToListAsync();

            return Ok(crops);
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _productRepo
                .GetAll()
                .Where(x => x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.IsActive
                })
                .OrderBy(x => x.Name)
                .ToListAsync();

            return Ok(products);
        }

        /// <summary>
        /// State-wise budget rows for State Budget Management. Amount for each state is its
        /// persisted StateBudgetAllocation for the given FY (0 if none saved yet), so a page
        /// reload always reflects what was actually saved to the database.
        /// </summary>
        [HttpGet("states-budget")]
        public async Task<IActionResult> GetStatesBudget([FromQuery] string? fy)
        {
            var allocations = string.IsNullOrWhiteSpace(fy)
                ? new Dictionary<int, decimal>()
                : await _db.Set<StateBudgetAllocation>()
                    .Where(a => a.FY == fy)
                    .ToDictionaryAsync(a => a.StateId, a => a.Amount);

            var states = await _stateRepo
                .GetAll()
                .Where(x => x.IsActive)
                .OrderBy(x => x.StateName)
                .Select(x => new { x.Id, x.StateName })
                .ToListAsync();

            var result = states.Select(x => new StateBudgetDto
            {
                StateId = x.Id,
                StateName = x.StateName,
                BudgetAmount = allocations.TryGetValue(x.Id, out var amount) ? amount : 0m
            }).ToList();

            return Ok(result);
        }

        /// <summary>
        /// Full replace of every state's allocation for one FY (the client always submits every
        /// state's current amount, not just the changed row, so double-counting an edited row
        /// against its own previous value can't happen). Validated against the AnnualBudgeting
        /// amount for that FY before anything is written; one DB transaction so a State Budget
        /// Management save can never partially apply.
        /// </summary>
        [HttpPut("state-budget")]
        public async Task<IActionResult> SaveStateBudget([FromBody] SaveStateBudgetRequest request)
        {
            var fy = (request.FY ?? string.Empty).Trim();
            if (fy.Length == 0)
                return BadRequest(new { message = "Financial Year is required." });

            if (request.Allocations == null || request.Allocations.Count == 0)
                return BadRequest(new { message = "No state allocations to save." });

            if (request.Allocations.Any(a => a.Amount < 0))
                return BadRequest(new { message = "Allocation amount cannot be negative." });

            if (request.Allocations.Select(a => a.StateId).Distinct().Count() != request.Allocations.Count)
                return BadRequest(new { message = "Duplicate state entries in the request." });

            var annualBudget = await _annualBudgetingRepo
                .GetAll()
                .FirstOrDefaultAsync(b => b.FY == fy);

            if (annualBudget == null)
                return BadRequest(new { message = $"No Annual Budget is defined for FY {fy}." });

            var totalRequested = request.Allocations.Sum(a => a.Amount);
            if (totalRequested > annualBudget.Amount)
                return BadRequest(new
                {
                    message = $"Allocation amount cannot exceed the remaining budget of ₹{annualBudget.Amount:N0}."
                });

            var stateIds = request.Allocations.Select(a => a.StateId).ToList();
            var validStateIds = await _stateRepo.GetAll()
                .Where(s => stateIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync();
            if (validStateIds.Count != stateIds.Distinct().Count())
                return BadRequest(new { message = "One or more states are invalid or inactive." });

            await using var transaction = await _db.Database.BeginTransactionAsync();

            var existing = await _db.Set<StateBudgetAllocation>()
                .Where(a => a.FY == fy && stateIds.Contains(a.StateId))
                .ToListAsync();
            var existingByStateId = existing.ToDictionary(a => a.StateId);

            foreach (var alloc in request.Allocations)
            {
                if (existingByStateId.TryGetValue(alloc.StateId, out var row))
                {
                    row.Amount = alloc.Amount;
                    row.UpdatedBy = CurrentUser;
                    row.UpdatedAt = DateTime.Now;
                }
                else
                {
                    _db.Set<StateBudgetAllocation>().Add(new StateBudgetAllocation
                    {
                        StateId = alloc.StateId,
                        FY = fy,
                        Amount = alloc.Amount,
                        CreatedBy = CurrentUser,
                        CreatedAt = DateTime.Now,
                        UpdatedBy = CurrentUser,
                        UpdatedAt = DateTime.Now
                    });
                }
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "State budget allocation saved successfully",
                totalAllocated = totalRequested,
                remaining = annualBudget.Amount - totalRequested
            });
        }

        /// <summary>
        /// Current submission status for one FY's state allocations, so the page can disable
        /// Submit For Validation once it has already been submitted (no separate wiring
        /// needed elsewhere - every row for an FY is always kept in sync with the same status).
        /// </summary>
        [HttpGet("state-budget/status")]
        public async Task<IActionResult> GetStateBudgetStatus([FromQuery] string fy)
        {
            if (string.IsNullOrWhiteSpace(fy))
                return BadRequest(new { message = "Financial Year is required." });

            var status = await _db.Set<StateBudgetAllocation>()
                .Where(a => a.FY == fy)
                .Select(a => a.Status)
                .FirstOrDefaultAsync();

            return Ok(new StateBudgetStatusDto { Status = status ?? "Draft" });
        }

        /// <summary>
        /// Submit For Validation: marks every saved state allocation for the FY as "Submitted".
        /// Reuses BudgetProgram.Status's existing string-status convention - no new workflow,
        /// no new status values. Re-validates against the AnnualBudgeting amount independently
        /// of Save Draft (defense in depth) and refuses a second submission for the same FY.
        /// </summary>
        [HttpPut("state-budget/submit")]
        public async Task<IActionResult> SubmitStateBudget([FromBody] SubmitStateBudgetRequest request)
        {
            var fy = (request.FY ?? string.Empty).Trim();
            if (fy.Length == 0)
                return BadRequest(new { message = "Financial Year is required." });

            var annualBudget = await _annualBudgetingRepo
                .GetAll()
                .FirstOrDefaultAsync(b => b.FY == fy);

            if (annualBudget == null)
                return BadRequest(new { message = $"No Annual Budget is defined for FY {fy}." });

            var rows = await _db.Set<StateBudgetAllocation>()
                .Where(a => a.FY == fy)
                .ToListAsync();

            if (rows.Count == 0)
                return BadRequest(new { message = "Save the state allocation before submitting for validation." });

            if (rows.Any(r => r.Status == "Submitted"))
                return BadRequest(new { message = $"The allocation for FY {fy} has already been submitted for validation." });

            var total = rows.Sum(r => r.Amount);
            if (total > annualBudget.Amount)
                return BadRequest(new
                {
                    message = $"Allocation amount cannot exceed the remaining budget of ₹{annualBudget.Amount:N0}."
                });

            await using var transaction = await _db.Database.BeginTransactionAsync();

            foreach (var row in rows)
            {
                row.Status = "Submitted";
                row.UpdatedBy = CurrentUser;
                row.UpdatedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                message = $"State budget allocation for FY {fy} submitted for validation successfully."
            });
        }
    }


}