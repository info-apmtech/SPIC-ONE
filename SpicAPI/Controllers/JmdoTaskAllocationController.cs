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

    public JmdoTaskAllocationController(AppDbContext db)
    {
        _db = db;
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

        return StatusCode(201, ToResponseDto(allocation, allocationDealers, programs.Count));
    }

    // GET api/JmdoTaskAllocation/mine/today
    [Authorize(Roles = "Admin,MO,MDO,JMDO")]
    [HttpGet("mine/today")]
    public async Task<ActionResult<JmdoTaskAllocationResponseDto>> GetMineToday()
    {
        var userId = CurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { Success = false, Message = "Could not identify the signed-in user." });

        var today = DateTime.Today;
        var allocation = await _db.JmdoTaskAllocations
            .Include(a => a.Dealers)
            .Include(a => a.Programs)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.SubmittedByUserId == userId && a.AllocationDate == today);

        if (allocation == null)
            return NoContent();

        return Ok(ToResponseDto(allocation, allocation.Dealers.ToList(), allocation.Programs.Count));
    }

    // GET api/JmdoTaskAllocation/pending
    // Feeds the MDO's "Allocation Requests" list - every Pending allocation
    // submitted by a JMDO at this MDO's HQ (same HQ-match convention as
    // api/Team/jmdos, since there's no literal reporting-line field).
    [Authorize(Roles = "Admin,MO,MDO")]
    [HttpGet("pending")]
    public async Task<ActionResult<List<PendingAllocationRowDto>>> GetPending()
    {
        var hqId = CurrentHqId();
        if (hqId is not > 0)
            return Ok(new List<PendingAllocationRowDto>());

        var allocations = await _db.JmdoTaskAllocations
            .Include(a => a.Dealers)
            .Include(a => a.Programs)
            .Where(a => a.Status == JmdoTaskAllocationStatus.Pending && a.HeadquarterId == hqId.Value)
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
                JmdoName = info?.Name ?? "Unknown JMDO",
                JmdoCode = info?.EmployeeCode,
                Location = info?.HeadquarterName,
                SubmittedAt = a.SubmittedAt,
                DealerCount = a.Dealers.Count,
                SpcmTarget = a.SpcmTarget,
                SoilSampleTarget = a.SoilSampleTarget,
                ProgramCount = a.Programs.Count,
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
                PlannedDate = d.PlannedDate
            }).ToList(),
            Programs = allocation.Programs.Select(p => new JmdoAllocationProgramRowDto
            {
                RowId = p.Id,
                Csr1Id = p.Csr1Id,
                ProgramName = p.ProgramName,
                Budget = p.Budget
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

        var already = await _db.JmdoTaskAllocationDealers.AnyAsync(d => d.AllocationId == id && d.SubDealerId == dto.DealerId);
        if (already)
            return BadRequest(new { Success = false, Message = "This dealer is already on the allocation." });

        var row = new JmdoTaskAllocationDealer
        {
            AllocationId = id,
            SubDealerId = dealer.Id,
            DealerName = dealer.FirmName,
            DealerCode = dealer.SubDealerCode ?? "",
            PlannedDate = dto.PlannedDate
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
            PlannedDate = row.PlannedDate
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

        row.PlannedDate = dto.PlannedDate;
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
            Budget = row.Budget
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

    // Shared by every edit/approve endpoint: loads the allocation, enforces HQ
    // scope, and rejects edits once it's no longer Pending (already reviewed).
    private async Task<(JmdoTaskAllocation? Allocation, ActionResult? Forbidden)> LoadEditableAllocation(int id)
    {
        var allocation = await _db.JmdoTaskAllocations.FindAsync(id);
        if (allocation == null)
            return (null, NotFound());

        var forbidden = CheckHqScope(allocation);
        if (forbidden != null)
            return (null, forbidden);

        if (allocation.Status != JmdoTaskAllocationStatus.Pending)
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
        int programCount) => new()
    {
        Id = allocation.Id,
        AllocationDate = allocation.AllocationDate,
        Status = allocation.Status.ToString(),
        SubmittedAt = allocation.SubmittedAt,
        DealerCount = dealers.Count,
        ProgramCount = programCount,
        DealerAssignments = dealers.Select(d => new JmdoDealerAssignmentResponseDto
        {
            DealerId = d.SubDealerId,
            DealerName = d.DealerName,
            DealerCode = d.DealerCode,
            Day = d.PlannedDay
        }).ToList(),
        SpcmTarget = allocation.SpcmTarget,
        SoilSampleTarget = allocation.SoilSampleTarget,
        UreaTarget = allocation.UreaTarget,
        DapTarget = allocation.DapTarget,
        NpsTarget = allocation.NpsTarget,
        OthersTarget = allocation.OthersTarget
    };
}
