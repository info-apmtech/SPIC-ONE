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
        private readonly ITokenIssuer _tokens;

        public AuthenticationController(
            IUserService userService,
            UserManager<UserInfo> userManager,
            SignInManager<UserInfo> signInManager,
            IConfiguration config,
            AppDbContext db,
            ITokenIssuer tokens)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _db = db;
            _tokens = tokens;
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

            // JWT + refresh token + designation snapshot: one shared issuer (SpicAPI/Services/TokenIssuer)
            // so the farmer OTP login returns exactly the same session.
            return Ok(await _tokens.IssueLoginAsync(user));
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

            var hash = TokenIssuer.HashToken(dto.RefreshToken);
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
            var newRefreshToken = await _tokens.IssueRefreshTokenAsync(user.Id);
            existing.IsRevoked = true;
            existing.RevokedAt = now;
            existing.ReplacedByTokenHash = TokenIssuer.HashToken(newRefreshToken);
            await _db.SaveChangesAsync();

            var token = await _tokens.GenerateJwtTokenAsync(user);
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
    }
}