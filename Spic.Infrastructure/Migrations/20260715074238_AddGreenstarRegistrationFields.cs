using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGreenstarRegistrationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "GreenstarDateOfAppointment",
                table: "DealerRegistrations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GreenstarTradeDepositAmountReg",
                table: "DealerRegistrations",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GreenstarTradeDepositDateReg",
                table: "DealerRegistrations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GreenstarTradeDepositReceiptNoReg",
                table: "DealerRegistrations",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DealerTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DptReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StateId = table.Column<int>(type: "integer", nullable: true),
                    DistrictId = table.Column<int>(type: "integer", nullable: true),
                    SubDistrictId = table.Column<int>(type: "integer", nullable: true),
                    RetailerId = table.Column<string>(type: "text", nullable: true),
                    RetailerName = table.Column<string>(type: "text", nullable: true),
                    DealerRegistrationId = table.Column<int>(type: "integer", nullable: true),
                    IfmsDealerId = table.Column<int>(type: "integer", nullable: true),
                    MobileNo = table.Column<string>(type: "text", nullable: true),
                    DealershipNature = table.Column<string>(type: "text", nullable: true),
                    Company = table.Column<string>(type: "text", nullable: true),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    OpeningBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SoldQuantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Availability = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DptReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IfmsDealers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IfmsId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    MobileNo = table.Column<string>(type: "text", nullable: true),
                    DealerTypeId = table.Column<int>(type: "integer", nullable: true),
                    DealershipNature = table.Column<string>(type: "text", nullable: true),
                    StateId = table.Column<int>(type: "integer", nullable: true),
                    DistrictId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IfmsDealers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesAndReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company = table.Column<string>(type: "text", nullable: true),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    StateId = table.Column<int>(type: "integer", nullable: true),
                    DistrictId = table.Column<int>(type: "integer", nullable: true),
                    DealerId = table.Column<string>(type: "text", nullable: true),
                    DealerNature = table.Column<string>(type: "text", nullable: true),
                    AgencyName = table.Column<string>(type: "text", nullable: true),
                    DealerRegistrationId = table.Column<int>(type: "integer", nullable: true),
                    IfmsDealerId = table.Column<int>(type: "integer", nullable: true),
                    OpeningBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CompWsSale = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CompWsSaleRcpt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReceivedFromWs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReceivedFromWsAck = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    WsRtSale = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    WsRtSaleRcpt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    WsWsSale = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    WsWsSaleRcpt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalSalesByWs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StockTransferWsToRetailer = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StockTransferWsToRetailerAck = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BalanceWithWs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalAckToWs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesAndReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesCompanySales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionId = table.Column<string>(type: "text", nullable: true),
                    InvoiceNo = table.Column<string>(type: "text", nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Marketer = table.Column<string>(type: "text", nullable: true),
                    Manufacturer = table.Column<string>(type: "text", nullable: true),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    DealerId = table.Column<string>(type: "text", nullable: true),
                    DealerName = table.Column<string>(type: "text", nullable: true),
                    DealerTypeId = table.Column<int>(type: "integer", nullable: true),
                    DealershipNature = table.Column<string>(type: "text", nullable: true),
                    MobileNo = table.Column<string>(type: "text", nullable: true),
                    DealerRegistrationId = table.Column<int>(type: "integer", nullable: true),
                    IfmsDealerId = table.Column<int>(type: "integer", nullable: true),
                    StateId = table.Column<int>(type: "integer", nullable: true),
                    DistrictId = table.Column<int>(type: "integer", nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    UnitId = table.Column<int>(type: "integer", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    QuantityMT = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: true),
                    EntryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LockDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AckThrough = table.Column<string>(type: "text", nullable: true),
                    TxnRemark = table.Column<string>(type: "text", nullable: true),
                    SubsidyMonth1 = table.Column<string>(type: "text", nullable: true),
                    SubsidyYear1 = table.Column<string>(type: "text", nullable: true),
                    Month1Qty = table.Column<decimal>(type: "numeric(18,3)", nullable: true),
                    SubsidyMonth2 = table.Column<string>(type: "text", nullable: true),
                    SubsidyYear2 = table.Column<string>(type: "text", nullable: true),
                    Month2Qty = table.Column<decimal>(type: "numeric(18,3)", nullable: true),
                    ChallanNo = table.Column<string>(type: "text", nullable: true),
                    DdNo = table.Column<string>(type: "text", nullable: true),
                    LorryNo = table.Column<string>(type: "text", nullable: true),
                    LorryCapacity = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    RetailerReceiptDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesCompanySales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesWholesalers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionId = table.Column<string>(type: "text", nullable: true),
                    InvoiceNo = table.Column<string>(type: "text", nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Marketer = table.Column<string>(type: "text", nullable: true),
                    Manufacturer = table.Column<string>(type: "text", nullable: true),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    WholesalerId = table.Column<string>(type: "text", nullable: true),
                    WholesalerAgencyName = table.Column<string>(type: "text", nullable: true),
                    WholesalerNature = table.Column<string>(type: "text", nullable: true),
                    StateId = table.Column<int>(type: "integer", nullable: true),
                    SellerDistrictId = table.Column<int>(type: "integer", nullable: true),
                    BuyerDistrictId = table.Column<int>(type: "integer", nullable: true),
                    DealerId = table.Column<string>(type: "text", nullable: true),
                    DealerTypeId = table.Column<int>(type: "integer", nullable: true),
                    DealerRegistrationId = table.Column<int>(type: "integer", nullable: true),
                    IfmsDealerId = table.Column<int>(type: "integer", nullable: true),
                    AgencyName = table.Column<string>(type: "text", nullable: true),
                    DealerNature = table.Column<string>(type: "text", nullable: true),
                    MobileNo = table.Column<string>(type: "text", nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    UnitId = table.Column<int>(type: "integer", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    QuantityMT = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    ReceivedQuantityMT = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: true),
                    TxnType = table.Column<string>(type: "text", nullable: true),
                    EntryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LockDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AckThrough = table.Column<string>(type: "text", nullable: true),
                    TxnRemark = table.Column<string>(type: "text", nullable: true),
                    SubsidyMonth1 = table.Column<string>(type: "text", nullable: true),
                    SubsidyYear1 = table.Column<string>(type: "text", nullable: true),
                    Month1Qty = table.Column<decimal>(type: "numeric(18,3)", nullable: true),
                    SubsidyMonth2 = table.Column<string>(type: "text", nullable: true),
                    SubsidyYear2 = table.Column<string>(type: "text", nullable: true),
                    Month2Qty = table.Column<decimal>(type: "numeric(18,3)", nullable: true),
                    ChallanNo = table.Column<string>(type: "text", nullable: true),
                    LorryNo = table.Column<string>(type: "text", nullable: true),
                    LorryCapacity = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    DispatchNo = table.Column<string>(type: "text", nullable: true),
                    RetailerReceiptDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesWholesalers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StateWiseGlobalStockReconciliations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StateId = table.Column<int>(type: "integer", nullable: true),
                    OpeningStock = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OpeningGIT = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ProductionImports = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Receipt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Dispatches = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Sales = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SalesReturn = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StockAdjustment = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ClosingGIT = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ClosingStock = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateWiseGlobalStockReconciliations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Statuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseDistrictWiseDetailsGlobalStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StateId = table.Column<int>(type: "integer", nullable: true),
                    DistrictId = table.Column<int>(type: "integer", nullable: true),
                    WarehouseId = table.Column<int>(type: "integer", nullable: true),
                    OpeningStockAtLocation = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OpeningStockGIT = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ImportsProduction = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Receipt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Dispatches = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Sales = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SalesReturn = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StockAdjustment = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ClosingGIT = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ClosingStock = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseDistrictWiseDetailsGlobalStocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WholesalerStockAsOnTodays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StateId = table.Column<int>(type: "integer", nullable: true),
                    DistrictId = table.Column<int>(type: "integer", nullable: true),
                    DealerRegistrationId = table.Column<int>(type: "integer", nullable: true),
                    IfmsDealerId = table.Column<int>(type: "integer", nullable: true),
                    AgencyName = table.Column<string>(type: "text", nullable: true),
                    DealerTypeId = table.Column<int>(type: "integer", nullable: true),
                    DealershipNature = table.Column<string>(type: "text", nullable: true),
                    CompetitorId = table.Column<int>(type: "integer", nullable: true),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    Stock = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StockDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WholesalerStockAsOnTodays", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DealerTypes");

            migrationBuilder.DropTable(
                name: "DptReports");

            migrationBuilder.DropTable(
                name: "IfmsDealers");

            migrationBuilder.DropTable(
                name: "Plants");

            migrationBuilder.DropTable(
                name: "SalesAndReceipts");

            migrationBuilder.DropTable(
                name: "SalesCompanySales");

            migrationBuilder.DropTable(
                name: "SalesWholesalers");

            migrationBuilder.DropTable(
                name: "StateWiseGlobalStockReconciliations");

            migrationBuilder.DropTable(
                name: "Statuses");

            migrationBuilder.DropTable(
                name: "WarehouseDistrictWiseDetailsGlobalStocks");

            migrationBuilder.DropTable(
                name: "WholesalerStockAsOnTodays");

            migrationBuilder.DropColumn(
                name: "GreenstarDateOfAppointment",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "GreenstarTradeDepositAmountReg",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "GreenstarTradeDepositDateReg",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "GreenstarTradeDepositReceiptNoReg",
                table: "DealerRegistrations");
        }
    }
}
