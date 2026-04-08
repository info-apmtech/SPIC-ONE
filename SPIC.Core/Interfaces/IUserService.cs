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
        Task<Userinfo> CreateUserAsync(CreateUserDto dto);
    }
}
