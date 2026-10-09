using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

// MD Portal - Program Master page, Program Type tab. ProgramTypes has no IsActive
// column, so this is list/add/edit only (no status toggle, no delete).
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProgramTypeController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProgramTypeController(AppDbContext db)
    {
        _db = db;
    }

    private string CurrentUser =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "System";

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.ProgramTypes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new ProgramTypeDto
            {
                Id = x.Id,
                Name = x.Name,
                IsChangeAmount = x.IsChangeAmount,
                CreatedBy = x.CreatedBy,
                UpdatedBy = x.UpdatedBy,
                CreatedAt = x.CreatedAt,
                ProgramCount = x.Programs.Count
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProgramTypeSaveRequest request)
    {
        var name = request?.Name?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Program Type name is required." });

        if (await NameExists(name, null))
            return Conflict(new { message = $"Program Type \"{name}\" already exists." });

        var entity = new ProgramType
        {
            Name = name,
            IsChangeAmount = request?.IsChangeAmount ?? false,
            CreatedBy = CurrentUser,
            UpdatedBy = CurrentUser,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _db.ProgramTypes.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Program Type created successfully", data = new { entity.Id, entity.Name, entity.IsChangeAmount } });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProgramTypeSaveRequest request)
    {
        var name = request?.Name?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Program Type name is required." });

        var entity = await _db.ProgramTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null) return NotFound(new { message = "Program Type not found." });

        if (await NameExists(name, id))
            return Conflict(new { message = $"Program Type \"{name}\" already exists." });

        entity.Name = name;
        entity.IsChangeAmount = request?.IsChangeAmount ?? false;
        entity.UpdatedBy = CurrentUser;
        entity.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Program Type updated successfully", data = new { entity.Id, entity.Name, entity.IsChangeAmount } });
    }

    private Task<bool> NameExists(string name, int? excludeId)
    {
        var lower = name.ToLower();
        return _db.ProgramTypes.AnyAsync(x =>
            x.Name.Trim().ToLower() == lower &&
            (excludeId == null || x.Id != excludeId));
    }
}
