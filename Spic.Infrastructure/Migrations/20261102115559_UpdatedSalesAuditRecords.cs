using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedSalesAuditRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalApprovedBudgetRs",
                table: "SalesAuditRecords",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinalSubmittedAt",
                table: "SalesAuditRecords",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFinalSubmitted",
                table: "SalesAuditRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SalesAuditAchievementLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CSR2Id = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    QuantityMT = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    ValueRs = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesAuditAchievementLines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesAuditPlanLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CSR2Id = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    QuantityMT = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    ValueRs = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesAuditPlanLines", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesAuditAchievementLines");

            migrationBuilder.DropTable(
                name: "SalesAuditPlanLines");

            migrationBuilder.DropColumn(
                name: "AdditionalApprovedBudgetRs",
                table: "SalesAuditRecords");

            migrationBuilder.DropColumn(
                name: "FinalSubmittedAt",
                table: "SalesAuditRecords");

            migrationBuilder.DropColumn(
                name: "IsFinalSubmitted",
                table: "SalesAuditRecords");
        }
    }
}
