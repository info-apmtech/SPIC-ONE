using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit.Cryptography;
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
        
        private readonly IGenericRepository<ProgramStateBudget> _programStateBudgetRepo;
        private readonly IGenericRepository<StateBudgetAllocation> _stateBudgetAllocationRepo;
        private string CurrentUser =>
    User.Identity?.Name ?? "System";

        /// <summary>
        /// True for the "Admin" role group used throughout the app (mirrors LoginState.IsAdmin):
        /// Admin/SuperAdmin/CorporateAdmin/Director/AVP. Same role-claim pattern already used by
        /// GetProgramMasterList above. Level 1 (State) budget summary access.
        /// </summary>
        private bool IsAdminUser()
        {
            var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            return roleClaim == AppRole.Admin.ToString()
                || roleClaim == AppRole.SuperAdmin.ToString()
                || roleClaim == AppRole.CorporateAdmin.ToString()
                || roleClaim == AppRole.Director.ToString()
                || roleClaim == AppRole.AVP.ToString();
        }

        /// <summary>True for the "SM" role group (mirrors LoginState.IsStateRole: SMD/SMM). Level 2 (Region) budget summary access.</summary>
        private bool IsSmUser()
        {
            var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            return roleClaim == AppRole.SMD.ToString() || roleClaim == AppRole.SMM.ToString();
        }

        /// <summary>True for the "RM" role group (mirrors LoginState.IsRegionRole: RM/RMD). Level 3 (Headquarters) budget summary access.</summary>
        private bool IsRmUser()
        {
            var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            return roleClaim == AppRole.RM.ToString() || roleClaim == AppRole.RMD.ToString();
        }

        /// <summary>
        /// Single authoritative rule set shared by every level's Save AND Submit endpoint for the
        /// Total Budget / Allocated / Remaining Amount summary - Save and Submit must never apply
        /// different rules, since that mismatch previously let an unreconciled row (e.g. Total=100,
        /// Allocated=20, Remaining=2) get written to the database by Save despite being rejected by
        /// Submit. TotalBudget/AllocatedAmount must be strictly positive, RemainingAmount may be
        /// zero but never negative, and the three must reconcile exactly.
        /// </summary>
        private static bool TryValidateSummaryAmounts(
            decimal totalBudget, decimal allocatedAmount, decimal remainingAmount, out string error)
        {
            if (totalBudget <= 0)
            {
                error = "Total Budget must be greater than 0.";
                return false;
            }

            if (allocatedAmount <= 0)
            {
                error = "Allocated Amount must be greater than 0.";
                return false;
            }

            if (remainingAmount < 0)
            {
                error = "Remaining Amount cannot be negative.";
                return false;
            }

            if (totalBudget != allocatedAmount + remainingAmount)
            {
                var expectedRemaining = totalBudget - allocatedAmount;
                error = expectedRemaining >= 0
                    ? $"Remaining Amount must be ₹{expectedRemaining:N0}. Current entered amount is ₹{remainingAmount:N0}."
                    : $"Allocated Amount (₹{allocatedAmount:N0}) cannot exceed Total Budget (₹{totalBudget:N0}).";
                return false;
            }

            error = "";
            return true;
        }

        public BudgetController(
    IGenericRepository<BudgetProgram> budgetRepo,
    IGenericRepository<ProgramMaster> programRepo,
    IGenericRepository<Headquarter> headquarterRepo,
    IGenericRepository<Zone> zoneRepo,
   IGenericRepository<EmployeeInformation> employeeRepo,
   IGenericRepository<Crop> cropRepo,
   IGenericRepository<Product> productRepo,
   IGenericRepository<ProgramStateBudget> programStateBudgetRepo,
   IGenericRepository<StateBudgetAllocation> stateBudgetAllocationRepo,
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
            _programStateBudgetRepo = programStateBudgetRepo;
            _stateBudgetAllocationRepo = stateBudgetAllocationRepo;
            _annualBudgetingRepo = annualBudgetingRepo;
            _db = db;
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SaveBudget(
     [FromBody] SaveBudgetBatchRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request == null ||
                request.Programs == null ||
                request.Programs.Count == 0)
            {
                return BadRequest(new
                {
                    message = "At least one program is required."
                });
            }

            var financialYear = request.FinancialYear?.Trim();

            if (string.IsNullOrWhiteSpace(financialYear))
            {
                return BadRequest(new
                {
                    message = "Financial year is required."
                });
            }

            if (request.Programs.Any(x =>
                x == null ||
                x.ProgramId <= 0))
            {
                return BadRequest(new
                {
                    message = "One or more programs are invalid."
                });
            }

            var programIds = request.Programs
                .Select(x => x.ProgramId)
                .ToList();

            if (programIds.Distinct().Count() != programIds.Count)
            {
                return BadRequest(new
                {
                    message = "Duplicate programs are not allowed."
                });
            }

            // =====================================================
            // LOGGED-IN USER LOCATION
            // =====================================================

            var stateClaim =
                User.FindFirst("spic:state_id")?.Value;

            var regionClaim =
                User.FindFirst("spic:region_id")?.Value;

            if (!int.TryParse(
                    stateClaim,
                    out int stateId)
                ||
                stateId <= 0)
            {
                return Unauthorized(new
                {
                    message = "State not assigned for user."
                });
            }

            int? regionId = null;

            if (int.TryParse(
                    regionClaim,
                    out int parsedRegionId)
                &&
                parsedRegionId > 0)
            {
                regionId = parsedRegionId;
            }

            // =====================================================
            // LOGGED-IN USER ROLE
            // =====================================================

            var roleClaim =
                User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrWhiteSpace(roleClaim))
            {
                return Forbid();
            }

            bool isSMMUser =
                roleClaim == AppRole.SMM.ToString();

            bool isSMDUser =
                roleClaim == AppRole.SMD.ToString();

            bool isRMUser =
                roleClaim == AppRole.RM.ToString();

            bool isRMDUser =
                roleClaim == AppRole.RMD.ToString();

            bool isMOUser =
                roleClaim == AppRole.MO.ToString() ||
                roleClaim == AppRole.MDO.ToString() ||
                roleClaim == AppRole.JMDO.ToString();

            bool isStateRole =
                isSMMUser ||
                isSMDUser;

            bool isRegionRole =
                isRMUser ||
                isRMDUser;

            if (!isStateRole &&
                !isRegionRole &&
                !isMOUser)
            {
                return Forbid();
            }

            // Region roles must have RegionId.
            if (isRegionRole &&
                !regionId.HasValue)
            {
                return Unauthorized(new
                {
                    message = "Region not assigned for user."
                });
            }

            try
            {
                // =================================================
                // VALIDATE PROGRAMS AGAINST STATE + ROLE
                // =================================================

                var stateBudgets =
                    _programStateBudgetRepo.GetAll();

                var allowedProgramCount =
                    await _programRepo
                        .GetAll()

                        .Where(x =>
                            programIds.Contains(x.Id))

                        .Where(x =>
                            stateBudgets.Any(psb =>
                                psb.ProgramId == x.Id &&
                                psb.StateId == stateId
                            ))

                        .Where(x =>
                            (
                                isMOUser &&
                                x.IsMO == true
                            )
                            ||
                            (
                                isRegionRole &&
                                x.IsRMDO == true
                            )
                            ||
                            (
                                isStateRole &&
                                x.IsSMDO == true
                            )
                        )

                        .CountAsync();

                if (allowedProgramCount != programIds.Count)
                {
                    return BadRequest(new
                    {
                        message =
                            "One or more programs are invalid or are not " +
                            "allocated to your state and role."
                    });
                }

                // =================================================
                // VALIDATE PROGRAM VALUES
                // =================================================

                foreach (var item in request.Programs)
                {
                    if (item.TotalBudget < 0)
                    {
                        return BadRequest(new
                        {
                            message =
                                $"Budget amount cannot be negative " +
                                $"for program {item.ProgramId}."
                        });
                    }

                    var monthlyCounts = new[]
                    {
                item.AprilCount,
                item.MayCount,
                item.JuneCount,
                item.JulyCount,
                item.AugustCount,
                item.SeptemberCount,
                item.OctoberCount,
                item.NovemberCount,
                item.DecemberCount,
                item.JanuaryCount,
                item.FebruaryCount,
                item.MarchCount
            };

                    if (monthlyCounts.Any(x => x < 0))
                    {
                        return BadRequest(new
                        {
                            message =
                                $"Monthly counts cannot be negative " +
                                $"for program {item.ProgramId}."
                        });
                    }

                    var monthlyAmounts = new[]
                    {
                item.April,
                item.May,
                item.June,
                item.July,
                item.August,
                item.September,
                item.October,
                item.November,
                item.December,
                item.January,
                item.February,
                item.March
            };

                    if (monthlyAmounts.Any(x => x < 0))
                    {
                        return BadRequest(new
                        {
                            message =
                                $"Monthly amounts cannot be negative " +
                                $"for program {item.ProgramId}."
                        });
                    }
                }

                // =================================================
                // BUILD PROGRAM DETAIL RECORDS
                // =================================================

                var programDetails =
                    request.Programs
                        .Select(item =>
                            new BudgetProgram
                            {
                                ProgramId =
                                    item.ProgramId,

                                TotalBudget =
                                    item.TotalBudget,

                                AprilCount =
                                    item.AprilCount,

                                April =
                                    item.April,

                                MayCount =
                                    item.MayCount,

                                May =
                                    item.May,

                                JuneCount =
                                    item.JuneCount,

                                June =
                                    item.June,

                                JulyCount =
                                    item.JulyCount,

                                July =
                                    item.July,

                                AugustCount =
                                    item.AugustCount,

                                August =
                                    item.August,

                                SeptemberCount =
                                    item.SeptemberCount,

                                September =
                                    item.September,

                                OctoberCount =
                                    item.OctoberCount,

                                October =
                                    item.October,

                                NovemberCount =
                                    item.NovemberCount,

                                November =
                                    item.November,

                                DecemberCount =
                                    item.DecemberCount,

                                December =
                                    item.December,

                                JanuaryCount =
                                    item.JanuaryCount,

                                January =
                                    item.January,

                                FebruaryCount =
                                    item.FebruaryCount,

                                February =
                                    item.February,

                                MarchCount =
                                    item.MarchCount,

                                March =
                                    item.March
                            })
                        .ToList();

                // =================================================
                // CALCULATE TOTAL PROGRAM ALLOCATION
                // =================================================

                decimal allocatedAmount =
                    programDetails.Sum(x =>
                        x.April +
                        x.May +
                        x.June +
                        x.July +
                        x.August +
                        x.September +
                        x.October +
                        x.November +
                        x.December +
                        x.January +
                        x.February +
                        x.March
                    );

                // =================================================
                // VALIDATE AVAILABLE ROLE BUDGET
                // =================================================

                if (request.ApprovedAmount <= 0)
                {
                    return BadRequest(new
                    {
                        message =
                            "Available budget must be greater than 0."
                    });
                }

                if (allocatedAmount <= 0)
                {
                    return BadRequest(new
                    {
                        message =
                            "Please allocate budget for at least one program."
                    });
                }

                /*
                 * IMPORTANT:
                 *
                 * User does NOT have to consume the full budget.
                 *
                 * Example:
                 * Available = 20L
                 * Allocated = 15L
                 * Remaining = 5L
                 *
                 * This is valid.
                 */
                if (allocatedAmount > request.ApprovedAmount)
                {
                    return BadRequest(new
                    {
                        message =
                            $"Allocated Budget ₹{allocatedAmount:N0} " +
                            $"cannot exceed Available Budget " +
                            $"₹{request.ApprovedAmount:N0}."
                    });
                }

                decimal remainingAmount =
                    request.ApprovedAmount -
                    allocatedAmount;

                // =================================================
                // CREATE PARENT BUDGET RECORD
                // =================================================

                var main =
                    new BudgetProgramMains
                    {
                        Status = "Draft",

                        StateId =
                            stateId,

                        /*
                         * RegionId:
                         *
                         * RM / RMD -> actual RegionId
                         * SMM / SMD / MO -> nullable
                         *
                         * If you want MO also tied to Region,
                         * this can simply remain regionId.
                         */
                        RegionId =
                            regionId,

                        FinancialYear =
                            financialYear,

                        CreatedBy =
                            CurrentUser,

                        CreatedAt =
                            DateTime.Now,

                        ValidateBy =
                            "",

                        ValidateAt =
                            null,

                        ApprovedBy =
                            "",

                        ApprovedAt =
                            null,

                        // Role's available budget
                        ApprovedAmount =
                            request.ApprovedAmount,

                        // Amount distributed into programs
                        AllocatedAmount =
                            allocatedAmount,

                        // Balance after program allocation
                        SIDAmount =
                            remainingAmount,

                        Programs =
                            programDetails
                    };

                _db.Set<BudgetProgramMains>()
                    .Add(main);

                await _db.SaveChangesAsync();

                // =================================================
                // RESPONSE
                // =================================================

                return Ok(new
                {
                    message =
                        "Budget created successfully.",

                    data = new
                    {
                        BudgetProgramMainId =
                            main.Id,

                        main.StateId,

                        main.RegionId,

                        Role =
                            roleClaim,

                        main.Status,

                        main.FinancialYear,

                        AvailableBudget =
                            main.ApprovedAmount,

                        main.AllocatedAmount,

                        RemainingAmount =
                            main.SIDAmount,

                        ProgramCount =
                            programDetails.Count
                    }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);

                return StatusCode(
                    500,
                    new
                    {
                        message =
                            "Budget save could not be confirmed. " +
                            "Check the saved budget list and " +
                            "server logs before retrying."
                    });
            }
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
           
            //entity.UpdatedBy = CurrentUser;
            //entity.UpdatedAt = DateTime.Now;

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

        
        [Authorize]
        [HttpGet("program-master-list")]
        public async Task<IActionResult> GetProgramMasterList()
        {
            // Get the logged-in user's state.
            var stateClaim = User.FindFirst("spic:state_id")?.Value;

            if (!int.TryParse(stateClaim, out int stateId))
            {
                return Unauthorized("State not assigned for user");
            }

            // Get the logged-in user's role name.
            var roleClaim = User.FindFirst(
                System.Security.Claims.ClaimTypes.Role
            )?.Value;

            // Do not allow access when the role is missing.
            if (string.IsNullOrWhiteSpace(roleClaim))
            {
                return Forbid();
            }

            // SMM / SMD users: check ProgramMasters.IsSMDO.
            bool isSMDOUser =
                roleClaim == AppRole.SMM.ToString() ||
                roleClaim == AppRole.SMD.ToString();

            // RM / RMD users: check ProgramMasters.IsRMDO.
            bool isRMDOUser =
                roleClaim == AppRole.RM.ToString() ||
                roleClaim == AppRole.RMD.ToString();

            // MO / MDO / JMDO users: check ProgramMasters.IsMO.
            bool isMOUser =
                roleClaim == AppRole.MO.ToString() ||
                roleClaim == AppRole.MDO.ToString() ||
                roleClaim == AppRole.JMDO.ToString();

            // No automatic access for other roles.
            if (!isSMDOUser && !isRMDOUser && !isMOUser)
            {
                return Forbid();
            }

            var stateBudgets = _programStateBudgetRepo.GetAll();
            var existingBudgets = _budgetRepo.GetAll();

            var programs = await _programRepo
                .GetAll()
                .Include(x => x.ProgramType)

                // 1. Only programs allocated to the logged-in user's state.
                .Where(x => stateBudgets.Any(psb =>
                    psb.ProgramId == x.Id &&
                    psb.StateId == stateId
                ))

                // 2. Only programs enabled for the logged-in user's role.
                .Where(x =>
                    (isMOUser && x.IsMO == true) ||
                    (isRMDOUser && x.IsRMDO == true) ||
                    (isSMDOUser && x.IsSMDO == true)
                )

                .Select(x => new
                {
                    Program = x,

                    // State-wise program budget.
                    StateBudget = stateBudgets
                        .FirstOrDefault(psb =>
                            psb.ProgramId == x.Id &&
                            psb.StateId == stateId
                        ),

                    // Existing monthly budget selection: unchanged.
                    Budget = existingBudgets
                        .Where(b => b.ProgramId == x.Id)
                        //.OrderByDescending(b => b.UpdatedAt)
                        .FirstOrDefault()
                })

                .Select(x => new ProgramWiseBudgetDto
                {
                    ProgramId = x.Program.Id,

                    ProgramTypeId = x.Program.ProgramTypeId,

                    ProgramType = x.Program.ProgramType != null
                        ? x.Program.ProgramType.Name
                        : "",

                    ProgramName = x.Program.Name,

                    // State-wise program budget.
                    BudgetAmount = x.StateBudget != null
                        ? x.StateBudget.BudgetAmount
                        : 0,

                    IsChangeAmount =
                        x.StateBudget != null &&
                        x.StateBudget.BudgetAmount > 0
                            ? false
                            : true,

                    // Existing total budget logic.
                    TotalBudget = x.Budget != null
                        ? x.Budget.TotalBudget
                        : (x.StateBudget != null
                            ? x.StateBudget.BudgetAmount
                            : 0),

                    // Monthly counts.
                    AprilCount = x.Budget != null ? x.Budget.AprilCount : 0,
                    MayCount = x.Budget != null ? x.Budget.MayCount : 0,
                    JuneCount = x.Budget != null ? x.Budget.JuneCount : 0,
                    JulyCount = x.Budget != null ? x.Budget.JulyCount : 0,
                    AugustCount = x.Budget != null ? x.Budget.AugustCount : 0,
                    SeptemberCount = x.Budget != null ? x.Budget.SeptemberCount : 0,
                    OctoberCount = x.Budget != null ? x.Budget.OctoberCount : 0,
                    NovemberCount = x.Budget != null ? x.Budget.NovemberCount : 0,
                    DecemberCount = x.Budget != null ? x.Budget.DecemberCount : 0,
                    JanuaryCount = x.Budget != null ? x.Budget.JanuaryCount : 0,
                    FebruaryCount = x.Budget != null ? x.Budget.FebruaryCount : 0,
                    MarchCount = x.Budget != null ? x.Budget.MarchCount : 0,

                    // Monthly budget amounts.
                    AprilBudget = x.Budget != null ? x.Budget.April : 0,
                    MayBudget = x.Budget != null ? x.Budget.May : 0,
                    JuneBudget = x.Budget != null ? x.Budget.June : 0,
                    JulyBudget = x.Budget != null ? x.Budget.July : 0,
                    AugustBudget = x.Budget != null ? x.Budget.August : 0,
                    SeptemberBudget = x.Budget != null ? x.Budget.September : 0,
                    OctoberBudget = x.Budget != null ? x.Budget.October : 0,
                    NovemberBudget = x.Budget != null ? x.Budget.November : 0,
                    DecemberBudget = x.Budget != null ? x.Budget.December : 0,
                    JanuaryBudget = x.Budget != null ? x.Budget.January : 0,
                    FebruaryBudget = x.Budget != null ? x.Budget.February : 0,
                    MarchBudget = x.Budget != null ? x.Budget.March : 0
                })
                .OrderBy(x => x.ProgramType)
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
                    x.ProgramId == model.ProgramId //&&
                    //x.FinancialYear == model.FinancialYear &&
                    //x.Status == "Draft"
                );


            if (existing != null)
            {
                existing.TotalBudget = model.TotalBudget;

                existing.AprilCount = model.AprilCount;
                existing.April = model.April;

                existing.MayCount = model.MayCount;
                existing.May = model.May;

                //existing.UpdatedBy = CurrentUser;
               // existing.UpdatedAt = DateTime.Now;


                await _budgetRepo.UpdateAsync(existing);
            }
            else
            {
                //model.Status = "Draft";
                //model.CreatedBy = CurrentUser;
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
                //.Where(x => x.Status == "Draft")
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


                   // SubmittedDate =
                        //x.CreatedAt,


                    //Status =
                       // x.Status,


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


                    Approved =0,
                        //x.Count(a => a.Status == "Approved"),


                    Pending =0,
                        //x.Count(a => a.Status == "Pending"),


                    Rejected =0,
                       // x.Count(a => a.Status == "Rejected"),


                    Draft =0
                      //  x.Count(a => a.Status == "Draft")

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

            if (annualBudget.Amount <= 0)
                return BadRequest(new { message = "Total Budget must be greater than 0." });

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

            // Link to the Level 1 summary for this FY, if one already exists (Summary-to-Detail
            // relationship key - see StateBudgetSummaryId doc comment). Saving the summary itself
            // backfills this the other way round, so it stays correct regardless of save order.
            var stateSummaryForLink = await _db.Set<StateBudgetSummary>().FirstOrDefaultAsync(s => s.FY == fy);

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
                    row.StateBudgetSummary = stateSummaryForLink;
                }
                else
                {
                    _db.Set<StateBudgetAllocation>().Add(new StateBudgetAllocation
                    {
                        StateId = alloc.StateId,
                        FY = fy,
                        Amount = alloc.Amount,
                        StateBudgetSummary = stateSummaryForLink,
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
        /// Submit For Validation: marks every saved state allocation for the FY as "Submitted",
        /// and cascades the same submission to every saved Region and Headquarters allocation
        /// for that FY (the existing lifecycle is reused as-is - there is no separate Region or
        /// Headquarters submission action). Reuses BudgetProgram.Status's existing string-status
        /// convention - no new workflow, no new status values. Re-validates against the
        /// AnnualBudgeting amount independently of Save Draft (defense in depth) and refuses a
        /// second submission for the same FY.
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
                _db.Set<StateBudgetAllocationHistory>().Add(ToStateHistory(row, "Submitted"));
            }

            var regionRows = await _db.Set<RegionBudgetAllocation>()
                .Where(a => a.FY == fy && a.Status != "Submitted")
                .ToListAsync();

            foreach (var row in regionRows)
            {
                row.Status = "Submitted";
                row.UpdatedBy = CurrentUser;
                row.UpdatedAt = DateTime.Now;
                _db.Set<RegionBudgetAllocationHistory>().Add(ToRegionHistory(row, "Submitted"));
            }

            var hqRows = await _db.Set<HeadquarterBudgetAllocation>()
                .Where(a => a.FY == fy && a.Status != "Submitted")
                .ToListAsync();

            foreach (var row in hqRows)
            {
                row.Status = "Submitted";
                row.UpdatedBy = CurrentUser;
                row.UpdatedAt = DateTime.Now;
                _db.Set<HeadquarterBudgetAllocationHistory>().Add(ToHeadquarterHistory(row, "Submitted"));
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                message = $"State budget allocation for FY {fy} submitted for validation successfully."
            });
        }

        /// <summary>
        /// Validate: moves every "Submitted" State/Region/Headquarters allocation for the FY to
        /// "Validated" (same State-level cascade convention as Submit For Validation - there is
        /// no separate Region or Headquarters validation action). Writes one history snapshot per
        /// row transitioned, preserving the complete approval trail.
        /// </summary>
        [HttpPut("state-budget/validate")]
        public async Task<IActionResult> ValidateStateBudget([FromBody] ValidateStateBudgetRequest request)
        {
            var fy = (request.FY ?? string.Empty).Trim();
            if (fy.Length == 0)
                return BadRequest(new { message = "Financial Year is required." });

            var rows = await _db.Set<StateBudgetAllocation>()
                .Where(a => a.FY == fy && a.Status == "Submitted")
                .ToListAsync();

            if (rows.Count == 0)
                return BadRequest(new { message = $"No submitted allocation found for FY {fy} to validate." });

            await using var transaction = await _db.Database.BeginTransactionAsync();

            var now = DateTime.Now;

            foreach (var row in rows)
            {
                row.Status = "Validated";
                row.ValidatedBy = CurrentUser;
                row.ValidatedDate = now;
                row.UpdatedBy = CurrentUser;
                row.UpdatedAt = now;
                _db.Set<StateBudgetAllocationHistory>().Add(ToStateHistory(row, "Validated"));
            }

            var regionRows = await _db.Set<RegionBudgetAllocation>()
                .Where(a => a.FY == fy && a.Status == "Submitted")
                .ToListAsync();

            foreach (var row in regionRows)
            {
                row.Status = "Validated";
                row.ValidatedBy = CurrentUser;
                row.ValidatedDate = now;
                row.UpdatedBy = CurrentUser;
                row.UpdatedAt = now;
                _db.Set<RegionBudgetAllocationHistory>().Add(ToRegionHistory(row, "Validated"));
            }

            var hqRows = await _db.Set<HeadquarterBudgetAllocation>()
                .Where(a => a.FY == fy && a.Status == "Submitted")
                .ToListAsync();

            foreach (var row in hqRows)
            {
                row.Status = "Validated";
                row.ValidatedBy = CurrentUser;
                row.ValidatedDate = now;
                row.UpdatedBy = CurrentUser;
                row.UpdatedAt = now;
                _db.Set<HeadquarterBudgetAllocationHistory>().Add(ToHeadquarterHistory(row, "Validated"));
            }

            // Same FY-wide cascade, applied to the Total Budget / Allocated / Remaining
            // summary rows alongside the detail allocation rows above - no separate
            // approval workflow, just the existing one reaching the summary records too.
            var stateSummary = await _db.Set<StateBudgetSummary>()
                .FirstOrDefaultAsync(s => s.FY == fy && s.Status == "Submitted");
            if (stateSummary != null)
            {
                stateSummary.Status = "Validated";
                stateSummary.ValidatedBy = CurrentUser;
                stateSummary.ValidatedDate = now;
                stateSummary.UpdatedBy = CurrentUser;
                stateSummary.UpdatedAt = now;
                _db.Set<StateBudgetSummaryHistory>().Add(ToStateSummaryHistory(stateSummary, "Validated"));
            }

            var regionSummaries = await _db.Set<RegionBudgetSummary>()
                .Where(s => s.FY == fy && s.Status == "Submitted")
                .ToListAsync();
            foreach (var row in regionSummaries)
            {
                row.Status = "Validated";
                row.ValidatedBy = CurrentUser;
                row.ValidatedDate = now;
                row.UpdatedBy = CurrentUser;
                row.UpdatedAt = now;
                _db.Set<RegionBudgetSummaryHistory>().Add(ToRegionSummaryHistory(row, "Validated"));
            }

            var hqSummaries = await _db.Set<HeadquarterBudgetSummary>()
                .Where(s => s.FY == fy && s.Status == "Submitted")
                .ToListAsync();
            foreach (var row in hqSummaries)
            {
                row.Status = "Validated";
                row.ValidatedBy = CurrentUser;
                row.ValidatedDate = now;
                row.UpdatedBy = CurrentUser;
                row.UpdatedAt = now;
                _db.Set<HeadquarterBudgetSummaryHistory>().Add(ToHeadquarterSummaryHistory(row, "Validated"));
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                message = $"State budget allocation for FY {fy} validated successfully."
            });
        }

        /// <summary>
        /// Approve: moves every "Validated" State/Region/Headquarters allocation for the FY to
        /// "Approved" (same State-level cascade convention as Submit/Validate). Writes one
        /// history snapshot per row transitioned, preserving the complete approval trail.
        /// </summary>
        [HttpPut("state-budget/approve")]
        public async Task<IActionResult> ApproveStateBudget([FromBody] ApproveStateBudgetRequest request)
        {
            var fy = (request.FY ?? string.Empty).Trim();
            if (fy.Length == 0)
                return BadRequest(new { message = "Financial Year is required." });

            var rows = await _db.Set<StateBudgetAllocation>()
                .Where(a => a.FY == fy && a.Status == "Validated")
                .ToListAsync();

            if (rows.Count == 0)
                return BadRequest(new { message = $"No validated allocation found for FY {fy} to approve." });

            await using var transaction = await _db.Database.BeginTransactionAsync();

            var now = DateTime.Now;

            foreach (var row in rows)
            {
                row.Status = "Approved";
                row.ApprovedBy = CurrentUser;
                row.ApprovedDate = now;
                row.UpdatedBy = CurrentUser;
                row.UpdatedAt = now;
                _db.Set<StateBudgetAllocationHistory>().Add(ToStateHistory(row, "Approved"));
            }

            var regionRows = await _db.Set<RegionBudgetAllocation>()
                .Where(a => a.FY == fy && a.Status == "Validated")
                .ToListAsync();

            foreach (var row in regionRows)
            {
                row.Status = "Approved";
                row.ApprovedBy = CurrentUser;
                row.ApprovedDate = now;
                row.UpdatedBy = CurrentUser;
                row.UpdatedAt = now;
                _db.Set<RegionBudgetAllocationHistory>().Add(ToRegionHistory(row, "Approved"));
            }

            var hqRows = await _db.Set<HeadquarterBudgetAllocation>()
                .Where(a => a.FY == fy && a.Status == "Validated")
                .ToListAsync();

            foreach (var row in hqRows)
            {
                row.Status = "Approved";
                row.ApprovedBy = CurrentUser;
                row.ApprovedDate = now;
                row.UpdatedBy = CurrentUser;
                row.UpdatedAt = now;
                _db.Set<HeadquarterBudgetAllocationHistory>().Add(ToHeadquarterHistory(row, "Approved"));
            }

            // Same FY-wide cascade, applied to the Total Budget / Allocated / Remaining
            // summary rows alongside the detail allocation rows above - no separate
            // approval workflow, just the existing one reaching the summary records too.
            var stateSummary = await _db.Set<StateBudgetSummary>()
                .FirstOrDefaultAsync(s => s.FY == fy && s.Status == "Validated");
            if (stateSummary != null)
            {
                stateSummary.Status = "Approved";
                stateSummary.ApprovedBy = CurrentUser;
                stateSummary.ApprovedDate = now;
                stateSummary.UpdatedBy = CurrentUser;
                stateSummary.UpdatedAt = now;
                _db.Set<StateBudgetSummaryHistory>().Add(ToStateSummaryHistory(stateSummary, "Approved"));
            }

            var regionSummaries = await _db.Set<RegionBudgetSummary>()
                .Where(s => s.FY == fy && s.Status == "Validated")
                .ToListAsync();
            foreach (var row in regionSummaries)
            {
                row.Status = "Approved";
                row.ApprovedBy = CurrentUser;
                row.ApprovedDate = now;
                row.UpdatedBy = CurrentUser;
                row.UpdatedAt = now;
                _db.Set<RegionBudgetSummaryHistory>().Add(ToRegionSummaryHistory(row, "Approved"));
            }

            var hqSummaries = await _db.Set<HeadquarterBudgetSummary>()
                .Where(s => s.FY == fy && s.Status == "Validated")
                .ToListAsync();
            foreach (var row in hqSummaries)
            {
                row.Status = "Approved";
                row.ApprovedBy = CurrentUser;
                row.ApprovedDate = now;
                row.UpdatedBy = CurrentUser;
                row.UpdatedAt = now;
                _db.Set<HeadquarterBudgetSummaryHistory>().Add(ToHeadquarterSummaryHistory(row, "Approved"));
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                message = $"State budget allocation for FY {fy} approved successfully."
            });
        }

        private static StateBudgetAllocationHistory ToStateHistory(StateBudgetAllocation row, string action) => new()
        {
            StateBudgetAllocationId = row.Id,
            StateId = row.StateId,
            FY = row.FY,
            Amount = row.Amount,
            Status = row.Status,
            CreatedBy = row.CreatedBy,
            CreatedAt = row.CreatedAt,
            ValidatedBy = row.ValidatedBy,
            ValidatedDate = row.ValidatedDate,
            ApprovedBy = row.ApprovedBy,
            ApprovedDate = row.ApprovedDate,
            Action = action,
            ActionDate = DateTime.Now
        };

        private static RegionBudgetAllocationHistory ToRegionHistory(RegionBudgetAllocation row, string action) => new()
        {
            RegionBudgetAllocationId = row.Id,
            StateId = row.StateId,
            RegionId = row.RegionId,
            FY = row.FY,
            Amount = row.Amount,
            Status = row.Status,
            CreatedBy = row.CreatedBy,
            CreatedAt = row.CreatedAt,
            ValidatedBy = row.ValidatedBy,
            ValidatedDate = row.ValidatedDate,
            ApprovedBy = row.ApprovedBy,
            ApprovedDate = row.ApprovedDate,
            Action = action,
            ActionDate = DateTime.Now
        };

        private static HeadquarterBudgetAllocationHistory ToHeadquarterHistory(HeadquarterBudgetAllocation row, string action) => new()
        {
            HeadquarterBudgetAllocationId = row.Id,
            RegionId = row.RegionId,
            HeadquarterId = row.HeadquarterId,
            FY = row.FY,
            Amount = row.Amount,
            Status = row.Status,
            CreatedBy = row.CreatedBy,
            CreatedAt = row.CreatedAt,
            ValidatedBy = row.ValidatedBy,
            ValidatedDate = row.ValidatedDate,
            ApprovedBy = row.ApprovedBy,
            ApprovedDate = row.ApprovedDate,
            Action = action,
            ActionDate = DateTime.Now
        };

        /// <summary>
        /// Region-wise budget rows for the State Budget Management page's Region Allocation
        /// section, scoped to one State + FY. Amount for each region is its persisted
        /// RegionBudgetAllocation for that FY (0 if none saved yet), so a page reload always
        /// reflects what was actually saved to the database.
        /// </summary>
        [HttpGet("regions-budget")]
        public async Task<IActionResult> GetRegionsBudget([FromQuery] int stateId, [FromQuery] string? fy)
        {
            var allocations = string.IsNullOrWhiteSpace(fy)
                ? new Dictionary<int, decimal>()
                : await _db.Set<RegionBudgetAllocation>()
                    .Where(a => a.StateId == stateId && a.FY == fy)
                    .ToDictionaryAsync(a => a.RegionId, a => a.Amount);

            var regions = await _db.Set<Region>()
                .Where(x => x.StateId == stateId && x.IsActive)
                .OrderBy(x => x.RegionName)
                .Select(x => new { x.Id, x.RegionName })
                .ToListAsync();

            var result = regions.Select(x => new RegionBudgetDto
            {
                RegionId = x.Id,
                RegionName = x.RegionName,
                BudgetAmount = allocations.TryGetValue(x.Id, out var amount) ? amount : 0m
            }).ToList();

            return Ok(result);
        }

        /// <summary>
        /// Full replace of every region's allocation for one State+FY (same full-replace
        /// convention as Save Draft for states, so an edited row's own previous value can
        /// never be double-counted). Independently validates: FY/State exist, every region
        /// is valid and belongs to the given State, no negative amounts, no duplicate region
        /// entries in the request, a State Budget Allocation exists for that FY, and the
        /// total never exceeds that State's allocated amount for the same FY.
        /// </summary>
        [HttpPut("region-budget")]
        public async Task<IActionResult> SaveRegionBudget([FromBody] SaveRegionBudgetRequest request)
        {
            var fy = (request.FY ?? string.Empty).Trim();
            if (fy.Length == 0)
                return BadRequest(new { message = "Financial Year is required." });

            if (request.StateId <= 0)
                return BadRequest(new { message = "A State must be selected." });

            if (request.Allocations == null || request.Allocations.Count == 0)
                return BadRequest(new { message = "No region allocations to save." });

            if (request.Allocations.Any(a => a.Amount < 0))
                return BadRequest(new { message = "Allocation amount cannot be negative." });

            if (request.Allocations.Select(a => a.RegionId).Distinct().Count() != request.Allocations.Count)
                return BadRequest(new { message = "Duplicate region entries in the request." });

            var state = await _stateRepo.GetAll().FirstOrDefaultAsync(s => s.Id == request.StateId);
            if (state == null)
                return BadRequest(new { message = "Selected state is invalid or inactive." });

            var stateAllocation = await _db.Set<StateBudgetAllocation>()
                .FirstOrDefaultAsync(a => a.StateId == request.StateId && a.FY == fy);
            if (stateAllocation == null)
                return BadRequest(new { message = $"No State Budget Allocation is saved for {state.StateName} in FY {fy}." });

            if (stateAllocation.Amount <= 0)
                return BadRequest(new { message = "Total Budget must be greater than 0." });

            var regionIds = request.Allocations.Select(a => a.RegionId).ToList();
            var regions = await _db.Set<Region>()
                .Where(r => regionIds.Contains(r.Id))
                .ToListAsync();

            if (regions.Count != regionIds.Distinct().Count())
                return BadRequest(new { message = "One or more regions are invalid." });

            if (regions.Any(r => r.StateId != request.StateId))
                return BadRequest(new { message = "One or more regions do not belong to the selected state." });

            var totalRequested = request.Allocations.Sum(a => a.Amount);
            if (totalRequested > stateAllocation.Amount)
                return BadRequest(new
                {
                    message = $"Region allocation cannot exceed the remaining state budget of ₹{stateAllocation.Amount:N0}."
                });

            await using var transaction = await _db.Database.BeginTransactionAsync();

            // Link to the Level 2 summary for this State+FY, if one already exists (Summary-to-Detail
            // relationship key - see RegionBudgetSummaryId doc comment). Saving the summary itself
            // backfills this the other way round, so it stays correct regardless of save order.
            var regionSummaryForLink = await _db.Set<RegionBudgetSummary>()
                .FirstOrDefaultAsync(s => s.StateId == request.StateId && s.FY == fy);

            var existing = await _db.Set<RegionBudgetAllocation>()
                .Where(a => a.FY == fy && regionIds.Contains(a.RegionId))
                .ToListAsync();
            var existingByRegionId = existing.ToDictionary(a => a.RegionId);

            foreach (var alloc in request.Allocations)
            {
                if (existingByRegionId.TryGetValue(alloc.RegionId, out var row))
                {
                    row.Amount = alloc.Amount;
                    row.UpdatedBy = CurrentUser;
                    row.UpdatedAt = DateTime.Now;
                    row.RegionBudgetSummary = regionSummaryForLink;
                }
                else
                {
                    _db.Set<RegionBudgetAllocation>().Add(new RegionBudgetAllocation
                    {
                        StateId = request.StateId,
                        RegionId = alloc.RegionId,
                        FY = fy,
                        Amount = alloc.Amount,
                        RegionBudgetSummary = regionSummaryForLink,
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
                message = "Region budget allocation saved successfully",
                totalAllocated = totalRequested,
                remaining = stateAllocation.Amount - totalRequested
            });
        }

        /// <summary>
        /// Headquarters-wise budget rows for the State Budget Management page's Headquarters
        /// Allocation section, scoped to one Region + FY. Amount for each headquarter is its
        /// persisted HeadquarterBudgetAllocation for that FY (0 if none saved yet), so a page
        /// reload always reflects what was actually saved to the database.
        /// </summary>
        [HttpGet("hq-budget")]
        public async Task<IActionResult> GetHeadquartersBudget([FromQuery] int regionId, [FromQuery] string? fy)
        {
            var allocations = string.IsNullOrWhiteSpace(fy)
                ? new Dictionary<int, decimal>()
                : await _db.Set<HeadquarterBudgetAllocation>()
                    .Where(a => a.RegionId == regionId && a.FY == fy)
                    .ToDictionaryAsync(a => a.HeadquarterId, a => a.Amount);

            var headquarters = await _headquarterRepo
                .GetAll()
                .Where(x => x.RegionId == regionId && x.IsActive)
                .OrderBy(x => x.HeadquarterName)
                .Select(x => new { x.Id, x.HeadquarterName })
                .ToListAsync();

            var result = headquarters.Select(x => new HeadquarterBudgetDto
            {
                HeadquarterId = x.Id,
                HeadquarterName = x.HeadquarterName,
                BudgetAmount = allocations.TryGetValue(x.Id, out var amount) ? amount : 0m
            }).ToList();

            return Ok(result);
        }

        /// <summary>
        /// Full replace of every headquarter's allocation for one Region+FY (same full-replace
        /// convention as Save Draft for states/regions, so an edited row's own previous value
        /// can never be double-counted). Independently validates: FY/Region exist, every
        /// headquarter is valid and belongs to the given Region, no negative amounts, no
        /// duplicate headquarter entries in the request, a Region Budget Allocation exists for
        /// that FY, and the total never exceeds that Region's allocated amount for the same FY.
        /// </summary>
        [HttpPut("hq-budget")]
        public async Task<IActionResult> SaveHeadquarterBudget([FromBody] SaveHeadquarterBudgetRequest request)
        {
            var fy = (request.FY ?? string.Empty).Trim();
            if (fy.Length == 0)
                return BadRequest(new { message = "Financial Year is required." });

            if (request.RegionId <= 0)
                return BadRequest(new { message = "A Region must be selected." });

            if (request.Allocations == null || request.Allocations.Count == 0)
                return BadRequest(new { message = "No headquarters allocations to save." });

            if (request.Allocations.Any(a => a.Amount < 0))
                return BadRequest(new { message = "Allocation amount cannot be negative." });

            if (request.Allocations.Select(a => a.HeadquarterId).Distinct().Count() != request.Allocations.Count)
                return BadRequest(new { message = "Duplicate headquarters entries in the request." });

            var region = await _db.Set<Region>().FirstOrDefaultAsync(r => r.Id == request.RegionId);
            if (region == null)
                return BadRequest(new { message = "Selected region is invalid or inactive." });

            var regionAllocation = await _db.Set<RegionBudgetAllocation>()
                .FirstOrDefaultAsync(a => a.RegionId == request.RegionId && a.FY == fy);
            if (regionAllocation == null)
                return BadRequest(new { message = $"No Region Budget Allocation is saved for {region.RegionName} in FY {fy}." });

            if (regionAllocation.Amount <= 0)
                return BadRequest(new { message = "Total Budget must be greater than 0." });

            var hqIds = request.Allocations.Select(a => a.HeadquarterId).ToList();
            var headquarters = await _headquarterRepo.GetAll()
                .Where(h => hqIds.Contains(h.Id))
                .ToListAsync();

            if (headquarters.Count != hqIds.Distinct().Count())
                return BadRequest(new { message = "One or more headquarters are invalid." });

            if (headquarters.Any(h => h.RegionId != request.RegionId))
                return BadRequest(new { message = "One or more headquarters do not belong to the selected region." });

            var totalRequested = request.Allocations.Sum(a => a.Amount);
            if (totalRequested > regionAllocation.Amount)
                return BadRequest(new
                {
                    message = $"Headquarters allocation cannot exceed the remaining region budget of ₹{regionAllocation.Amount:N0}."
                });

            await using var transaction = await _db.Database.BeginTransactionAsync();

            // Link to the Level 3 summary for this Region+FY, if one already exists (Summary-to-Detail
            // relationship key - see HeadquarterBudgetSummaryId doc comment). Saving the summary itself
            // backfills this the other way round, so it stays correct regardless of save order.
            var hqSummaryForLink = await _db.Set<HeadquarterBudgetSummary>()
                .FirstOrDefaultAsync(s => s.RegionId == request.RegionId && s.FY == fy);

            var existing = await _db.Set<HeadquarterBudgetAllocation>()
                .Where(a => a.FY == fy && hqIds.Contains(a.HeadquarterId))
                .ToListAsync();
            var existingByHqId = existing.ToDictionary(a => a.HeadquarterId);

            foreach (var alloc in request.Allocations)
            {
                if (existingByHqId.TryGetValue(alloc.HeadquarterId, out var row))
                {
                    row.Amount = alloc.Amount;
                    row.UpdatedBy = CurrentUser;
                    row.UpdatedAt = DateTime.Now;
                    row.HeadquarterBudgetSummary = hqSummaryForLink;
                }
                else
                {
                    _db.Set<HeadquarterBudgetAllocation>().Add(new HeadquarterBudgetAllocation
                    {
                        RegionId = request.RegionId,
                        HeadquarterId = alloc.HeadquarterId,
                        FY = fy,
                        Amount = alloc.Amount,
                        HeadquarterBudgetSummary = hqSummaryForLink,
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
                message = "Headquarters budget allocation saved successfully",
                totalAllocated = totalRequested,
                remaining = regionAllocation.Amount - totalRequested
            });
        }

        [HttpGet("state-allocated-budget")]
        public async Task<IActionResult> GetStateAllocatedBudget(int stateId)
        {
            var amount = await _stateBudgetAllocationRepo
                .GetAll()
                .Where(x => x.StateId == stateId)
                .Select(x => x.Amount)
                .FirstOrDefaultAsync();


            return Ok(amount);
        }

        // ================================================================================
        // 3-level budget allocation SUMMARY (Total Budget / Allocated / Remaining Amount).
        // Separate from the *BudgetAllocation detail tables above (state-budget, region-budget,
        // hq-budget), which keep working unchanged for the individual State/Region/HQ rows.
        // All three values here are plain manually-entered fields - Remaining Amount is never
        // recalculated from Total - Allocated, on either the client or the server.
        // ================================================================================

        /// <summary>Level 1 (Admin) summary for one FY. Zero/"Draft" when nothing has been saved yet.</summary>
        [HttpGet("state-budget-summary")]
        public async Task<IActionResult> GetStateBudgetSummary([FromQuery] string fy)
        {
            if (string.IsNullOrWhiteSpace(fy))
                return BadRequest(new { message = "Financial Year is required." });

            var summary = await _db.Set<StateBudgetSummary>().FirstOrDefaultAsync(s => s.FY == fy);

            return Ok(new StateBudgetSummaryDto
            {
                FY = fy,
                TotalBudget = summary?.TotalBudget ?? 0m,
                AllocatedAmount = summary?.AllocatedAmount ?? 0m,
                RemainingAmount = summary?.RemainingAmount ?? 0m,
                Status = summary?.Status ?? "Draft"
            });
        }

        /// <summary>
        /// Save (Draft) the Level 1 summary for one FY. Admin-only. All three values are
        /// persisted exactly as submitted.
        /// </summary>
        [HttpPut("state-budget-summary")]
        public async Task<IActionResult> SaveStateBudgetSummary([FromBody] SaveStateBudgetSummaryRequest request)
        {
            if (!IsAdminUser())
                return Forbid();

            var fy = (request.FY ?? string.Empty).Trim();
            if (fy.Length == 0)
                return BadRequest(new { message = "Financial Year is required." });

            // Save is just as authoritative as Submit: a row that doesn't reconcile is never
            // written to the database at all, Draft or otherwise.
            if (!TryValidateSummaryAmounts(request.TotalBudget, request.AllocatedAmount, request.RemainingAmount, out var validationError))
                return BadRequest(new { message = validationError });

            var summary = await _db.Set<StateBudgetSummary>().FirstOrDefaultAsync(s => s.FY == fy);
            if (summary == null)
            {
                summary = new StateBudgetSummary
                {
                    FY = fy,
                    CreatedBy = CurrentUser,
                    CreatedAt = DateTime.Now
                };
                _db.Set<StateBudgetSummary>().Add(summary);
            }

            summary.TotalBudget = request.TotalBudget;
            summary.AllocatedAmount = request.AllocatedAmount;
            summary.RemainingAmount = request.RemainingAmount;
            summary.UpdatedBy = CurrentUser;
            summary.UpdatedAt = DateTime.Now;

            // Save flow: link this summary to every existing detail row for the same FY (Summary-to-
            // Detail relationship key - see StateBudgetSummaryId doc comment). Uses the navigation,
            // not summary.Id directly, so EF fixes up the FK even for a brand new summary whose Id
            // isn't assigned until SaveChangesAsync below.
            var stateDetailRows = await _db.Set<StateBudgetAllocation>().Where(a => a.FY == fy).ToListAsync();
            foreach (var detailRow in stateDetailRows)
            {
                detailRow.StateBudgetSummary = summary;
            }

            await _db.SaveChangesAsync();

            return Ok(new { message = "State budget summary saved successfully." });
        }

        /// <summary>
        /// Submit the Level 1 summary for one FY. Admin-only. Re-validates TotalBudget =
        /// AllocatedAmount + RemainingAmount server-side (mandatory - the frontend check is UX
        /// only) before marking it "Submitted" and writing a history snapshot.
        /// </summary>
        [HttpPut("state-budget-summary/submit")]
        public async Task<IActionResult> SubmitStateBudgetSummary([FromBody] SubmitStateBudgetSummaryRequest request)
        {
            if (!IsAdminUser())
                return Forbid();

            var fy = (request.FY ?? string.Empty).Trim();
            if (fy.Length == 0)
                return BadRequest(new { message = "Financial Year is required." });

            var summary = await _db.Set<StateBudgetSummary>().FirstOrDefaultAsync(s => s.FY == fy);
            if (summary == null)
                return BadRequest(new { message = $"Save the state budget summary for FY {fy} before submitting." });

            // Same rule set as Save - re-validated independently here since Submit is the
            // authoritative gate for progressing Status, regardless of what Save already enforced.
            if (!TryValidateSummaryAmounts(summary.TotalBudget, summary.AllocatedAmount, summary.RemainingAmount, out var validationError))
                return BadRequest(new { message = validationError });

            summary.Status = "Submitted";
            summary.UpdatedBy = CurrentUser;
            summary.UpdatedAt = DateTime.Now;
            _db.Set<StateBudgetSummaryHistory>().Add(ToStateSummaryHistory(summary, "Submitted"));

            await _db.SaveChangesAsync();

            return Ok(new { message = $"State budget summary for FY {fy} submitted successfully." });
        }

        /// <summary>Level 2 (SM) summary for one State+FY. Zero/"Draft" when nothing has been saved yet.</summary>
        [HttpGet("region-budget-summary")]
        public async Task<IActionResult> GetRegionBudgetSummary([FromQuery] int stateId, [FromQuery] string fy)
        {
            if (string.IsNullOrWhiteSpace(fy))
                return BadRequest(new { message = "Financial Year is required." });

            var summary = await _db.Set<RegionBudgetSummary>()
                .FirstOrDefaultAsync(s => s.StateId == stateId && s.FY == fy);

            return Ok(new RegionBudgetSummaryDto
            {
                StateId = stateId,
                FY = fy,
                TotalBudget = summary?.TotalBudget ?? 0m,
                AllocatedAmount = summary?.AllocatedAmount ?? 0m,
                RemainingAmount = summary?.RemainingAmount ?? 0m,
                Status = summary?.Status ?? "Draft"
            });
        }

        /// <summary>
        /// Save (Draft) the Level 2 summary for one State+FY. Admin or SM. All three values are
        /// persisted exactly as submitted.
        /// </summary>
        [HttpPut("region-budget-summary")]
        public async Task<IActionResult> SaveRegionBudgetSummary([FromBody] SaveRegionBudgetSummaryRequest request)
        {
            if (!IsAdminUser() && !IsSmUser())
                return Forbid();

            var fy = (request.FY ?? string.Empty).Trim();
            if (fy.Length == 0)
                return BadRequest(new { message = "Financial Year is required." });

            if (request.StateId <= 0)
                return BadRequest(new { message = "A State must be selected." });

            // Save is just as authoritative as Submit: a row that doesn't reconcile is never
            // written to the database at all, Draft or otherwise.
            if (!TryValidateSummaryAmounts(request.TotalBudget, request.AllocatedAmount, request.RemainingAmount, out var validationError))
                return BadRequest(new { message = validationError });

            var state = await _stateRepo.GetAll().FirstOrDefaultAsync(s => s.Id == request.StateId);
            if (state == null)
                return BadRequest(new { message = "Selected state is invalid or inactive." });

            var stateAllocationForSave = await _db.Set<StateBudgetAllocation>()
                .FirstOrDefaultAsync(a => a.StateId == request.StateId && a.FY == fy);
            if (stateAllocationForSave != null && request.TotalBudget > stateAllocationForSave.Amount)
                return BadRequest(new
                {
                    message = $"Total Budget cannot exceed the applicable State budget of ₹{stateAllocationForSave.Amount:N0}."
                });

            var summary = await _db.Set<RegionBudgetSummary>()
                .FirstOrDefaultAsync(s => s.StateId == request.StateId && s.FY == fy);
            if (summary == null)
            {
                summary = new RegionBudgetSummary
                {
                    StateId = request.StateId,
                    FY = fy,
                    CreatedBy = CurrentUser,
                    CreatedAt = DateTime.Now
                };
                _db.Set<RegionBudgetSummary>().Add(summary);
            }

            summary.TotalBudget = request.TotalBudget;
            summary.AllocatedAmount = request.AllocatedAmount;
            summary.RemainingAmount = request.RemainingAmount;
            summary.UpdatedBy = CurrentUser;
            summary.UpdatedAt = DateTime.Now;

            // Save flow: link this summary to every existing detail row for the same State+FY
            // (Summary-to-Detail relationship key - see RegionBudgetSummaryId doc comment). Uses the
            // navigation, not summary.Id directly, so EF fixes up the FK even for a brand new summary
            // whose Id isn't assigned until SaveChangesAsync below.
            var regionDetailRows = await _db.Set<RegionBudgetAllocation>()
                .Where(a => a.StateId == request.StateId && a.FY == fy)
                .ToListAsync();
            foreach (var detailRow in regionDetailRows)
            {
                detailRow.RegionBudgetSummary = summary;
            }

            await _db.SaveChangesAsync();

            return Ok(new { message = "Region budget summary saved successfully." });
        }

        /// <summary>
        /// Submit the Level 2 summary for one State+FY. Admin or SM. Re-validates TotalBudget =
        /// AllocatedAmount + RemainingAmount, and that TotalBudget does not exceed the
        /// applicable State's own allocated amount (StateBudgetAllocation.Amount), before
        /// marking it "Submitted" and writing a history snapshot.
        /// </summary>
        [HttpPut("region-budget-summary/submit")]
        public async Task<IActionResult> SubmitRegionBudgetSummary([FromBody] SubmitRegionBudgetSummaryRequest request)
        {
            if (!IsAdminUser() && !IsSmUser())
                return Forbid();

            var fy = (request.FY ?? string.Empty).Trim();
            if (fy.Length == 0)
                return BadRequest(new { message = "Financial Year is required." });

            if (request.StateId <= 0)
                return BadRequest(new { message = "A State must be selected." });

            var summary = await _db.Set<RegionBudgetSummary>()
                .FirstOrDefaultAsync(s => s.StateId == request.StateId && s.FY == fy);
            if (summary == null)
                return BadRequest(new { message = $"Save the region budget summary for FY {fy} before submitting." });

            // Same rule set as Save - re-validated independently here since Submit is the
            // authoritative gate for progressing Status, regardless of what Save already enforced.
            if (!TryValidateSummaryAmounts(summary.TotalBudget, summary.AllocatedAmount, summary.RemainingAmount, out var validationError))
                return BadRequest(new { message = validationError });

            var stateAllocation = await _db.Set<StateBudgetAllocation>()
                .FirstOrDefaultAsync(a => a.StateId == request.StateId && a.FY == fy);
            if (stateAllocation != null && summary.TotalBudget > stateAllocation.Amount)
                return BadRequest(new
                {
                    message = $"Total Budget cannot exceed the applicable State budget of ₹{stateAllocation.Amount:N0}."
                });

            summary.Status = "Submitted";
            summary.UpdatedBy = CurrentUser;
            summary.UpdatedAt = DateTime.Now;
            _db.Set<RegionBudgetSummaryHistory>().Add(ToRegionSummaryHistory(summary, "Submitted"));

            await _db.SaveChangesAsync();

            return Ok(new { message = $"Region budget summary for FY {fy} submitted successfully." });
        }

        /// <summary>Level 3 (RM) summary for one Region+FY. Zero/"Draft" when nothing has been saved yet.</summary>
        [HttpGet("hq-budget-summary")]
        public async Task<IActionResult> GetHeadquarterBudgetSummary([FromQuery] int regionId, [FromQuery] string fy)
        {
            if (string.IsNullOrWhiteSpace(fy))
                return BadRequest(new { message = "Financial Year is required." });

            var summary = await _db.Set<HeadquarterBudgetSummary>()
                .FirstOrDefaultAsync(s => s.RegionId == regionId && s.FY == fy);

            return Ok(new HeadquarterBudgetSummaryDto
            {
                RegionId = regionId,
                FY = fy,
                TotalBudget = summary?.TotalBudget ?? 0m,
                AllocatedAmount = summary?.AllocatedAmount ?? 0m,
                RemainingAmount = summary?.RemainingAmount ?? 0m,
                Status = summary?.Status ?? "Draft"
            });
        }

        /// <summary>
        /// Save (Draft) the Level 3 summary for one Region+FY. Admin or RM. All three values
        /// are persisted exactly as submitted.
        /// </summary>
        [HttpPut("hq-budget-summary")]
        public async Task<IActionResult> SaveHeadquarterBudgetSummary([FromBody] SaveHeadquarterBudgetSummaryRequest request)
        {
            if (!IsAdminUser() && !IsRmUser())
                return Forbid();

            var fy = (request.FY ?? string.Empty).Trim();
            if (fy.Length == 0)
                return BadRequest(new { message = "Financial Year is required." });

            if (request.RegionId <= 0)
                return BadRequest(new { message = "A Region must be selected." });

            // Save is just as authoritative as Submit: a row that doesn't reconcile is never
            // written to the database at all, Draft or otherwise.
            if (!TryValidateSummaryAmounts(request.TotalBudget, request.AllocatedAmount, request.RemainingAmount, out var validationError))
                return BadRequest(new { message = validationError });

            var region = await _db.Set<Region>().FirstOrDefaultAsync(r => r.Id == request.RegionId);
            if (region == null)
                return BadRequest(new { message = "Selected region is invalid or inactive." });

            var regionAllocationForSave = await _db.Set<RegionBudgetAllocation>()
                .FirstOrDefaultAsync(a => a.RegionId == request.RegionId && a.FY == fy);
            if (regionAllocationForSave != null && request.TotalBudget > regionAllocationForSave.Amount)
                return BadRequest(new
                {
                    message = $"Total Budget cannot exceed the applicable Region budget of ₹{regionAllocationForSave.Amount:N0}."
                });

            var summary = await _db.Set<HeadquarterBudgetSummary>()
                .FirstOrDefaultAsync(s => s.RegionId == request.RegionId && s.FY == fy);
            if (summary == null)
            {
                summary = new HeadquarterBudgetSummary
                {
                    RegionId = request.RegionId,
                    FY = fy,
                    CreatedBy = CurrentUser,
                    CreatedAt = DateTime.Now
                };
                _db.Set<HeadquarterBudgetSummary>().Add(summary);
            }

            summary.TotalBudget = request.TotalBudget;
            summary.AllocatedAmount = request.AllocatedAmount;
            summary.RemainingAmount = request.RemainingAmount;
            summary.UpdatedBy = CurrentUser;
            summary.UpdatedAt = DateTime.Now;

            // Save flow: link this summary to every existing detail row for the same Region+FY
            // (Summary-to-Detail relationship key - see HeadquarterBudgetSummaryId doc comment). Uses
            // the navigation, not summary.Id directly, so EF fixes up the FK even for a brand new
            // summary whose Id isn't assigned until SaveChangesAsync below.
            var hqDetailRows = await _db.Set<HeadquarterBudgetAllocation>()
                .Where(a => a.RegionId == request.RegionId && a.FY == fy)
                .ToListAsync();
            foreach (var detailRow in hqDetailRows)
            {
                detailRow.HeadquarterBudgetSummary = summary;
            }

            await _db.SaveChangesAsync();

            return Ok(new { message = "Headquarters budget summary saved successfully." });
        }

        /// <summary>
        /// Submit the Level 3 summary for one Region+FY. Admin or RM. Re-validates TotalBudget =
        /// AllocatedAmount + RemainingAmount, and that TotalBudget does not exceed the
        /// applicable Region's own allocated amount (RegionBudgetAllocation.Amount), before
        /// marking it "Submitted" and writing a history snapshot.
        /// </summary>
        [HttpPut("hq-budget-summary/submit")]
        public async Task<IActionResult> SubmitHeadquarterBudgetSummary([FromBody] SubmitHeadquarterBudgetSummaryRequest request)
        {
            if (!IsAdminUser() && !IsRmUser())
                return Forbid();

            var fy = (request.FY ?? string.Empty).Trim();
            if (fy.Length == 0)
                return BadRequest(new { message = "Financial Year is required." });

            if (request.RegionId <= 0)
                return BadRequest(new { message = "A Region must be selected." });

            var summary = await _db.Set<HeadquarterBudgetSummary>()
                .FirstOrDefaultAsync(s => s.RegionId == request.RegionId && s.FY == fy);
            if (summary == null)
                return BadRequest(new { message = $"Save the headquarters budget summary for FY {fy} before submitting." });

            // Same rule set as Save - re-validated independently here since Submit is the
            // authoritative gate for progressing Status, regardless of what Save already enforced.
            if (!TryValidateSummaryAmounts(summary.TotalBudget, summary.AllocatedAmount, summary.RemainingAmount, out var validationError))
                return BadRequest(new { message = validationError });

            var regionAllocation = await _db.Set<RegionBudgetAllocation>()
                .FirstOrDefaultAsync(a => a.RegionId == request.RegionId && a.FY == fy);
            if (regionAllocation != null && summary.TotalBudget > regionAllocation.Amount)
                return BadRequest(new
                {
                    message = $"Total Budget cannot exceed the applicable Region budget of ₹{regionAllocation.Amount:N0}."
                });

            summary.Status = "Submitted";
            summary.UpdatedBy = CurrentUser;
            summary.UpdatedAt = DateTime.Now;
            _db.Set<HeadquarterBudgetSummaryHistory>().Add(ToHeadquarterSummaryHistory(summary, "Submitted"));

            await _db.SaveChangesAsync();

            return Ok(new { message = $"Headquarters budget summary for FY {fy} submitted successfully." });
        }

        private static StateBudgetSummaryHistory ToStateSummaryHistory(StateBudgetSummary row, string action) => new()
        {
            StateBudgetSummaryId = row.Id,
            FY = row.FY,
            TotalBudget = row.TotalBudget,
            AllocatedAmount = row.AllocatedAmount,
            RemainingAmount = row.RemainingAmount,
            Status = row.Status,
            CreatedBy = row.CreatedBy,
            CreatedAt = row.CreatedAt,
            ValidatedBy = row.ValidatedBy,
            ValidatedDate = row.ValidatedDate,
            ApprovedBy = row.ApprovedBy,
            ApprovedDate = row.ApprovedDate,
            Action = action,
            ActionDate = DateTime.Now
        };

        private static RegionBudgetSummaryHistory ToRegionSummaryHistory(RegionBudgetSummary row, string action) => new()
        {
            RegionBudgetSummaryId = row.Id,
            StateId = row.StateId,
            FY = row.FY,
            TotalBudget = row.TotalBudget,
            AllocatedAmount = row.AllocatedAmount,
            RemainingAmount = row.RemainingAmount,
            Status = row.Status,
            CreatedBy = row.CreatedBy,
            CreatedAt = row.CreatedAt,
            ValidatedBy = row.ValidatedBy,
            ValidatedDate = row.ValidatedDate,
            ApprovedBy = row.ApprovedBy,
            ApprovedDate = row.ApprovedDate,
            Action = action,
            ActionDate = DateTime.Now
        };

        private static HeadquarterBudgetSummaryHistory ToHeadquarterSummaryHistory(HeadquarterBudgetSummary row, string action) => new()
        {
            HeadquarterBudgetSummaryId = row.Id,
            RegionId = row.RegionId,
            FY = row.FY,
            TotalBudget = row.TotalBudget,
            AllocatedAmount = row.AllocatedAmount,
            RemainingAmount = row.RemainingAmount,
            Status = row.Status,
            CreatedBy = row.CreatedBy,
            CreatedAt = row.CreatedAt,
            ValidatedBy = row.ValidatedBy,
            ValidatedDate = row.ValidatedDate,
            ApprovedBy = row.ApprovedBy,
            ApprovedDate = row.ApprovedDate,
            Action = action,
            ActionDate = DateTime.Now
        };

        /// <summary>
        /// Read-only diagnostic for data that may already have been written while
        /// Save didn't yet enforce the full TotalBudget/AllocatedAmount/RemainingAmount rule (fixed
        /// in this change - see TryValidateSummaryAmounts). Lists every existing summary row at all
        /// three levels that fails the rule today, so it can be reviewed and corrected manually;
        /// this endpoint never modifies or deletes anything itself.
        /// </summary>
        [HttpGet("budget-summary-audit")]
        public async Task<IActionResult> GetBudgetSummaryAudit()
        {
            var badState = await _db.Set<StateBudgetSummary>()
                .Where(s => s.TotalBudget <= 0 || s.AllocatedAmount <= 0 || s.RemainingAmount < 0
                    || s.TotalBudget != s.AllocatedAmount + s.RemainingAmount)
                .Select(s => new { Level = "State", s.Id, s.FY, s.TotalBudget, s.AllocatedAmount, s.RemainingAmount, s.Status })
                .ToListAsync();

            var badRegion = await _db.Set<RegionBudgetSummary>()
                .Where(s => s.TotalBudget <= 0 || s.AllocatedAmount <= 0 || s.RemainingAmount < 0
                    || s.TotalBudget != s.AllocatedAmount + s.RemainingAmount)
                .Select(s => new { Level = "Region", s.Id, s.StateId, s.FY, s.TotalBudget, s.AllocatedAmount, s.RemainingAmount, s.Status })
                .ToListAsync();

            var badHq = await _db.Set<HeadquarterBudgetSummary>()
                .Where(s => s.TotalBudget <= 0 || s.AllocatedAmount <= 0 || s.RemainingAmount < 0
                    || s.TotalBudget != s.AllocatedAmount + s.RemainingAmount)
                .Select(s => new { Level = "Headquarters", s.Id, s.RegionId, s.FY, s.TotalBudget, s.AllocatedAmount, s.RemainingAmount, s.Status })
                .ToListAsync();

            return Ok(new
            {
                totalInvalidRecords = badState.Count + badRegion.Count + badHq.Count,
                state = badState,
                region = badRegion,
                headquarters = badHq
            });
        }

        

        [HttpGet("role-budget/state")]
        public async Task<IActionResult> GetStateRoleBudget(
    [FromQuery] int stateId,
    [FromQuery] string fy)
        {
            if (stateId <= 0)
            {
                return BadRequest(new
                {
                    message = "State is required."
                });
            }

            if (string.IsNullOrWhiteSpace(fy))
            {
                return BadRequest(new
                {
                    message = "Financial Year is required."
                });
            }

            var stateAllocation = await _db
                .Set<StateBudgetAllocation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.StateId == stateId &&
                    x.FY == fy);

            if (stateAllocation == null)
            {
                return Ok(new RoleBudgetDto
                {
                    BudgetAmount = 0,
                    Source = "StateBudgetAllocation"
                });
            }

            return Ok(new RoleBudgetDto
            {
                BudgetAmount = stateAllocation.Amount,
                Source = "StateBudgetAllocation"
            });
        }

        [HttpGet("role-budget/state-remaining")]
        public async Task<IActionResult> GetStateRemainingBudget(
    [FromQuery] int stateId,
    [FromQuery] string fy)
        {
            if (stateId <= 0)
            {
                return BadRequest(new
                {
                    message = "State is required."
                });
            }

            if (string.IsNullOrWhiteSpace(fy))
            {
                return BadRequest(new
                {
                    message = "Financial Year is required."
                });
            }

            var summary = await _db
                .Set<RegionBudgetSummary>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.StateId == stateId &&
                    x.FY == fy);

            return Ok(new RoleBudgetDto
            {
                BudgetAmount =
                    summary?.RemainingAmount ?? 0,

                Source =
                    "RegionBudgetSummary.RemainingAmount"
            });
        }

        [HttpGet("role-budget/region")]
        public async Task<IActionResult> GetRegionRoleBudget(
    [FromQuery] int regionId,
    [FromQuery] string fy)
        {
            if (regionId <= 0)
            {
                return BadRequest(new
                {
                    message = "Region is required."
                });
            }

            if (string.IsNullOrWhiteSpace(fy))
            {
                return BadRequest(new
                {
                    message = "Financial Year is required."
                });
            }

            var regionAllocation = await _db
                .Set<RegionBudgetAllocation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.RegionId == regionId &&
                    x.FY == fy);

            return Ok(new RoleBudgetDto
            {
                BudgetAmount =
                    regionAllocation?.Amount ?? 0,

                Source =
                    "RegionBudgetAllocation"
            });
        }

        [HttpGet("role-budget/region-remaining")]
        public async Task<IActionResult> GetRegionRemainingBudget(
    [FromQuery] int regionId,
    [FromQuery] string fy)
        {
            if (regionId <= 0)
            {
                return BadRequest(new
                {
                    message = "Region is required."
                });
            }

            if (string.IsNullOrWhiteSpace(fy))
            {
                return BadRequest(new
                {
                    message = "Financial Year is required."
                });
            }

            var summary = await _db
                .Set<HeadquarterBudgetSummary>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.RegionId == regionId &&
                    x.FY == fy);

            return Ok(new RoleBudgetDto
            {
                BudgetAmount =
                    summary?.RemainingAmount ?? 0,

                Source =
                    "HeadquarterBudgetSummary.RemainingAmount"
            });
        }
    }


}