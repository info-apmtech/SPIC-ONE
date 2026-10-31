using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetReallocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SubDealerCode",
                table: "SubDealerRegistrations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "BudgetReallocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FinancialYearId = table.Column<int>(type: "integer", nullable: false),
                    SourceLevel = table.Column<int>(type: "integer", nullable: false),
                    SourceId = table.Column<int>(type: "integer", nullable: false),
                    DestinationLevel = table.Column<int>(type: "integer", nullable: false),
                    DestinationId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    InitiatorRole = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    InitiatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetReallocations", x => x.Id);
                });

            //migrationBuilder.CreateTable(
            //    name: "GpsSettings",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "integer", nullable: false)
            //            .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            //        IntervalSeconds = table.Column<int>(type: "integer", nullable: false),
            //        IsActive = table.Column<bool>(type: "boolean", nullable: false),
            //        CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
            //        CreatedBy = table.Column<string>(type: "text", nullable: true),
            //        UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
            //        UpdatedBy = table.Column<string>(type: "text", nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_GpsSettings", x => x.Id);
            //    });

            migrationBuilder.CreateTable(
                name: "BudgetReallocationApprovals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BudgetReallocationId = table.Column<int>(type: "integer", nullable: false),
                    StageRole = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Decision = table.Column<int>(type: "integer", nullable: false),
                    ApprovedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetReallocationApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetReallocationApprovals_BudgetReallocations_BudgetReall~",
                        column: x => x.BudgetReallocationId,
                        principalTable: "BudgetReallocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            //migrationBuilder.CreateIndex(
            //    name: "IX_JmdoTaskAllocations_HeadquarterId",
            //    table: "JmdoTaskAllocations",
            //    column: "HeadquarterId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_JmdoTaskAllocationDealers_DealerRegistrationId",
            //    table: "JmdoTaskAllocationDealers",
            //    column: "DealerRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetReallocationApprovals_BudgetReallocationId",
                table: "BudgetReallocationApprovals",
                column: "BudgetReallocationId");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_JmdoTaskAllocationDealers_DealerRegistrations_DealerRegistr~",
            //    table: "JmdoTaskAllocationDealers",
            //    column: "DealerRegistrationId",
            //    principalTable: "DealerRegistrations",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_JmdoTaskAllocationDealers_DealerRegistrations_DealerRegistr~",
            //    table: "JmdoTaskAllocationDealers");

            migrationBuilder.DropTable(
                name: "BudgetReallocationApprovals");

            //migrationBuilder.DropTable(
            //    name: "GpsSettings");

            migrationBuilder.DropTable(
                name: "BudgetReallocations");

            //migrationBuilder.DropIndex(
            //    name: "IX_JmdoTaskAllocations_HeadquarterId",
            //    table: "JmdoTaskAllocations");

            //migrationBuilder.DropIndex(
            //    name: "IX_JmdoTaskAllocationDealers_DealerRegistrationId",
            //    table: "JmdoTaskAllocationDealers");

            migrationBuilder.AlterColumn<string>(
                name: "SubDealerCode",
                table: "SubDealerRegistrations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
