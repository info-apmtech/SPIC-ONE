using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using SPIC.Core.Entities;
using Spic.Infrastructure.Data;
using System.Reflection;
using SPIC.Core.DTOs;


namespace SpicAPI.Controllers;

[ApiController]
[Authorize] // IMPORTANT: tighten to existing SPIC page/action policy before exposing in production.
[Route("api/RMDHQMapping")]
public sealed class RMDHQMappingController : ControllerBase
{
    private readonly AppDbContext _db;
    private DbSet<RMDHQMapping> Mappings => _db.RMDHQMappings;

    // Uses the existing AppDbContext registered in your application DI.
    public RMDHQMappingController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("employees")]
    public async Task<IActionResult> Employees(CancellationToken ct) => Ok(await GetEmployeesAsync(ct));

    [HttpGet("states")]
    public async Task<IActionResult> States(CancellationToken ct) => Ok(await GetStatesAsync(ct));

    [HttpGet("regions/{stateId:int}")]
    public async Task<IActionResult> Regions(int stateId, CancellationToken ct)
        => Ok(await GetRegionsAsync(stateId, ct));

    [HttpGet("headquarters/{regionId:int}")]
    public async Task<IActionResult> Headquarters(int regionId, CancellationToken ct)
        => Ok(await GetHeadquartersAsync(regionId, ct));

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var records = await Mappings.AsNoTracking().OrderByDescending(x => x.Id).ToListAsync(ct);
        var employees = (await GetEmployeesAsync(ct)).ToDictionary(x => x.Id, x => x.Name);
        var states = (await GetStatesAsync(ct)).ToDictionary(x => x.Id, x => x.Name);
        var results = new List<MappingView>();
        var regionsByState = new Dictionary<int, Dictionary<int, string>>();
        foreach (var m in records)
        {
            if (!regionsByState.ContainsKey(m.StateId))
                regionsByState[m.StateId] = (await GetRegionsAsync(m.StateId, ct)).ToDictionary(x => x.Id, x => x.Name);
            results.Add(new MappingView
            {
                Id = m.Id,
                EmployeeId = m.EmployeeId,
                EmployeeName = employees.GetValueOrDefault(m.EmployeeId, m.EmployeeId.ToString()),
                StateId = m.StateId,
                StateName = states.GetValueOrDefault(m.StateId, m.StateId.ToString()),
                RegionId = m.RegionId,
                RegionName = regionsByState[m.StateId].GetValueOrDefault(m.RegionId, m.RegionId.ToString()),
                HeadquartersIds = m.HeadquartersIds,
                CreatedAt = m.CreatedAt,
                CreatedBy = m.CreatedBy,
                UpdatedAt = m.UpdatedAt,
                UpdatedBy = m.UpdatedBy
            });
        }
        return Ok(results);
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] SaveMappingRequest request, CancellationToken ct)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actor)) return Unauthorized();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (request.HeadquartersIds.Count == 0 || request.HeadquartersIds.Any(x => x <= 0))
            return BadRequest("Select at least one valid HQ.");
        var hqIds = request.HeadquartersIds.Distinct().OrderBy(x => x).ToList();
        if (!(await GetEmployeesAsync(ct)).Any(x => x.Id == request.EmployeeId))
            return BadRequest("Employee is not an eligible RMD employee.");
        if (!(await GetStatesAsync(ct)).Any(x => x.Id == request.StateId))
            return BadRequest("Invalid state.");
        if (!(await GetRegionsAsync(request.StateId, ct)).Any(x => x.Id == request.RegionId))
            return BadRequest("Region does not belong to state.");
        var permittedHQIds = (await GetHeadquartersAsync(request.RegionId, ct)).Select(x => x.Id).ToHashSet();
        if (hqIds.Any(x => !permittedHQIds.Contains(x)))
            return BadRequest("One or more HQs do not belong to selected region.");

        // One row per employee + state + region; edits must not change identity of an existing row.
        var match = await Mappings.SingleOrDefaultAsync(x => x.EmployeeId == request.EmployeeId
            && x.StateId == request.StateId && x.RegionId == request.RegionId, ct);
        if (request.Id is > 0 && (match is null || match.Id != request.Id))
            return Conflict("Mapping has changed or employee/state/region does not match this record.");
        var joined = string.Join(",", hqIds);
        if (joined.Length > 4000) return BadRequest("Too many headquarters selected.");
        var now = DateTime.UtcNow;
        if (match is null)
        {
            match = new RMDHQMapping
            {
                EmployeeId = request.EmployeeId,
                StateId = request.StateId,
                RegionId = request.RegionId,
                HeadquartersIds = joined,
                CreatedAt = now,
                CreatedBy = actor
            };
            Mappings.Add(match);
        }
        else if (match.HeadquartersIds != joined)
        {
            match.HeadquartersIds = joined;
            match.UpdatedAt = now;
            match.UpdatedBy = actor;
        }
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { return Conflict("Duplicate mapping or database constraint error."); }
        return Ok(new { match.Id, match.HeadquartersIds });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(User.FindFirstValue(ClaimTypes.NameIdentifier))) return Unauthorized();
        var row = await Mappings.FindAsync(new object[] { id }, ct);
        if (row == null) return NotFound();
        Mappings.Remove(row);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // Master lookup implementation uses the AppDbContext DbSets confirmed in
    // your uploaded context. Display-name variants are kept here because the
    // State/Region/HQ/EmployeeInformation class source was not supplied.
    // Verify them against your real entities before deploying.
    private static object? Field(object item, params string[] names)
    {
        foreach (var name in names)
        {
            var property = item.GetType().GetProperty(name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property != null) return property.GetValue(item);
        }
        return null;
    }
    private static int Key(object item, params string[] fields)
    {
        var value = Field(item, fields);
        return value != null && int.TryParse(value.ToString(), out var id) ? id : 0;
    }
    private static string Label(object item, params string[] fields)
    {
        var value = Field(item, fields)?.ToString();
        return !string.IsNullOrWhiteSpace(value) ? value : $"#{Key(item, "Id")}";
    }
    private async Task<List<LookupItem>> GetEmployeesAsync(CancellationToken ct)
    {
        // IMPORTANT: EmployeeInformation's exact RMD designation/role mapping
        // is not in the supplied AppDbContext. Currently lists all employees.
        // Apply your existing RMD-only eligibility filter before production.
        var rows = await _db.EmployeeInformation.AsNoTracking().ToListAsync(ct);
        return rows.Where(x => Key(x, "Id") > 0)
            .Select(x => new LookupItem
            {
                Id = Key(x, "Id"),
                Name = Label(x, "EmployeeName", "Name", "FullName", "EmployeeFullName", "FirstName")
            })
            .OrderBy(x => x.Name).ToList();
    }
    private async Task<List<LookupItem>> GetStatesAsync(CancellationToken ct)
    {
        var rows = await _db.States.AsNoTracking().ToListAsync(ct);
        return rows.Where(x => Key(x, "Id") > 0)
            .Select(x => new LookupItem { Id = Key(x, "Id"), Name = Label(x, "Name", "StateName") })
            .OrderBy(x => x.Name).ToList();
    }
    private async Task<List<LookupItem>> GetRegionsAsync(int stateId, CancellationToken ct)
    {
        var rows = await _db.Regions.AsNoTracking().ToListAsync(ct);
        return rows.Where(x => Key(x, "StateId") == stateId && Key(x, "Id") > 0)
            .Select(x => new LookupItem { Id = Key(x, "Id"), Name = Label(x, "Name", "RegionName") })
            .OrderBy(x => x.Name).ToList();
    }
    private async Task<List<LookupItem>> GetHeadquartersAsync(int regionId, CancellationToken ct)
    {
        var rows = await _db.Headquarters.AsNoTracking().ToListAsync(ct);
        return rows.Where(x => Key(x, "RegionId") == regionId && Key(x, "Id") > 0)
            .Select(x => new LookupItem { Id = Key(x, "Id"), Name = Label(x, "Name", "HeadquarterName", "HQName") })
            .OrderBy(x => x.Name).ToList();
    }
}
