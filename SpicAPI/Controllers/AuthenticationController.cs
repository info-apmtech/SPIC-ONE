using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using Spic.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static SPIC.Core.DTOs.AdminViewModel;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly UserManager<UserInfo> _userManager;
        private readonly SignInManager<UserInfo> _signInManager;
        private readonly AppDbContext _db;

        public AuthenticationController(
            IUserService userService,
            UserManager<UserInfo> userManager,
            SignInManager<UserInfo> signInManager,
            IConfiguration config,
            AppDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _db = db;
        }


        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
                return Unauthorized("Invalid username or password");

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
                return Unauthorized("Invalid username or password");

            var token = await GenerateJwtToken(user);

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

            // Resolve the user's designation -> RoleAccess (CSV of PagePermission names)
            // and Designation Name (e.g. "SDWA"), kept separate from AppRole.
            string? roleAccess = null;
            string? designationName = null;
            if (user.DesignationId.HasValue && user.DesignationId.Value > 0)
            {
                var desig = await _db.Designations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == user.DesignationId.Value && d.IsActive);
                roleAccess = desig?.RoleAccess;
                designationName = desig?.Name;
            }

            var responseData = new LoginResponseModel
            {
                Token = $"Bearer {token}",
                User = user,
                Expiration = jwtToken?.ValidTo ?? DateTime.UtcNow.AddHours(1),
                RoleAccess = roleAccess,
                DesignationName = designationName
            };

            return Ok(responseData);
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new { message = "Logged out successfully" });
        }

        private async Task<string> GenerateJwtToken(UserInfo user)
        {
            var jwtConfig = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var empLogin = await _db.Employeelogins
                .FirstOrDefaultAsync(l =>
                    l.UserId == user.Id ||
                    l.UserId == user.UserName);
            claims.Add(new Claim("spic:state_id", empLogin?.StateId.ToString() ?? "0"));
            claims.Add(new Claim("spic:region_id", empLogin?.RegionId.ToString() ?? "0"));
            claims.Add(new Claim("spic:hq_id", empLogin?.HeadquartersId.ToString() ?? "0"));

            var token = new JwtSecurityToken(
                issuer: jwtConfig["Issuer"],
                audience: jwtConfig["Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtConfig["ExpiryMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}