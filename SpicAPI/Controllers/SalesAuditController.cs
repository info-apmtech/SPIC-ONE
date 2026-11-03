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
    // ProgramMaster IDs explicitly approved for Sales Audit by SPIC business owners.
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

    // Read-only list: never fabricate turnover values, scheme dates, statuses or records.
    [HttpGet("eligible")]
    public async Task<IActionResult> GetEligible()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var programIds = (_options.EligibleProgramIds ?? Array.Empty<int>())
            .Where(x => x > 0).Distinct().ToArray();

        // Restrict both visibility and downstream actions to the CSR-1 assigned person.
        // This is an authorization check, not a user-supplied filter.
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

        // Optional business-managed eligibility filter. Empty configuration means
        // all finally approved, assigned CSR-2 programs are listed.
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
        var byCsr2 = savedRecords.GroupBy(x => x.CSR2Id).ToDictionary(x => x.Key, x => x.First());

        return Ok(rows.Select(x => {
            byCsr2.TryGetValue(x.CSR2Id, out var record);
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
                DocumentStatus = record?.DocumentStatus ?? "Not Started"
            };
        }).ToList());
    }
}
