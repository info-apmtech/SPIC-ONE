using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
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
}
