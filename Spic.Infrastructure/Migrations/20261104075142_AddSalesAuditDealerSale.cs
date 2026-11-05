using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesAuditDealerSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesAuditDealerSales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerRegistrationId = table.Column<int>(type: "integer", nullable: true),
                    BillingDocument = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Item = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    BillingDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SalesOrder = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SalesOrderDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CustomerNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    CustomerDistrict = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    BasicValue = table.Column<decimal>(type: "numeric", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    RegionCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    RegionDescription = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SalesOffice = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    SalesOfficeDescription = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    UploadFileName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ImportedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceRowJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesAuditDealerSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesAuditDealerSales_DealerRegistrations_DealerRegistratio~",
                        column: x => x.DealerRegistrationId,
                        principalTable: "DealerRegistrations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesAuditDealerSales_DealerRegistrationId",
                table: "SalesAuditDealerSales",
                column: "DealerRegistrationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesAuditDealerSales");
        }
    }
}
