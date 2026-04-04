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
        Task<ServiceResult> SeedDefaultUserAsync(SeedUserDto dto);
        Task<Userinfo> CreateUserAsync(CreateUserDto dto);
    }
}
