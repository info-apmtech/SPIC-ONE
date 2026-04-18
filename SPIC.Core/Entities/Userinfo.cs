using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPIC.Core.Entities
{
    public class UserInfo : IdentityUser
    {
        public required string? Name { get; set; }
        public int? DesignationId { get; set; }
        public Designation? Designation { get; set; }
        public AppRole Role { get; set; }
        public required string Password { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public required string UpdatedBy { get; set; }
        public bool IsActive { get; set; }
    }
    public class Designation
    {
        public int Id { get; set; }
        public required string Name { get; set; }
    }
    public enum AppRole
    {
        Admin, CorporateAdmin, Director, Avp,  SM, SMM, RM,RMO,MDO,MO,JMDO,Dealer,Formaer
    }
}
