using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v14dealerUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnnualSaleDataLastFY",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    OwnRetailsSale = table.Column<decimal>(type: "numeric", nullable: false),
                    SaleToDealer = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnualSaleDataLastFY", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Buildings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    BuildingName = table.Column<string>(type: "text", nullable: true),
                    PropertyValue = table.Column<decimal>(type: "numeric", nullable: false),
                    SurveyNumber = table.Column<string>(type: "text", nullable: true),
                    LandSize = table.Column<decimal>(type: "numeric", nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    UploadedBuildingDocumentPath = table.Column<string>(type: "text", nullable: true),
                    UploadedECDocumentPath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompaniesOperatingInAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    CompaniesOperating = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompaniesOperatingInAreas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditLimitProposals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    FY1 = table.Column<int>(type: "integer", nullable: false),
                    FY2 = table.Column<int>(type: "integer", nullable: false),
                    FY3 = table.Column<int>(type: "integer", nullable: false),
                    Q1Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q2Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q3Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q4Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q5Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q6Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q7Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q8Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q9Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q10Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q11Mark = table.Column<double>(type: "double precision", nullable: false),
                    AdditionalCreditLimit = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditLimitProposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditLimitSalesPerformances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreditLimitId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    FY1Qty = table.Column<decimal>(type: "numeric", nullable: false),
                    FY1Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    FY2Qty = table.Column<decimal>(type: "numeric", nullable: false),
                    FY2Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    FY3Qty = table.Column<decimal>(type: "numeric", nullable: false),
                    FY3Amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditLimitSalesPerformances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerAssetBanks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    BankId = table.Column<int>(type: "integer", nullable: false),
                    BankBranch = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<decimal>(type: "numeric", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    FileUploadPath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerAssetBanks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerAssetLands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    LandName = table.Column<string>(type: "text", nullable: true),
                    SurvayNumber = table.Column<string>(type: "text", nullable: true),
                    LandSize = table.Column<decimal>(type: "numeric", nullable: false),
                    PropertyValue = table.Column<decimal>(type: "numeric", nullable: false),
                    UploadedLandDocumentPath = table.Column<string>(type: "text", nullable: true),
                    UploadedECDocumentPath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerAssetLands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerInfrastructures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    OwnGodownCapacity = table.Column<decimal>(type: "numeric", nullable: false),
                    RentGodownCapacity = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerInfrastructures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerRegistrationDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    SpecimanFilePath = table.Column<string>(type: "text", nullable: false),
                    BankGauranteeFilePath = table.Column<string>(type: "text", nullable: false),
                    FY1 = table.Column<int>(type: "integer", nullable: false),
                    FY1ITReturnFilePath = table.Column<string>(type: "text", nullable: false),
                    FY2 = table.Column<int>(type: "integer", nullable: false),
                    FY2ITReturnFilePath = table.Column<string>(type: "text", nullable: false),
                    ValuationCertificateFilePath = table.Column<string>(type: "text", nullable: false),
                    RetailerListFilePath = table.Column<string>(type: "text", nullable: false),
                    PartnershipDeadFilePath = table.Column<string>(type: "text", nullable: true),
                    BoardReasolutionFilePath = table.Column<string>(type: "text", nullable: true),
                    AffidavitFilePath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerRegistrationDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserTableId = table.Column<string>(type: "text", nullable: false),
                    IsDealer = table.Column<bool>(type: "boolean", nullable: false),
                    InSpic = table.Column<bool>(type: "boolean", nullable: false),
                    InGreenStar = table.Column<bool>(type: "boolean", nullable: false),
                    DealerCode = table.Column<string>(type: "text", nullable: true),
                    SPICCode = table.Column<string>(type: "text", nullable: true),
                    GreenStarCode = table.Column<string>(type: "text", nullable: true),
                    TnCode = table.Column<string>(type: "text", nullable: true),
                    StateId = table.Column<int>(type: "integer", nullable: false),
                    Region = table.Column<int>(type: "integer", nullable: false),
                    HQ = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ParentDealer = table.Column<int>(type: "integer", nullable: false),
                    FirmName = table.Column<string>(type: "text", nullable: false),
                    DateOfAppointment = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BusinessEntityType = table.Column<string>(type: "text", nullable: true),
                    LastTransactionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsLastTransactionIsSale = table.Column<bool>(type: "boolean", nullable: true),
                    IsFinalAmountSettled = table.Column<bool>(type: "boolean", nullable: true),
                    DebitorBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    GoogleMapURL = table.Column<string>(type: "text", nullable: true),
                    ShopNoORRoomNoOrBlockNo = table.Column<string>(type: "text", nullable: false),
                    Street = table.Column<string>(type: "text", nullable: true),
                    SubVillage = table.Column<string>(type: "text", nullable: true),
                    Village = table.Column<string>(type: "text", nullable: false),
                    PinCode = table.Column<string>(type: "text", nullable: false),
                    Block = table.Column<string>(type: "text", nullable: true),
                    Taluk = table.Column<string>(type: "text", nullable: true),
                    DistrictId = table.Column<int>(type: "integer", nullable: false),
                    DealerStateId = table.Column<int>(type: "integer", nullable: false),
                    OfficialContactNumber = table.Column<string>(type: "text", nullable: false),
                    WhatsAppNumber = table.Column<string>(type: "text", nullable: false),
                    AlternativeNumber = table.Column<string>(type: "text", nullable: true),
                    AccountHolderName = table.Column<string>(type: "text", nullable: false),
                    AccountNumber = table.Column<string>(type: "text", nullable: false),
                    BankId = table.Column<int>(type: "integer", nullable: false),
                    Branch = table.Column<string>(type: "text", nullable: false),
                    IFSC = table.Column<string>(type: "text", nullable: false),
                    GSTNumber = table.Column<string>(type: "text", nullable: true),
                    GSTFilePath = table.Column<string>(type: "text", nullable: true),
                    PANNumber = table.Column<string>(type: "text", nullable: true),
                    PANFilePath = table.Column<string>(type: "text", nullable: true),
                    AadhaarNumber = table.Column<string>(type: "text", nullable: true),
                    AadhaarFilePath = table.Column<string>(type: "text", nullable: true),
                    WholeSaleFertilizerLicenseNumber = table.Column<string>(type: "text", nullable: true),
                    WholesaleLicenseExpiryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    WholesalemFMSCode = table.Column<string>(type: "text", nullable: true),
                    WholesaleLicenseFilePath = table.Column<string>(type: "text", nullable: true),
                    RetailFertilizerLicenseNumber = table.Column<string>(type: "text", nullable: true),
                    RetailLicenseExpiryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RetailmFMSCode = table.Column<string>(type: "text", nullable: true),
                    RetailLicenseFilePath = table.Column<string>(type: "text", nullable: true),
                    IsOfficeAutomation = table.Column<bool>(type: "boolean", nullable: false),
                    ExpectedOfficeAutomationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsSDWA = table.Column<bool>(type: "boolean", nullable: false),
                    EntityType = table.Column<int>(type: "integer", nullable: true),
                    CreditLimitExperiance = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerRegistrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Experiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    CompanyId = table.Column<string>(type: "text", nullable: false),
                    NoOfYears = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    TurnOver = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Experiences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Investments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    CapitalInvestment = table.Column<decimal>(type: "numeric", nullable: false),
                    CapitalInvestmentRemarks = table.Column<string>(type: "text", nullable: true),
                    CashCreditLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    CashCreditLimitRrmarks = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Investments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoanLiabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    LoanSource = table.Column<string>(type: "text", nullable: true),
                    LoanValue = table.Column<decimal>(type: "numeric", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanLiabilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    NameOfBlock = table.Column<string>(type: "text", nullable: false),
                    MajorCrops = table.Column<int>(type: "integer", nullable: false),
                    NoOfDealer = table.Column<int>(type: "integer", nullable: false),
                    NoOfFarmer = table.Column<int>(type: "integer", nullable: false),
                    SeasonFromMonth = table.Column<int>(type: "integer", nullable: false),
                    SeasonToMonth = table.Column<int>(type: "integer", nullable: false),
                    IsCanal = table.Column<bool>(type: "boolean", nullable: false),
                    IsTank = table.Column<bool>(type: "boolean", nullable: false),
                    IsWell = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketDetails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Movables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    AssetValue = table.Column<decimal>(type: "numeric", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OwnerShipInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FatherName = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    AadhaarNumber = table.Column<string>(type: "text", nullable: false),
                    AadhaarFilePath = table.Column<string>(type: "text", nullable: false),
                    PANNumber = table.Column<string>(type: "text", nullable: false),
                    PANFilePath = table.Column<string>(type: "text", nullable: false),
                    ProprietorImagePath = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerShipInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartnerFamilyDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnershipPartnerId = table.Column<int>(type: "integer", nullable: false),
                    FamilyMemberName = table.Column<string>(type: "text", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Age = table.Column<int>(type: "integer", nullable: false),
                    RelationshipId = table.Column<int>(type: "integer", nullable: false),
                    Occupation = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerFamilyDetails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartnerOccupations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnershipPartnerId = table.Column<int>(type: "integer", nullable: false),
                    NameofCompany = table.Column<string>(type: "text", nullable: false),
                    SectorId = table.Column<int>(type: "integer", nullable: false),
                    AnnualTurnover = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerOccupations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortFacilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    PortId = table.Column<int>(type: "integer", nullable: false),
                    Distance = table.Column<double>(type: "double precision", nullable: false),
                    Freight = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortFacilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RailFacilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    RailFacilitiesId = table.Column<int>(type: "integer", nullable: false),
                    Distance = table.Column<double>(type: "double precision", nullable: false),
                    Freight = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RailFacilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesPlannings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    AprilQty = table.Column<decimal>(type: "numeric", nullable: false),
                    AprilAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    MayQty = table.Column<decimal>(type: "numeric", nullable: false),
                    MayAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    JuneQty = table.Column<decimal>(type: "numeric", nullable: false),
                    JuneAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    JulyQty = table.Column<decimal>(type: "numeric", nullable: false),
                    JulyAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    AugustQty = table.Column<decimal>(type: "numeric", nullable: false),
                    AugustAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    SeptemberQty = table.Column<decimal>(type: "numeric", nullable: false),
                    SeptemberAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    OctoberQty = table.Column<decimal>(type: "numeric", nullable: false),
                    OctoberAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    NovemberQty = table.Column<decimal>(type: "numeric", nullable: false),
                    NovemberAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    DecemberQty = table.Column<decimal>(type: "numeric", nullable: false),
                    DecemberAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    JanuaryQty = table.Column<decimal>(type: "numeric", nullable: false),
                    JanuaryAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    FebruaryQty = table.Column<decimal>(type: "numeric", nullable: false),
                    FebruaryAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    MarchQty = table.Column<decimal>(type: "numeric", nullable: false),
                    MarchAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesPlannings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseFacilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    WarehouseId = table.Column<int>(type: "integer", nullable: false),
                    Distance = table.Column<double>(type: "double precision", nullable: false),
                    Freight = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseFacilities", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnnualSaleDataLastFY");

            migrationBuilder.DropTable(
                name: "Buildings");

            migrationBuilder.DropTable(
                name: "CompaniesOperatingInAreas");

            migrationBuilder.DropTable(
                name: "CreditLimitProposals");

            migrationBuilder.DropTable(
                name: "CreditLimitSalesPerformances");

            migrationBuilder.DropTable(
                name: "DealerAssetBanks");

            migrationBuilder.DropTable(
                name: "DealerAssetLands");

            migrationBuilder.DropTable(
                name: "DealerInfrastructures");

            migrationBuilder.DropTable(
                name: "DealerRegistrationDocuments");

            migrationBuilder.DropTable(
                name: "DealerRegistrations");

            migrationBuilder.DropTable(
                name: "Experiences");

            migrationBuilder.DropTable(
                name: "Investments");

            migrationBuilder.DropTable(
                name: "LoanLiabilities");

            migrationBuilder.DropTable(
                name: "MarketDetails");

            migrationBuilder.DropTable(
                name: "Movables");

            migrationBuilder.DropTable(
                name: "OwnerShipInfos");

            migrationBuilder.DropTable(
                name: "PartnerFamilyDetails");

            migrationBuilder.DropTable(
                name: "PartnerOccupations");

            migrationBuilder.DropTable(
                name: "PortFacilities");

            migrationBuilder.DropTable(
                name: "RailFacilities");

            migrationBuilder.DropTable(
                name: "SalesPlannings");

            migrationBuilder.DropTable(
                name: "WarehouseFacilities");
        }
    }
}
