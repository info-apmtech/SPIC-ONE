using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPIC.Core.Entities
{
    public class Userinfo : IdentityUser
    {
        public string? Name { get; set; }
        [PersonalData]
       public DateTime Createddate { get; set; } = DateTime.Now;
    }
}
