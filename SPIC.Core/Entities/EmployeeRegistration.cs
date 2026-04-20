using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SPIC.Core.Entities
{
    public class EmployeeRegistration
    {
        public class EmployeeInformation
        {
            [Key]
            public int Id { get; set; } 
            [Display(Name = "Employee ID")]
            public string EmployeeCode { get; set; }
            [Required]
            [Display(Name = "Employee Name")]
            public string Name { get; set; }
            [Display(Name = "Personal Phone Number")]
            public string PersonalPhoneNumber { get; set; }
            [Display(Name = "Official Phone Number")]
            public string OfficialPhoneNumber { get; set; }
            [EmailAddress]
            [Display(Name = "Email ID")]
            public string Email { get; set; }
            public string CreatedBy { get; set; }
            public string? UpdatedBy { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.Now;
			public DateTime UpdatedAt { get; set; } = DateTime.Now;

        }
        public class Employeelogin
        {
            [Key]
            public int Id { get; set; } /*= Guid.NewGuid().ToString();*/
			//[Display(Name = "Employee")]
			//public string EmployeeId { get; set; }
			[Required]
			public string UserId { get; set; }
			[Required]
            [Display(Name = "Role")]
            public AppRole Role { get; set; }
            // 🔹 Role Specification
            [Display(Name = "Zone")]
            public int ZoneId { get; set; }
            [Display(Name = "State")]
            public int StateId { get; set; }
            [Display(Name = "Region")]
            public int RegionId { get; set; }
            [Display(Name = "Headquarters")]
            public int HeadquartersId { get; set; }
            //[Required]
            //[Display(Name = "User Name")]
            //public string UserName { get; set; }
            //[Required]
            //[Display(Name = "Password")]
            //public string Password { get; set; } 
            // 🔹 Status
            [Display(Name = "Status")]
            public bool IsActive { get; set; } = true;
            public int EmployeeInformationID { get; set; }

        }
    }
	public class EmployeeLoginCreateRequest
	{
		public int EmployeeInformationID { get; set; }
		public string UserName { get; set; } = "";
		public string Password { get; set; } = "";
		public string? Email { get; set; }
		public string? Name { get; set; }
		public int? DesignationId { get; set; }
		public AppRole Role { get; set; }
		public int ZoneId { get; set; }
		public int StateId { get; set; }
		public int RegionId { get; set; }
		public int HeadquartersId { get; set; }
		public bool IsActive { get; set; } = true;
		public string? CreatedBy { get; set; }
		public string? UpdatedBy { get; set; }
	}
}
