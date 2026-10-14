using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.LabReports;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using static SPIC.Core.Entities.EmployeeRegistration;

namespace SPIC.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamController : ControllerBase
{
    private readonly AppDbContext _db;

    public TeamController(AppDbContext db)
    {
        _db = db;
    }

    // Claim helper, copied from JmdoTaskAllocationController so "current user's HQ" never drifts apart.
    private int? CurrentHqId() => ReadIntClaim("spic:hq_id", "HQId", "HqId");

    private int? ReadIntClaim(params string[] names)
    {
        foreach (var name in names)
        {
            var value = User.FindFirst(name)?.Value;
            if (int.TryParse(value, out var id) && id > 0)
                return id;
        }

        return null;
    }

    // GET api/Team/jmdos
    // There's no org-chart/reporting-line field anywhere in the data model (no
    // ManagerId/ReportsTo) - HQ is the only scoping signal MDO/MO/JMDO rows
    // carry, and MDO/MO/JMDO are all required to have one on registration, so
    // "same HQ as the signed-in MDO" is used as "this MDO's team".
    [Authorize(Roles = "Admin,MO,MDO")]
    [HttpGet("jmdos")]
    public async Task<ActionResult<List<TeamJmdoDto>>> GetJmdos()
    {
        var hqId = CurrentHqId();
        if (hqId is not > 0)
            return Ok(new List<TeamJmdoDto>());

        var team = await (
            from login in _db.Employeelogins
            join info in _db.EmployeeInformation on login.EmployeeInformationID equals info.Id
            join hq in _db.Headquarters on login.HeadquartersId equals hq.Id into hqJoin
            from hq in hqJoin.DefaultIfEmpty()
            where login.Role == AppRole.JMDO && login.HeadquartersId == hqId.Value && login.IsActive
            orderby info.Name
            select new
            {
                login.Id,
                login.UserId,
                Jmdo = new TeamJmdoDto
                {
                    EmployeeLoginId = login.Id,
                    Name = info.Name,
                    EmployeeCode = info.EmployeeCode,
                    IsActive = login.IsActive,
                    HeadquarterName = hq != null ? hq.HeadquarterName : null
                }
            }).ToListAsync();

        if (team.Count == 0)
            return Ok(new List<TeamJmdoDto>());

        var userIds = team.Select(t => t.UserId).ToList();

        var today = DateTime.Today;
        var attendanceToday = await _db.JMDOAttendances
            .Where(a => userIds.Contains(a.UserId) && a.Date >= today && a.Date < today.AddDays(1))
            .ToDictionaryAsync(a => a.UserId, a => new { a.AttendanceStatus, a.DutyStatus });

        var allocationStats = await _db.JmdoTaskAllocations
            .Where(a => userIds.Contains(a.SubmittedByUserId))
            .GroupBy(a => a.SubmittedByUserId)
            .Select(g => new
            {
                UserId = g.Key,
                Total = g.Count(),
                Pending = g.Count(a => a.Status == JmdoTaskAllocationStatus.Pending),
                Approved = g.Count(a => a.Status == JmdoTaskAllocationStatus.Approved),
                ApprovedToday = g.Count(a => a.Status == JmdoTaskAllocationStatus.Approved
                    && a.ReviewedAt != null && a.ReviewedAt.Value.Date == today)
            })
            .ToDictionaryAsync(x => x.UserId);

        foreach (var member in team)
        {
            if (attendanceToday.TryGetValue(member.UserId, out var attendance))
            {
                member.Jmdo.IsOnLeaveToday = attendance.AttendanceStatus == JMDOAttendanceStatus.OnLeave;
                member.Jmdo.IsOnDutyToday = attendance.DutyStatus == JMDODutyStatus.OnDuty;
            }

            if (allocationStats.TryGetValue(member.UserId, out var stats))
            {
                member.Jmdo.AssignedCount = stats.Total;
                member.Jmdo.InProgressCount = stats.Pending;
                member.Jmdo.CompletedCount = stats.Approved;
                member.Jmdo.ApprovedTodayCount = stats.ApprovedToday;
            }
        }

        return Ok(team.Select(t => t.Jmdo).ToList());
    }

    // The exact "this MDO's team" predicate GetJmdos applies above (active JMDO logins at the
    // caller's HQ) - every yearly-target endpoint goes through this so its scope can't drift.
    private IQueryable<Employeelogin> TeamJmdoLogins(int hqId) =>
        _db.Employeelogins.Where(l => l.Role == AppRole.JMDO && l.HeadquartersId == hqId && l.IsActive);

