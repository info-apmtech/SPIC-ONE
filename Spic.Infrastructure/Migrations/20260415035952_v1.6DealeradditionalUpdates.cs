using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v16DealeradditionalUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Buildings");

            migrationBuilder.DropTable(
                name: "CompaniesOperatingInAreas");

            migrationBuilder.DropTable(
                name: "CreditLimitProposals");

            migrationBuilder.DropTable(
                name: "CreditLimitSalesPerformances");

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
                name: "PortFacilities");

            migrationBuilder.DropTable(
                name: "RailFacilities");

            migrationBuilder.DropTable(
                name: "WarehouseFacilities");

            migrationBuilder.CreateTable(
                name: "DealerApprovalHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    ApprovedBy = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerApprovalHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerAssetBuildings",
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
                    table.PrimaryKey("PK_DealerAssetBuildings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerAssetMovables",
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
                    table.PrimaryKey("PK_DealerAssetMovables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerCompaniesOperatingInAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    CompaniesOperating = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerCompaniesOperatingInAreas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerCreditLimitProposals",
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
                    table.PrimaryKey("PK_DealerCreditLimitProposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerCreditLimitSalesPerformances",
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
                    table.PrimaryKey("PK_DealerCreditLimitSalesPerformances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerExperiences",
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
                    table.PrimaryKey("PK_DealerExperiences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerInvestments",
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
                    table.PrimaryKey("PK_DealerInvestments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerLoanLiabilities",
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
                    table.PrimaryKey("PK_DealerLoanLiabilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerMarketDetails",
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
                    table.PrimaryKey("PK_DealerMarketDetails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerOwnershipInfos",
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
                    table.PrimaryKey("PK_DealerOwnershipInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerPortFacilities",
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
                    table.PrimaryKey("PK_DealerPortFacilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerRailFacilities",
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
                    table.PrimaryKey("PK_DealerRailFacilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerWarehouseFacilities",
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
                    table.PrimaryKey("PK_DealerWarehouseFacilities", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DealerApprovalHistories");

            migrationBuilder.DropTable(
                name: "DealerAssetBuildings");

            migrationBuilder.DropTable(
                name: "DealerAssetMovables");

            migrationBuilder.DropTable(
                name: "DealerCompaniesOperatingInAreas");

            migrationBuilder.DropTable(
                name: "DealerCreditLimitProposals");

            migrationBuilder.DropTable(
                name: "DealerCreditLimitSalesPerformances");

            migrationBuilder.DropTable(
                name: "DealerExperiences");

            migrationBuilder.DropTable(
                name: "DealerInvestments");

            migrationBuilder.DropTable(
                name: "DealerLoanLiabilities");

            migrationBuilder.DropTable(
                name: "DealerMarketDetails");

            migrationBuilder.DropTable(
                name: "DealerOwnershipInfos");

            migrationBuilder.DropTable(
                name: "DealerPortFacilities");

            migrationBuilder.DropTable(
                name: "DealerRailFacilities");

            migrationBuilder.DropTable(
                name: "DealerWarehouseFacilities");

            migrationBuilder.CreateTable(
                name: "Buildings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BuildingName = table.Column<string>(type: "text", nullable: true),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    LandSize = table.Column<decimal>(type: "numeric", nullable: true),
                    PropertyValue = table.Column<decimal>(type: "numeric", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    SurveyNumber = table.Column<string>(type: "text", nullable: true),
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
                    CompaniesOperating = table.Column<string>(type: "text", nullable: true),
                    DealerId = table.Column<int>(type: "integer", nullable: false)
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
                    AdditionalCreditLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    FY1 = table.Column<int>(type: "integer", nullable: false),
                    FY2 = table.Column<int>(type: "integer", nullable: false),
                    FY3 = table.Column<int>(type: "integer", nullable: false),
                    Q10Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q11Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q1Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q2Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q3Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q4Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q5Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q6Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q7Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q8Mark = table.Column<double>(type: "double precision", nullable: false),
                    Q9Mark = table.Column<double>(type: "double precision", nullable: false)
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
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    CreditLimitId = table.Column<int>(type: "integer", nullable: false),
                    FY1Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    FY1Qty = table.Column<decimal>(type: "numeric", nullable: false),
                    FY2Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    FY2Qty = table.Column<decimal>(type: "numeric", nullable: false),
                    FY3Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    FY3Qty = table.Column<decimal>(type: "numeric", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditLimitSalesPerformances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Experiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<string>(type: "text", nullable: false),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    NoOfYears = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    TurnOver = table.Column<decimal>(type: "numeric", nullable: false)
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
                    CapitalInvestment = table.Column<decimal>(type: "numeric", nullable: false),
                    CapitalInvestmentRemarks = table.Column<string>(type: "text", nullable: true),
                    CashCreditLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    CashCreditLimitRrmarks = table.Column<string>(type: "text", nullable: true),
                    DealerId = table.Column<int>(type: "integer", nullable: false)
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
                    IsCanal = table.Column<bool>(type: "boolean", nullable: false),
                    IsTank = table.Column<bool>(type: "boolean", nullable: false),
                    IsWell = table.Column<bool>(type: "boolean", nullable: false),
                    MajorCrops = table.Column<int>(type: "integer", nullable: false),
                    NameOfBlock = table.Column<string>(type: "text", nullable: false),
                    NoOfDealer = table.Column<int>(type: "integer", nullable: false),
                    NoOfFarmer = table.Column<int>(type: "integer", nullable: false),
                    SeasonFromMonth = table.Column<int>(type: "integer", nullable: false),
                    SeasonToMonth = table.Column<int>(type: "integer", nullable: false)
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
                    AssetValue = table.Column<decimal>(type: "numeric", nullable: false),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
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
                    AadhaarFilePath = table.Column<string>(type: "text", nullable: false),
                    AadhaarNumber = table.Column<string>(type: "text", nullable: false),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    FatherName = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PANFilePath = table.Column<string>(type: "text", nullable: false),
                    PANNumber = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    ProprietorImagePath = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerShipInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortFacilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    Distance = table.Column<double>(type: "double precision", nullable: false),
                    Freight = table.Column<double>(type: "double precision", nullable: false),
                    PortId = table.Column<int>(type: "integer", nullable: false)
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
                    Distance = table.Column<double>(type: "double precision", nullable: false),
                    Freight = table.Column<double>(type: "double precision", nullable: false),
                    RailFacilitiesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RailFacilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseFacilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    Distance = table.Column<double>(type: "double precision", nullable: false),
                    Freight = table.Column<double>(type: "double precision", nullable: false),
                    WarehouseId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseFacilities", x => x.Id);
                });
        }
    }
}
