using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BudgetController : ControllerBase
    {
        private readonly IGenericRepository<BudgetProgram> _budgetRepo;
        private readonly IGenericRepository<ProgramMaster> _programRepo;
        public BudgetController(
    IGenericRepository<BudgetProgram> budgetRepo,
    IGenericRepository<ProgramMaster> programRepo)
        {
            _budgetRepo = budgetRepo;
            _programRepo = programRepo;
        }


        [HttpPost]
        public async Task<IActionResult> SaveBudget([FromBody] BudgetProgram model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.CreatedAt = DateTime.Now;
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
                .Select(x => new ProgramWiseBudgetDto
                {
                    ProgramId = x.Id,

                    ProgramType = x.ProgramType != null
                        ? x.ProgramType.Name
                        : "",

                    ProgramName = x.Name,


                    // ProgramType Budget Settings
                    //BudgetAmount = x.ProgramType != null
                    //    ? x.ProgramType.BudgetAmount
                    //    : 0,
                    BudgetAmount = 0,

                    IsChangeAmount = x.ProgramType != null
                        ? x.ProgramType.IsChangeAmount
                        : false,


                    // Default Budget Entry
                    //TotalBudget = x.ProgramType != null &&
                    //              !x.ProgramType.IsChangeAmount
                    //    ? x.ProgramType.BudgetAmount
                    //    : 0,
                    TotalBudget =  0,


                    AprilBudget = 0,
                    MayBudget = 0,
                    JuneBudget = 0,
                    JulyBudget = 0,
                    AugustBudget = 0,
                    SeptemberBudget = 0,
                    OctoberBudget = 0,
                    NovemberBudget = 0,
                    DecemberBudget = 0,
                    JanuaryBudget = 0,
                    FebruaryBudget = 0,
                    MarchBudget = 0

                })
                .ToListAsync();


            return Ok(programs);
        }
    }
}