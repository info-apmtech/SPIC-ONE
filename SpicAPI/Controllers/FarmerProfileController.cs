using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
using Spic.Infrastructure.Data;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// The signed-in farmer's own profile (docs/farmer-portal.md). Farmer role only; every query is
    /// scoped to the caller's user id, so a farmer can never read or write another farmer's row.
    ///
    ///   GET api/Farmer/profile -> FarmerProfileDto, or 404 { profileComplete: false, sasLinked, phone }
    ///   PUT api/Farmer/profile -> validates (FarmerProfileRules + state / district exist and match),
    ///                             saves, and copies the Name (never the email) to the Identity user
    ///
    /// State and district dropdowns use the existing api/State and api/District/byState/{id}.
    /// </summary>
    [FarmerAccess]
    [Authorize(Roles = nameof(AppRole.Farmer))]
    [ApiController]
    [Route("api/Farmer/profile")]
    public class FarmerProfileController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly UserManager<UserInfo> _userManager;

        public FarmerProfileController(AppDbContext db, UserManager<UserInfo> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        private string? CallerId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var sasLinked = await IsSasLinkedAsync(userId);
            var profile = await _db.FarmerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile is null)
            {
                var phone = await _db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.PhoneNumber).FirstOrDefaultAsync();
                return NotFound(new FarmerProfileMissingDto { ProfileComplete = false, SasLinked = sasLinked, Phone = phone });
            }

            return Ok(await ToDtoAsync(profile, sasLinked));
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] FarmerProfileUpsertDto? dto)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            if (dto is null) return BadRequest(new FarmerProfileErrorDto { Message = "Please fill in the form." });

            var errors = FarmerProfileRules.Validate(dto);

            if (!errors.ContainsKey("stateId") &&
                !await _db.States.AsNoTracking().AnyAsync(s => s.Id == dto.StateId && s.IsActive))
                errors["stateId"] = "Please choose your state from the list.";

            if (!errors.ContainsKey("districtId") && !errors.ContainsKey("stateId"))
            {
                var districtStateId = await _db.Districts.AsNoTracking()
                    .Where(d => d.Id == dto.DistrictId && d.IsActive)
                    .Select(d => (int?)d.StateId)
                    .FirstOrDefaultAsync();
                if (districtStateId is null)
                    errors["districtId"] = "Please choose your district from the list.";
                else if (districtStateId != dto.StateId)
                    errors["districtId"] = "This district is not in the state you chose.";
            }

            if (errors.Count > 0)
                return BadRequest(new FarmerProfileErrorDto { Message = "Please check the highlighted fields.", Errors = errors });

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return Unauthorized();

            var now = DateTime.UtcNow;
            var profile = await _db.FarmerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile is null)
            {
                profile = new FarmerProfile { UserId = userId, CreatedAtUtc = now };
                _db.FarmerProfiles.Add(profile);
            }

            var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
            profile.Name = dto.Name!.Trim();
            profile.Email = email;
            profile.Phone = user.PhoneNumber ?? profile.Phone;
            profile.StateId = dto.StateId;
            profile.DistrictId = dto.DistrictId;
            profile.Village = dto.Village!.Trim();
            profile.Taluk = string.IsNullOrWhiteSpace(dto.Taluk) ? null : dto.Taluk.Trim();
            profile.PinCode = dto.PinCode!.Trim();
            profile.Latitude = dto.Latitude;
            profile.Longitude = dto.Longitude;
            profile.PrimaryCrops = FarmerProfileRules.JoinCrops(dto.PrimaryCrops);
            profile.LandAcres = dto.LandAcres;
            profile.IrrigationType = dto.IrrigationType;
            profile.PreferredLanguage = string.IsNullOrWhiteSpace(dto.PreferredLanguage)
                ? FarmerProfileRules.DefaultLanguage
                : dto.PreferredLanguage.Trim();
            profile.UpdatedAtUtc = now;

            // The account shows the same name everywhere (top bar, staff lists). The email stays on
            // the farmer profile ONLY: Identity emails are not unique here (RequireUniqueEmail = false)
            // and staff profile flows call FindByEmailAsync, which throws when two accounts share one,
            // so a farmer typing a staff member's email must never reach AppUsers. An Identity email
            // the account already has is left alone.
            user.Name = profile.Name;
            user.UpdatedAt = now;
            user.UpdatedBy = user.UserName ?? "FarmerOtp";

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Two first saves at once: the unique UserId index kept one row. Ask for a retry.
                return Conflict(new FarmerProfileErrorDto { Message = "Your profile was just saved from another screen. Please try again." });
            }

            return Ok(await ToDtoAsync(profile, await IsSasLinkedAsync(userId)));
        }

        private Task<bool> IsSasLinkedAsync(string userId) =>
            _db.SasFarmers.AsNoTracking().AnyAsync(f => !f.IsDeleted && f.UserId == userId);

        private async Task<FarmerProfileDto> ToDtoAsync(FarmerProfile p, bool sasLinked)
        {
            var stateName = await _db.States.AsNoTracking().Where(s => s.Id == p.StateId).Select(s => s.StateName).FirstOrDefaultAsync();
            var districtName = await _db.Districts.AsNoTracking().Where(d => d.Id == p.DistrictId).Select(d => d.DistrictName).FirstOrDefaultAsync();

            return new FarmerProfileDto
            {
                Name = p.Name,
                Email = p.Email,
                Phone = p.Phone,
                StateId = p.StateId,
                StateName = stateName,
                DistrictId = p.DistrictId,
                DistrictName = districtName,
                Village = p.Village,
                Taluk = p.Taluk,
                PinCode = p.PinCode,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                PrimaryCrops = FarmerProfileRules.SplitCrops(p.PrimaryCrops),
                LandAcres = p.LandAcres,
                IrrigationType = p.IrrigationType,
                PreferredLanguage = p.PreferredLanguage,
                ProfileComplete = FarmerProfileRules.IsComplete(p.Name, p.StateId, p.DistrictId, p.Village, p.PinCode),
                SasLinked = sasLinked,
                UpdatedAtUtc = p.UpdatedAtUtc
            };
        }
    }
}
