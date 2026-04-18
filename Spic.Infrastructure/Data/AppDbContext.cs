using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SPIC.Core.Entities.EmployeeRegistration;


namespace Spic.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<UserInfo>
	{
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserInfo>().ToTable("AppUsers");
            builder.Entity<IdentityRole>().ToTable("AppRoles"); // optional, rename roles too
            builder.Entity<IdentityUserRole<string>>().ToTable("AppUserRoles");
            builder.Entity<IdentityUserClaim<string>>().ToTable("AppUserClaims");
            builder.Entity<IdentityUserLogin<string>>().ToTable("AppUserLogins");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("AppRoleClaims");
            builder.Entity<IdentityUserToken<string>>().ToTable("AppUserTokens");

            // restrict delete on Designation
            builder.Entity<UserInfo>()
                .HasOne(u => u.Designation)
                .WithMany()
                .HasForeignKey(u => u.DesignationId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // User related
        public DbSet<Designation> Designations { get; set; }

        // Location
        public DbSet<Zone> Zones { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<SubDistrict> SubDistricts { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<Headquarter> Headquarters { get; set; }

        // Settings
        public DbSet<Crop> Crops { get; set; }
        public DbSet<Competitor> Competitors { get; set; }
        public DbSet<Sector> Sectors { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<RackPoint> RackPoints { get; set; }
        public DbSet<Port> Ports { get; set; }
        public DbSet<Bank> Banks { get; set; }
        public DbSet<FinancialYear> FinancialYears { get; set; }
        public DbSet<Relationship> Relationships { get; set; }
        public DbSet<DealerRegistration> DealerRegistrations { get; set; }

        // Dealer Registration Sub-Entities
        public DbSet<DealerExperience> DealerExperiences { get; set; }
        public DbSet<AnnualSaleDataLastFYofDealerRegistration> AnnualSaleDataLastFY { get; set; }
        public DbSet<DealerWarehouseFacilities> DealerWarehouseFacilities { get; set; }
        public DbSet<DealerRailFacilities> DealerRailFacilities { get; set; }
        public DbSet<DealerPortFacilities> DealerPortFacilities { get; set; }
        public DbSet<DealerMarketDetail> DealerMarketDetails { get; set; }
        public DbSet<DealerCompaniesOperatingInArea> DealerCompaniesOperatingInAreas { get; set; }
        public DbSet<DealerOwnershipInfo> DealerOwnershipInfos { get; set; }
        public DbSet<PartnerFamilyDetails> PartnerFamilyDetails { get; set; }
        public DbSet<PartnerOccupation> PartnerOccupations { get; set; }
        public DbSet<SalesPlanningInDealerRegistration> SalesPlannings { get; set; }
        public DbSet<DealerAssetBank> DealerAssetBanks { get; set; }
        public DbSet<DealerAssetLand> DealerAssetLands { get; set; }
        public DbSet<DealerAssetBuilding> DealerAssetBuildings { get; set; }
        public DbSet<DealerLoanLiabilities> DealerLoanLiabilities { get; set; }
        public DbSet<DealerCreditLimitProposal> DealerCreditLimitProposals { get; set; }
        public DbSet<DealerCreditLimitSalesPerformance> DealerCreditLimitSalesPerformances { get; set; }
        public DbSet<DealerRegistrationDocuments> DealerRegistrationDocuments { get; set; }
        public DbSet<DealerApprovalHistory> DealerApprovalHistories { get; set; }
        public DbSet<EmployeeInformation> EmployeeInformation { get; set; }
        public DbSet<Employeelogin> Employeelogins { get; set; }
    }
}
