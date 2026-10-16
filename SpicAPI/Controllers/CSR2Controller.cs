using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using System.Security.Claims;
using static SPIC.Core.Entities.EmployeeRegistration;

// REQUIRED: add the namespace containing your existing AppDbContext.
// Example: using SPIC.Infrastructure.Data;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CSR2Controller : ControllerBase
    {
        private readonly AppDbContext _db;
        public CSR2Controller(AppDbContext db) => _db = db;

        // Only approved CSR-1 programs are ready for execution.
        // Scope is intentionally limited to creator for non-admin users.
        // Adapt to existing CSR-1 hierarchy authorization before production.
        private IQueryable<CSR1> VisibleApprovedCSR1()
        {
            var query = _db.Set<CSR1>()
                .AsNoTracking()
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
                .AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    ProgramName = x.Name,
                    ProgramType = x.ProgramType.Name
                }).ToDictionaryAsync(x => x.Id);

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
            var csr1 = await VisibleApprovedCSR1()
                .FirstOrDefaultAsync(x => x.Id == csr1Id);
            if (csr1 == null)
                return NotFound(new { message = "Approved CSR1 record not found." });

            var master = await _db.Set<ProgramMaster>()
                .AsNoTracking()
                .Where(x => x.Id == csr1.ProgramId)
                .Select(x => new
                {
                    ProgramName = x.Name,
                    ProgramType = x.ProgramType.Name
                }).FirstOrDefaultAsync();

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
            // EmployeeInformation table supplies name and official/personal mobile numbers.
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

            // Employeelogins supplies assigned roles (which are stored as AppRole).
            // Load only active logins; remove the filter if inactive roles should also appear.
            var logins = await _db.Set<Employeelogin>()
                .AsNoTracking()
                .Where(l => l.IsActive)
                .Select(l => new { l.EmployeeInformationID, l.Role })
                .ToListAsync();

            var rolesByEmployee = logins
                .GroupBy(l => l.EmployeeInformationID)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Role.ToString())
                          .Distinct()
                          .OrderBy(role => role)
                          .ToList());

            var result = people.Select(e => new
            {
                e.Id,
                e.Name,
                e.OfficialPhoneNumber,
                e.PersonalPhoneNumber,
                Roles = rolesByEmployee.TryGetValue(e.Id, out var roles)
                    ? roles
                    : new List<string>()
            })
            .OrderBy(e => e.Name)
            .ToList();

            return Ok(result);
        }
    }
}
