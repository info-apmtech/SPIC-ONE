using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using Spic.Infrastructure.Data;
using SpicAPI.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
                designationName = desig?.Name;

                if (desig != null && !string.IsNullOrWhiteSpace(desig.RoleAccess))
                {
                    roleAccess = desig.RoleAccess;
                }
            }

            var refreshToken = await IssueRefreshTokenAsync(user.Id);

            var responseData = new LoginResponseModel
            {
                Token = $"Bearer {token}",
                User = new LoginUserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Name = user.Name,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role.ToString(),
                    DesignationId = user.DesignationId,
                    IsActive = user.IsActive
                },
                Expiration = jwtToken?.ValidTo ?? DateTime.UtcNow.AddHours(1),
                RoleAccess = roleAccess,
                DesignationName = designationName,
                RefreshToken = refreshToken
            };

            return Ok(responseData);
        }

        // POST api/Authentication/refresh
        // Trades a still-valid refresh token for a new JWT + a new, rotated refresh token -
        // lets a client (in particular the JMDO native background tracking service, which can
        // easily outlive a single 60-minute JWT) keep working past token expiry without a full
        // re-login. AuthHttpMessageHandler also calls this once on any 401 before forcing logout.
        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.RefreshToken))
                return BadRequest(new { message = "Refresh token is required." });

            var hash = HashToken(dto.RefreshToken);
            var now = DateTime.Now;

            var existing = await _db.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == hash);

            // Same response for "doesn't exist", "revoked" and "expired" - no information leak
            // about which specific reason a given raw value failed for.
            if (existing == null || existing.IsRevoked || existing.ExpiresAt <= now)
                return Unauthorized(new { message = "Refresh token is invalid or expired. Please sign in again." });

            var user = await _userManager.FindByIdAsync(existing.UserId);
            if (user == null || !user.IsActive)
                return Unauthorized(new { message = "This account is no longer active." });

            // Rotate: the redeemed token is revoked immediately and linked to its replacement, so
            // a replayed copy of this same raw value can never be redeemed a second time.
            var newRefreshToken = await IssueRefreshTokenAsync(user.Id);
            existing.IsRevoked = true;
            existing.RevokedAt = now;
            existing.ReplacedByTokenHash = HashToken(newRefreshToken);
            await _db.SaveChangesAsync();

            var token = await GenerateJwtToken(user);
            var jwtToken = new JwtSecurityTokenHandler().ReadToken(token) as JwtSecurityToken;

            return Ok(new RefreshTokenResponseDto
            {
                Token = $"Bearer {token}",
                Expiration = jwtToken?.ValidTo ?? DateTime.UtcNow.AddHours(1),
                RefreshToken = newRefreshToken
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Revokes every still-active refresh token for the caller (resolved from the JWT, not
            // the request body - a logout can't be used to revoke someone else's tokens). Previously
            // a complete no-op; a refresh token issued at login would otherwise remain redeemable
            // forever after "logging out".
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var now = DateTime.Now;
                var activeTokens = await _db.RefreshTokens
                    .Where(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAt > now)
                    .ToListAsync();

                foreach (var t in activeTokens)
                {
                    t.IsRevoked = true;
                    t.RevokedAt = now;
                }

                if (activeTokens.Count > 0)
                    await _db.SaveChangesAsync();
            }

            return Ok(new { message = "Logged out successfully" });
        }

        // Issues and persists a new refresh token for the given user, returning the raw value
        // (the only time it's ever available in plaintext - only TokenHash is stored).
        private async Task<string> IssueRefreshTokenAsync(string userId)
        {
            var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace('+', '-').Replace('/', '_').TrimEnd('='); // URL-safe

            var refreshExpiryDays = _config.GetValue<double?>("Jwt:RefreshExpiryDays") ?? 2;

            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = userId,
                TokenHash = HashToken(raw),
                ExpiresAt = DateTime.Now.AddDays(refreshExpiryDays),
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return raw;
        }

        private static string HashToken(string rawToken) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        /// <summary>
        /// The signed-in user's Designation.RoleAccess, re-read from the database.
        ///
        /// Login captures RoleAccess once and the client keeps that snapshot for the whole
        /// session; without this endpoint a Designation edited by an administrator would never
        /// reach a user who is already signed in. MainLayout calls it exactly once per app load
        /// (no polling, no refresh loop) and falls back to the session snapshot on any failure.
        /// Returns "" when the user has no active designation or no grant, matching the shape of
        /// the login response.
        /// </summary>
        [HttpGet("permissions")]
        public async Task<IActionResult> Permissions()
        {
            // Resolve by the NameIdentifier claim, the same way every other controller in this
            // project identifies the caller, rather than by username lookup.
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = userId is null ? null : await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized();

            string? roleAccess = null;
            if (user.DesignationId.HasValue && user.DesignationId.Value > 0)
            {
                var desig = await _db.Designations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == user.DesignationId.Value && d.IsActive);

                if (desig != null && !string.IsNullOrWhiteSpace(desig.RoleAccess))
                {
                    roleAccess = desig.RoleAccess;
                }
            }

            return Ok(roleAccess ?? string.Empty);
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

            // The current login row is resolved through the one shared rule so the
            // location claims in this token describe the same row the Profile page
            // displays and the Profile update writes. The previous bare
            // FirstOrDefault had no IsActive filter and no ordering, so a user with
            // more than one Employeelogin row could get claims from a deactivated
            // row that differed per request.
            var resolvedLogin = await _db.Employeelogins
                .ResolveAsync(user.Id, user.UserName);

            if (resolvedLogin.HasMultipleActiveRows)
                Console.WriteLine($"[Auth] DATA INTEGRITY: user '{user.Id}' has " +
                                  $"{resolvedLogin.ActiveCandidates} active Employeelogin rows " +
                                  $"(of {resolvedLogin.TotalCandidates} total). Resolved " +
                                  $"Employeelogin.Id={resolvedLogin.Row?.Id} by the shared rule.");

            var empLogin = resolvedLogin.Row;

            claims.Add(new Claim("spic:state_id", empLogin?.StateId.ToString() ?? "0"));
            claims.Add(new Claim("spic:region_id", empLogin?.RegionId.ToString() ?? "0"));
            claims.Add(new Claim("spic:hq_id", empLogin?.HeadquartersId.ToString() ?? "0"));
            claims.Add(new Claim("spic:zone_id", empLogin?.ZoneId.ToString() ?? "0"));

            // SpecialAdmin-only: database-backed multi-location scope.
            // For every other role these claims are omitted entirely, so existing
            // single-location behavior (spic:state_id etc.) is unchanged.
            if (user.Role == AppRole.SpecialAdmin)
            {
                var employeeInfoId = empLogin?.EmployeeInformationID ?? 0;

                List<SpecialAdminLocations> specialAdminLocations;
                if (employeeInfoId > 0)
                {
                    specialAdminLocations = await _db.SpecialAdminLocations
                        .AsNoTracking()
                        .Where(l => l.EmployeeInformationID == employeeInfoId)
                        .ToListAsync();
                }
                else
                {
                    specialAdminLocations = new List<SpecialAdminLocations>();
                }

                // If the employeelogin row couldn't be resolved (or isn't linked to
                // an EmployeeInformation >= 1), fall back to matching through any
                // employeelogin row that maps back to this user so the assigned
                // scope is never silently dropped from the token.
                if (specialAdminLocations.Count == 0)
                {
                    specialAdminLocations = await (
                        from loc in _db.SpecialAdminLocations
                        join login in _db.Employeelogins
                            on loc.EmployeeInformationID equals login.EmployeeInformationID
                        where login.UserId == user.Id || login.UserId == user.UserName
                        select loc
                    ).AsNoTracking().ToListAsync();
                }

                var assignedStateIds = specialAdminLocations.Select(l => l.StateId).Where(s => s > 0).Distinct().ToList();
                var assignedRegionIds = specialAdminLocations.Select(l => l.RegionId).Where(r => r > 0).Distinct().ToList();
                var assignedHqIds = specialAdminLocations.Select(l => l.HeadquarterId).Where(h => h > 0).Distinct().ToList();

                if (assignedStateIds.Count > 0)
                    claims.Add(new Claim("spic:assigned_state_ids", string.Join(",", assignedStateIds)));
                if (assignedRegionIds.Count > 0)
                    claims.Add(new Claim("spic:assigned_region_ids", string.Join(",", assignedRegionIds)));
                if (assignedHqIds.Count > 0)
                    claims.Add(new Claim("spic:assigned_hq_ids", string.Join(",", assignedHqIds)));
            }

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