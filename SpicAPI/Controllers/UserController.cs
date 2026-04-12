using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

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

        //[Authorize(Roles = "Admin")]
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] UserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

        [HttpPut("update-user")]
        public async Task<IActionResult> UpdateUser([FromBody] UserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
