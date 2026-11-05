using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using SpicAPI.Services;
using System.Text.RegularExpressions;
using static SPIC.Core.Entities.EmployeeRegistration;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeRegistrationController : ControllerBase
    {
        private readonly IGenericRepository<EmployeeInformation> _employeeRepo;

        public EmployeeRegistrationController(IGenericRepository<EmployeeInformation> employeeRepo)
        {
            _employeeRepo = employeeRepo;
        }

        [HttpPost]
        public async Task<IActionResult> SaveEmployeeInformation([FromBody] EmployeeInformation model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var errors = ValidateEmployeeInformationFields(model);
            if (errors.Count > 0)
                return BadRequest(new { errors });

            if (!string.IsNullOrWhiteSpace(model.EmployeeCode))
            {
                var trimmedCode = model.EmployeeCode.Trim();
                var exists = await _employeeRepo.GetWhere(e => e.EmployeeCode == trimmedCode).AnyAsync();
                if (exists)
                {
                    errors["EmployeeCode"] = new[] { "Employee ID already exists." };
                    return BadRequest(new { errors });
                }
            }

            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;

            var created = await _employeeRepo.CreateAsync(model);

            return Ok(new
            {
                message = "EmployeeInformation created successfully",
                data = created
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _employeeRepo.GetAll().ToListAsync();
            return Ok(items);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllWithInactive()
        {
            var items = await _employeeRepo.GetAllWithInactive().ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _employeeRepo.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] EmployeeInformation entity)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var errors = ValidateEmployeeInformationFields(entity);
            if (errors.Count > 0)
                return BadRequest(new { errors });

            if (!string.IsNullOrWhiteSpace(entity.EmployeeCode))
            {
                var trimmedCode = entity.EmployeeCode.Trim();
                var exists = await _employeeRepo.GetWhere(e => e.Id != id && e.EmployeeCode == trimmedCode).AnyAsync();
                if (exists)
                {
                    errors["EmployeeCode"] = new[] { "Employee ID already exists." };
                    return BadRequest(new { errors });
                }
            }

            entity.UpdatedAt = DateTime.Now;
            var updated = await _employeeRepo.PatchAsync(id, entity);

            if (updated == null) return NotFound();

            return Ok(new
            {
                message = "EmployeeInformation updated successfully",
                data = updated
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _employeeRepo.DeleteAsync(id);
            if (!deleted) return NotFound();

            return Ok(new
            {
                message = "EmployeeInformation deleted successfully"
            });
        }

        private static Dictionary<string, string[]> ValidateEmployeeInformationFields(EmployeeInformation employee)
        {
            var errors = new Dictionary<string, string[]>();

            void Add(string field, string message) =>
                errors[field] = new[] { message };

            if (string.IsNullOrWhiteSpace(employee.Name))
                Add("Name", "Employee Name is required.");

            if (string.IsNullOrWhiteSpace(employee.EmployeeCode))
                Add("EmployeeCode", "Employee ID is required.");

            var personalPhone = (employee.PersonalPhoneNumber ?? string.Empty).Trim();
            if (personalPhone.Length == 0)
                Add("PersonalPhoneNumber", "Personal Phone Number is required.");
            else if (!Regex.IsMatch(personalPhone, @"^\d{10}$"))
                Add("PersonalPhoneNumber", "Personal Phone Number must be 10 digits.");

            var email = (employee.Email ?? string.Empty).Trim();
            if (email.Length == 0)
                Add("Email", "Email ID is required.");
            else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                Add("Email", "Enter a valid Email ID.");

            return errors;
        }
    }

    [Route("api/[controller]")]
    public class EmployeeloginController(IGenericRepository<Employeelogin> repo)
        : GenericCrudController<Employeelogin>(repo);

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeLoginSetupController : ControllerBase
    {
        private static readonly AppRole[] EmployeeManagementRoles =
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
            AppRole.SpecialAdmin,
            AppRole.CommonRole,
            AppRole.MDAdmin,
            AppRole.MDHOD
        };

        private readonly UserManager<UserInfo> _userManager;
        private readonly IGenericRepository<Employeelogin> _employeeLoginRepo;
        private readonly IGenericRepository<EmployeeInformation> _employeeInfoRepo;
        private readonly IGenericRepository<SpecialAdminLocations> _specialAdminLocationsRepo;
        private readonly IGenericRepository<Designation> _designationRepo;
        private readonly IGenericRepository<Zone> _zoneRepo;
        private readonly IGenericRepository<State> _stateRepo;
        private readonly IGenericRepository<Region> _regionRepo;
        private readonly IGenericRepository<Headquarter> _headquarterRepo;

        public EmployeeLoginSetupController(
            UserManager<UserInfo> userManager,
            IGenericRepository<Employeelogin> employeeLoginRepo,
            IGenericRepository<EmployeeInformation> employeeInfoRepo,
            IGenericRepository<SpecialAdminLocations> specialAdminLocationsRepo,
            IGenericRepository<Designation> designationRepo,
            IGenericRepository<Zone> zoneRepo,
            IGenericRepository<State> stateRepo,
            IGenericRepository<Region> regionRepo,
            IGenericRepository<Headquarter> headquarterRepo)
        {
            _userManager = userManager;
            _employeeLoginRepo = employeeLoginRepo;
            _employeeInfoRepo = employeeInfoRepo;
            _specialAdminLocationsRepo = specialAdminLocationsRepo;
            _designationRepo = designationRepo;
            _zoneRepo = zoneRepo;
            _stateRepo = stateRepo;
            _regionRepo = regionRepo;
            _headquarterRepo = headquarterRepo;
        }

        [HttpPost]
        public async Task<IActionResult> SaveEmployeeLogin([FromBody] EmployeeLoginCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!IsValidUserName(request.UserName))
                return BadRequest("Username must not contain spaces or invisible characters.");

            var employee = await _employeeInfoRepo
                .GetWhere(x => x.Id == request.EmployeeInformationID)
                .FirstOrDefaultAsync();

            if (employee == null)
                return BadRequest("EmployeeInformation not found");

            if (request.RegionIds != null)
            {
                request.RegionIds = request.RegionIds.Where(id => id > 0).Distinct().ToArray();
                request.RegionId = request.RegionIds.FirstOrDefault();
            }
            else
            {
                request.RegionIds = request.RegionId > 0 ? new[] { request.RegionId } : Array.Empty<int>();
            }

            if (request.HeadquartersIds != null)
            {
                request.HeadquartersIds = request.HeadquartersIds.Where(id => id > 0).Distinct().ToArray();
                request.HeadquartersId = request.HeadquartersIds.FirstOrDefault();
            }
            else
            {
                request.HeadquartersIds = request.HeadquartersId > 0 ? new[] { request.HeadquartersId } : Array.Empty<int>();
            }

            var designation = await ResolveDesignationAsync(request.DesignationId);
            var errors = ValidateEmployeeLoginRequest(request, employee, designation, isCreate: true);
            if (errors.Count == 0)
                await ValidateGeographyHierarchyAsync(request, errors);

            if (errors.Count > 0)
                return BadRequest(new { errors });

            NormalizeGeographyForRole(request);

            var existingUser = await _userManager.FindByNameAsync(request.UserName);
            if (existingUser != null)
                return BadRequest(new { errors = new Dictionary<string, string[]> { ["UserName"] = new[] { "Username already exists." } } });

            //if (!string.IsNullOrWhiteSpace(request.Email))
            //{
            //	var existingEmail = await _userManager.FindByEmailAsync(request.Email);
            //	if (existingEmail != null)
            //		return BadRequest("Email already exists");
            //}

            var user = new UserInfo
            {
                UserName = request.UserName,
                Password = request.Password,
                Email = request.Email ?? employee.Email,
                PhoneNumber = employee.OfficialPhoneNumber,
                Name = request.Name ?? employee.Name,
                DesignationId = request.DesignationId,
                Role = request.Role,
                CreatedAt = DateTime.Now,
                CreatedBy = request.CreatedBy,
                UpdatedAt = DateTime.Now,
                UpdatedBy = request.UpdatedBy ?? "System",
                IsActive = request.IsActive
            };

            var identityResult = await _userManager.CreateAsync(user, request.Password);
            if (!identityResult.Succeeded)
                return BadRequest(identityResult.Errors.Select(x => x.Description));

            var login = new Employeelogin
            {
                UserId = user.Id,
                EmployeeInformationID = request.EmployeeInformationID,
                Role = request.Role,
                ZoneId = request.ZoneId,
                StateId = request.StateId,
                RegionId = request.RegionId,
                HeadquartersId = request.HeadquartersId,
                RegionIds = request.RegionIds,
                HeadquartersIds = request.HeadquartersIds,
                IsActive = request.IsActive
            };

            var createdLogin = await _employeeLoginRepo.CreateAsync(login);

            // SpecialAdmin: persist multi-location assignments (ignored for other roles)
            if (request.Role == AppRole.SpecialAdmin && request.SpecialAdminLocations != null)
            {
                await SaveSpecialAdminLocationsAsync(request, currentUser: request.CreatedBy ?? "System");
            }

            return Ok(new
            {
                message = "Employeelogin created successfully",
                data = new
                {
                    createdLogin.Id,
                    createdLogin.UserId,
                    createdLogin.EmployeeInformationID,
                    createdLogin.Role,
                    createdLogin.ZoneId,
                    createdLogin.StateId,
                    createdLogin.RegionId,
                    createdLogin.HeadquartersId,
                    createdLogin.RegionIds,
                    createdLogin.HeadquartersIds,
                    createdLogin.IsActive,
                    UserName = user.UserName,
                    user.Email
                }
            });
        }

        [HttpGet("by-employee/{employeeId}")]
        public async Task<IActionResult> GetByEmployeeId(int employeeId)
        {
            var employee = await _employeeInfoRepo
                .GetWhere(x => x.Id == employeeId)
                .FirstOrDefaultAsync();

            if (employee == null)
                return NotFound("EmployeeInformation not found");

            var login = await _employeeLoginRepo
                .GetWhere(x => x.EmployeeInformationID == employeeId)
                .InCurrentRowOrder()
                .FirstOrDefaultAsync();

            if (login == null)
                return NotFound("Employeelogin not found");

            var user = await _userManager.FindByIdAsync(login.UserId);
            if (user == null)
                return NotFound("UserInfo not found");

            return Ok(new EmployeeLoginCreateRequest
            {
                EmployeeInformationID = employee.Id,
                UserName = user.UserName ?? "",
                Password = user.Password ?? "",
                Email = user.Email,
                Name = employee.Name,
                DesignationId = user.DesignationId,
                Role = login.Role,
                ZoneId = login.ZoneId,
                StateId = login.StateId,
                RegionId = login.RegionId,
                HeadquartersId = login.HeadquartersId,
                RegionIds = login.RegionIds ?? (login.RegionId > 0 ? new[] { login.RegionId } : Array.Empty<int>()),
                HeadquartersIds = login.HeadquartersIds ?? (login.HeadquartersId > 0 ? new[] { login.HeadquartersId } : Array.Empty<int>()),
                IsActive = login.IsActive,
                CreatedBy = employee.CreatedBy,
                UpdatedBy = employee.UpdatedBy,
                SpecialAdminLocations = await LoadSpecialAdminLocationsAsync(employee.Id, login.Role)
            });
        }

        [HttpPut("{employeeId}")]
        public async Task<IActionResult> UpdateEmployeeLogin(int employeeId, [FromBody] EmployeeLoginCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var employee = await _employeeInfoRepo
                .GetWhere(x => x.Id == employeeId)
                .FirstOrDefaultAsync();

            if (employee == null)
                return NotFound("EmployeeInformation not found");

            var login = await _employeeLoginRepo
                .GetWhere(x => x.EmployeeInformationID == employeeId)
                .InCurrentRowOrder()
                .FirstOrDefaultAsync();

            if (login == null)
                return NotFound("Employeelogin not found");

            var user = await _userManager.FindByIdAsync(login.UserId);
            if (user == null)
                return NotFound("UserInfo not found");

            if (request.RegionIds != null)
            {
                request.RegionIds = request.RegionIds.Where(id => id > 0).Distinct().ToArray();
                request.RegionId = request.RegionIds.FirstOrDefault();
            }
            else
            {
                request.RegionIds = request.RegionId > 0 ? new[] { request.RegionId } : Array.Empty<int>();
            }

            if (request.HeadquartersIds != null)
            {
                request.HeadquartersIds = request.HeadquartersIds.Where(id => id > 0).Distinct().ToArray();
                request.HeadquartersId = request.HeadquartersIds.FirstOrDefault();
            }
            else
            {
                request.HeadquartersIds = request.HeadquartersId > 0 ? new[] { request.HeadquartersId } : Array.Empty<int>();
            }

            var designation = await ResolveDesignationAsync(request.DesignationId);
            var errors = ValidateEmployeeLoginRequest(request, employee, designation, isCreate: false);
            if (errors.Count == 0)
                await ValidateGeographyHierarchyAsync(request, errors);

            if (errors.Count > 0)
                return BadRequest(new { errors });

            NormalizeGeographyForRole(request);

            if (user.UserName != request.UserName && !IsValidUserName(request.UserName))
                return BadRequest("Username must not contain spaces or invisible characters.");

            var existingUserWithSameName = await _userManager.FindByNameAsync(request.UserName);
            if (existingUserWithSameName != null && existingUserWithSameName.Id != user.Id)
                return BadRequest(new { errors = new Dictionary<string, string[]> { ["UserName"] = new[] { "Username already exists." } } });

            //if (!string.IsNullOrWhiteSpace(request.Email))
            //{
            //	var existingEmail = await _userManager.FindByEmailAsync(request.Email);
            //	if (existingEmail != null && existingEmail.Id != user.Id)
            //		return BadRequest("Email already exists");
            //}

            employee.Name = request.Name ?? employee.Name;
            employee.Email = request.Email ?? employee.Email;
            employee.UpdatedAt = DateTime.Now;
            employee.UpdatedBy = request.UpdatedBy ?? "System";

            await _employeeInfoRepo.PatchAsync(employeeId, employee);

            user.UserName = request.UserName;
            user.NormalizedUserName = request.UserName.ToUpper();
            user.Email = request.Email ?? user.Email;
            user.NormalizedEmail = (request.Email ?? user.Email ?? "").ToUpper();
            user.Name = request.Name ?? user.Name;
            user.DesignationId = request.DesignationId;
            user.Role = request.Role;
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.Now;
            user.UpdatedBy = request.UpdatedBy ?? "System";

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.Password = request.Password;
                // Setting PasswordHash directly bypasses UserManager entirely, so the configured
                // password policy (Program.cs's options.Password.RequiredLength, etc.) was never
                // being enforced here - only on initial creation via CreateAsync. Running it
                // through the same validators CreateAsync uses internally keeps this in sync with
                // whatever the policy is configured to, instead of duplicating a hardcoded length
                // check that would drift from Program.cs over time.
                foreach (var validator in _userManager.PasswordValidators)
                {
                    var validationResult = await validator.ValidateAsync(_userManager, user, request.Password);
                    if (!validationResult.Succeeded)
                        return BadRequest(validationResult.Errors.Select(x => x.Description));
                }

                user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, request.Password);
            }

            var userUpdateResult = await _userManager.UpdateAsync(user);
            if (!userUpdateResult.Succeeded)
                return BadRequest(userUpdateResult.Errors.Select(x => x.Description));

            login.Role = request.Role;
            login.ZoneId = request.ZoneId;
            login.StateId = request.StateId;
            login.RegionId = request.RegionId;
            login.HeadquartersId = request.HeadquartersId;
            login.RegionIds = request.RegionIds;
            login.HeadquartersIds = request.HeadquartersIds;
            login.IsActive = request.IsActive;

            await _employeeLoginRepo.PatchAsync(login.Id, login);

            // SpecialAdmin: replace multi-location assignments
            if (request.Role == AppRole.SpecialAdmin)
            {
                await ReplaceSpecialAdminLocationsAsync(request);
            }

            return Ok(new
            {
                message = "Employee details updated successfully"
            });
        }

        [HttpDelete("{employeeId}")]
        public async Task<IActionResult> DeleteEmployeeLogin(int employeeId)
        {
            var employee = await _employeeInfoRepo
                .GetWhere(x => x.Id == employeeId)
                .FirstOrDefaultAsync();

            if (employee == null)
                return NotFound("EmployeeInformation not found");

            var login = await _employeeLoginRepo
                .GetWhere(x => x.EmployeeInformationID == employeeId)
                .InCurrentRowOrder()
                .FirstOrDefaultAsync();

            if (login != null)
            {
                var user = await _userManager.FindByIdAsync(login.UserId);
                if (user != null)
                {
                    await _userManager.DeleteAsync(user);
                }

                await _employeeLoginRepo.DeleteAsync(login.Id);
            }

            await DeleteSpecialAdminLocationsForEmployeeAsync(employeeId);

            await _employeeInfoRepo.DeleteAsync(employeeId);

            return Ok(new
            {
                message = "Employee deleted successfully"
            });
        }
        [HttpDelete("login/{loginId}")]
        public async Task<IActionResult> DeleteSpecificLogin(int loginId)
        {
            var login = await _employeeLoginRepo.GetByIdAsync(loginId);
            if (login == null)
                return NotFound("Employeelogin not found");

            var empId = login.EmployeeInformationID;

            // 1. Delete the Identity User account
            var user = await _userManager.FindByIdAsync(login.UserId);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            // 2. Delete the specific role/login
            await _employeeLoginRepo.DeleteAsync(loginId);

            // SpecialAdmin: clean up any multi-location rows for this employee
            await DeleteSpecialAdminLocationsForEmployeeAsync(empId);

            // 3. Check if this was the last remaining role for the employee. 
            // If they have no logins left, delete the main employee record.
            var remainingLogins = await _employeeLoginRepo
                .GetWhere(x => x.EmployeeInformationID == empId)
                .CountAsync();

            if (remainingLogins == 0)
            {
                await _employeeInfoRepo.DeleteAsync(empId);
            }

            return Ok(new { message = "Specific login deleted successfully" });
        }
        [HttpGet("by-login/{loginId}")]
        public async Task<IActionResult> GetByLoginId(int loginId)
        {
            var login = await _employeeLoginRepo.GetByIdAsync(loginId);
            if (login == null)
                return NotFound("Employeelogin not found");

            var employee = await _employeeInfoRepo.GetByIdAsync(login.EmployeeInformationID);
            if (employee == null)
                return NotFound("EmployeeInformation not found");

            var user = await _userManager.FindByIdAsync(login.UserId);

            return Ok(new EmployeeLoginCreateRequest
            {
                EmployeeInformationID = employee.Id,
                UserName = user?.UserName ?? "",
                Password = user?.Password ?? "",
                Email = user?.Email,
                Name = employee.Name,
                DesignationId = user?.DesignationId,
                Role = login.Role,
                ZoneId = login.ZoneId,
                StateId = login.StateId,
                RegionId = login.RegionId,
                HeadquartersId = login.HeadquartersId,
                RegionIds = login.RegionIds ?? (login.RegionId > 0 ? new[] { login.RegionId } : Array.Empty<int>()),
                HeadquartersIds = login.HeadquartersIds ?? (login.HeadquartersId > 0 ? new[] { login.HeadquartersId } : Array.Empty<int>()),
                IsActive = login.IsActive,
                CreatedBy = employee.CreatedBy,
                UpdatedBy = employee.UpdatedBy,
                SpecialAdminLocations = await LoadSpecialAdminLocationsAsync(login.EmployeeInformationID, login.Role)
            });
        }

        [HttpPut("login/{loginId}")]
        public async Task<IActionResult> UpdateSpecificLogin(int loginId, [FromBody] EmployeeLoginCreateRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var login = await _employeeLoginRepo.GetByIdAsync(loginId);
            if (login == null) return NotFound("Employeelogin not found");

            var employee = await _employeeInfoRepo.GetByIdAsync(login.EmployeeInformationID);
            if (employee == null)
                return NotFound("EmployeeInformation not found");

            var user = await _userManager.FindByIdAsync(login.UserId);
            if (user == null)
                return NotFound("UserInfo not found");

            if (request.RegionIds != null)
            {
                request.RegionIds = request.RegionIds.Where(id => id > 0).Distinct().ToArray();
                request.RegionId = request.RegionIds.FirstOrDefault();
            }
            else
            {
                request.RegionIds = request.RegionId > 0 ? new[] { request.RegionId } : Array.Empty<int>();
            }

            if (request.HeadquartersIds != null)
            {
                request.HeadquartersIds = request.HeadquartersIds.Where(id => id > 0).Distinct().ToArray();
                request.HeadquartersId = request.HeadquartersIds.FirstOrDefault();
            }
            else
            {
                request.HeadquartersIds = request.HeadquartersId > 0 ? new[] { request.HeadquartersId } : Array.Empty<int>();
            }

            var designation = await ResolveDesignationAsync(request.DesignationId);
            var errors = ValidateEmployeeLoginRequest(request, employee, designation, isCreate: false);
            if (errors.Count == 0)
                await ValidateGeographyHierarchyAsync(request, errors);

            if (errors.Count > 0)
                return BadRequest(new { errors });

            NormalizeGeographyForRole(request);

            if (user.UserName != request.UserName && !IsValidUserName(request.UserName))
                return BadRequest("Username must not contain spaces or invisible characters.");

            var existingUserWithSameName = await _userManager.FindByNameAsync(request.UserName);
            if (existingUserWithSameName != null && existingUserWithSameName.Id != user.Id)
                return BadRequest(new { errors = new Dictionary<string, string[]> { ["UserName"] = new[] { "Username already exists." } } });

            // Update Identity User
            user.UserName = request.UserName;
            user.NormalizedUserName = request.UserName.ToUpper();
            user.Email = request.Email ?? user.Email;
            user.NormalizedEmail = (request.Email ?? user.Email ?? "").ToUpper();
            user.Name = request.Name ?? user.Name;
            user.DesignationId = request.DesignationId;
            user.Role = request.Role;
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.Now;
            user.UpdatedBy = User?.Identity?.Name ?? "System";

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.Password = request.Password;
                // See the identical comment in UpdateEmployeeLogin above - setting PasswordHash
                // directly bypasses UserManager's password policy validators entirely.
                foreach (var validator in _userManager.PasswordValidators)
                {
                    var validationResult = await validator.ValidateAsync(_userManager, user, request.Password);
                    if (!validationResult.Succeeded)
                        return BadRequest(validationResult.Errors.Select(x => x.Description));
                }

                user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, request.Password);
            }

            var userUpdateResult = await _userManager.UpdateAsync(user);
            if (!userUpdateResult.Succeeded) return BadRequest(userUpdateResult.Errors.Select(x => x.Description));

            // Update Login/Role details
            login.Role = request.Role;
            login.ZoneId = request.ZoneId;
            login.StateId = request.StateId;
            login.RegionId = request.RegionId;
            login.HeadquartersId = request.HeadquartersId;
            login.RegionIds = request.RegionIds;
            login.HeadquartersIds = request.HeadquartersIds;
            login.IsActive = request.IsActive;

            await _employeeLoginRepo.PatchAsync(login.Id, login);

            // SpecialAdmin: replace multi-location assignments
            if (request.Role == AppRole.SpecialAdmin)
            {
                await ReplaceSpecialAdminLocationsAsync(request);
            }

            return Ok(new { message = "Employee role details updated successfully" });
        }

        private static bool IsValidUserName(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return false;
            return Regex.IsMatch(userName, @"\A[\x21-\x7E]+\z");
        }

        private async Task<Designation?> ResolveDesignationAsync(int? designationId)
        {
            if (designationId is null or <= 0)
                return null;

            // Deliberately does not filter on IsActive: a deactivated designation on an
            // existing employee must not block an unrelated edit. Active status is
            // enforced only where it changes access (CommonRole / AVP Office).
            return await _designationRepo
                .GetWhere(d => d.Id == designationId.Value)
                .FirstOrDefaultAsync();
        }

        private static Dictionary<string, string[]> ValidateEmployeeLoginRequest(
            EmployeeLoginCreateRequest request,
            EmployeeInformation employee,
            Designation? designation,
            bool isCreate)
        {
            var errors = new Dictionary<string, string[]>();

            void Add(string field, string message)
            {
                errors[field] = errors.TryGetValue(field, out var existing)
                    ? existing.Append(message).ToArray()
                    : new[] { message };
            }

            if (!EmployeeManagementRoles.Contains(request.Role))
                Add("Role", "Select a valid role.");

            if (string.IsNullOrWhiteSpace(request.UserName))
                Add("UserName", "Username is required.");
            else if (!IsValidUserName(request.UserName))
                Add("UserName", "Username must not contain spaces or invisible characters.");

            if (isCreate && string.IsNullOrWhiteSpace(request.Password))
                Add("Password", "Password is required.");

            var name = (request.Name ?? employee.Name ?? string.Empty).Trim();
            if (name.Length == 0)
                Add("Name", "Employee Name is required.");

            if (string.IsNullOrWhiteSpace(employee.EmployeeCode))
                Add("EmployeeCode", "Employee ID is required.");

            var personalPhone = (employee.PersonalPhoneNumber ?? string.Empty).Trim();
            if (personalPhone.Length == 0)
                Add("PersonalPhoneNumber", "Personal Phone Number is required.");
            else if (!Regex.IsMatch(personalPhone, @"^\d{10}$"))
                Add("PersonalPhoneNumber", "Personal Phone Number must be 10 digits.");

            var email = (request.Email ?? employee.Email ?? string.Empty).Trim();
            if (email.Length == 0)
                Add("Email", "Email ID is required.");
            else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                Add("Email", "Enter a valid Email ID.");

            if (request.DesignationId is > 0 && designation == null)
                Add("DesignationId", "Select a valid designation.");

            if (request.Role == AppRole.CommonRole)
            {
                if (request.DesignationId is null or <= 0)
                    Add("DesignationId", "Select a designation for CommonRole.");
                else if (designation != null && !designation.IsActive)
                    Add("DesignationId", "Select an active designation.");
            }

            switch (request.Role)
            {
                case AppRole.AVP:
                    if (request.ZoneId <= 0)
                        Add("ZoneId", "Zone is required.");
                    break;

                case AppRole.SMD:
                case AppRole.SMM:
                case AppRole.MDAdmin:
                case AppRole.MDHOD:
                    if (request.StateId <= 0)
                        Add("StateId", "State is required.");
                    break;

                case AppRole.RM:
                case AppRole.RMD:
                    if (request.StateId <= 0)
                        Add("StateId", "State is required.");
                    if (request.RegionId <= 0 && (request.RegionIds == null || request.RegionIds.Length == 0))
                        Add("RegionId", "Region is required.");
                    break;

                case AppRole.MO:
                case AppRole.MDO:
                case AppRole.JMDO:
                    if (request.StateId <= 0)
                        Add("StateId", "State is required.");
                    if (request.RegionId <= 0 && (request.RegionIds == null || request.RegionIds.Length == 0))
                        Add("RegionId", "Region is required.");
                    if (request.HeadquartersId <= 0 && (request.HeadquartersIds == null || request.HeadquartersIds.Length == 0))
                        Add("HeadquartersId", "Headquarters is required.");
                    break;

                // CommonRole geography is optional for every designation, including
                // AVP Office. Supplied IDs are checked for existence/consistency by
                // ValidateCommonRoleGeographyAsync instead of requiring parents here.
                case AppRole.CommonRole:
                    break;

                case AppRole.SpecialAdmin:
                    if (request.SpecialAdminLocations == null ||
                        !request.SpecialAdminLocations.Any(l => l.StateId > 0))
                        Add("SpecialAdminLocations", "At least one State is required for SpecialAdmin.");
                    break;
            }

            return errors;
        }

        private async Task ValidateGeographyHierarchyAsync(
            EmployeeLoginCreateRequest request,
            Dictionary<string, string[]> errors)
        {
            if (request.Role == AppRole.CommonRole)
            {
                await ValidateCommonRoleGeographyAsync(request, errors);
                return;
            }

            void Add(string field, string message)
            {
                errors[field] = errors.TryGetValue(field, out var existing)
                    ? (existing.Contains(message) ? existing : existing.Append(message).ToArray())
                    : new[] { message };
            }

            if (request.Role is AppRole.RM or AppRole.RMD)
            {
                var checkRegionIds = (request.RegionIds != null && request.RegionIds.Length > 0)
                    ? request.RegionIds
                    : (request.RegionId > 0 ? new[] { request.RegionId } : Array.Empty<int>());

                foreach (var rId in checkRegionIds)
                {
                    var region = await _regionRepo.GetWhere(r => r.Id == rId).FirstOrDefaultAsync();
                    if (region == null)
                        Add("RegionId", "Select a valid Region.");
                    else if (request.StateId > 0 && region.StateId != request.StateId)
                        Add("RegionId", "Selected Region does not belong to the selected State.");
                }
            }
            else if (request.Role is AppRole.MO or AppRole.MDO or AppRole.JMDO)
            {
                if (request.RegionId > 0)
                {
                    var region = await _regionRepo.GetWhere(r => r.Id == request.RegionId).FirstOrDefaultAsync();
                    if (region == null)
                        Add("RegionId", "Select a valid Region.");
                    else if (request.StateId > 0 && region.StateId != request.StateId)
                        Add("RegionId", "Selected Region does not belong to the selected State.");
                }

                var checkHqIds = (request.HeadquartersIds != null && request.HeadquartersIds.Length > 0)
                    ? request.HeadquartersIds
                    : (request.HeadquartersId > 0 ? new[] { request.HeadquartersId } : Array.Empty<int>());

                foreach (var hqId in checkHqIds)
                {
                    var headquarter = await _headquarterRepo.GetWhere(h => h.Id == hqId).FirstOrDefaultAsync();
                    if (headquarter == null)
                        Add("HeadquartersId", "Select a valid Headquarters.");
                    else if (request.RegionId > 0 && headquarter.RegionId != request.RegionId)
                        Add("HeadquartersId", "Selected Headquarters does not belong to the selected Region.");
                }
            }
        }

        private async Task ValidateCommonRoleGeographyAsync(
            EmployeeLoginCreateRequest request,
            Dictionary<string, string[]> errors)
        {
            void Add(string field, string message)
            {
                errors[field] = errors.TryGetValue(field, out var existing)
                    ? (existing.Contains(message) ? existing : existing.Append(message).ToArray())
                    : new[] { message };
            }

            var zone = request.ZoneId > 0
                ? await _zoneRepo.GetWhere(z => z.Id == request.ZoneId).FirstOrDefaultAsync()
                : null;
            if (request.ZoneId > 0 && zone == null)
                Add("ZoneId", "Select a valid Zone.");

            var state = request.StateId > 0
                ? await _stateRepo.GetWhere(s => s.Id == request.StateId).FirstOrDefaultAsync()
                : null;
            if (request.StateId > 0 && state == null)
                Add("StateId", "Select a valid State.");
            if (zone != null && state != null && state.ZoneId != zone.Id)
                Add("StateId", "Selected State does not belong to the selected Zone.");

            var checkRegionIds = (request.RegionIds != null && request.RegionIds.Length > 0)
                ? request.RegionIds
                : (request.RegionId > 0 ? new[] { request.RegionId } : Array.Empty<int>());

            var validatedRegions = new List<Region>();
            foreach (var rId in checkRegionIds)
            {
                var region = await _regionRepo.GetWhere(r => r.Id == rId).FirstOrDefaultAsync();
                if (region == null)
                {
                    Add("RegionId", "Select a valid Region.");
                    continue;
                }
                validatedRegions.Add(region);

                if (state != null && region.StateId != state.Id)
                    Add("RegionId", "Selected Region does not belong to the selected State.");

                if (state == null && zone != null)
                {
                    var regionState = await _stateRepo.GetWhere(s => s.Id == region.StateId).FirstOrDefaultAsync();
                    if (regionState == null)
                        Add("RegionId", "Selected Region is not linked to a valid State.");
                    else if (regionState.ZoneId != zone.Id)
                        Add("RegionId", "Selected Region does not belong to the selected Zone.");
                }
            }

            var checkHqIds = (request.HeadquartersIds != null && request.HeadquartersIds.Length > 0)
                ? request.HeadquartersIds
                : (request.HeadquartersId > 0 ? new[] { request.HeadquartersId } : Array.Empty<int>());

            foreach (var hqId in checkHqIds)
            {
                var headquarter = await _headquarterRepo.GetWhere(h => h.Id == hqId).FirstOrDefaultAsync();
                if (headquarter == null)
                {
                    Add("HeadquartersId", "Select a valid Headquarters.");
                    continue;
                }

                if (validatedRegions.Count > 0)
                {
                    if (!validatedRegions.Any(r => r.Id == headquarter.RegionId))
                        Add("HeadquartersId", "Selected Headquarters does not belong to the selected Region.");
                }
                else
                {
                    var hqRegion = await _regionRepo.GetWhere(r => r.Id == headquarter.RegionId).FirstOrDefaultAsync();
                    if (hqRegion == null)
                    {
                        Add("HeadquartersId", "Selected Headquarters is not linked to a valid Region.");
                    }
                    else if (state != null && hqRegion.StateId != state.Id)
                    {
                        Add("HeadquartersId", "Selected Headquarters does not belong to the selected State.");
                    }
                    else if (state == null && zone != null)
                    {
                        var hqState = await _stateRepo.GetWhere(s => s.Id == hqRegion.StateId).FirstOrDefaultAsync();
                        if (hqState == null)
                            Add("HeadquartersId", "Selected Headquarters is not linked to a valid State.");
                        else if (hqState.ZoneId != zone.Id)
                            Add("HeadquartersId", "Selected Headquarters does not belong to the selected Zone.");
                    }
                }
            }
        }

        private static void NormalizeGeographyForRole(EmployeeLoginCreateRequest request)
        {
            switch (request.Role)
            {
                case AppRole.AVP:
                    request.StateId = 0;
                    request.RegionId = 0;
                    request.HeadquartersId = 0;
                    request.RegionIds = Array.Empty<int>();
                    request.HeadquartersIds = Array.Empty<int>();
                    break;

                case AppRole.SMD:
                case AppRole.SMM:
                case AppRole.MDAdmin:
                case AppRole.MDHOD:
                    request.ZoneId = 0;
                    request.RegionId = 0;
                    request.HeadquartersId = 0;
                    request.RegionIds = Array.Empty<int>();
                    request.HeadquartersIds = Array.Empty<int>();
                    break;

                case AppRole.RM:
                case AppRole.RMD:
                    request.ZoneId = 0;
                    request.HeadquartersId = 0;
                    request.HeadquartersIds = Array.Empty<int>();
                    break;

                case AppRole.MO:
                case AppRole.MDO:
                case AppRole.JMDO:
                    request.ZoneId = 0;
                    request.RegionIds = request.RegionId > 0 ? new[] { request.RegionId } : Array.Empty<int>();
                    break;

                case AppRole.CommonRole:
                    // Preserve every supplied CommonRole location field, regardless
                    // of the selected designation (including AVP Office).
                    break;

                case AppRole.SpecialAdmin:
                    break;

                default:
                    request.ZoneId = 0;
                    request.StateId = 0;
                    request.RegionId = 0;
                    request.HeadquartersId = 0;
                    request.RegionIds = Array.Empty<int>();
                    request.HeadquartersIds = Array.Empty<int>();
                    break;
            }
        }

        private async Task<List<SpecialAdminLocationItem>?> LoadSpecialAdminLocationsAsync(int employeeInfoId, AppRole role)
        {
            if (role != AppRole.SpecialAdmin)
                return null;

            var rows = await _specialAdminLocationsRepo
                .GetWhere(x => x.EmployeeInformationID == employeeInfoId)
                .ToListAsync();

            return rows.Select(r => new SpecialAdminLocationItem
            {
                StateId = r.StateId,
                RegionId = r.RegionId,
                HeadquarterId = r.HeadquarterId
            }).ToList();
        }

        // Only used on create (when there are no existing rows to remove).
        private async Task SaveSpecialAdminLocationsAsync(EmployeeLoginCreateRequest request, string currentUser)
        {
            foreach (var loc in request.SpecialAdminLocations!)
            {
                if (loc.StateId <= 0)
                    continue;

                await _specialAdminLocationsRepo.CreateAsync(new SpecialAdminLocations
                {
                    EmployeeInformationID = request.EmployeeInformationID,
                    StateId = loc.StateId,
                    RegionId = Math.Max(0, loc.RegionId),
                    HeadquarterId = Math.Max(0, loc.HeadquarterId),
                    CreatedAt = DateTime.Now,
                    CreatedBy = currentUser,
                    UpdatedAt = DateTime.Now,
                    UpdatedBy = currentUser
                });
            }
        }

        // Replaces all existing SpecialAdmin location rows for the employee with
        // the set supplied in the request (deletes orphans, prevents duplicates).
        private async Task ReplaceSpecialAdminLocationsAsync(EmployeeLoginCreateRequest request)
        {
            var existing = await _specialAdminLocationsRepo
                .GetWhere(x => x.EmployeeInformationID == request.EmployeeInformationID)
                .ToListAsync();

            foreach (var row in existing)
            {
                await _specialAdminLocationsRepo.DeleteAsync(row.Id);
            }

            if (request.SpecialAdminLocations != null)
            {
                await SaveSpecialAdminLocationsAsync(request, request.UpdatedBy ?? "System");
            }
        }

        private async Task DeleteSpecialAdminLocationsForEmployeeAsync(int employeeInfoId)
        {
            var rows = await _specialAdminLocationsRepo
                .GetWhere(x => x.EmployeeInformationID == employeeInfoId)
                .ToListAsync();

            foreach (var row in rows)
            {
                await _specialAdminLocationsRepo.DeleteAsync(row.Id);
            }
        }
    }

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserInfoController : ControllerBase
    {
        private readonly UserManager<UserInfo> _userManager;

        public UserInfoController(UserManager<UserInfo> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userManager.Users
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.Name,
                    u.IsActive,
                    u.DesignationId,
                    u.Password
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}