    // GET api/Team/yearly-targets
    // Current-FY (April-March) yearly targets for the caller's own team only.
    [Authorize(Roles = "Admin,MO,MDO")]
    [HttpGet("yearly-targets")]
    public async Task<ActionResult<List<JmdoYearlyTargetDto>>> GetYearlyTargets()
    {
        var hqId = CurrentHqId();
        if (hqId is not > 0)
            return Ok(new List<JmdoYearlyTargetDto>());

        var fy = LabReportRules.FinancialYearStart(DateTime.Today);

        var rows = await (
            from target in _db.JmdoYearlyTargets
            join login in TeamJmdoLogins(hqId.Value) on target.EmployeeLoginId equals login.Id
            join info in _db.EmployeeInformation on login.EmployeeInformationID equals info.Id
            join hq in _db.Headquarters on login.HeadquartersId equals hq.Id into hqJoin
            from hq in hqJoin.DefaultIfEmpty()
            where target.FinancialYearStart == fy
            orderby info.Name
            select new JmdoYearlyTargetDto
            {
                EmployeeLoginId = login.Id,
                Name = info.Name,
                EmployeeCode = info.EmployeeCode,
                HeadquarterName = hq != null ? hq.HeadquarterName : null,
                FinancialYearStart = target.FinancialYearStart,
                SpcmTarget = target.SpcmTarget,
                SoilSampleTarget = target.SoilSampleTarget,
                UpdatedAt = target.UpdatedAt
            }).AsNoTracking().ToListAsync();

        return Ok(rows);
    }

    // PUT api/Team/yearly-targets
    // Upsert on (EmployeeLoginId, current FY): updates the existing row, inserts only if none.
    [Authorize(Roles = "Admin,MO,MDO")]
    [HttpPut("yearly-targets")]
    public async Task<ActionResult<JmdoYearlyTargetDto>> SaveYearlyTarget([FromBody] SaveJmdoYearlyTargetDto dto)
    {
        if (dto == null || dto.EmployeeLoginId <= 0)
            return BadRequest(new { Success = false, Message = "Select a JMDO." });
        if (dto.SpcmTarget is not >= 0 || dto.SoilSampleTarget is not >= 0)
            return BadRequest(new { Success = false, Message = "SPCM and Soil Sample targets are required and cannot be negative." });

        var hqId = CurrentHqId();
        if (hqId is not > 0)
            return StatusCode(403, new { Success = false, Message = "Your login has no headquarters, so it has no JMDO team." });

        // Never trust the posted id on its own - it must resolve to a JMDO on the caller's team.
        var jmdo = await (
            from login in TeamJmdoLogins(hqId.Value)
            join info in _db.EmployeeInformation on login.EmployeeInformationID equals info.Id
            join hq in _db.Headquarters on login.HeadquartersId equals hq.Id into hqJoin
            from hq in hqJoin.DefaultIfEmpty()
            where login.Id == dto.EmployeeLoginId
            select new { login.Id, login.HeadquartersId, info.Name, info.EmployeeCode, HeadquarterName = hq != null ? hq.HeadquarterName : null }
            ).AsNoTracking().FirstOrDefaultAsync();
        if (jmdo == null)
            return StatusCode(403, new { Success = false, Message = "This JMDO is not part of your team." });

        var fy = LabReportRules.FinancialYearStart(DateTime.Today);
        var actor = User.Identity?.Name;
        var now = DateTime.Now;

        var target = await _db.JmdoYearlyTargets
            .FirstOrDefaultAsync(t => t.EmployeeLoginId == jmdo.Id && t.FinancialYearStart == fy);

        if (target is null)
        {
            target = new JmdoYearlyTarget
            {
                EmployeeLoginId = jmdo.Id,
                FinancialYearStart = fy,
                CreatedBy = actor,
                CreatedAt = now
            };
            _db.JmdoYearlyTargets.Add(target);
        }

        target.SpcmTarget = dto.SpcmTarget!.Value;
        target.SoilSampleTarget = dto.SoilSampleTarget!.Value;
        target.HeadquarterId = jmdo.HeadquartersId;
        target.UpdatedBy = actor;
        target.UpdatedAt = now;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Another save for the same JMDO/FY landed between our read and insert.
            return Conflict(new { Success = false, Message = "This JMDO's target was just saved by another request. Please reload and try again." });
        }

        return Ok(new JmdoYearlyTargetDto
        {
            EmployeeLoginId = jmdo.Id,
            Name = jmdo.Name,
            EmployeeCode = jmdo.EmployeeCode,
            HeadquarterName = jmdo.HeadquarterName,
            FinancialYearStart = target.FinancialYearStart,
            SpcmTarget = target.SpcmTarget,
            SoilSampleTarget = target.SoilSampleTarget,
            UpdatedAt = target.UpdatedAt
        });
    }
}
