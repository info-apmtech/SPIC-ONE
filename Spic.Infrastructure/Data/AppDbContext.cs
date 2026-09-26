using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

            // Guest House physical room allocation. The no-double-allocation overlap rule
            // (same physical RoomNumber cannot be shared by two active stays) is enforced by
            // a PostgreSQL EXCLUDE constraint on (GuestHouseRoomId, RoomNumber, date range)
            // that must be created with the required migration + btree_gist extension.
            // EF cannot express EXCLUDE constraints, so at runtime the invariant is also
            // re-verified inside a serializable transaction before allocation rows are saved.
            builder.Entity<GuestHouseRoomAllocation>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.HasOne(a => a.GuestHouseBooking)
                    .WithMany(b => b.RoomAllocations)
                    .HasForeignKey(a => a.GuestHouseBookingId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(a => a.GuestHouseRoom)
                    .WithMany(r => r.Allocations)
                    .HasForeignKey(a => a.GuestHouseRoomId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.GuestHouse)
                    .WithMany()
                    .HasForeignKey(a => a.GuestHouseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(a => a.RoomNumber).IsRequired().HasMaxLength(50);
                entity.HasIndex(a => a.GuestHouseBookingId);
                entity.HasIndex(a => new { a.GuestHouseRoomId, a.RoomNumber });
            });

        // =====================================================================
        //  Permission System Phase 1 - page catalog tables (ADDITIVE ONLY).
        //
        //  These two tables are a normalized compatibility mirror of the existing
        //  PagePermission enum + Designation.RoleAccess. Nothing at runtime reads
        //  them yet - login, LoginState, NavMenu, PageGuard, Designation.razor
        //  and the server-side SDWA checks all continue to use RoleAccess exactly
        //  as before. They exist only so a future Phase 2+ can become database
        //  driven without a destructive rewrite.
        // =====================================================================

        builder.Entity<ApplicationPage>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Key).IsRequired();
            entity.HasIndex(p => p.Key).IsUnique();
        });

        builder.Entity<DesignationPermission>(entity =>
        {
            entity.HasKey(dp => new { dp.DesignationId, dp.PageId });

            entity.HasOne(dp => dp.Designation)
                .WithMany()
                .HasForeignKey(dp => dp.DesignationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(dp => dp.Page)
                .WithMany()
                .HasForeignKey(dp => dp.PageId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(dp => dp.PageId);
        });

        // Seed the catalog from the existing PagePermission enum + its
        // PageModuleAttribute metadata, preserving every exact existing key and
        // the existing display/grouping behavior. Future enum changes surface
        // as model diffs in later EF migrations.
            builder.Entity<ApplicationPage>().HasData(
                Enum.GetValues<PagePermission>()
                    .Select((page, index) => new ApplicationPage
                    {
                        Id = index + 1,
                        Key = RoleAccessPermissions.KeyFor(page),
                        Name = PageDisplayName(page.ToString()),
                        Module = PageModuleName(page.ToString()),
                        SortOrder = index,
                        HasActions = true,
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedAt = staticDate,
                        UpdatedBy = "System",
                        UpdatedAt = staticDate
                    })
                    .ToArray());


        // ---------------------------------------------------------------- Digital Library
        builder.Entity<LibraryContent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Kind, x.Status });
            entity.HasIndex(x => x.PublishedAt);
        });

        builder.Entity<LibraryConversation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.UpdatedAt });
            entity.HasMany(x => x.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LibraryMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ConversationId);
        });

        // ---------------------------------------------------------------- SAS sample collection
        builder.Entity<SasFarmer>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Mobile);
            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => x.UserId);
        });

        builder.Entity<SampleCollection>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CollectedByUserId);
            entity.HasIndex(x => x.CollectionDate);
            entity.Property(x => x.TotalAmount).HasColumnType("numeric(12,2)");
            entity.HasOne(x => x.Consignment)
                .WithMany(c => c.Collections)
                .HasForeignKey(x => x.ConsignmentId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.Items)
                .WithOne(i => i.Collection)
                .HasForeignKey(i => i.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Payments)
                .WithOne(p => p.Collection)
                .HasForeignKey(p => p.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SampleItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CollectionId);
            entity.HasIndex(x => x.FarmerId);
            entity.Property(x => x.Amount).HasColumnType("numeric(12,2)");
            entity.HasOne(x => x.Farmer)
                .WithMany()
                .HasForeignKey(x => x.FarmerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Results)
                .WithOne(r => r.SampleItem)
                .HasForeignKey(r => r.SampleItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SamplePayment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CollectionId);
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.Amount).HasColumnType("numeric(12,2)");
        });

        builder.Entity<SampleConsignment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.DispatchedAt);
            entity.Property(x => x.PackageWeightKg).HasColumnType("numeric(8,2)");
            entity.HasMany(x => x.Photos)
                .WithOne(p => p.Consignment)
                .HasForeignKey(p => p.ConsignmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ConsignmentPhoto>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ConsignmentId);
        });

        builder.Entity<SasStatusEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CollectionId);
            entity.HasIndex(x => x.ConsignmentId);
        });

        builder.Entity<SampleLabResult>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.SampleItemId);
        });

        builder.Entity<SasSampleCharge>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.SampleType, x.Category }).IsUnique();
            entity.Property(x => x.AmountPerSample).HasColumnType("numeric(12,2)");
            entity.HasData(
                new SasSampleCharge { Id = 1, SampleType = SampleType.Soil, Category = SamplePaidCategory.Farmer, AmountPerSample = 100m, NoOfTests = 1, IsActive = true },
                new SasSampleCharge { Id = 2, SampleType = SampleType.Water, Category = SamplePaidCategory.Farmer, AmountPerSample = 100m, NoOfTests = 1, IsActive = true },
                new SasSampleCharge { Id = 3, SampleType = SampleType.SoilAndWater, Category = SamplePaidCategory.Farmer, AmountPerSample = 150m, NoOfTests = 2, IsActive = true },
                new SasSampleCharge { Id = 4, SampleType = SampleType.Soil, Category = SamplePaidCategory.Ngo, AmountPerSample = 150m, NoOfTests = 1, IsActive = true },
                new SasSampleCharge { Id = 5, SampleType = SampleType.Water, Category = SamplePaidCategory.Ngo, AmountPerSample = 150m, NoOfTests = 1, IsActive = true },
                new SasSampleCharge { Id = 6, SampleType = SampleType.SoilAndWater, Category = SamplePaidCategory.Ngo, AmountPerSample = 200m, NoOfTests = 2, IsActive = true });
        });

        builder.Entity<SasCourier>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasData(
                new SasCourier { Id = 1, Name = "Professional Courier", TrackingUrlTemplate = "https://www.tpcindia.com/Tracking2014.aspx?id={0}", IsActive = true },
                new SasCourier { Id = 2, Name = "Blue Dart Express", TrackingUrlTemplate = "https://www.bluedart.com/tracking?awb={0}", IsActive = true },
                new SasCourier { Id = 3, Name = "DTDC", TrackingUrlTemplate = "https://www.dtdc.in/tracking.asp?awb={0}", IsActive = true },
                new SasCourier { Id = 4, Name = "India Post", TrackingUrlTemplate = "https://www.indiapost.gov.in/_layouts/15/DOP.Portal.Tracking/TrackConsignment.aspx", IsActive = true });
        });

        // ---------------------------------------------------------------- Knowledge Community
        builder.Entity<CommunityPost>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.LastActivityAt);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.AuthorUserId);
            entity.HasIndex(x => x.Product);
            entity.HasMany(x => x.Replies)
                .WithOne(r => r.Post)
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Attachments)
                .WithOne(a => a.Post)
                .HasForeignKey(a => a.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CommunityPostReply>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.PostId, x.CreatedAt });
            entity.HasIndex(x => x.ParentReplyId);
        });

        builder.Entity<CommunityPostAttachment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.PostId);
            entity.HasIndex(x => x.ReplyId);
        });

        builder.Entity<CommunityReaction>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.TargetType, x.TargetId, x.Kind }).IsUnique();
            entity.HasIndex(x => new { x.TargetType, x.TargetId, x.Kind });
        });

        builder.Entity<CommunityProductMember>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.ProductName }).IsUnique();
        });

        // The IFMS automation keeps its own tables in its own database; see
        // IfmsDbContext. They are deliberately not reachable from here.
        }

        // Same prettification the existing Designation UI uses (Designation.razor
        // DisplayFor): "AnnualSales" -> "Annual Sales". Lower-case keys such as
        // "dealerreviewlist" are left as-is, matching that UI exactly.
        private static string PageDisplayName(string key)
        {
            if (string.Equals(key, nameof(PagePermission.QRScanner), StringComparison.OrdinalIgnoreCase))
                return "QR Scanner";

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < key.Length; i++)
            {
                if (i > 0 && char.IsUpper(key[i]) && !char.IsUpper(key[i - 1]))
                    sb.Append(' ');
                sb.Append(key[i]);
            }
            return sb.ToString();
        }

        // Module grouping from PageModuleAttribute (null = standalone page).
        private static string? PageModuleName(string key)
        {
            var field = typeof(PagePermission).GetField(key);
            return field?.GetCustomAttribute<PageModuleAttribute>()?.Module;
        }

        // User related
        public DbSet<Designation> Designations { get; set; }

        // Permission System Phase 1 (additive - not read by any runtime flow yet)
        public DbSet<ApplicationPage> Pages { get; set; }
        public DbSet<DesignationPermission> DesignationPermissions { get; set; }

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
		public DbSet<LogisticsApprovalHistory> LogisticsHistory { get; set; }

		//// Sub Dealer & Employee Beneficiary Master
		public DbSet<SubDealerBeneficiary> SubDealerBeneficiaries { get; set; }
		public DbSet<EmployeeBeneficiary> EmployeeBeneficiaries { get; set; }

		//// SpecialAdmin multi-location assignments
		public DbSet<SpecialAdminLocations> SpecialAdminLocations { get; set; }

		//// Welfare Scheme
		public DbSet<WelfareApplication> WelfareApplications { get; set; }
		public DbSet<WelfareApplicationDocument> WelfareApplicationDocuments { get; set; }
		public DbSet<WelfareApplicationApproval> WelfareApplicationApprovals { get; set; }
		public DbSet<WelfareApplicationActionLog> WelfareApplicationActionLogs { get; set; }

		//// Guest House Master Data
		public DbSet<GuestHouse> GuestHouses { get; set; }
		public DbSet<GuestHouseRoom> GuestHouseRooms { get; set; }
		public DbSet<GuestHouseImage> GuestHouseImages { get; set; }
		//public DbSet<GuestHouseRoomImage> GuestHouseRoomImages { get; set; }
		//public DbSet<GuestHouseRoomAmenity> GuestHouseRoomAmenities { get; set; }
		public DbSet<GuestHouseRoomAvailability> GuestHouseRoomAvailabilities { get; set; }
		public DbSet<GuestHouseBooking> GuestHouseBookings { get; set; }
		public DbSet<GuestHouseBookingGuest> GuestHouseBookingGuests { get; set; }
		//public DbSet<GuestHouseBookingDocument> GuestHouseBookingDocuments { get; set; }
		public DbSet<GuestHouseBookingPayment> GuestHouseBookingPayments { get; set; }
		//public DbSet<GuestHouseCancellationPolicy> GuestHouseCancellationPolicies { get; set; }
		//public DbSet<GuestHouseBookingCancellation> GuestHouseBookingCancellations { get; set; }
		//public DbSet<GuestHouseBookingRefund> GuestHouseBookingRefunds { get; set; }
		public DbSet<GuestHouseBill> GuestHouseBills { get; set; }
		public DbSet<GuestHouseBillLineItem> GuestHouseBillLineItems { get; set; }
		public DbSet<GuestHouseRoomAllocation> GuestHouseRoomAllocations { get; set; }

		//// SDWA Company Details Master
		public DbSet<SdwaCompany> SdwaCompanies { get; set; }
		public DbSet<SdwaCompanyGuestHouse> SdwaCompanyGuestHouses { get; set; }

		//// Contact Us
		//public DbSet<ContactUsMessage> ContactUsMessages { get; set; }

		//// Digital Library
		public DbSet<LibraryContent> LibraryContents { get; set; }
		public DbSet<LibraryConversation> LibraryConversations { get; set; }
		public DbSet<LibraryMessage> LibraryMessages { get; set; }

		//// SAS: sample collection
		public DbSet<SasFarmer> SasFarmers { get; set; }
		public DbSet<SampleCollection> SampleCollections { get; set; }
		public DbSet<SampleItem> SampleItems { get; set; }
		public DbSet<SamplePayment> SamplePayments { get; set; }
		public DbSet<SampleConsignment> SampleConsignments { get; set; }
		public DbSet<ConsignmentPhoto> ConsignmentPhotos { get; set; }
		public DbSet<SasStatusEvent> SasStatusEvents { get; set; }
		public DbSet<SampleLabResult> SampleLabResults { get; set; }
		public DbSet<SasSampleCharge> SasSampleCharges { get; set; }
		public DbSet<SasCourier> SasCouriers { get; set; }

		//// Knowledge Community
		public DbSet<CommunityPost> CommunityPosts { get; set; }
		public DbSet<CommunityPostReply> CommunityPostReplies { get; set; }
		public DbSet<CommunityPostAttachment> CommunityPostAttachments { get; set; }
		public DbSet<CommunityReaction> CommunityReactions { get; set; }
		public DbSet<CommunityProductMember> CommunityProductMembers { get; set; }

        // The IFMS automation keeps its own tables in its own database; see
        // IfmsDbContext. They are deliberately not reachable from here.

        //Budget
        public DbSet<ProgramType> ProgramTypes { get; set; }
        public DbSet<ProgramMaster> ProgramMasters { get; set; }
        public DbSet<BudgetProgram> BudgetPrograms { get; set; }
    }
}
