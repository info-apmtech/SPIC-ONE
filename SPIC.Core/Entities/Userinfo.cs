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
        public string? RoleAccess { get; set; }
        public bool IsActive { get; set; } = true;
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
    public enum AppRole
    {
        Admin, CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO, Dealer, Farmer
    }
    public enum PagePermission
    {
        [PageModule("DealerRegistration")] Dashboard,
        [PageModule("DealerRegistration")] Register,
        [PageModule("DealerRegistration")] Experience,
        [PageModule("DealerRegistration")] AnnualSales,
        [PageModule("DealerRegistration")] Warehouse,
        [PageModule("DealerRegistration")] MarketDetails,
        [PageModule("DealerRegistration")] Companies,
        [PageModule("DealerRegistration")] Proprietor,
        [PageModule("DealerRegistration")] SalesPlaning,
        [PageModule("DealerRegistration")] Investment,
        [PageModule("DealerRegistration")] CreditLimit,
        [PageModule("DealerRegistration")] CreditLimitForGreenStar,
        [PageModule("DealerRegistration")] Enclosures,
        [PageModule("DealerRegistration")] FinalSubmission,
        [PageModule("DealerRegistration")] DealershipPDF,
        [PageModule("DealerRegistration")] SavedDealerReview,
        Designation, 
        [PageModule("Employee Management")] EmployeeManagement, 
        [PageModule("Employee Management")] EmployeeRegistration, 
        dealerreviewlist, CreditLimitSales, LocationMaster, Agriculture, Logistics, Financial, Relationship, Schemes,
        CompanySales, SalesReport, AgeingReport, Acknowledgement, LiquidationCycle, BudgetSubmissions, WelfareSchemes, Purchases, Rewards, CropAdvice, YieldPrediction, DiseaseDetection,
        Community, Notifications, Profile, CSR1Create, CSR1Management,

    }
}
