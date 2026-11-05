using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesAuditDealerMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesAuditDealerMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CSR2Id = table.Column<int>(type: "integer", nullable: false),
                    CustomerNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    CustomerDistrict = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    QuantityMT = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    ActualSalesRs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PlannedCommitmentRs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MappedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UploadFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesAuditDealerMappings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesAuditDealerMappings");
        }
    }
}
