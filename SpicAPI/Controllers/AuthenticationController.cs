using Microsoft.AspNetCore.Mvc;
using SPIC.Core.DTOs;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly IUserService _userService;

        public AuthenticationController(IUserService userService)
        {
            _userService = userService;
        }
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid data");

            var user = await _userService.CreateUserAsync(dto);
            return Ok(user);
        }
    }
}
