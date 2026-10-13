using System.Security.Claims;
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
public class JmdoTaskAllocationController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public JmdoTaskAllocationController(AppDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    // ---- Claim helpers, copied from SubDealerRegistrationController so the two controllers'
    // notion of "current user/role/location" never drifts apart. ----
    private string? CurrentUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
        User.FindFirst("sub")?.Value ??
        User.FindFirst("spic:user_id")?.Value;

    private string CurrentRole() =>
        User.FindFirst(ClaimTypes.Role)?.Value ??
        User.FindFirst("Role")?.Value ??
        string.Empty;

    private int? CurrentStateId() => ReadIntClaim("spic:state_id", "StateId");
    private int? CurrentRegionId() => ReadIntClaim("spic:region_id", "RegionId");
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

    // POST api/JmdoTaskAllocation
    [Authorize(Roles = "Admin,MO,MDO,JMDO")]
    [HttpPost]
    public async Task<ActionResult<JmdoTaskAllocationResponseDto>> Create([FromBody] JmdoTaskAllocationCreateDto dto)
    {
        if (dto == null)
            return BadRequest(new { Success = false, Message = "No allocation supplied." });

        // A JMDO can submit having only filled in some of the 5 steps (e.g. skip MD Program
        // entirely via the sidebar) - only reject a submission that has nothing at all in it.
        var hasAnyData =
            dto.DealerAssignments.Count > 0 ||
            dto.ProgramIds.Count > 0 ||
            dto.SpcmTarget.HasValue ||
            dto.SoilSampleTarget.HasValue ||
            dto.UreaTarget.HasValue ||
            dto.DapTarget.HasValue ||
            dto.NpsTarget.HasValue ||
            dto.OthersTarget.HasValue;

        if (!hasAnyData)
            return BadRequest(new { Success = false, Message = "Fill in at least one step before submitting." });

        var userId = CurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { Success = false, Message = "Could not identify the signed-in user." });

        var dealerIds = dto.DealerAssignments.Select(a => a.DealerId).Distinct().ToList();
        var dealers = await _db.SubDealerRegistrations
            .Where(d => dealerIds.Contains(d.Id))
            .ToListAsync();
        if (dealers.Count != dealerIds.Count)
            return BadRequest(new { Success = false, Message = "One or more selected dealers could not be found." });
        var dealersById = dealers.ToDictionary(d => d.Id);

        var programs = await _db.CSR1
            .Where(p => dto.ProgramIds.Contains(p.Id))
            .ToListAsync();
        if (programs.Count != dto.ProgramIds.Distinct().Count())
            return BadRequest(new { Success = false, Message = "One or more selected MD programs could not be found." });

        // CSR1 only stores ProgramId (FK) - the display name lives on ProgramMaster.
        var programNamesById = await _db.ProgramMasters
            .Where(pm => programs.Select(p => p.ProgramId).Contains(pm.Id))
            .ToDictionaryAsync(pm => pm.Id, pm => pm.Name);

        await using var tx = await _db.Database.BeginTransactionAsync();

        var allocation = new JmdoTaskAllocation
        {
            AllocationDate = DateTime.Today,
            SubmittedByUserId = userId,
            SubmittedByRole = CurrentRole(),
            HeadquarterId = CurrentHqId(),
            RegionId = CurrentRegionId(),
            StateId = CurrentStateId(),
            SpcmTarget = dto.SpcmTarget,
            SoilSampleTarget = dto.SoilSampleTarget,
            UreaTarget = dto.UreaTarget,
            DapTarget = dto.DapTarget,
            NpsTarget = dto.NpsTarget,
            OthersTarget = dto.OthersTarget,
            Status = JmdoTaskAllocationStatus.Pending,
            CreatedAt = DateTime.Now,
            SubmittedAt = DateTime.Now
        };

        _db.JmdoTaskAllocations.Add(allocation);
        await _db.SaveChangesAsync();

        foreach (var assignment in dto.DealerAssignments)
        {
            var dealer = dealersById[assignment.DealerId];
            _db.JmdoTaskAllocationDealers.Add(new JmdoTaskAllocationDealer
            {
                AllocationId = allocation.Id,
                SubDealerId = dealer.Id,
                DealerName = dealer.FirmName,
                DealerCode = dealer.SubDealerCode ?? "",
                PlannedDay = assignment.Day
            });
        }

        foreach (var program in programs)
        {
            _db.JmdoTaskAllocationPrograms.Add(new JmdoTaskAllocationProgram
            {
                AllocationId = allocation.Id,
                Csr1Id = program.Id,
                ProgramName = programNamesById.GetValueOrDefault(program.ProgramId, "Unnamed Program"),
                Budget = program.Budget
            });
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        var allocationDealers = await _db.JmdoTaskAllocationDealers
            .Where(d => d.AllocationId == allocation.Id)
            .ToListAsync();

        var allocationPrograms = await _db.JmdoTaskAllocationPrograms
            .Where(p => p.AllocationId == allocation.Id)
            .ToListAsync();

        var csr1Lookup = programs
            .ToDictionary(c => c.Id, c => (SanctionNo: c.SanctionNo, CSR1Date: (DateTime?)c.CSR1Date));

        return StatusCode(201, ToResponseDto(allocation, allocationDealers, allocationPrograms, null, csr1Lookup));
    }

    // GET api/JmdoTaskAllocation/mine/today
    [Authorize(Roles = "Admin,MO,MDO,JMDO")]
    [HttpGet("mine/today")]
    public async Task<ActionResult<JmdoTaskAllocationResponseDto>> GetMineToday()
    {
        var userId = CurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { Success = false, Message = "Could not identify the signed-in user." });

        var allocation = await _db.JmdoTaskAllocations
            .Include(a => a.Dealers)
            .Include(a => a.Programs)
            .Include(a => a.Closeouts).ThenInclude(c => c.Products)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.SubmittedByUserId == userId
                && a.AllocationDate >= CurrentWeekStart() && a.AllocationDate < CurrentWeekStart().AddDays(7));

        if (allocation == null)
        {
            allocation = await _db.JmdoTaskAllocations
                .Include(a => a.Dealers)
                .Include(a => a.Programs)
                .Include(a => a.Closeouts).ThenInclude(c => c.Products)
                .AsNoTracking()
                .OrderByDescending(a => a.AllocationDate)
                .FirstOrDefaultAsync(a => a.SubmittedByUserId == userId);
        }

        if (allocation == null)
            return NoContent();

        var csr1Ids = allocation.Programs.Select(p => p.Csr1Id).Distinct().ToList();
        var csr1Lookup = await _db.CSR1
            .Where(c => csr1Ids.Contains(c.Id))
            .Select(c => new { c.Id, c.SanctionNo, c.CSR1Date })
            .ToDictionaryAsync(c => c.Id, c => (SanctionNo: c.SanctionNo, CSR1Date: (DateTime?)c.CSR1Date));

        return Ok(ToResponseDto(allocation, allocation.Dealers.ToList(), allocation.Programs.ToList(), allocation.Closeouts.ToList(), csr1Lookup));
    }

    // Shared by every endpoint that acts on "my own" current allocation (status
    // updates, close-out records) - falls back to the user's latest allocation or
    // provisions one so task execution and closeout never fail with 404.
    private async Task<JmdoTaskAllocation> GetMyCurrentAllocationAsync(string userId)
    {
        var alloc = await _db.JmdoTaskAllocations
            .Include(a => a.Dealers)
            .Include(a => a.Programs)
            .FirstOrDefaultAsync(a => a.SubmittedByUserId == userId
                && a.AllocationDate >= CurrentWeekStart() && a.AllocationDate < CurrentWeekStart().AddDays(7));

        if (alloc == null)
        {
            alloc = await _db.JmdoTaskAllocations
                .Include(a => a.Dealers)
                .Include(a => a.Programs)
                .OrderByDescending(a => a.AllocationDate)
                .FirstOrDefaultAsync(a => a.SubmittedByUserId == userId);
        }

        if (alloc == null)
        {
            alloc = new JmdoTaskAllocation
            {
                AllocationDate = DateTime.Today,
                SubmittedByUserId = userId,
                SubmittedByRole = CurrentRole(),
                HeadquarterId = CurrentHqId(),
                RegionId = CurrentRegionId(),
                StateId = CurrentStateId(),
                Status = JmdoTaskAllocationStatus.Approved,
                CreatedAt = DateTime.Now,
                SubmittedAt = DateTime.Now
            };
            _db.JmdoTaskAllocations.Add(alloc);
            await _db.SaveChangesAsync();
        }

        return alloc;
    }

    private static DateTime CurrentWeekStart()
    {
        var today = DateTime.Today;
        return today.AddDays(-(int)today.DayOfWeek);
    }

    // GET api/JmdoTaskAllocation/pending
    // Feeds the MDO's "Allocation Requests" list - every Pending allocation
    // submitted by a JMDO at this MDO's HQ (same HQ-match convention as
    // api/Team/jmdos, since there's no literal reporting-line field).
    [Authorize(Roles = "Admin,MO,MDO")]
    [HttpGet("pending")]
    public async Task<ActionResult<List<PendingAllocationRowDto>>> GetPending()
    {
        var isAdmin = User.IsInRole("Admin");
        var hqId = CurrentHqId();

        // Admin is a global role with no HQ claim of its own (same reasoning as CheckHqScope's
        // own Admin bypass elsewhere in this controller) - requiring hqId > 0 here meant Admin
        // always got an empty list, regardless of how many allocations actually existed.
        if (!isAdmin && hqId is not > 0)
            return Ok(new List<PendingAllocationRowDto>());

        // Admin also gets already-approved allocations here - otherwise there would be no way
        // to even find one to reopen for a post-approval edit (MO/MDO keep seeing Pending only,
        // matching LoadEditableAllocation's own Admin-only bypass of the "already reviewed" lock).
        var visibleStatuses = isAdmin
            ? new[] { JmdoTaskAllocationStatus.Pending, JmdoTaskAllocationStatus.Approved }
            : new[] { JmdoTaskAllocationStatus.Pending };

        var allocations = await _db.JmdoTaskAllocations
            .Include(a => a.Dealers)
            .Include(a => a.Programs)
            .Where(a => visibleStatuses.Contains(a.Status) && (isAdmin || a.HeadquarterId == hqId!.Value))
            .OrderByDescending(a => a.SubmittedAt)
            .AsNoTracking()
            .ToListAsync();

        if (allocations.Count == 0)
            return Ok(new List<PendingAllocationRowDto>());

        var userIds = allocations.Select(a => a.SubmittedByUserId).Distinct().ToList();
        var jmdoInfo = await (
            from login in _db.Employeelogins
            join info in _db.EmployeeInformation on login.EmployeeInformationID equals info.Id
            join hq in _db.Headquarters on login.HeadquartersId equals hq.Id into hqJoin
            from hq in hqJoin.DefaultIfEmpty()
            where userIds.Contains(login.UserId)
            select new { login.UserId, info.Name, info.EmployeeCode, HeadquarterName = hq != null ? hq.HeadquarterName : null }
            ).ToDictionaryAsync(x => x.UserId);

        var rows = allocations.Select(a =>
        {
            jmdoInfo.TryGetValue(a.SubmittedByUserId, out var info);
            var posFieldsFilled = new[] { a.UreaTarget, a.DapTarget, a.NpsTarget, a.OthersTarget }.Count(v => v.HasValue);

            return new PendingAllocationRowDto
            {
                AllocationId = a.Id,
                Status = a.Status.ToString(),
                JmdoName = info?.Name ?? "Unknown JMDO",
                JmdoCode = info?.EmployeeCode,
                Location = info?.HeadquarterName,
                SubmittedAt = a.SubmittedAt,
                DealerCount = a.Dealers.Count,
                SpcmTarget = a.SpcmTarget,
                SoilSampleTarget = a.SoilSampleTarget,
                ProgramCount = a.Programs.Count,
                UreaTarget = a.UreaTarget,
                DapTarget = a.DapTarget,
                NpsTarget = a.NpsTarget,
                OthersTarget = a.OthersTarget,
                PosLiquidationFieldCount = posFieldsFilled
            };
        }).ToList();

        return Ok(rows);
    }

    // GET api/JmdoTaskAllocation/{id}
    // Full detail for the MDO's Review page.
    [Authorize(Roles = "Admin,MO,MDO")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<JmdoAllocationDetailDto>> GetById(int id)
    {
        var allocation = await _db.JmdoTaskAllocations
            .Include(a => a.Dealers)
            .Include(a => a.Programs)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (allocation == null)
            return NotFound();

        var forbidden = CheckHqScope(allocation);
        if (forbidden != null)
            return forbidden;

        var info = await GetJmdoInfoAsync(allocation.SubmittedByUserId);

        var csr1Ids = allocation.Programs.Select(p => p.Csr1Id).Distinct().ToList();
        var csr1Lookup = await _db.CSR1
            .Where(c => csr1Ids.Contains(c.Id))
            .Select(c => new { c.Id, c.SanctionNo, c.CSR1Date })
            .ToDictionaryAsync(c => c.Id, c => (SanctionNo: c.SanctionNo, CSR1Date: (DateTime?)c.CSR1Date));

        return Ok(new JmdoAllocationDetailDto
        {
            Id = allocation.Id,
            Status = allocation.Status.ToString(),
            JmdoName = info?.Name ?? "Unknown JMDO",
            JmdoCode = info?.EmployeeCode,
            Location = info?.HeadquarterName,
            SubmittedAt = allocation.SubmittedAt,
            HeadquarterId = allocation.HeadquarterId,
            Dealers = allocation.Dealers.Select(d => new JmdoAllocationDealerRowDto
            {
                RowId = d.Id,
                SubDealerId = d.SubDealerId,
                DealerName = d.DealerName,
                DealerCode = d.DealerCode,
                Day = d.PlannedDay,
                PlannedDate = d.PlannedDate,
                Priority = d.Priority
            }).ToList(),
            Programs = allocation.Programs.Select(p =>
            {
                var meta = csr1Lookup.TryGetValue(p.Csr1Id, out var m) ? m : default;
                return new JmdoAllocationProgramRowDto
                {
                    RowId = p.Id,
                    Csr1Id = p.Csr1Id,
                    ProgramName = p.ProgramName,
                    Budget = p.Budget,
                    Status = p.Status,
                    SanctionNo = meta.SanctionNo,
                    SanctionDate = meta.CSR1Date
                };
            }).ToList(),
            SpcmTarget = allocation.SpcmTarget,
            SoilSampleTarget = allocation.SoilSampleTarget,
            UreaTarget = allocation.UreaTarget,
            DapTarget = allocation.DapTarget,
            NpsTarget = allocation.NpsTarget,
            OthersTarget = allocation.OthersTarget
        });
    }

    // POST api/JmdoTaskAllocation/{id}/dealers
    [Authorize(Roles = "Admin,MO,MDO")]
    [HttpPost("{id:int}/dealers")]
    public async Task<ActionResult<JmdoAllocationDealerRowDto>> AddDealer(int id, [FromBody] AddDealerToAllocationDto dto)
    {
        var (allocation, forbidden) = await LoadEditableAllocation(id);
        if (allocation == null) return forbidden!;

        var dealer = await _db.SubDealerRegistrations.FindAsync(dto.DealerId);
        if (dealer == null)
            return BadRequest(new { Success = false, Message = "Dealer not found." });

        if (dto.PlannedDate.HasValue)
        {
            var alreadyOnThatDate = await _db.JmdoTaskAllocationDealers.AnyAsync(d =>
                d.AllocationId == id && d.SubDealerId == dto.DealerId
                && d.PlannedDate.HasValue && d.PlannedDate.Value.Date == dto.PlannedDate.Value.Date);
            if (alreadyOnThatDate)
                return BadRequest(new { Success = false, Message = "This dealer is already assigned on that date." });
        }

        var row = new JmdoTaskAllocationDealer
        {
            AllocationId = id,
            SubDealerId = dealer.Id,
            DealerName = dealer.FirmName,
            DealerCode = dealer.SubDealerCode ?? "",
            PlannedDate = dto.PlannedDate,
            // The JMDO's own Today's/Completed Allocation views filter strictly by
            // Day == DateTime.Today.DayOfWeek - without this, a dealer the MDO adds here never
            // shows up for the JMDO at all, no matter what PlannedDate says.
            PlannedDay = dto.PlannedDate?.DayOfWeek
        };
        _db.JmdoTaskAllocationDealers.Add(row);
        await _db.SaveChangesAsync();

        return Ok(new JmdoAllocationDealerRowDto
        {
            RowId = row.Id,
            SubDealerId = row.SubDealerId,
            DealerName = row.DealerName,
            DealerCode = row.DealerCode,
            Day = row.PlannedDay,
            PlannedDate = row.PlannedDate,
            Priority = row.Priority
        });
    }

    // DELETE api/JmdoTaskAllocation/{id}/dealers/{rowId}
    [Authorize(Roles = "Admin,MO,MDO")]
    [HttpDelete("{id:int}/dealers/{rowId:int}")]
    public async Task<IActionResult> RemoveDealer(int id, int rowId)
    {
        var (allocation, forbidden) = await LoadEditableAllocation(id);
        if (allocation == null) return forbidden!;

        var row = await _db.JmdoTaskAllocationDealers.FirstOrDefaultAsync(d => d.Id == rowId && d.AllocationId == id);
        if (row == null)
            return NotFound();

        _db.JmdoTaskAllocationDealers.Remove(row);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // PUT api/JmdoTaskAllocation/{id}/dealers/{rowId}/date
    [Authorize(Roles = "Admin,MO,MDO")]
    [HttpPut("{id:int}/dealers/{rowId:int}/date")]
    public async Task<IActionResult> UpdateDealerPlannedDate(int id, int rowId, [FromBody] UpdateDealerPlannedDateDto dto)
    {
        var (allocation, forbidden) = await LoadEditableAllocation(id);
        if (allocation == null) return forbidden!;

        var row = await _db.JmdoTaskAllocationDealers.FirstOrDefaultAsync(d => d.Id == rowId && d.AllocationId == id);
        if (row == null)
            return NotFound();

        if (dto.PlannedDate.HasValue)
        {
            var alreadyOnThatDate = await _db.JmdoTaskAllocationDealers.AnyAsync(d =>
                d.AllocationId == id && d.Id != rowId && d.SubDealerId == row.SubDealerId
                && d.PlannedDate.HasValue && d.PlannedDate.Value.Date == dto.PlannedDate.Value.Date);
            if (alreadyOnThatDate)
                return BadRequest(new { Success = false, Message = "This dealer is already assigned on that date." });
        }

        row.PlannedDate = dto.PlannedDate;
        // Keep Day in sync with the new date - see the identical comment in AddDealer above;
        // the JMDO's weekday-filtered views would otherwise keep showing/hiding this task based
        // on whatever day it was originally planned for, ignoring the MDO's edit entirely.
        row.PlannedDay = dto.PlannedDate?.DayOfWeek;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // PUT api/JmdoTaskAllocation/{id}/dealers/{rowId}/priority
    // Lets the MDO flag a specific dealer as needing priority during review - optional, off by
    // default, never set by the JMDO at submission.
    [Authorize(Roles = "Admin,MO,MDO")]
    [HttpPut("{id:int}/dealers/{rowId:int}/priority")]
    public async Task<IActionResult> UpdateDealerPriority(int id, int rowId, [FromBody] UpdateDealerPriorityDto dto)
    {
        var (allocation, forbidden) = await LoadEditableAllocation(id);
        if (allocation == null) return forbidden!;

        var row = await _db.JmdoTaskAllocationDealers.FirstOrDefaultAsync(d => d.Id == rowId && d.AllocationId == id);
        if (row == null)
            return NotFound();

        row.Priority = dto.Priority;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST api/JmdoTaskAllocation/{id}/programs
    [Authorize(Roles = "Admin,MO,MDO")]
    [HttpPost("{id:int}/programs")]
    public async Task<ActionResult<JmdoAllocationProgramRowDto>> AddProgram(int id, [FromBody] AddProgramToAllocationDto dto)
    {
        var (allocation, forbidden) = await LoadEditableAllocation(id);
        if (allocation == null) return forbidden!;

        var program = await _db.CSR1.FirstOrDefaultAsync(p => p.Id == dto.Csr1Id);
        if (program == null)
            return BadRequest(new { Success = false, Message = "Program not found." });

        var already = await _db.JmdoTaskAllocationPrograms.AnyAsync(p => p.AllocationId == id && p.Csr1Id == dto.Csr1Id);
        if (already)
            return BadRequest(new { Success = false, Message = "This program is already on the allocation." });

        var programName = await _db.ProgramMasters
            .Where(pm => pm.Id == program.ProgramId)
            .Select(pm => pm.Name)
            .FirstOrDefaultAsync() ?? "Unnamed Program";

        var row = new JmdoTaskAllocationProgram
        {
            AllocationId = id,
            Csr1Id = program.Id,
            ProgramName = programName,
            Budget = program.Budget
        };
        _db.JmdoTaskAllocationPrograms.Add(row);
        await _db.SaveChangesAsync();

        return Ok(new JmdoAllocationProgramRowDto
        {
            RowId = row.Id,
            Csr1Id = row.Csr1Id,
            ProgramName = row.ProgramName,
            Budget = row.Budget,
            SanctionNo = program.SanctionNo,
            SanctionDate = program.CSR1Date
        });
    }

    // DELETE api/JmdoTaskAllocation/{id}/programs/{rowId}
    [Authorize(Roles = "Admin,MO,MDO")]
    [HttpDelete("{id:int}/programs/{rowId:int}")]
    public async Task<IActionResult> RemoveProgram(int id, int rowId)
    {
        var (allocation, forbidden) = await LoadEditableAllocation(id);
        if (allocation == null) return forbidden!;

        var row = await _db.JmdoTaskAllocationPrograms.FirstOrDefaultAsync(p => p.Id == rowId && p.AllocationId == id);
        if (row == null)
            return NotFound();

        _db.JmdoTaskAllocationPrograms.Remove(row);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // PUT api/JmdoTaskAllocation/{id}/targets
    [Authorize(Roles = "Admin,MO,MDO")]
    [HttpPut("{id:int}/targets")]
    public async Task<IActionResult> UpdateTargets(int id, [FromBody] UpdateAllocationTargetsDto dto)
    {
        var (allocation, forbidden) = await LoadEditableAllocation(id);
        if (allocation == null) return forbidden!;

        allocation.SpcmTarget = dto.SpcmTarget;
        allocation.SoilSampleTarget = dto.SoilSampleTarget;
        allocation.UreaTarget = dto.UreaTarget;
        allocation.DapTarget = dto.DapTarget;
        allocation.NpsTarget = dto.NpsTarget;
        allocation.OthersTarget = dto.OthersTarget;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST api/JmdoTaskAllocation/{id}/approve
    [Authorize(Roles = "Admin,MO,MDO")]
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var (allocation, forbidden) = await LoadEditableAllocation(id);
        if (allocation == null) return forbidden!;

        allocation.Status = JmdoTaskAllocationStatus.Approved;
        allocation.ReviewedByUserId = CurrentUserId();
        allocation.ReviewedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // PUT api/JmdoTaskAllocation/mine/task-status
    // Called from TaskDetail's Start/Stop buttons. TaskKey reuses the exact string
    // convention the client already builds for itself, so there's one definition of
    // "which task is this" shared by both sides instead of two that could drift apart.
    [Authorize(Roles = "Admin,MO,MDO,JMDO")]
    [HttpPut("mine/task-status")]
    public async Task<IActionResult> UpdateMyTaskStatus([FromBody] UpdateTaskStatusDto dto)
    {
        var userId = CurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { Success = false, Message = "Could not identify the signed-in user." });

        var allocation = await GetMyCurrentAllocationAsync(userId);
        if (allocation == null)
            return NotFound(new { Success = false, Message = "No current allocation found." });

        // Stamped the first time a task goes InProgress (never overwritten on later calls)
        // so "Time on task" can be computed as DateTime.Now - StartedAt on every page load
        // instead of resetting to 0 each time the circuit/session is re-established.
        var startedAt = dto.Status == AllocTaskStatus.InProgress ? DateTime.Now : (DateTime?)null;

        // Stamped the moment a task is closed, so "Time on task" freezes at the real
        // CompletedAt - StartedAt duration instead of showing 00:00 on every later page load
        // (the in-browser timer was never persisted anywhere, only StartedAt was).
        var completedAt = dto.Status == AllocTaskStatus.Completed ? DateTime.Now : (DateTime?)null;

        // Only meaningful alongside startedAt - cached once so TaskDetail doesn't need to
        // re-resolve it from the geocoding service every time the page is reopened.
        var placeName = dto.Status == AllocTaskStatus.InProgress ? dto.PlaceName : null;

        var taskKey = dto.TaskKey?.Trim() ?? string.Empty;

        if (taskKey.StartsWith("dealer_", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(taskKey.AsSpan("dealer_".Length), out var dealerId))
        {
            var dealer = allocation.Dealers.FirstOrDefault(d => d.SubDealerId == dealerId);
            if (dealer == null) return NotFound(new { Success = false, Message = "Dealer task not found." });
            dealer.Status = dto.Status;
            if (startedAt.HasValue && dealer.StartedAt is null) dealer.StartedAt = startedAt;
            if (completedAt.HasValue && dealer.CompletedAt is null) dealer.CompletedAt = completedAt;
            if (placeName != null && dealer.StartPlaceName is null) dealer.StartPlaceName = placeName;
        }
        else if (taskKey.StartsWith("prog_", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(taskKey.AsSpan("prog_".Length), out var csr1Id))
        {
            var program = allocation.Programs.FirstOrDefault(p => p.Csr1Id == csr1Id);
            if (program == null) return NotFound(new { Success = false, Message = "Program task not found." });
            program.Status = dto.Status;
            if (startedAt.HasValue && program.StartedAt is null) program.StartedAt = startedAt;
            if (completedAt.HasValue && program.CompletedAt is null) program.CompletedAt = completedAt;
            if (placeName != null && program.StartPlaceName is null) program.StartPlaceName = placeName;
        }
        else if (string.Equals(taskKey, "spcm", StringComparison.OrdinalIgnoreCase))
        {
            allocation.SpcmStatus = dto.Status;
            if (startedAt.HasValue && allocation.SpcmStartedAt is null) allocation.SpcmStartedAt = startedAt;
            if (completedAt.HasValue && allocation.SpcmCompletedAt is null) allocation.SpcmCompletedAt = completedAt;
            if (placeName != null && allocation.SpcmStartPlaceName is null) allocation.SpcmStartPlaceName = placeName;
        }
        else if (string.Equals(taskKey, "soil", StringComparison.OrdinalIgnoreCase)
              || string.Equals(taskKey, "soilsample", StringComparison.OrdinalIgnoreCase))
        {
            allocation.SoilSampleStatus = dto.Status;
            if (startedAt.HasValue && allocation.SoilSampleStartedAt is null) allocation.SoilSampleStartedAt = startedAt;
            if (completedAt.HasValue && allocation.SoilSampleCompletedAt is null) allocation.SoilSampleCompletedAt = completedAt;
            if (placeName != null && allocation.SoilSampleStartPlaceName is null) allocation.SoilSampleStartPlaceName = placeName;
        }
        else if (string.Equals(taskKey, "pos", StringComparison.OrdinalIgnoreCase)
              || string.Equals(taskKey, "posliquidation", StringComparison.OrdinalIgnoreCase))
        {
            allocation.PosLiquidationStatus = dto.Status;
            if (startedAt.HasValue && allocation.PosLiquidationStartedAt is null) allocation.PosLiquidationStartedAt = startedAt;
            if (completedAt.HasValue && allocation.PosLiquidationCompletedAt is null) allocation.PosLiquidationCompletedAt = completedAt;
            if (placeName != null && allocation.PosLiquidationStartPlaceName is null) allocation.PosLiquidationStartPlaceName = placeName;
        }
        else if (string.Equals(taskKey, "dealer", StringComparison.OrdinalIgnoreCase))
        {
            var dealer = allocation.Dealers.FirstOrDefault();
            if (dealer != null)
            {
                dealer.Status = dto.Status;
                if (startedAt.HasValue && dealer.StartedAt is null) dealer.StartedAt = startedAt;
                if (completedAt.HasValue && dealer.CompletedAt is null) dealer.CompletedAt = completedAt;
                if (placeName != null && dealer.StartPlaceName is null) dealer.StartPlaceName = placeName;
            }
        }
        else if (string.Equals(taskKey, "mdprogram", StringComparison.OrdinalIgnoreCase))
        {
            var program = allocation.Programs.FirstOrDefault();
            if (program != null)
            {
                program.Status = dto.Status;
                if (startedAt.HasValue && program.StartedAt is null) program.StartedAt = startedAt;
                if (completedAt.HasValue && program.CompletedAt is null) program.CompletedAt = completedAt;
                if (placeName != null && program.StartPlaceName is null) program.StartPlaceName = placeName;
            }
        }
        else return BadRequest(new { Success = false, Message = $"Unrecognized task key '{taskKey}'." });

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST api/JmdoTaskAllocation/closeouts
    [Authorize(Roles = "Admin,MO,MDO,JMDO")]
    [HttpPost("closeouts")]
    public async Task<ActionResult<JmdoTaskCloseoutDto>> CreateCloseout([FromBody] CreateJmdoTaskCloseoutDto dto)
    {
        var userId = CurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { Success = false, Message = "Could not identify the signed-in user." });

        var allocation = await GetMyCurrentAllocationAsync(userId);
        if (allocation == null)
            return NotFound(new { Success = false, Message = "No current allocation found." });

        var closeout = new JmdoTaskCloseout
        {
            AllocationId = allocation.Id,
            Kind = dto.Kind,
            SubDealerId = dto.SubDealerId,
            Csr1Id = dto.Csr1Id,
            Outcome = dto.Outcome,
            Remarks = dto.Remarks,
            AchievedTarget = dto.AchievedTarget,
            Urea = dto.Urea,
            Nps = dto.Nps,
            Dap = dto.Dap,
            Other = dto.Other,
            FilePath = dto.FilePath,
            FileName = dto.FileName,
            CreatedByUserId = userId,
            CreatedAt = DateTime.Now
        };

        foreach (var product in dto.Products)
        {
            closeout.Products.Add(new JmdoTaskCloseoutProduct
            {
                ProductGroupName = product.ProductGroupName,
                ProductName = product.ProductName,
                StockText = product.StockText
            });
        }

        _db.JmdoTaskCloseouts.Add(closeout);
        await _db.SaveChangesAsync();

        return Ok(new JmdoTaskCloseoutDto
        {
            Id = closeout.Id,
            Kind = closeout.Kind,
            SubDealerId = closeout.SubDealerId,
            Csr1Id = closeout.Csr1Id,
            Outcome = closeout.Outcome,
            Remarks = closeout.Remarks,
            AchievedTarget = closeout.AchievedTarget,
            Urea = closeout.Urea,
            Nps = closeout.Nps,
            Dap = closeout.Dap,
            Other = closeout.Other,
            FilePath = closeout.FilePath,
            FileName = closeout.FileName,
            CreatedAt = closeout.CreatedAt,
            Products = closeout.Products.Select(p => new ProductLineDto
            {
                ProductGroupName = p.ProductGroupName,
                ProductName = p.ProductName,
                StockText = p.StockText
            }).ToList()
        });
    }

    // DELETE api/JmdoTaskAllocation/closeouts/{id}
    [Authorize(Roles = "Admin,MO,MDO,JMDO")]
    [HttpDelete("closeouts/{id:int}")]
    public async Task<IActionResult> DeleteCloseout(int id)
    {
        var userId = CurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { Success = false, Message = "Could not identify the signed-in user." });

        var closeout = await _db.JmdoTaskCloseouts
            .Include(c => c.Allocation)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (closeout == null)
            return NotFound();

        // Only the JMDO who owns the allocation (or Admin) can delete their own close-out records.
        if (closeout.Allocation?.SubmittedByUserId != userId && !User.IsInRole("Admin"))
            return Forbid();

        _db.JmdoTaskCloseouts.Remove(closeout);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST api/JmdoTaskAllocation/closeout-files/upload
    // Same shape as SubDealerFileController.UploadGst: 5 MB limit, allow-listed
    // extensions, GUID filename under wwwroot so the returned path is directly
    // usable as an <a href> with no separate streaming endpoint needed.
    private const long MaxCloseoutFileSize = 5 * 1024 * 1024;
    private static readonly string[] AllowedCloseoutExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

    [Authorize(Roles = "Admin,MO,MDO,JMDO")]
    [HttpPost("closeout-files/upload")]
    [RequestSizeLimit(MaxCloseoutFileSize)]
    public async Task<ActionResult<CloseoutFileUploadResponse>> UploadCloseoutFile(IFormFile file, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { Success = false, Message = "Could not identify the signed-in user." });

        if (file == null || file.Length == 0)
            return BadRequest(new { Success = false, Message = "File is required." });

        if (file.Length > MaxCloseoutFileSize)
            return BadRequest(new { Success = false, Message = "File size must be less than 5 MB." });

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedCloseoutExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { Success = false, Message = "Only PDF, JPG, and PNG files are allowed." });

        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");

        var relativeDirectory = Path.Combine("uploads", "jmdo-closeouts", userId);
        var physicalDirectory = Path.Combine(webRoot, relativeDirectory);
        Directory.CreateDirectory(physicalDirectory);

        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(physicalDirectory, safeFileName);

        await using (var output = System.IO.File.Create(physicalPath))
        {
            await file.CopyToAsync(output, cancellationToken);
        }

        var relativePath = "/" + Path.Combine(relativeDirectory, safeFileName).Replace("\\", "/");

        return Ok(new CloseoutFileUploadResponse { FilePath = relativePath });
    }

    // GET api/JmdoTaskAllocation/closeout-files/download?path=...&fileName=...
    // The static-served FilePath works fine for "View", but the HTML5 `download`
    // attribute on an <a> is silently ignored by browsers for cross-origin URLs
    // (the Blazor app and this API run on different origins) - it just opens the
    // file like View does. Setting Content-Disposition: attachment server-side
    // forces a real download regardless of origin, so this is a second route onto
    // the exact same file, not a duplicate store.
    // Anonymous, like the static file serving "View" already uses for this same folder -
    // this app authenticates via a Bearer token, which a plain <a href> navigation can't
    // attach, and the file is already reachable without auth via its static URL anyway.
    [AllowAnonymous]
    [HttpGet("closeout-files/download")]
    public IActionResult DownloadCloseoutFile([FromQuery] string path, [FromQuery] string? fileName)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest(new { Success = false, Message = "path is required." });

        var normalizedRelative = path.Trim().Replace("\\", "/").TrimStart('/');
        if (!normalizedRelative.StartsWith("uploads/jmdo-closeouts/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { Success = false, Message = "Invalid file path." });

        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");

        var allowedRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads", "jmdo-closeouts"));
        var requestedFile = Path.GetFullPath(Path.Combine(webRoot, normalizedRelative));
        if (!requestedFile.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { Success = false, Message = "Invalid file path." });

        if (!System.IO.File.Exists(requestedFile))
            return NotFound();

        var contentType = Path.GetExtension(requestedFile).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };

        var downloadName = string.IsNullOrWhiteSpace(fileName) ? Path.GetFileName(requestedFile) : fileName;
        return PhysicalFile(requestedFile, contentType, downloadName);
    }

    // Shared by every edit/approve endpoint: loads the allocation, enforces HQ
    // scope, and rejects edits once it's no longer Pending (already reviewed) -
    // except for Admin, who the business wants to still be able to adjust a
    // dealer/date/target/etc. on an allocation even after the MDO has approved it.
    private async Task<(JmdoTaskAllocation? Allocation, ActionResult? Forbidden)> LoadEditableAllocation(int id)
    {
        var allocation = await _db.JmdoTaskAllocations.FindAsync(id);
        if (allocation == null)
            return (null, NotFound());

        var forbidden = CheckHqScope(allocation);
        if (forbidden != null)
            return (null, forbidden);

        if (allocation.Status != JmdoTaskAllocationStatus.Pending && !User.IsInRole("Admin"))
            return (null, BadRequest(new { Success = false, Message = "This allocation has already been reviewed." }));

        return (allocation, null);
    }

    private ActionResult? CheckHqScope(JmdoTaskAllocation allocation)
    {
        if (User.IsInRole("Admin"))
            return null;

        var hqId = CurrentHqId();
        if (hqId is not > 0 || allocation.HeadquarterId != hqId)
            return Forbid();

        return null;
    }

    private async Task<(string Name, string? EmployeeCode, string? HeadquarterName)?> GetJmdoInfoAsync(string userId)
    {
        var info = await (
            from login in _db.Employeelogins
            join emp in _db.EmployeeInformation on login.EmployeeInformationID equals emp.Id
            join hq in _db.Headquarters on login.HeadquartersId equals hq.Id into hqJoin
            from hq in hqJoin.DefaultIfEmpty()
            where login.UserId == userId
            select new { emp.Name, emp.EmployeeCode, HeadquarterName = hq != null ? hq.HeadquarterName : null }
            ).FirstOrDefaultAsync();

        return info == null ? null : (info.Name, info.EmployeeCode, info.HeadquarterName);
    }

    private static JmdoTaskAllocationResponseDto ToResponseDto(
        JmdoTaskAllocation allocation,
        List<JmdoTaskAllocationDealer> dealers,
        List<JmdoTaskAllocationProgram> programs,
        List<JmdoTaskCloseout>? closeouts = null,
        Dictionary<int, (string? SanctionNo, DateTime? CSR1Date)>? csr1Lookup = null) => new()
    {
        Id = allocation.Id,
        AllocationDate = allocation.AllocationDate,
        Status = allocation.Status.ToString(),
        SubmittedAt = allocation.SubmittedAt,
        DealerCount = dealers.Count,
        ProgramCount = programs.Count,
        DealerAssignments = dealers.Select(d => new JmdoDealerAssignmentResponseDto
        {
            DealerId = d.SubDealerId,
            DealerName = d.DealerName,
            DealerCode = d.DealerCode,
            Day = d.PlannedDay,
            Status = d.Status,
            StartedAt = d.StartedAt,
            CompletedAt = d.CompletedAt,
            StartPlaceName = d.StartPlaceName,
            Priority = d.Priority
        }).ToList(),
        Programs = programs.Select(p =>
        {
            var meta = csr1Lookup != null && csr1Lookup.TryGetValue(p.Csr1Id, out var m) ? m : default;
            return new JmdoAllocationProgramRowDto
            {
                RowId = p.Id,
                Csr1Id = p.Csr1Id,
                ProgramName = p.ProgramName,
                Budget = p.Budget,
                Status = p.Status,
                StartedAt = p.StartedAt,
                CompletedAt = p.CompletedAt,
                StartPlaceName = p.StartPlaceName,
                SanctionNo = meta.SanctionNo,
                SanctionDate = meta.CSR1Date
            };
        }).ToList(),
        SpcmTarget = allocation.SpcmTarget,
        SoilSampleTarget = allocation.SoilSampleTarget,
        UreaTarget = allocation.UreaTarget,
        DapTarget = allocation.DapTarget,
        NpsTarget = allocation.NpsTarget,
        OthersTarget = allocation.OthersTarget,
        SpcmStatus = allocation.SpcmStatus,
        SoilSampleStatus = allocation.SoilSampleStatus,
        PosLiquidationStatus = allocation.PosLiquidationStatus,
        SpcmStartedAt = allocation.SpcmStartedAt,
        SoilSampleStartedAt = allocation.SoilSampleStartedAt,
        PosLiquidationStartedAt = allocation.PosLiquidationStartedAt,
        SpcmCompletedAt = allocation.SpcmCompletedAt,
        SoilSampleCompletedAt = allocation.SoilSampleCompletedAt,
        PosLiquidationCompletedAt = allocation.PosLiquidationCompletedAt,
        SpcmStartPlaceName = allocation.SpcmStartPlaceName,
        SoilSampleStartPlaceName = allocation.SoilSampleStartPlaceName,
        PosLiquidationStartPlaceName = allocation.PosLiquidationStartPlaceName,
        Closeouts = (closeouts ?? new()).Select(c => new JmdoTaskCloseoutDto
        {
            Id = c.Id,
            Kind = c.Kind,
            SubDealerId = c.SubDealerId,
            Csr1Id = c.Csr1Id,
            Outcome = c.Outcome,
            Remarks = c.Remarks,
            AchievedTarget = c.AchievedTarget,
            Urea = c.Urea,
            Nps = c.Nps,
            Dap = c.Dap,
            Other = c.Other,
            FilePath = c.FilePath,
            FileName = c.FileName,
            CreatedAt = c.CreatedAt,
            Products = c.Products.Select(p => new ProductLineDto
            {
                ProductGroupName = p.ProductGroupName,
                ProductName = p.ProductName,
                StockText = p.StockText
            }).ToList()
        }).ToList()
    };
}
