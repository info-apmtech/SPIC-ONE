using SPIC.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPIC.Core.DTOs
{
    public class CreateUserDto
    {
        public string Name { get; set; } = "";
		public string UserName { get; set; }
		public string Email { get; set; } = "";
        public string Password { get; set; } = "";
		public AppRole Role { get; set; }
	}
    public class SeedUserDto
    {
       // public string SiteCode { get; set; } = string.Empty;
        public string Name { get; set; } = "Admin";
        public string UserName { get; set; } 
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
		public AppRole Role { get; set; }
	}
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
