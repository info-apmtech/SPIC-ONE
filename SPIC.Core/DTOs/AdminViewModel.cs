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
			public string UserName { get; set; }
			//public string Email { get; set; } = "";
            public string Password { get; set; } = "";
        }


        public class LoginResponseModel
        {
            public string Token { get; set; }
            public Userinfo User { get; set; }
            public DateTime Expiration { get; set; }

        }
    }


 




}
