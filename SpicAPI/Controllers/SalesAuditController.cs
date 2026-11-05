using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using System.Security.Claims;
using static SPIC.Core.Entities.EmployeeRegistration;

namespace SpicAPI.Controllers;

public sealed class SalesAuditEligibilityOptions
{
    public int[] EligibleProgramIds { get; set; } = Array.Empty<int>();
}

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class SalesAuditController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SalesAuditEligibilityOptions _options;

    public SalesAuditController(AppDbContext db, IOptions<SalesAuditEligibilityOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    [HttpGet("eligible")]
    public async Task<IActionResult> GetEligible()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var programIds = (_options.EligibleProgramIds ?? Array.Empty<int>())
            .Where(x => x > 0).Distinct().ToArray();

        var activeUser = await _db.Set<Employeelogin>().AsNoTracking()
            .AnyAsync(x => x.IsActive && x.UserId == userId, HttpContext.RequestAborted);
        if (!activeUser) return Forbid();

        var query = from c2 in _db.Set<CSR2>().AsNoTracking()
                    join c1 in _db.Set<CSR1>().AsNoTracking() on c2.CSR1Id equals c1.Id
                    join pm in _db.Set<ProgramMaster>().AsNoTracking() on c1.ProgramId equals pm.Id
                    where c2.Status == CSR2Status.Approved
                          && c1.Status == CSR1Status.Approved && c1.IsActive
                          && c1.ResponsiblePersonId == userId
                    select new { c2, c1, pm };

        if (programIds.Length > 0)
            query = query.Where(x => programIds.Contains(x.c1.ProgramId));

        var rows = await query.OrderByDescending(x => x.c2.Id)
            .Select(x => new {
                CSR2Id = x.c2.Id,
                SanctionNumber = x.c1.SanctionNo ?? "",
                ProgramName = x.pm.Name,
                Location = x.c2.ActualLocation ?? ""
            }).ToListAsync(HttpContext.RequestAborted);

        if (rows.Count == 0) return Ok(Array.Empty<object>());

        var csr2Ids = rows.Select(x => x.CSR2Id).ToArray();
        var savedRecords = await _db.Set<SalesAuditRecord>().AsNoTracking()
            .Where(x => csr2Ids.Contains(x.CSR2Id))
            .Select(x => new {
                x.CSR2Id,
                x.SchemeFrom,
                x.SchemeTo,
                x.RequiredTurnover,
                x.ActualTurnover,
                x.DocumentStatus
            }).ToListAsync(HttpContext.RequestAborted);
        var byCsr2 = savedRecords.GroupBy(x => x.CSR2Id)
            .ToDictionary(x => x.Key, x => x.First());

        return Ok(rows.Select(x => {
            byCsr2.TryGetValue(x.CSR2Id, out var record);
            var requiredCr = record?.RequiredTurnover ?? 0m;
            var actualCr = record?.ActualTurnover ?? 0m;
            var status = record?.DocumentStatus ?? "Not Started";
            return new
            {
                x.CSR2Id,
                x.SanctionNumber,
                x.ProgramName,
                x.Location,
                SchemeFrom = record?.SchemeFrom,
                SchemeTo = record?.SchemeTo,
                RequiredTurnover = record?.RequiredTurnover,
                ActualTurnover = record?.ActualTurnover,
                DocumentStatus = status,
                ProgressPercent = requiredCr > 0m ? actualCr / requiredCr * 100m : 0m,
                RemainingTurnover = Math.Max(0m, requiredCr - actualCr),
                IsAuditCompleted = status == "Completed",
                CanSaveAchievement = status is "Plan Confirmed" or "In Progress"
            };
        }).ToList());
    }
}
