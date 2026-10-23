using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace SPIC.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExpenseInformationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ExpenseInformationController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.ExpenseInformations
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ExpenseInformation model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return BadRequest(new
                {
                    message = "Expense name is required."
                });
            }

            var name = model.Name.Trim();

            var exists = await _context.ExpenseInformations
                .AnyAsync(x => x.Name.ToLower() == name.ToLower());

            if (exists)
            {
                return BadRequest(new
                {
                    message = "Expense name already exists."
                });
            }

            var entity = new ExpenseInformation
            {
                Name = name,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = model.CreatedBy
            };

            _context.ExpenseInformations.Add(entity);

            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] ExpenseInformation model)
        {
            var entity = await _context.ExpenseInformations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
            {
                return NotFound(new
                {
                    message = "Expense information not found."
                });
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return BadRequest(new
                {
                    message = "Expense name is required."
                });
            }

            var name = model.Name.Trim();

            var duplicate = await _context.ExpenseInformations
                .AnyAsync(x =>
                    x.Id != id &&
                    x.Name.ToLower() == name.ToLower());

            if (duplicate)
            {
                return BadRequest(new
                {
                    message = "Expense name already exists."
                });
            }

            entity.Name = name;
            entity.IsActive = model.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = model.UpdatedBy;

            await _context.SaveChangesAsync();

            return Ok(entity);
        }

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromQuery] bool isActive)
        {
            var entity = await _context.ExpenseInformations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
            {
                return NotFound(new
                {
                    message = "Expense information not found."
                });
            }

            entity.IsActive = isActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Status updated successfully."
            });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.ExpenseInformations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
            {
                return NotFound(new
                {
                    message = "Expense information not found."
                });
            }

            _context.ExpenseInformations.Remove(entity);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Expense information deleted successfully."
            });
        }
    }
}