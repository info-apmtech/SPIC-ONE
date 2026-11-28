using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }   

        /// <summary>
        /// The roles that may create, edit, delete or (de)activate accounts here (final fix wave,
        /// docs/farmer-portal.md "Hardening"). No page in this repository calls these four endpoints
        /// (git grep: none), so the restriction cannot break a staff flow; it closes them to every
        /// dealer, field role and farmer, which a bare [Authorize] let through.
        /// </summary>
        public const string AccountAdminRoles = "Admin,SuperAdmin,CorporateAdmin";

        /// <summary>
        /// SuperAdmin and Farmer accounts are made only by a SuperAdmin: a SuperAdmin bypasses every
        /// rule, and Farmer accounts come from phone sign-up (FarmerAuthController), never from here.
        /// </summary>
        public static bool MayAssignRole(AppRole? callerRole, AppRole requested) =>
            requested is not (AppRole.SuperAdmin or AppRole.Farmer) || callerRole == AppRole.SuperAdmin;

        private AppRole? CallerRole =>
            Enum.TryParse<AppRole>(User.FindFirstValue(ClaimTypes.Role), out var r) ? r : null;

        [AllowAnonymous]
        [HttpGet("seed-default-user")]
        public async Task<IActionResult> SeedDefaultUser()
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var dto = new UserDto
            {
                Name = "Admin User",
                Email = "spic@apmiot.com",
                UserName = "admin",
                Password = "SpicAdmin)9*7^5$3@",
                Role = AppRole.Admin,
            };

            var result = await _userService.SeedDefaultUserAsync(dto);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(new
            {
                message = result.Message
            });
        }

        [Authorize(Roles = AccountAdminRoles)]
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] UserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!MayAssignRole(CallerRole, dto.Role))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only a SuperAdmin can create this kind of account." });

            var user = await _userService.CreateUserAsync(dto);

            return Ok(new
            {
                message = "User created successfully",
                user.Id,
                user.Name,
                user.Email,
                user.UserName,
                Role = user.Role.ToString(),
                Designation = user.Designation,
            });
        }

        [Authorize(Roles = AccountAdminRoles)]
        [HttpPut("update-user")]
        public async Task<IActionResult> UpdateUser([FromBody] UserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!MayAssignRole(CallerRole, dto.Role))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only a SuperAdmin can give an account this role." });

            var user = await _userService.UpdateUserAsync(dto);

            return Ok(new
            {
                message = "User updated successfully",
                user.Id,
                user.Name,
                user.Email,
                user.UserName,
                Role = user.Role.ToString(),
                Designation = user.Designation
            });
        }

        [Authorize(Roles = AccountAdminRoles)]
        [HttpDelete("delete-user/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var result = await _userService.DeleteUserAsync(userId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(new
            {
                message = result.Message
            });
        }

        [Authorize(Roles = AccountAdminRoles)]
        [HttpPatch("update-status/{userId}")]
        public async Task<IActionResult> UpdateStatus(string userId, [FromQuery] bool isActive)
        {
            var result = await _userService.UpdateStatusAsync(userId, isActive);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(new
            {
                message = result.Message
            });
        }
    }
}
