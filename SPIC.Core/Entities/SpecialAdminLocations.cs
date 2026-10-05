using System;

namespace SPIC.Core.Entities
{
    // Multi-location assignments for a SpecialAdmin. Unlike the single
    // Zone/State/Region/Headquarters stored on Employeelogin, one SpecialAdmin
    // may be assigned to many states (each optionally narrowed to regions/HQs).
    public class SpecialAdminLocations
    {
        public int Id { get; set; }

        // Reference to the employee (EmployeeInformation) this mapping belongs to.
        public int EmployeeInformationID { get; set; }

        public int StateId { get; set; }
        public int RegionId { get; set; }
        public int HeadquarterId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public string? UpdatedBy { get; set; }
    }
}
