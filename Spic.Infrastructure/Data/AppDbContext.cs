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

            // Seed LyingWithMaster
            var staticDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            builder.Entity<LyingWithMaster>().HasData(
                new LyingWithMaster { Id = 1, Name = "Retailer", IsActive = true, CreatedAt = staticDate, UpdatedAt = staticDate, UpdatedBy = "System" },
                new LyingWithMaster { Id = 2, Name = "Wholesaler", IsActive = true, CreatedAt = staticDate, UpdatedAt = staticDate, UpdatedBy = "System" },
                new LyingWithMaster { Id = 3, Name = "Rake Point", IsActive = true, CreatedAt = staticDate, UpdatedAt = staticDate, UpdatedBy = "System" },
                new LyingWithMaster { Id = 4, Name = "Warehouse", IsActive = true, CreatedAt = staticDate, UpdatedAt = staticDate, UpdatedBy = "System" }
            );
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
        public DbSet<ProductGroup> ProductGroups { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        //public DbSet<CandFWarehouse> CandFWarehouses { get; set; }
        public DbSet<LyingWithMaster> LyingWithMasters { get; set; }

        public DbSet<SalesWholesaler> SalesWholesalers { get; set; }//  IFMS Wholesaler sales
		public DbSet<SalesAndReceipt> SalesAndReceipts { get; set; }//  IFMS sales and receipt
		public DbSet<SalesCompanySale> SalesCompanySales { get; set; }//  IFMS Company sales
		public DbSet<DptReport> DptReports { get; set; }//  IFMS DPT Report
		public DbSet<WholesalerStockAsOnToday> WholesalerStockAsOnTodays { get; set; }//  IFMS Wholesaler stock as on today
		public DbSet<RackPoint> RackPoints { get; set; }
        public DbSet<Port> Ports { get; set; }
        public DbSet<Bank> Banks { get; set; }
        public DbSet<FinancialYear> FinancialYears { get; set; }
        public DbSet<Relationship> Relationships { get; set; }
        public DbSet<DealerRegistration> DealerRegistrations { get; set; }
        public DbSet<Plant> Plants { get; set; }//  IFMS Plant Master
		public DbSet<DealerType> DealerTypes { get; set; }//  IFMS Dealer Type Master
		public DbSet<Status> Statuses { get; set; }//  IFMS Status Master
		public DbSet<IfmsDealer> IfmsDealers { get; set; }//  IFMS Dealer Master
		public DbSet<Company> Companies { get; set; } 
        public DbSet<DealershipNature> DealershipNatures { get; set; }//  IFMS DealershipNatures 
		public DbSet<TxnType> TxnTypes { get; set; }//  IFMS TxnTypes
		public DbSet<AckThrough> AckThroughs { get; set; }//  IFMS AckThroughs
        public DbSet<StateGlobalStockReconciliation> StateGlobalStockReconciliations { get; set; }//  IFMS State Global Stock Reconciliation
		public DbSet<WarehouseDistrictGlobalStockReconciliation> WarehouseDistrictGlobalStockReconciliations { get; set; }//  IFMS Warehouse District Global Stock Reconciliation

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
        public DbSet<CreditLimitHistory> CreditLimitHistories { get; set; }
        public DbSet<DealerRegistrationDocuments> DealerRegistrationDocuments { get; set; }
        public DbSet<DealerApprovalHistory> DealerApprovalHistories { get; set; }
        public DbSet<EmployeeInformation> EmployeeInformation { get; set; }
        public DbSet<Employeelogin> Employeelogins { get; set; }
        public DbSet<DealerCreditLimitSales> DealerCreditLimitSalesData { get; set; }
        public DbSet<IfmsProduct> IfmsProducts { get; set; }
		public DbSet<PVTMaster> PVTMasters { get; set; }
		public DbSet<RakePointMaster> RakePointMasters { get; set; }
        public DbSet<SubDealerRegistration> SubDealerRegistrations { get; set; }

        //// Welfare Scheme
        //public DbSet<WelfareScheme> WelfareSchemes { get; set; }
        //public DbSet<WelfareSchemeDocumentRequirement> WelfareSchemeDocumentRequirements { get; set; }
        //public DbSet<WelfareApplication> WelfareApplications { get; set; }
        //public DbSet<WelfareApplicationDocument> WelfareApplicationDocuments { get; set; }
        //public DbSet<WelfareApplicationApproval> WelfareApplicationApprovals { get; set; }

        //// Guest House Booking
        //public DbSet<GuestHouse> GuestHouses { get; set; }
        //public DbSet<GuestHouseImage> GuestHouseImages { get; set; }
        //public DbSet<GuestHouseRoom> GuestHouseRooms { get; set; }
        //public DbSet<GuestHouseRoomImage> GuestHouseRoomImages { get; set; }
        //public DbSet<GuestHouseRoomAmenity> GuestHouseRoomAmenities { get; set; }
        //public DbSet<GuestHouseRoomAvailability> GuestHouseRoomAvailabilities { get; set; }
        //public DbSet<GuestHouseBooking> GuestHouseBookings { get; set; }
        //public DbSet<GuestHouseBookingGuest> GuestHouseBookingGuests { get; set; }
        //public DbSet<GuestHouseBookingDocument> GuestHouseBookingDocuments { get; set; }
        //public DbSet<GuestHouseBookingPayment> GuestHouseBookingPayments { get; set; }
        //public DbSet<GuestHouseCancellationPolicy> GuestHouseCancellationPolicies { get; set; }
        //public DbSet<GuestHouseBookingCancellation> GuestHouseBookingCancellations { get; set; }
        //public DbSet<GuestHouseBookingRefund> GuestHouseBookingRefunds { get; set; }

        //// Contact Us
        //public DbSet<ContactUsMessage> ContactUsMessages { get; set; }

    }
}
