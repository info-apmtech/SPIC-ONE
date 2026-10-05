using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

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
            dto.DealerIds.Count > 0 ||
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

        var dealers = await _db.SubDealerRegistrations
            .Where(d => dto.DealerIds.Contains(d.Id))
            .ToListAsync();
        if (dealers.Count != dto.DealerIds.Distinct().Count())
            return BadRequest(new { Success = false, Message = "One or more selected dealers could not be found." });

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

        foreach (var dealer in dealers)
        {
            _db.JmdoTaskAllocationDealers.Add(new JmdoTaskAllocationDealer
            {
                AllocationId = allocation.Id,
                SubDealerId = dealer.Id,
                DealerName = dealer.FirmName,
                DealerCode = dealer.SubDealerCode ?? ""
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

        return StatusCode(201, ToResponseDto(allocation, dealers.Count, programs.Count));
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

        return Ok(ToResponseDto(allocation, allocation.Dealers.Count, allocation.Programs.Count));
    }

    private static JmdoTaskAllocationResponseDto ToResponseDto(JmdoTaskAllocation allocation, int dealerCount, int programCount) => new()
    {
        Id = allocation.Id,
        AllocationDate = allocation.AllocationDate,
        Status = allocation.Status.ToString(),
        SubmittedAt = allocation.SubmittedAt,
        DealerCount = dealerCount,
        ProgramCount = programCount,
        SpcmTarget = allocation.SpcmTarget,
        SoilSampleTarget = allocation.SoilSampleTarget,
        UreaTarget = allocation.UreaTarget,
        DapTarget = allocation.DapTarget,
        NpsTarget = allocation.NpsTarget,
        OthersTarget = allocation.OthersTarget
    };
}
