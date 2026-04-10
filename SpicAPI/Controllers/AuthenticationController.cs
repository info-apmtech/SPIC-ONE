using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static SPIC.Core.DTOs.AdminViewModel;

namespace SpicAPI.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IUserService _userService;
        private readonly UserManager<UserInfo> _userManager;
        private readonly SignInManager<UserInfo> _signInManager;

        public AuthenticationController(IUserService userService , UserManager<UserInfo> userManager, SignInManager<UserInfo> signInManager, IConfiguration config)
        {
            _userService = userService;
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
        }
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid data");

            var user = await _userService.CreateUserAsync(dto);
            return Ok(user);
        }



        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Name);
            if (user == null)
                return Unauthorized("Invalid username or password");

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
                return Unauthorized("Invalid username or password");

            var token = GenerateJwtToken(user.Id);

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

            //return Ok(new
            //{
            //    token = $"Bearer {token}",
            //    user = user,
            //    validtill = jwtToken?.ValidTo
            //});
            var responseData = new LoginResponseModel()
            {
                Token = token,
                User = user,
                Expiration = jwtToken?.ValidTo ?? DateTime.UtcNow.AddHours(1)
            };

            return Ok(responseData);
        }
        private string GenerateJwtToken(string username)
        {
            var jwtConfig = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtConfig["Issuer"],
                audience: jwtConfig["Audience"],
                claims: new[] { new Claim(ClaimTypes.Name, username) },
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtConfig["ExpireMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
