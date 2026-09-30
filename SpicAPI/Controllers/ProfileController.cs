using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SpicAPI.Services;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using System.Security.Claims;
using System.Text.RegularExpressions;
using static SPIC.Core.Entities.EmployeeRegistration;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// Self-service profile for the currently logged-in user.
    ///
    /// The target record is resolved exclusively from the authenticated
    /// claims â€” the request body carries no id, so a caller can never address
    /// another employee:
    ///
    ///   ClaimTypes.NameIdentifier
    ///     -> UserInfo.Id
    ///     -> Employeelogin.UserId
    ///     -> Employeelogin.EmployeeInformationID
    ///     -> EmployeeInformation.Id
    ///
    /// All 12 fields shown on the Employee branch of the Profile page are
    /// writable. Because Role / Designation / location / Status feed the JWT
    /// claims and the RoleAccess -> AllowedPages permission chain, the response
    /// tells the caller when the saved values invalidate the current token so
    /// the client can force a fresh sign-in instead of continuing with stale
    /// claims.
    ///
    /// Dealer profile tabs are out of scope; this endpoint serves the employee
    /// profile only.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        /// <summary>
        /// The roles Profile Edit offers, listed explicitly to match Profile.razor's
        /// EditableRoles one-for-one.
        ///
        /// This used to be derived as "every enum value except Dealer and Farmer",
        /// which is a second, independently maintained copy of the employee role
        /// policy and silently drifts whenever the enum or the employee editor
        /// changes. Note that EmployeeRegistrationController applies no role check at
        /// all, because that endpoint is admin-only. Profile is self-service for any
        /// signed-in user, so the check is kept here to stop a user promoting their
        /// own row to an administrative role by editing their profile.
        /// </summary>
        private static readonly AppRole[] AssignableRoles =
        {
            AppRole.CorporateAdmin,
            AppRole.Director,
            AppRole.AVP,
            AppRole.SMD,
            AppRole.SMM,
            AppRole.RM,
            AppRole.RMD,
            AppRole.MDO,
            AppRole.MO,
            AppRole.JMDO,
            AppRole.SpecialAdmin
        };

        private readonly UserManager<UserInfo> _userManager;
        private readonly IGenericRepository<EmployeeInformation> _employeeRepo;
        private readonly IGenericRepository<Employeelogin> _employeeLoginRepo;
        private readonly IGenericRepository<Designation> _designationRepo;
        private readonly IGenericRepository<Zone> _zoneRepo;
        private readonly IGenericRepository<State> _stateRepo;
        private readonly IGenericRepository<Region> _regionRepo;
        private readonly IGenericRepository<Headquarter> _headquarterRepo;
        private readonly IGenericRepository<SpecialAdminLocations> _specialAdminLocationsRepo;
        private readonly AppDbContext _db;
        private readonly ILogger<ProfileController> _logger;

        // TEMPORARY DIAGNOSTIC â€” remove once the save flow is confirmed.
        private const bool ProfileSaveDebug = true;

        public ProfileController(
            UserManager<UserInfo> userManager,
            IGenericRepository<EmployeeInformation> employeeRepo,
            IGenericRepository<Employeelogin> employeeLoginRepo,
            IGenericRepository<Designation> designationRepo,
            IGenericRepository<Zone> zoneRepo,
            IGenericRepository<State> stateRepo,
            IGenericRepository<Region> regionRepo,
            IGenericRepository<Headquarter> headquarterRepo,
            IGenericRepository<SpecialAdminLocations> specialAdminLocationsRepo,
            AppDbContext db,
            ILogger<ProfileController> logger)
        {
            _userManager = userManager;
            _employeeRepo = employeeRepo;
            _employeeLoginRepo = employeeLoginRepo;
            _designationRepo = designationRepo;
            _zoneRepo = zoneRepo;
            _stateRepo = stateRepo;
            _regionRepo = regionRepo;
            _headquarterRepo = headquarterRepo;
            _specialAdminLocationsRepo = specialAdminLocationsRepo;
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// The caller's own identity id. Falls back to the short JWT claim
        /// names in case a token was issued without the mapped claim type.
        /// Never read from the request.
        /// </summary>
        private string? CurrentUserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("nameid")?.Value
            ?? User.FindFirst("sub")?.Value;

        private string CurrentUserAudit => CurrentUserId ?? "System";

        [HttpGet]
        public async Task<IActionResult> GetMyProfile()
        {
            var currentUserId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(currentUserId))
                return NotFound(new { message = "Authenticated user could not be resolved." });

            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            if (currentUser == null)
                return NotFound(new { message = "Authenticated user could not be resolved." });

            // Admin/SuperAdmin have no Employeelogin/EmployeeInformation row at all
            // (they are seeded/created straight into AspNetUsers) so their profile
            // is served directly from UserInfo instead of the employee chain below.
            if (IsAdminRole(currentUser.Role))
                return Ok(MapAdminToDto(currentUser));

            var resolved = await ResolveOwnProfileAsync();
            if (resolved.Employee == null)
                return NotFound(new { message = resolved.Error });

            var dto = MapToDto(resolved.Employee, resolved.User!, resolved.Login!);
            
            if (dto.Role == AppRole.SpecialAdmin)
            {
                var rows = await _specialAdminLocationsRepo
                    .GetWhere(x => x.EmployeeInformationID == resolved.Employee.Id)
                    .ToListAsync();
                
                dto.SpecialAdminLocations = rows.Select(r => new SpecialAdminLocationItem
                {
                    StateId = r.StateId,
                    RegionId = r.RegionId,
                    HeadquarterId = r.HeadquarterId
                }).ToList();
            }

            return Ok(dto);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateMyProfile([FromBody] ProfileUpdateDto request)
        {
            // TEMPORARY DIAGNOSTIC â€” remove once the save flow is confirmed.
            if (ProfileSaveDebug)
            {
                _logger.LogWarning("PROFILESAVE enter CurrentUserId={UserId} NameId={NameId} Nameid={NameId2} Sub={Sub}",
                    CurrentUserId,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    User.FindFirst("nameid")?.Value,
                    User.FindFirst("sub")?.Value);
                _logger.LogWarning("PROFILESAVE request EmployeeCode={Code} Name={Name} Email={Email} Personal={Personal} Official={Official} Role={Role}({RoleInt}) Desig={Desig} Zone={Zone} State={State} Region={Region} Hq={Hq} IsActive={Active}",
                    request.EmployeeCode, request.Name, request.Email,
                    request.PersonalPhoneNumber, request.OfficialPhoneNumber,
                    request.Role, (int)request.Role, request.DesignationId,
                    request.ZoneId, request.StateId, request.RegionId,
                    request.HeadquartersId, request.IsActive);
            }

            if (!ModelState.IsValid)
                return BadRequest(new { message = FirstModelError() });

            // Admin/SuperAdmin edit their own UserInfo row directly and never reach
            // the employee validation/transaction below.
            var callingUserId = CurrentUserId;
            var callingUser = string.IsNullOrWhiteSpace(callingUserId)
                ? null
                : await _userManager.FindByIdAsync(callingUserId);
            if (callingUser != null && IsAdminRole(callingUser.Role))
                return await UpdateMyAdminProfileAsync(callingUser, request);

            var employeeCode = (request.EmployeeCode ?? string.Empty).Trim();
            var name = (request.Name ?? string.Empty).Trim();
            var email = (request.Email ?? string.Empty).Trim();
            var personalPhone = (request.PersonalPhoneNumber ?? string.Empty).Trim();
            var officialPhone = (request.OfficialPhoneNumber ?? string.Empty).Trim();

            if (employeeCode.Length == 0)
                return BadRequest(new { message = "Employee Code is required." });

            if (name.Length == 0)
                return BadRequest(new { message = "Name is required." });

            if (email.Length == 0)
                return BadRequest(new { message = "Email is required." });

            if (!Regex.IsMatch(personalPhone, @"^[0-9]{10}$"))
                return BadRequest(new { message = "Personal phone number must be 10 digits." });

            if (!Regex.IsMatch(officialPhone, @"^[0-9]{10}$"))
                return BadRequest(new { message = "Official phone number must be 10 digits." });

            // System.Text.Json maps a JSON number straight onto the enum without
            // range-checking it, so an undefined value (e.g. 999) arrives here
            // intact. Validate against the enum and against the assignable set.
            if (!Enum.IsDefined(typeof(AppRole), request.Role))
                return BadRequest(new { message = "Select a valid role." });

            if (!AssignableRoles.Contains(request.Role))
                return BadRequest(new
                {
                    message = "That role cannot be set from the employee profile. " +
                              "Dealer and Farmer are managed through the registration flow."
                });

            if (request.DesignationId < 0)
                return BadRequest(new { message = "Select a valid designation." });

            if (request.ZoneId < 0 || request.StateId < 0 || request.RegionId < 0 || request.HeadquartersId < 0)
                return BadRequest(new { message = "Select valid location values." });

            var resolved = await ResolveOwnProfileAsync();
            var employee = resolved.Employee;
            var user = resolved.User;
            var login = resolved.Login;
            if (employee == null || user == null || login == null)
                return NotFound(new { message = resolved.Error });

            if (ProfileSaveDebug)
            {
                _logger.LogWarning("PROFILESAVE resolved UserInfo.Id={UserRow} Employeelogin.Id={LoginRow} EmployeeInformation.Id={EmpRow} EmployeeInformationID={EmpFk} UserName={UserName}",
                    user.Id, login.Id, employee.Id, login.EmployeeInformationID, user.UserName);
            }

            // Capture the current security / data-scope values BEFORE mutating so
            // the response can tell the client whether the token went stale.
            var originalRole = login.Role;
            var originalUserRole = user.Role;
            var originalDesignationId = user.DesignationId ?? 0;
            var originalZoneId = login.ZoneId;
            var originalStateId = login.StateId;
            var originalRegionId = login.RegionId;
            var originalHeadquartersId = login.HeadquartersId;

            // Employee Code has no unique index and no application-level guard
            // anywhere in the solution, so the duplicate check is added here.
            var codeLower = employeeCode.ToLower();
            var codeTaken = await _employeeRepo.GetAll()
                .AnyAsync(x => x.Id != employee.Id && x.EmployeeCode.ToLower() == codeLower);
            if (codeTaken)
                return BadRequest(new { message = "Employee Code is already in use." });

            // EmployeeInformation.Email is likewise unindexed and unconstrained.
            var emailLower = email.ToLower();
            var employeeEmailTaken = await _employeeRepo.GetAll()
                .AnyAsync(x => x.Id != employee.Id && x.Email.ToLower() == emailLower);
            if (employeeEmailTaken)
                return BadRequest(new { message = "Email is already used by another employee." });

            // AppUsers.NormalizedEmail carries a non-unique index, so Identity
            // will not reject a duplicate â€” enforce it here.
            var userWithEmail = await _userManager.FindByEmailAsync(email);
            if (userWithEmail != null && userWithEmail.Id != user.Id)
                return BadRequest(new { message = "Email is already used by another account." });

            // ---- Designation is written straight through, with no existence or
            //      IsActive check, exactly as EmployeeLoginSetupController does
            //      (0 clears it). This check previously rejected the employee's own
            //      stored designation once it was deactivated, blocking unrelated
            //      edits such as a phone or name change with "The selected
            //      designation is inactive" - the same failure mode as the region
            //      validation that used to sit above it.
            // ---- Location masters are written straight through, with no existence,
            //      IsActive or parent-child validation. This is deliberately
            //      identical to EmployeeLoginSetupController.UpdateEmployeeLogin,
            //      which assigns Role/ZoneId/StateId/RegionId/HeadquartersId/IsActive
            //      without checking them. The Profile page used to re-validate every
            //      location level, which made a pre-existing row (for example a Region
            //      that is no longer active) block an unrelated edit such as renaming
            //      the employee, surfacing as "The selected region is inactive".

            // One transaction across AppUsers, EmployeeInformation and
            // Employeelogin so the three records can never drift apart.
            // UserManager's UpdateAsync reuses the same scoped AppDbContext, so
            // its SaveChanges participates here.
            if (ProfileSaveDebug)
            {
                var conn = _db.Database.GetDbConnection();
                var dbName = conn.Database;
                _logger.LogWarning("PROFILESAVE all validations passed, beginning transaction. DbContextHash={Ctx} DataSource={DataSource} Database={Db}",
                    _db.GetHashCode(),
                    conn.DataSource,
                    string.IsNullOrEmpty(dbName) ? "(not open yet)" : dbName);
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Identity side first: UpdateAsync normalizes NormalizedEmail
                // (and re-runs the user validators). UserName, Password,
                // PasswordHash, IsActive and every other Identity/security field
                // are deliberately left untouched.
                user.Name = name;
                user.Email = email;
                user.PhoneNumber = officialPhone; // Official phone sync point
                user.DesignationId = request.DesignationId > 0 ? request.DesignationId : null;
                // AuthenticationController.GenerateJwtToken builds the role claim
                // from UserInfo.Role, while the Profile page renders
                // Employeelogin.Role. Both are written so the value shown on the
                // page and the value baked into the next token cannot disagree.
                user.Role = request.Role;
                user.UpdatedAt = DateTime.Now;
                user.UpdatedBy = CurrentUserAudit;

                var userResult = await _userManager.UpdateAsync(user);
                if (ProfileSaveDebug)
                {
                    _logger.LogWarning("PROFILESAVE UserManager.UpdateAsync Succeeded={Ok} Name={Name} Email={Email} NormalizedEmail={NormEmail} UserName={UserName} Phone={Phone} Desig={Desig} Role={Role} Errors={Errors}",
                        userResult.Succeeded, user.Name, user.Email, user.NormalizedEmail, user.UserName,
                        user.PhoneNumber, user.DesignationId, user.Role,
                        userResult.Succeeded ? "-" : string.Join(" | ", userResult.Errors.Select(e => e.Code + ": " + e.Description)));
                }

                if (!userResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new
                    {
                        message = userResult.Errors.FirstOrDefault()?.Description
                                  ?? "Profile could not be updated."
                    });
                }

                // Employee side. The entity is the tracked row loaded by
                // FindAsync, so CreatedAt/CreatedBy are written back unchanged
                // (unlike PatchAsync, which would null the NOT NULL CreatedBy
                // when bound from a partial DTO).
                employee.EmployeeCode = employeeCode;
                employee.Name = name;
                employee.Email = email;
                employee.PersonalPhoneNumber = personalPhone;
                employee.OfficialPhoneNumber = officialPhone;
                employee.UpdatedAt = DateTime.Now;
                employee.UpdatedBy = CurrentUserAudit;

                await _employeeRepo.UpdateAsync(employee);

                if (ProfileSaveDebug)
                {
                    _logger.LogWarning("PROFILESAVE EmployeeInformation saved. EntityState={State} Id={Id} Code={Code} Name={Name} Email={Email} Personal={Personal} Official={Official} UpdatedBy={UpdatedBy}",
                        _db.Entry(employee).State.ToString(), employee.Id, employee.EmployeeCode,
                        employee.Name, employee.Email, employee.PersonalPhoneNumber,
                        employee.OfficialPhoneNumber, employee.UpdatedBy);
                }

                // Employeelogin side. This entity has no UpdatedBy/UpdatedAt
                // columns, so no audit stamp is written here â€” adding them would
                // require a schema migration.
                login.Role = request.Role;
                login.ZoneId = request.ZoneId;
                login.StateId = request.StateId;
                login.RegionId = request.RegionId;
                login.HeadquartersId = request.HeadquartersId;
                login.IsActive = request.IsActive;

                await _employeeLoginRepo.UpdateAsync(login);

                if (ProfileSaveDebug)
                {
                    _logger.LogWarning("PROFILESAVE Employeelogin saved. EntityState={State} Id={Id} Role={Role} Zone={Zone} State={State2} Region={Region} Hq={Hq} IsActive={Active}",
                        _db.Entry(login).State.ToString(), login.Id, login.Role,
                        login.ZoneId, login.StateId, login.RegionId,
                        login.HeadquartersId, login.IsActive);
                }

                await transaction.CommitAsync();

                if (ProfileSaveDebug)
                    _logger.LogWarning("PROFILESAVE transaction COMMITTED.");

                if (request.Role == AppRole.SpecialAdmin)
                {
                    var existing = await _specialAdminLocationsRepo
                        .GetWhere(x => x.EmployeeInformationID == employee.Id)
                        .ToListAsync();

                    foreach (var row in existing)
                    {
                        await _specialAdminLocationsRepo.DeleteAsync(row.Id);
                    }

                    if (request.SpecialAdminLocations != null)
                    {
                        foreach (var loc in request.SpecialAdminLocations)
                        {
                            if (loc.StateId <= 0)
                                continue;

                            await _specialAdminLocationsRepo.CreateAsync(new SpecialAdminLocations
                            {
                                EmployeeInformationID = employee.Id,
                                StateId = loc.StateId,
                                RegionId = Math.Max(0, loc.RegionId),
                                HeadquarterId = Math.Max(0, loc.HeadquarterId),
                                CreatedAt = DateTime.Now,
                                CreatedBy = CurrentUserAudit,
                                UpdatedAt = DateTime.Now,
                                UpdatedBy = CurrentUserAudit
                            });
                        }
                    }
                }

                var scopeChanged =
                    originalRole != request.Role
                    || originalUserRole != request.Role
                    || originalDesignationId != request.DesignationId
                    || originalZoneId != request.ZoneId
                    || originalStateId != request.StateId
                    || originalRegionId != request.RegionId
                    || originalHeadquartersId != request.HeadquartersId;

                return Ok(new
                {
                    message = "Profile updated successfully",
                    // The client must drop the token and re-login so the new role,
                    // designation -> RoleAccess -> AllowedPages and the
                    // spic:state_id / region_id / hq_id / zone_id claims are
                    // rebuilt from the database. Deactivating yourself always
                    // forces this too.
                    requiresReauthentication = scopeChanged || !request.IsActive,
                    isActive = request.IsActive,
                    data = MapToDto(employee, user, login)
                });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { message = "Profile could not be updated. Please try again." });
            }
        }

        /// <summary>
        /// Resolves the caller's own user + login + employee records through the one
        /// shared rule in EmployeeloginSelection, so GET, PUT, the Profile page and
        /// the JWT claims all act on the same Employeelogin row. GetAllWithInactive is
        /// used because GetAll() filters IsActive == true, which would make an
        /// inactive user unable to re-activate themselves.
        /// </summary>
        private async Task<(UserInfo? User, EmployeeInformation? Employee, Employeelogin? Login, string Error)> ResolveOwnProfileAsync()
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId))
                return (null, null, null, "Authenticated user could not be resolved.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (null, null, null, "Authenticated user could not be resolved.");

            // Resolved through the one shared rule, so GET, PUT, the Profile page
            // and the JWT location claims all act on the same row. The previous
            // "first active, else first" query had no tie-break, so a user with
            // more than one Employeelogin row could have the card, the modal and
            // the update on three different records. GetAllWithInactive is kept so
            // a user whose only row is deactivated can still edit their profile.
            var resolved = await _employeeLoginRepo.GetAllWithInactive()
                .ResolveAsync(user.Id, user.UserName);

            if (resolved.HasMultipleActiveRows)
                _logger.LogWarning("PROFILESAVE DATA INTEGRITY: user {UserId} has {ActiveCount} active " +
                                   "Employeelogin rows of {TotalCount} total. Resolved Employeelogin.Id={LoginId}.",
                    user.Id, resolved.ActiveCandidates, resolved.TotalCandidates, resolved.Row?.Id);

            var login = resolved.Row;
            if (login == null)
                return (null, null, null, "No employee login is linked to this account.");

            if (ProfileSaveDebug)
                _logger.LogWarning("PROFILESAVE Resolved LoginId={LoginId} EmployeeInformationId={EmployeeInformationId} " +
                                   "UserId={UserId} Role={Role} Zone={Zone} State={State} Region={Region} Hq={Hq} " +
                                   "IsActive={IsActive} candidates={Total} activeCandidates={Active}",
                    login.Id, login.EmployeeInformationID, login.UserId, login.Role, login.ZoneId,
                    login.StateId, login.RegionId, login.HeadquartersId, login.IsActive,
                    resolved.TotalCandidates, resolved.ActiveCandidates);

            var employee = await _employeeRepo.GetByIdAsync(login.EmployeeInformationID);
            if (employee == null)
                return (null, null, null, "No employee record is linked to this login.");

            return (user, employee, login, string.Empty);
        }

        /// <summary>Admin and SuperAdmin are seeded/created straight into
        /// AspNetUsers with no linked EmployeeInformation/Employeelogin row, so
        /// they get their own resolution and mapping path instead of
        /// ResolveOwnProfileAsync/MapToDto below.</summary>
        private static bool IsAdminRole(AppRole role) => role == AppRole.Admin || role == AppRole.SuperAdmin;

        private static ProfileDto MapAdminToDto(UserInfo user) => new()
        {
            LoginId = 0,
            EmployeeInformationId = 0,
            EmployeeCode = string.Empty,
            Name = user.Name ?? string.Empty,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            PersonalPhoneNumber = string.Empty,
            OfficialPhoneNumber = string.Empty,
            Role = user.Role,
            DesignationId = user.DesignationId ?? 0,
            ZoneId = 0,
            StateId = 0,
            RegionId = 0,
            HeadquartersId = 0,
            IsActive = user.IsActive
        };

        /// <summary>
        /// Admin/SuperAdmin self-edit: Name, Email and Designation only. Role,
        /// UserName and Status are never written here, so they stay read-only for
        /// Admin exactly as the Admin Profile UI intends. No re-authentication is
        /// ever required, since none of the security-scoping fields change.
        /// </summary>
        private async Task<IActionResult> UpdateMyAdminProfileAsync(UserInfo user, ProfileUpdateDto request)
        {
            var name = (request.Name ?? string.Empty).Trim();
            var email = (request.Email ?? string.Empty).Trim();

            if (name.Length == 0)
                return BadRequest(new { message = "Name is required." });

            if (email.Length == 0)
                return BadRequest(new { message = "Email is required." });

            if (request.DesignationId < 0)
                return BadRequest(new { message = "Select a valid designation." });

            var userWithEmail = await _userManager.FindByEmailAsync(email);
            if (userWithEmail != null && userWithEmail.Id != user.Id)
                return BadRequest(new { message = "Email is already used by another account." });

            user.Name = name;
            user.Email = email;
            user.DesignationId = request.DesignationId > 0 ? request.DesignationId : null;
            user.UpdatedAt = DateTime.Now;
            user.UpdatedBy = CurrentUserAudit;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new
                {
                    message = result.Errors.FirstOrDefault()?.Description ?? "Profile could not be updated."
                });

            return Ok(new
            {
                message = "Profile updated successfully",
                requiresReauthentication = false,
                isActive = user.IsActive,
                data = MapAdminToDto(user)
            });
        }

        private static ProfileDto MapToDto(EmployeeInformation employee, UserInfo user, Employeelogin login) => new()
        {
            LoginId = login.Id,
            EmployeeInformationId = employee.Id,
            EmployeeCode = employee.EmployeeCode ?? string.Empty,
            Name = employee.Name ?? string.Empty,
            Email = employee.Email ?? string.Empty,
            PersonalPhoneNumber = employee.PersonalPhoneNumber ?? string.Empty,
            OfficialPhoneNumber = employee.OfficialPhoneNumber ?? string.Empty,
            Role = login.Role,
            DesignationId = user.DesignationId ?? 0,
            ZoneId = login.ZoneId,
            StateId = login.StateId,
            RegionId = login.RegionId,
            HeadquartersId = login.HeadquartersId,
            IsActive = login.IsActive
        };

        private string FirstModelError() =>
            string.Join(" ",
                ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid input." : e.ErrorMessage));
    }
}
