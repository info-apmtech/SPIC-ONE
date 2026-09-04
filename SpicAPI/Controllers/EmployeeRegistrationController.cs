using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
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
    }

    [Route("api/[controller]")]
    public class EmployeeloginController(IGenericRepository<Employeelogin> repo)
        : GenericCrudController<Employeelogin>(repo);

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeLoginSetupController : ControllerBase
    {
        private readonly UserManager<UserInfo> _userManager;
        private readonly IGenericRepository<Employeelogin> _employeeLoginRepo;
        private readonly IGenericRepository<EmployeeInformation> _employeeInfoRepo;

        public EmployeeLoginSetupController(
            UserManager<UserInfo> userManager,
            IGenericRepository<Employeelogin> employeeLoginRepo,
            IGenericRepository<EmployeeInformation> employeeInfoRepo)
        {
            _userManager = userManager;
            _employeeLoginRepo = employeeLoginRepo;
            _employeeInfoRepo = employeeInfoRepo;
        }

        [HttpPost]
        public async Task<IActionResult> SaveEmployeeLogin([FromBody] EmployeeLoginCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var employee = await _employeeInfoRepo
                .GetWhere(x => x.Id == request.EmployeeInformationID)
                .FirstOrDefaultAsync();

            if (employee == null)
                return BadRequest("EmployeeInformation not found");

            var existingUser = await _userManager.FindByNameAsync(request.UserName);
            if (existingUser != null)
                return BadRequest("Username already exists");

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
                IsActive = request.IsActive
            };

            var createdLogin = await _employeeLoginRepo.CreateAsync(login);

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
                IsActive = login.IsActive,
                CreatedBy = employee.CreatedBy,
                UpdatedBy = employee.UpdatedBy
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
                .FirstOrDefaultAsync();

            if (login == null)
                return NotFound("Employeelogin not found");

            var user = await _userManager.FindByIdAsync(login.UserId);
            if (user == null)
                return NotFound("UserInfo not found");

            var existingUserWithSameName = await _userManager.FindByNameAsync(request.UserName);
            if (existingUserWithSameName != null && existingUserWithSameName.Id != user.Id)
                return BadRequest("Username already exists");

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
            user.Password = request.Password;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
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
            login.IsActive = request.IsActive;

            await _employeeLoginRepo.PatchAsync(login.Id, login);

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
                IsActive = login.IsActive,
                CreatedBy = employee.CreatedBy,
                UpdatedBy = employee.UpdatedBy
            });
        }

        [HttpPut("login/{loginId}")]
        public async Task<IActionResult> UpdateSpecificLogin(int loginId, [FromBody] EmployeeLoginCreateRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var login = await _employeeLoginRepo.GetByIdAsync(loginId);
            if (login == null) return NotFound("Employeelogin not found");

            var employee = await _employeeInfoRepo.GetByIdAsync(login.EmployeeInformationID);
            var user = await _userManager.FindByIdAsync(login.UserId);

            var existingUserWithSameName = await _userManager.FindByNameAsync(request.UserName);
            if (existingUserWithSameName != null && existingUserWithSameName.Id != user.Id)
                return BadRequest("Username already exists");

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
            user.Password = request.Password;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
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
            login.IsActive = request.IsActive;

            await _employeeLoginRepo.PatchAsync(login.Id, login);

            return Ok(new { message = "Employee role details updated successfully" });
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