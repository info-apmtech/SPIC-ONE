using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using Spic.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static SPIC.Core.DTOs.AdminViewModel;

namespace SpicAPI.Services
{
    /// <summary>
    /// Issues the signed-in session: the JWT (with the location claims), the rotating refresh token
    /// and the login response built from them. Moved verbatim out of AuthenticationController
    /// (2026-11-27) so the farmer OTP login (FarmerAuthController) issues exactly the same pair as
    /// the username / password login.
    /// </summary>
    public interface ITokenIssuer
    {
        /// <summary>JWT + refresh token + designation snapshot, the body of api/Authentication/login.</summary>
        Task<LoginResponseModel> IssueLoginAsync(UserInfo user);

        /// <summary>A signed JWT (raw, without the "Bearer " prefix).</summary>
        Task<string> GenerateJwtTokenAsync(UserInfo user);

        /// <summary>Persists a new refresh token for the user and returns its raw value (the only time it exists in plaintext).</summary>
        Task<string> IssueRefreshTokenAsync(string userId);
    }

    public sealed class TokenIssuer : ITokenIssuer
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;

        public TokenIssuer(IConfiguration config, AppDbContext db)
        {
            _config = config;
            _db = db;
        }

        public static string HashToken(string rawToken) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        public async Task<LoginResponseModel> IssueLoginAsync(UserInfo user)
        {
            var token = await GenerateJwtTokenAsync(user);

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

            return new LoginResponseModel
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
        }

        public async Task<string> IssueRefreshTokenAsync(string userId)
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

        public async Task<string> GenerateJwtTokenAsync(UserInfo user)
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

            if (empLogin != null)
            {
                var regionIdsClaimValue = empLogin.RegionIds != null
                    ? string.Join(",", empLogin.RegionIds.Where(id => id > 0))
                    : (empLogin.RegionId > 0 ? empLogin.RegionId.ToString() : "");
                claims.Add(new Claim("spic:region_ids", regionIdsClaimValue));

                var hqIdsClaimValue = empLogin.HeadquartersIds != null
                    ? string.Join(",", empLogin.HeadquartersIds.Where(id => id > 0))
                    : (empLogin.HeadquartersId > 0 ? empLogin.HeadquartersId.ToString() : "");
                claims.Add(new Claim("spic:hq_ids", hqIdsClaimValue));
            }
            else
            {
                claims.Add(new Claim("spic:region_ids", ""));
                claims.Add(new Claim("spic:hq_ids", ""));
            }

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
