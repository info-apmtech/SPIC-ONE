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

    // Same rule as JmdoTaskAllocationController.EffectiveDealerId - DealerRegistrationId for
    // rows created after the dealer source switched to DealerRegistration, negated SubDealerId
    // for legacy rows, so a legacy and a new-style id can never collide.
    private static int EffectiveDealerId(JmdoTaskAllocationDealer d) => d.DealerRegistrationId ?? -d.SubDealerId;

    // One flattened task row, built once in GetTeamReports and read from both the JMDO-wise
    // summary table (grouped per JMDO) and the Detailed Task Report (shown as-is).
    private sealed record TeamTaskRow(
        string JmdoUserId,
        string Type,
        string Summary,
        string Resolution,
        string Status,
        DateTime TaskDate,
        double? HoursTaken,
        bool IsCompletedThisMonth,
        DateTime? CompletedAt,
        DateTime AssignedAt,
        bool? IsOnTime);

    
    [Authorize(Roles = "Admin,MO,MDO")]
    [HttpGet("reports")]
    public async Task<ActionResult<MdoTeamReportsDto>> GetTeamReports()
    {
        var hqId = CurrentHqId();
        if (hqId is not > 0)
            return Ok(new MdoTeamReportsDto());

        var team = await (
            from login in TeamJmdoLogins(hqId.Value)
            join info in _db.EmployeeInformation on login.EmployeeInformationID equals info.Id
            select new { login.UserId, info.Name }
            ).ToListAsync();

        if (team.Count == 0)
            return Ok(new MdoTeamReportsDto());

        var nameByUserId = team.ToDictionary(t => t.UserId, t => t.Name);
        var userIds = team.Select(t => t.UserId).ToList();

        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var allocations = await _db.JmdoTaskAllocations
            .Include(a => a.Dealers)
            .Include(a => a.Programs)
            .Include(a => a.Closeouts)
            .AsNoTracking()
            .Where(a => userIds.Contains(a.SubmittedByUserId))
            .ToListAsync();

        JmdoTaskCloseout? LatestCloseout(JmdoTaskAllocation a, JmdoCloseoutKind kind, int? subId) =>
            a.Closeouts
                .Where(c => c.Kind == kind && (subId == null || (kind == JmdoCloseoutKind.Dealer ? c.SubDealerId == subId : c.Csr1Id == subId)))
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault();

        static string OutcomeOrDefault(JmdoTaskCloseout? closeout) =>
            closeout?.Outcome is { Length: > 0 } outcome ? outcome : "Completed";

        static string RemarksOrDash(JmdoTaskCloseout? closeout) =>
            string.IsNullOrWhiteSpace(closeout?.Remarks) ? "—" : closeout!.Remarks!;

        static string StatusLabel(AllocTaskStatus status, string outcomeIfCompleted) => status switch
        {
            AllocTaskStatus.Completed => outcomeIfCompleted,
            AllocTaskStatus.InProgress => "In Progress",
            _ => "Not Started"
        };

        static double? HoursBetween(DateTime? started, DateTime? completed) =>
            started.HasValue && completed.HasValue && completed > started
                ? Math.Round((completed.Value - started.Value).TotalHours, 1)
                : (double?)null;

        var rows = new List<TeamTaskRow>();

        foreach (var a in allocations)
        {
            if (!nameByUserId.ContainsKey(a.SubmittedByUserId)) continue;
            var assignedAt = a.ReviewedAt ?? a.SubmittedAt;

            foreach (var d in a.Dealers)
            {
                var taskDate = d.PlannedDate ?? a.AllocationDate;
                if (taskDate < monthStart || taskDate >= monthEnd) continue;

                var closeout = LatestCloseout(a, JmdoCloseoutKind.Dealer, EffectiveDealerId(d));
                var isCompleted = d.Status == AllocTaskStatus.Completed && d.CompletedAt.HasValue;
                bool? onTime = isCompleted && d.PlannedDate.HasValue
                    ? d.CompletedAt!.Value.Date <= d.PlannedDate.Value.Date
                    : null;

                rows.Add(new TeamTaskRow(a.SubmittedByUserId, "Dealer", d.DealerName, RemarksOrDash(closeout),
                    StatusLabel(d.Status, OutcomeOrDefault(closeout)), taskDate, HoursBetween(d.StartedAt, d.CompletedAt),
                    isCompleted, d.CompletedAt, assignedAt, onTime));
            }

            foreach (var p in a.Programs)
            {
                var taskDate = a.AllocationDate;
                if (taskDate < monthStart || taskDate >= monthEnd) continue;

                var closeout = LatestCloseout(a, JmdoCloseoutKind.MdProgram, p.Csr1Id);
                var isCompleted = p.Status == AllocTaskStatus.Completed && p.CompletedAt.HasValue;

                rows.Add(new TeamTaskRow(a.SubmittedByUserId, "MD Program", p.ProgramName, RemarksOrDash(closeout),
                    StatusLabel(p.Status, OutcomeOrDefault(closeout)), taskDate, HoursBetween(p.StartedAt, p.CompletedAt),
                    isCompleted, p.CompletedAt, assignedAt, null));
            }

            if (a.SpcmTarget is > 0 && a.AllocationDate >= monthStart && a.AllocationDate < monthEnd)
            {
                var closeout = LatestCloseout(a, JmdoCloseoutKind.Spcm, null);
                var isCompleted = a.SpcmStatus == AllocTaskStatus.Completed && a.SpcmCompletedAt.HasValue;

                rows.Add(new TeamTaskRow(a.SubmittedByUserId, "SPCM", $"Target - {a.SpcmTarget}", RemarksOrDash(closeout),
                    StatusLabel(a.SpcmStatus, OutcomeOrDefault(closeout)), a.AllocationDate, HoursBetween(a.SpcmStartedAt, a.SpcmCompletedAt),
                    isCompleted, a.SpcmCompletedAt, assignedAt, null));
            }

            // Soil Sample never has a JmdoTaskCloseout of its own (its Start button goes straight
            // to the SAS Sample Collection wizard, never TaskDetail's close-out form) - so it has
            // no Resolution/Outcome and never contributes an hours-taken figure.
            if (a.SoilSampleTarget is > 0 && a.AllocationDate >= monthStart && a.AllocationDate < monthEnd)
            {
                var isCompleted = a.SoilSampleStatus == AllocTaskStatus.Completed;

                rows.Add(new TeamTaskRow(a.SubmittedByUserId, "Soil Sample", $"Today's target - {a.SoilSampleTarget}", "—",
                    StatusLabel(a.SoilSampleStatus, "Completed"), a.AllocationDate, null,
                    isCompleted, a.SoilSampleCompletedAt, assignedAt, null));
            }

            if ((a.UreaTarget > 0 || a.NpsTarget > 0 || a.DapTarget > 0 || a.OthersTarget > 0)
                && a.AllocationDate >= monthStart && a.AllocationDate < monthEnd)
            {
                var closeout = LatestCloseout(a, JmdoCloseoutKind.PosLiquidation, null);
                var isCompleted = a.PosLiquidationStatus == AllocTaskStatus.Completed && a.PosLiquidationCompletedAt.HasValue;

                rows.Add(new TeamTaskRow(a.SubmittedByUserId, "PoS Liquidation", "PoS Liquidation", RemarksOrDash(closeout),
                    StatusLabel(a.PosLiquidationStatus, OutcomeOrDefault(closeout)), a.AllocationDate, HoursBetween(a.PosLiquidationStartedAt, a.PosLiquidationCompletedAt),
                    isCompleted, a.PosLiquidationCompletedAt, assignedAt, null));
            }
        }

        var soilCollections = await _db.SampleCollections.AsNoTracking()
            .Where(c => userIds.Contains(c.CollectedByUserId) && !c.IsDeleted && c.CreatedAt >= monthStart && c.CreatedAt < monthEnd)
            .Select(c => c.CollectedByUserId)
            .ToListAsync();

        var farmersThisMonth = await _db.SasFarmers.AsNoTracking()
            .Where(f => f.CreatedByUserId != null && userIds.Contains(f.CreatedByUserId) && !f.IsDeleted
                && f.CreatedAt >= monthStart && f.CreatedAt < monthEnd)
            .Select(f => f.CreatedByUserId!)
            .ToListAsync();

        var farmersAllTime = await _db.SasFarmers.AsNoTracking()
            .CountAsync(f => f.CreatedByUserId != null && userIds.Contains(f.CreatedByUserId) && !f.IsDeleted);

        var jmdoSummaries = team.Select(t =>
        {
            var jmdoRows = rows.Where(r => r.JmdoUserId == t.UserId).ToList();
            var onTimeEvaluable = jmdoRows.Where(r => r.IsOnTime.HasValue).ToList();

            return new MdoReportJmdoSummaryRowDto
            {
                JmdoName = t.Name,
                TasksClosedThisMonth = jmdoRows.Count(r => r.IsCompletedThisMonth),
                OnTimePercent = onTimeEvaluable.Count > 0
                    ? Math.Round(100.0 * onTimeEvaluable.Count(r => r.IsOnTime == true) / onTimeEvaluable.Count, 0)
                    : (double?)null,
                SoilSamplesThisMonth = soilCollections.Count(u => u == t.UserId),
                FarmersAddedThisMonth = farmersThisMonth.Count(u => u == t.UserId)
            };
        }).ToList();

        var completedRows = rows.Where(r => r.IsCompletedThisMonth).ToList();
        var turnaroundDays = completedRows
            .Where(r => r.CompletedAt.HasValue)
            .Select(r => (r.CompletedAt!.Value - r.AssignedAt).TotalDays)
            .Where(d => d >= 0)
            .ToList();

        return Ok(new MdoTeamReportsDto
        {
            TasksClosedThisMonth = completedRows.Count,
            AvgTurnaroundDays = turnaroundDays.Count > 0 ? Math.Round(turnaroundDays.Average(), 1) : (double?)null,
            SoilSamplesThisMonth = soilCollections.Count,
            FarmersCoveredAllTime = farmersAllTime,
            JmdoSummaries = jmdoSummaries,
            DetailedTasks = rows
                .OrderByDescending(r => r.TaskDate)
                .Select(r => new MdoReportDetailRowDto
                {
                    JmdoName = nameByUserId[r.JmdoUserId],
                    Type = r.Type,
                    Summary = r.Summary,
                    Resolution = r.Resolution,
                    Status = r.Status,
                    TaskDate = r.TaskDate,
                    HoursTaken = r.HoursTaken
                }).ToList()
        });
    }

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
