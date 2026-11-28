using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
using SPIC.Core.Interfaces;
using Spic.Infrastructure.Data;
using SpicAPI.Services;
using System.Security.Cryptography;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// Farmer portal sign-in with a mobile number and a one-time code (docs/farmer-portal.md).
    ///
    ///   POST api/FarmerAuth/otp/request  { phone }        -> sends a 6-digit code (limits: 30 s per
    ///                                                        number, 5 per number per hour, per IP per hour
    ///                                                        FarmerPortal:OtpPerIpHourlyCap (20), all numbers
    ///                                                        per hour FarmerPortal:OtpGlobalHourlyCap (500))
    ///   POST api/FarmerAuth/otp/verify   { phone, code }  -> the normal login response + profileComplete / isNewUser
    ///
    /// The first verified code creates the farmer's account (Role = Farmer, UserName = "f" + mobile,
    /// a random password nobody knows) and links an unlinked SAS farmer record with the same mobile.
    /// Numbers that belong to staff or dealer accounts are refused: those users use the normal login.
    /// All rules live in SPIC.Core.FarmerPortal.FarmerOtpRules (pinned by the regression harness).
    /// Phone numbers are only ever logged masked (98xxxxxx21). The client IP is the SPIC web host's
    /// X-Spic-Client-Ip when it carries the right FarmerPortal:TrustedClientKey, else the proxy chain.
    /// </summary>
    [AllowAnonymous]
    [FarmerAccess] // OTP sign-in: a farmer may sign in again while an older token is still attached
    [ApiController]
    [Route("api/[controller]")]
    public class FarmerAuthController : ControllerBase
    {
        private const string StaffNumberMessage =
            "This number is registered to a SPIC staff or dealer account. Please use the normal login.";
        private const string InvalidPhoneMessage = "Please enter a valid 10-digit mobile number.";
        private const string CreatedBy = "FarmerOtp";

        private readonly AppDbContext _db;
        private readonly UserManager<UserInfo> _userManager;
        private readonly ITokenIssuer _tokens;
        private readonly ISmsSender _sms;
        private readonly IConfiguration _config;
        private readonly IHostEnvironment _env;
        private readonly ILogger<FarmerAuthController> _logger;

        public FarmerAuthController(AppDbContext db, UserManager<UserInfo> userManager, ITokenIssuer tokens, ISmsSender sms,
            IConfiguration config, IHostEnvironment env, ILogger<FarmerAuthController> logger)
        {
            _db = db;
            _userManager = userManager;
            _tokens = tokens;
            _sms = sms;
            _config = config;
            _env = env;
            _logger = logger;
        }

        // POST api/FarmerAuth/otp/request
        [HttpPost("otp/request")]
        public async Task<IActionResult> RequestOtp([FromBody] FarmerOtpRequestDto? dto)
        {
            var phone = FarmerPhone.Normalise(dto?.Phone);
            if (!FarmerPhone.IsValid(phone))
                return BadRequest(Error(InvalidPhoneMessage));

            // Never pretend: without a real SMS provider a production farmer would wait for a code
            // that is only written to the server log.
            if (_env.IsProduction() && !_sms.DeliversRealSms)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, Error("SMS is not set up yet. Please try later."));

            var now = DateTime.UtcNow;
            var ip = FarmerOtpRules.ClientIp(
                Request.Headers[FarmerOtpRules.ClientIpHeader].ToString(),
                Request.Headers[FarmerOtpRules.ClientKeyHeader].ToString(),
                _config["FarmerPortal:TrustedClientKey"],
                Request.Headers["X-Forwarded-For"].ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            var limits = OtpLimits.From(_config["FarmerPortal:OtpPerIpHourlyCap"], _config["FarmerPortal:OtpGlobalHourlyCap"]);

            // Old challenges no longer count for any limit; drop them so phone numbers are not kept.
            var retentionCutoff = now - FarmerOtpRules.Retention;
            await _db.FarmerOtpChallenges.Where(c => c.CreatedAtUtc < retentionCutoff).ExecuteDeleteAsync();

            // Count and insert under PostgreSQL advisory locks (per number, per IP, then the global
            // counter - always in that order), held until the transaction ends. Parallel requests,
            // also from another API replica, therefore wait for each other and see each other's
            // rows, so the caps hold exactly. The SMS goes out only after the commit.
            string? code = null;
            await using (var tx = await _db.Database.BeginTransactionAsync())
            {
                await LockAsync("farmer-otp:phone:" + phone);
                if (ip is not null) await LockAsync("farmer-otp:ip:" + ip);
                await LockAsync("farmer-otp:global");

                var windowStart = now - FarmerOtpRules.LimitWindow;
                var phoneSends = await _db.FarmerOtpChallenges.AsNoTracking()
                    .Where(c => c.Phone == phone && c.CreatedAtUtc > windowStart)
                    .Select(c => c.CreatedAtUtc)
                    .ToListAsync();
                // Staff-number probes (below) count for the IP limit, not for the global SMS count.
                var ipSends = ip is null
                    ? 0
                    : await _db.FarmerOtpChallenges.CountAsync(c => c.RequestIp == ip && c.CreatedAtUtc > windowStart);
                var globalSends = await _db.FarmerOtpChallenges
                    .CountAsync(c => c.CreatedAtUtc > windowStart && c.CodeHash != ProbeHash);

                var decision = FarmerOtpRules.DecideSend(now, phoneSends, ipSends, globalSends, limits);
                if (!decision.Allowed)
                {
                    _logger.LogInformation("Farmer OTP for {Phone} refused: {Outcome}", FarmerPhone.Mask(phone), decision.Outcome);
                    if (decision.Outcome == OtpSendOutcome.GlobalHourLimit)
                        _logger.LogWarning("Farmer OTP global hourly cap ({Cap}) reached: sign-in codes are paused.", limits.GlobalHourly);
                    Response.Headers.RetryAfter = decision.RetryAfterSeconds.ToString();
                    return StatusCode(StatusCodes.Status429TooManyRequests, new FarmerAuthError
                    {
                        Message = decision.Outcome switch
                        {
                            OtpSendOutcome.Cooldown => $"Please wait {decision.RetryAfterSeconds} seconds before asking for a new code.",
                            OtpSendOutcome.PhoneHourLimit => $"Too many codes were sent to this number. Please try again in about {Math.Max(1, (decision.RetryAfterSeconds + 59) / 60)} minutes.",
                            OtpSendOutcome.GlobalHourLimit => "Many farmers are signing in right now. Please try again in a little while.",
                            _ => "Too many sign-in codes were asked for from this network. Please try again later."
                        },
                        RetryAfterSeconds = decision.RetryAfterSeconds
                    });
                }

                if (await BelongsToNonFarmerAsync(phone))
                {
                    // Counted against the client IP (a dead row with no number and no code), so this
                    // answer cannot be used freely to find which numbers belong to staff or dealers.
                    _db.FarmerOtpChallenges.Add(new FarmerOtpChallenge
                    {
                        Phone = string.Empty,
                        CodeHash = ProbeHash,
                        CreatedAtUtc = now,
                        ExpiresAtUtc = now,
                        ConsumedAtUtc = now,
                        RequestIp = ip
                    });
                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                    return Conflict(Error(StaffNumberMessage));
                }

                code = FarmerOtpRules.GenerateCode();
                _db.FarmerOtpChallenges.Add(new FarmerOtpChallenge
                {
                    Phone = phone,
                    CodeHash = FarmerOtpRules.HashCode(code, phone, Pepper()),
                    CreatedAtUtc = now,
                    ExpiresAtUtc = now + FarmerOtpRules.CodeLifetime,
                    RequestIp = ip
                });
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }

            // The row stays even when sending fails, so a failing provider cannot be hammered past the limits.
            var sent = await _sms.SendOtpAsync(phone, code!, HttpContext.RequestAborted);
            if (!sent.Success)
                return StatusCode(StatusCodes.Status502BadGateway, Error("We could not send the SMS right now. Please try again in a minute."));

            return Ok(new FarmerOtpRequestResponse
            {
                Success = true,
                ExpiresInSeconds = (int)FarmerOtpRules.CodeLifetime.TotalSeconds,
                ResendInSeconds = (int)FarmerOtpRules.ResendCooldown.TotalSeconds,
                DevCode = _env.IsDevelopment() && !_sms.DeliversRealSms ? code : null
            });
        }

        // POST api/FarmerAuth/otp/verify
        [HttpPost("otp/verify")]
        public async Task<IActionResult> VerifyOtp([FromBody] FarmerOtpVerifyDto? dto)
        {
            var phone = FarmerPhone.Normalise(dto?.Phone);
            if (!FarmerPhone.IsValid(phone))
                return BadRequest(Error(InvalidPhoneMessage));

            var code = dto?.Code?.Trim();
            if (!FarmerOtpRules.IsWellFormedCode(code))
                return BadRequest(Error("Please enter the 6-digit code from the SMS."));

            var now = DateTime.UtcNow;
            var challenge = await _db.FarmerOtpChallenges.AsNoTracking()
                .Where(c => c.Phone == phone && c.ConsumedAtUtc == null && c.ExpiresAtUtc > now)
                .OrderByDescending(c => c.CreatedAtUtc)
                .FirstOrDefaultAsync();

            var outcome = FarmerOtpRules.DecideVerify(now, challenge?.ExpiresAtUtc, challenge?.ConsumedAtUtc,
                challenge?.Attempts ?? 0, challenge?.CodeHash, FarmerOtpRules.HashCode(code!, phone, Pepper()));

            const string expiredMessage = "This code has expired. Please ask for a new code.";
            const string tooManyMessage = "Too many tries, request a new code.";

            switch (outcome)
            {
                case OtpVerifyOutcome.NoChallenge:
                    return BadRequest(Error(expiredMessage));
                case OtpVerifyOutcome.TooManyAttempts:
                    return BadRequest(Error(tooManyMessage));
                case OtpVerifyOutcome.WrongCode:
                {
                    // Atomic, so parallel guesses cannot get past the attempt limit.
                    var counted = await _db.FarmerOtpChallenges
                        .Where(c => c.Id == challenge!.Id && c.ConsumedAtUtc == null && c.Attempts < FarmerOtpRules.MaxAttempts)
                        .ExecuteUpdateAsync(s => s.SetProperty(c => c.Attempts, c => c.Attempts + 1));
                    var left = FarmerOtpRules.TriesLeftAfterWrong(challenge!.Attempts);
                    if (counted == 0 || left == 0)
                        return BadRequest(Error(tooManyMessage));
                    return BadRequest(Error(left == 1
                        ? "That code is not right. 1 try left."
                        : $"That code is not right. {left} tries left."));
                }
            }

            // Only reachable with a live code (request refuses staff numbers), so this is no free
            // staff-number oracle; it covers a number that became a staff number after the code went out.
            if (await BelongsToNonFarmerAsync(phone))
                return Conflict(Error(StaffNumberMessage));

            // Single use: only the request that flips ConsumedAtUtc gets a session.
            var consumed = await _db.FarmerOtpChallenges
                .Where(c => c.Id == challenge!.Id && c.ConsumedAtUtc == null && c.Attempts < FarmerOtpRules.MaxAttempts)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.ConsumedAtUtc, now));
            if (consumed == 0)
                return BadRequest(Error(expiredMessage));

            var (user, isNewUser) = await FindOrCreateFarmerAsync(phone);
            if (user is null)
                return StatusCode(StatusCodes.Status500InternalServerError, Error("We could not create your account. Please try again."));

            if (!user.IsActive)
                return StatusCode(StatusCodes.Status403Forbidden, Error("This account is switched off. Please contact SPIC support."));

            await LinkSasFarmerAsync(user, phone);

            var profile = await _db.FarmerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == user.Id);
            // Continuing past the login page's consent line accepts the current privacy notice (final fix wave).
            // A new farmer has no profile yet: the first profile save records it.
            if (profile is not null && FarmerPrivacyRules.NeedsAcceptance(profile.PrivacyVersion))
                await _db.FarmerProfiles.Where(p => p.Id == profile.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.PrivacyAcceptedAt, (DateTime?)now)
                        .SetProperty(p => p.PrivacyVersion, FarmerPrivacyRules.Version));
            var profileComplete = profile is not null &&
                FarmerProfileRules.IsComplete(profile.Name, profile.StateId, profile.DistrictId, profile.Village, profile.PinCode);

            var login = await _tokens.IssueLoginAsync(user);
            _logger.LogInformation("Farmer {Phone} signed in by OTP (new account: {IsNew})", FarmerPhone.Mask(phone), isNewUser);

            return Ok(FarmerOtpVerifyResponse.From(login, profileComplete, isNewUser));
        }

        // ---------------------------------------------------------------- helpers

        private static FarmerAuthError Error(string message) => new() { Message = message };

        /// <summary>CodeHash of a staff-number probe row: it only counts for the per-IP limit.</summary>
        private const string ProbeHash = "";

        /// <summary>A transaction-scoped PostgreSQL advisory lock on a text key.</summary>
        private Task LockAsync(string key) =>
            _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))");

        /// <summary>FarmerPortal:OtpPepper, or the JWT signing key when it is not set.</summary>
        private string Pepper()
        {
            var pepper = _config["FarmerPortal:OtpPepper"];
            return string.IsNullOrWhiteSpace(pepper) ? _config["Jwt:Key"] ?? string.Empty : pepper;
        }

        /// <summary>True when a staff / dealer (any non-Farmer) account carries this mobile.</summary>
        private Task<bool> BelongsToNonFarmerAsync(string phone)
        {
            var variants = FarmerPhone.StoredVariants(phone);
            return _db.Users.AsNoTracking().AnyAsync(u => u.Role != AppRole.Farmer &&
                ((u.PhoneNumber != null && variants.Contains(u.PhoneNumber)) || u.UserName == phone));
        }

        /// <summary>
        /// The farmer account for this mobile: the self sign-up user name first, then any existing
        /// Farmer account (for example one an administrator made for SAS) with the same number, else
        /// a new account. Returns null only if Identity refuses to create it.
        /// </summary>
        private async Task<(UserInfo? User, bool IsNew)> FindOrCreateFarmerAsync(string phone)
        {
            var existing = await FindFarmerAsync(phone);
            if (existing is not null)
            {
                // The OTP just proved the number (an admin-made Farmer account may not say so yet).
                if (!existing.PhoneNumberConfirmed || string.IsNullOrEmpty(existing.PhoneNumber))
                {
                    existing.PhoneNumber ??= phone;
                    existing.PhoneNumberConfirmed = true;
                    await _db.SaveChangesAsync();
                }
                return (existing, false);
            }

            var now = DateTime.UtcNow;
            var user = new UserInfo
            {
                UserName = FarmerPhone.UserNameFor(phone),
                Name = null,
                PhoneNumber = phone,
                PhoneNumberConfirmed = true,
                Role = AppRole.Farmer,
                IsActive = true,
                // The legacy plaintext column stays empty for OTP accounts: the Identity password
                // below is random and never shown to anyone, so the account can only sign in by OTP.
                Password = string.Empty,
                CreatedAt = now,
                CreatedBy = CreatedBy,
                UpdatedAt = now,
                UpdatedBy = CreatedBy
            };

            var result = await _userManager.CreateAsync(user, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
            if (result.Succeeded) return (user, true);

            // Two verifies for the same number at once: the other request created it first.
            var raced = await FindFarmerAsync(phone);
            if (raced is not null) return (raced, false);

            _logger.LogError("Could not create the farmer account for {Phone}: {Errors}", FarmerPhone.Mask(phone),
                string.Join("; ", result.Errors.Select(e => e.Code)));
            return (null, false);
        }

        private async Task<UserInfo?> FindFarmerAsync(string phone)
        {
            var userName = FarmerPhone.UserNameFor(phone);
            var variants = FarmerPhone.StoredVariants(phone);
            var candidates = await _db.Users
                .Where(u => u.Role == AppRole.Farmer &&
                            (u.UserName == userName || (u.PhoneNumber != null && variants.Contains(u.PhoneNumber))))
                .ToListAsync();
            return candidates.FirstOrDefault(u => u.UserName == userName)
                   ?? candidates.OrderBy(u => u.CreatedAt).FirstOrDefault();
        }

        /// <summary>
        /// The OTP proves the phone, so an unlinked SAS farmer record with the same mobile is linked
        /// to this account (soil samples and reports then show up). A user already linked to a SAS
        /// record keeps that one.
        /// </summary>
        private async Task LinkSasFarmerAsync(UserInfo user, string phone)
        {
            if (await _db.SasFarmers.AnyAsync(f => !f.IsDeleted && f.UserId == user.Id)) return;

            var variants = FarmerPhone.StoredVariants(phone);
            var sas = await _db.SasFarmers
                .Where(f => !f.IsDeleted && f.UserId == null && variants.Contains(f.Mobile))
                .OrderBy(f => f.Id)
                .FirstOrDefaultAsync();
            if (sas is null) return;

            sas.UserId = user.Id;
            sas.UpdatedBy = user.UserName;
            sas.UpdatedAt = DateTime.Now; // SasFarmers keep the SAS module's local-time convention
            await _db.SaveChangesAsync();
            _logger.LogInformation("Linked SAS farmer {SasFarmerId} to farmer account {Phone}", sas.Id, FarmerPhone.Mask(phone));
        }
    }
}
