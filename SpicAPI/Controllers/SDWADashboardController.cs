using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SDWADashboardController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SDWADashboardController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("dealer-details")]
        public async Task<ActionResult<SDWADashboardDealerDto>> GetDealerDetails()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            var dealer = await _db.DealerRegistrations
                .FirstOrDefaultAsync(d => d.UserTableId == userId);

            var stateName = string.Empty;
            if (dealer != null && dealer.StateId > 0)
            {
                var state = await _db.States.FirstOrDefaultAsync(s => s.Id == dealer.StateId);
                stateName = state?.StateName ?? string.Empty;
            }

            var dto = new SDWADashboardDealerDto
            {
                DealerName = user.Name ?? string.Empty,
                DealerCode = dealer?.SPICCode ?? dealer?.DealerCode ?? string.Empty,
                Region = stateName,
                ProfileCompletion = CalculateProfileCompletion(dealer)
            };

            return Ok(dto);
        }

        private static int CalculateProfileCompletion(SPIC.Core.Entities.DealerRegistration? dealer)
        {
            if (dealer == null)
                return 0;

            int filled = 0;
            int total = 10;

            if (!string.IsNullOrWhiteSpace(dealer.FirmName)) filled++;
            if (!string.IsNullOrWhiteSpace(dealer.SPICCode) || !string.IsNullOrWhiteSpace(dealer.DealerCode)) filled++;
            if (dealer.StateId > 0) filled++;
            if (!string.IsNullOrWhiteSpace(dealer.OfficialContactNumber)) filled++;
            if (!string.IsNullOrWhiteSpace(dealer.WhatsAppNumber)) filled++;
            if (!string.IsNullOrWhiteSpace(dealer.GSTNumber)) filled++;
            if (!string.IsNullOrWhiteSpace(dealer.PANNumber)) filled++;
            if (!string.IsNullOrWhiteSpace(dealer.AadhaarNumber)) filled++;
            if (!string.IsNullOrWhiteSpace(dealer.AccountHolderName) && !string.IsNullOrWhiteSpace(dealer.AccountNumber)) filled++;
            if (!string.IsNullOrWhiteSpace(dealer.Village) && !string.IsNullOrWhiteSpace(dealer.PinCode)) filled++;

            return (int)Math.Round((double)filled / total * 100);
        }
    }
}
