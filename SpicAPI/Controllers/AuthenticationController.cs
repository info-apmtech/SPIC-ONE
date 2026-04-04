using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.WSIdentity;
using SPIC.Core.DTOs;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/Authentication")]
    public class AuthenticationController : Controller
    {
        private readonly TokenService _tokenService;
        private readonly IUserService _userService;

        public AuthenticationController(IUserService userService)
        {
            _userService = userService;
        }
        [HttpPost("seed-default-user")]
        public async Task<IActionResult> SeedDefaultUser([FromBody] SeedUserDto dto)
        {
            var result = await _userService.SeedDefaultUserAsync(dto);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
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
