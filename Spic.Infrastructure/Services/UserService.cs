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
using Microsoft.AspNetCore.Http;

namespace Spic.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<UserInfo> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly RoleManager<IdentityRole> _roleManager;

		public UserService(AppDbContext context, UserManager<UserInfo> userManager, RoleManager<IdentityRole> roleManager, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
			_roleManager = roleManager;
		}

        public async Task<UserInfo> CreateUserAsync(UserDto dto)
        {
            var exists = await _userManager.FindByIdAsync(dto.Id);
            //get logged in current user id
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User is not logged in.");
            var createdBy = currentUserId;
            if (exists != null)
            {
                //throw new Exception("User already exists.");
                createdBy = exists.CreatedBy ?? createdBy; // If user already exists, use the existing CreatedBy value
            }

            var user = new UserInfo
            {
                Name = dto.Name,
                Email = dto.Email,
                UserName = dto.UserName,
                Password = dto.Password,
                Role = dto.Role,
                DesignationId = dto.DesignationId ?? 0,
                CreatedAt = DateTime.Now,
                CreatedBy = createdBy,
                UpdatedBy = currentUserId
            };

			var result = await _userManager.CreateAsync(user, dto.Password);

			if (!result.Succeeded)
			{
				throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
			}

			await EnsureRoleAndAssignAsync(user, dto.Role);

			return user;
		}

		public async Task<ServiceResult> SeedDefaultUserAsync(UserDto dto)
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

			var user = new UserInfo
			{
				Name = dto.Name,
				Email = dto.Email,
				UserName = dto.UserName,
				Password = dto.Password,
				Role = dto.Role,
				CreatedAt = DateTime.Now,
				CreatedBy = "Default",
				UpdatedBy = "Default",
				IsActive = true
			};

			var createUserResult = await _userManager.CreateAsync(user, dto.Password);

			if (!createUserResult.Succeeded)
			{
				return new ServiceResult
				{
					Success = false,
					Message = string.Join(", ", createUserResult.Errors.Select(e => e.Description))
				};
			}

			await EnsureRoleAndAssignAsync(user, dto.Role);

			return new ServiceResult
			{
				Success = true,
				Message = "Default user created successfully."
			};
		}

		public async Task<UserInfo> UpdateUserAsync(UserDto dto)
		{
			var currentUserId = _httpContextAccessor.HttpContext?.User?
				.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
				?? throw new UnauthorizedAccessException("User is not logged in.");

			var user = await _userManager.FindByIdAsync(dto.Id)
				?? throw new Exception("User not found.");

			// Update user fields
			user.Name = dto.Name;
			user.UserName = dto.UserName ?? user.UserName;
			user.Email = dto.Email ?? user.Email;
			user.Role = dto.Role;
			user.DesignationId = dto.DesignationId ?? user.DesignationId;
			user.UpdatedAt = DateTime.Now;
			user.UpdatedBy = currentUserId;

			var result = await _userManager.UpdateAsync(user);

			if (!result.Succeeded)
				throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

			// Update role in AppRoles / AppUserRoles
			var oldRoles = await _userManager.GetRolesAsync(user);

			if (oldRoles.Any())
			{
				var removeRoleResult = await _userManager.RemoveFromRolesAsync(user, oldRoles);

				if (!removeRoleResult.Succeeded)
					throw new Exception(string.Join(", ", removeRoleResult.Errors.Select(e => e.Description)));
			}

			await EnsureRoleAndAssignAsync(user, dto.Role);

			// Change password only if provided and changed
			if (!string.IsNullOrWhiteSpace(dto.Password) && dto.Password != user.Password)
			{
				var token = await _userManager.GeneratePasswordResetTokenAsync(user);

				var passResult = await _userManager.ResetPasswordAsync(user, token, dto.Password);

				if (!passResult.Succeeded)
					throw new Exception(string.Join(", ", passResult.Errors.Select(e => e.Description)));

				user.Password = dto.Password;
				user.UpdatedAt = DateTime.Now;
				user.UpdatedBy = currentUserId;

				var passwordUpdateResult = await _userManager.UpdateAsync(user);

				if (!passwordUpdateResult.Succeeded)
					throw new Exception(string.Join(", ", passwordUpdateResult.Errors.Select(e => e.Description)));
			}

			return user;
		}

		public async Task<ServiceResult> UpdateStatusAsync(string userId, bool isActive)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User is not logged in.");

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.Now;
            user.UpdatedBy = currentUserId;

            var result = await _userManager.UpdateAsync(user);

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
                Message = isActive
                    ? "User activated successfully."
                    : "User deactivated successfully."
            };
        }

        public async Task<ServiceResult> DeleteUserAsync(string userId)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User is not logged in.");

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            if (user.Id == currentUserId)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "You cannot delete your own account."
                };
            }

            // Hard delete — permanently removes the user
            var result = await _userManager.DeleteAsync(user);

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
                Message = "User deleted permanently."
            };
        }
		private async Task EnsureRoleAndAssignAsync(UserInfo user, AppRole role)
		{
			var roleName = role.ToString();

			if (!await _roleManager.RoleExistsAsync(roleName))
			{
				var roleResult = await _roleManager.CreateAsync(new IdentityRole(roleName));

				if (!roleResult.Succeeded)
					throw new Exception(string.Join(", ", roleResult.Errors.Select(e => e.Description)));
			}

			if (!await _userManager.IsInRoleAsync(user, roleName))
			{
				var addRoleResult = await _userManager.AddToRoleAsync(user, roleName);

				if (!addRoleResult.Succeeded)
					throw new Exception(string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
			}
		}
	}
}
