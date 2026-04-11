using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spic.Infrastructure.Services
{
	public class UserService : IUserService
	{
		private readonly AppDbContext _context;
		private readonly UserManager<Userinfo> _userManager;
		public UserService(AppDbContext context, UserManager<Userinfo> userManager)
		{
			_context = context;
			_userManager = userManager;
		}

		public async Task<Userinfo> CreateUserAsync(CreateUserDto dto)
		{
			var exists = await _userManager.FindByEmailAsync(dto.Email);
			if (exists != null)
			{
				throw new Exception("User already exists.");
			}

			var user = new Userinfo
			{
				Name = dto.Name,
				Email = dto.Email,
				UserName = dto.UserName,
				Password = dto.Password,
				Role = dto.Role
			};

			var result = await _userManager.CreateAsync(user, dto.Password);

			if (!result.Succeeded)
			{
				throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
			}

			return user;
		}

		public async Task<ServiceResult> SeedDefaultUserAsync(SeedUserDto dto)
		{
			var exists = await _context.Users.AnyAsync(x => x.Email == dto.Email);

			if (exists)
			{
				return new ServiceResult
				{
					Success = false,
					Message = "User already exists for this site."
				};
			}

			var user = new Userinfo
			{
				Name = dto.Name,
				Email = dto.Email,
				UserName = dto.UserName,
				Password = dto.Password,
				Role = dto.Role
			};

			var result = await _userManager.CreateAsync(user, dto.Password);

			if (!result.Succeeded)
			{
				return new ServiceResult
				{
					Success = false,
					Message = string.Join(", ", result.Errors.Select(e => e.Description))
				};
			}

			return new ServiceResult
			{
				Success = true,
				Message = "Default user created successfully."
			};
		}
	}
}
