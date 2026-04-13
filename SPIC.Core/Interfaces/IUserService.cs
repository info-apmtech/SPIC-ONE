using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPIC.Core.Interfaces
{
    public interface IUserService
    {
        Task<ServiceResult> SeedDefaultUserAsync(UserDto dto);
        Task<UserInfo> CreateUserAsync(UserDto dto);
        Task<UserInfo> UpdateUserAsync(UserDto dto);
        Task<ServiceResult> UpdateStatusAsync(string userId, bool isActive);
        Task<ServiceResult> DeleteUserAsync(string userId);
    }
}
