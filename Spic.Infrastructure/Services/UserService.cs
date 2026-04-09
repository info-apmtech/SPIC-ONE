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

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Userinfo> CreateUserAsync(CreateUserDto dto)
        {
            var user = new Userinfo
            {
                Name = dto.Name,
                Email = dto.Email,
               PasswordHash = dto.Password
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<ServiceResult> SeedDefaultUserAsync(SeedUserDto dto)
        {
            var exists = await _context.Users
                .AnyAsync(x => x.Email == dto.Email);

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
                PasswordHash = dto.Password,
               // SiteCode = dto.SiteCode
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                Success = true,
                Message = "Default user created successfully."
            };
        }
    }
}
