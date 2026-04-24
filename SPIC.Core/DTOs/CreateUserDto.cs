using SPIC.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPIC.Core.DTOs
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = "";
        public required string UserName { get; set; }
        public string? Email { get; set; }
        public string Password { get; set; } = "";
        public AppRole Role { get; set; }
        public int? DesignationId { get; set; }
        public bool IsActive { get; set; }
    }
    public class ServiceResult
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public string? JsonData { get; set; }
    }





}
