using SPIC.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
    public class AdminViewModel

    {
        public class LoginViewModel
        {
            //public string Name { get; set; } = "";
            public required string UserName { get; set; }
            //public string Email { get; set; } = "";
            public string Password { get; set; } = "";
        }


        public class LoginResponseModel
        {
            public required string Token { get; set; }
            public required UserInfo User { get; set; }
            public DateTime Expiration { get; set; }
            public string? RoleAccess { get; set; }
            // Name of the user's assigned Designation (e.g. "SDWA"), resolved from
            // UserInfo.DesignationId. Separate from AppRole — a user's AppRole
            // never changes based on their Designation.
            public string? DesignationName { get; set; }

        }
    }







}
