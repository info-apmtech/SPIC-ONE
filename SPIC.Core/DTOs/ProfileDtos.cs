using SPIC.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace SPIC.Core.DTOs
{
    /// <summary>
    /// Self-service profile update for the currently logged-in user. Covers all 12
    /// fields rendered on the Employee branch of the Profile page.
    ///
    /// Ownership is resolved on the server from the authenticated claims, so the
    /// contract deliberately carries no UserId, no EmployeeInformation.Id and no
    /// Employeelogin.Id. Every Identity/audit field (CreatedBy/At,
    /// UpdatedBy/At, Password, PasswordHash, SecurityStamp, ConcurrencyStamp,
    /// NormalizedEmail, NormalizedUserName) stays server-managed and is not
    /// bindable.
    ///
    /// Type notes, taken from the entities:
    ///   EmployeeInformation.EmployeeCode/Name/Email/PersonalPhoneNumber/
    ///     OfficialPhoneNumber  -> string
    ///   Employeelogin.Role      -> AppRole   (serialized as its numeric value;
    ///                              the project registers AddControllers()
    ///                              without JsonStringEnumConverter)
    ///   UserInfo.DesignationId  -> int?      (0 is the existing "no designation"
    ///                              convention used by EmployeeRegistration)
    ///   Employeelogin.ZoneId / StateId / RegionId / HeadquartersId -> int
    ///                              (0 is the existing "not narrowed" convention)
    ///   Employeelogin.IsActive  -> bool
    /// </summary>
    public class ProfileUpdateDto
    {
        [Required(ErrorMessage = "Employee Code is required.")]
        [StringLength(50, ErrorMessage = "Employee Code cannot exceed 50 characters.")]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Personal phone number is required.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Personal phone number must be 10 digits.")]
        public string PersonalPhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Official phone number is required.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Official phone number must be 10 digits.")]
        public string OfficialPhoneNumber { get; set; } = string.Empty;

        /// <summary>Employeelogin.Role. The server range-checks this against
        /// <see cref="AppRole"/> before writing, so an undefined numeric value
        /// cannot be smuggled in.</summary>
        public AppRole Role { get; set; }

        /// <summary>UserInfo.DesignationId. 0 clears the designation.</summary>
        public int DesignationId { get; set; }

        /// <summary>Employeelogin.ZoneId. 0 leaves the zone unset.</summary>
        public int ZoneId { get; set; }

        /// <summary>Employeelogin.StateId. 0 leaves the state unset.</summary>
        public int StateId { get; set; }

        /// <summary>Employeelogin.RegionId. 0 leaves the region unset.</summary>
        public int RegionId { get; set; }

        /// <summary>Employeelogin.HeadquartersId. 0 leaves the headquarter unset.</summary>
        public int HeadquartersId { get; set; }

        /// <summary>Employeelogin.IsActive (Status: Active / Inactive).</summary>
        public bool IsActive { get; set; } = true;

        public List<EmployeeRegistration.SpecialAdminLocationItem>? SpecialAdminLocations { get; set; }
    }

    /// <summary>
    /// The caller's own profile, self-scoped. Carries ids rather than lookup text
    /// for Designation/Zone/State/Region/Headquarter so the edit form can bind the
    /// dropdowns directly to the current selection, exactly as the entities store
    /// them. The page resolves the display names from the same master APIs.
    /// </summary>
    public class ProfileDto
    {
        /// <summary>Identity of the Employeelogin row the server resolved through
        /// the shared selection rule. The Profile page must display and edit this
        /// exact row rather than picking one itself, because GET and PUT resolve it
        /// the same way.</summary>
        public int LoginId { get; set; }
        public int EmployeeInformationId { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PersonalPhoneNumber { get; set; } = string.Empty;
        public string OfficialPhoneNumber { get; set; } = string.Empty;

        public AppRole Role { get; set; }
        public int DesignationId { get; set; }
        public int ZoneId { get; set; }
        public int StateId { get; set; }
        public int RegionId { get; set; }
        public int HeadquartersId { get; set; }
        public bool IsActive { get; set; } = true;

        public List<EmployeeRegistration.SpecialAdminLocationItem>? SpecialAdminLocations { get; set; }
    }
}
