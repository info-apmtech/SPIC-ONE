using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v14AddBudgetAllocationApprovalHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "StateBudgetAllocations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "StateBudgetAllocations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidatedBy",
                table: "StateBudgetAllocations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidatedDate",
                table: "StateBudgetAllocations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "RegionBudgetAllocations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "RegionBudgetAllocations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidatedBy",
                table: "RegionBudgetAllocations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidatedDate",
                table: "RegionBudgetAllocations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "HeadquarterBudgetAllocations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "HeadquarterBudgetAllocations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidatedBy",
                table: "HeadquarterBudgetAllocations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidatedDate",
                table: "HeadquarterBudgetAllocations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HeadquarterBudgetAllocationHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HeadquarterBudgetAllocationId = table.Column<int>(type: "integer", nullable: false),
                    RegionId = table.Column<int>(type: "integer", nullable: false),
                    HeadquarterId = table.Column<int>(type: "integer", nullable: false),
                    FY = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ValidatedBy = table.Column<string>(type: "text", nullable: true),
                    ValidatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeadquarterBudgetAllocationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HeadquarterBudgetAllocationHistories_HeadquarterBudgetAlloc~",
                        column: x => x.HeadquarterBudgetAllocationId,
                        principalTable: "HeadquarterBudgetAllocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegionBudgetAllocationHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegionBudgetAllocationId = table.Column<int>(type: "integer", nullable: false),
                    StateId = table.Column<int>(type: "integer", nullable: false),
                    RegionId = table.Column<int>(type: "integer", nullable: false),
                    FY = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ValidatedBy = table.Column<string>(type: "text", nullable: true),
                    ValidatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionBudgetAllocationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegionBudgetAllocationHistories_RegionBudgetAllocations_Reg~",
                        column: x => x.RegionBudgetAllocationId,
                        principalTable: "RegionBudgetAllocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StateBudgetAllocationHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StateBudgetAllocationId = table.Column<int>(type: "integer", nullable: false),
                    StateId = table.Column<int>(type: "integer", nullable: false),
                    FY = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ValidatedBy = table.Column<string>(type: "text", nullable: true),
                    ValidatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateBudgetAllocationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StateBudgetAllocationHistories_StateBudgetAllocations_State~",
                        column: x => x.StateBudgetAllocationId,
                        principalTable: "StateBudgetAllocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HeadquarterBudgetAllocationHistories_HeadquarterBudgetAlloc~",
                table: "HeadquarterBudgetAllocationHistories",
                column: "HeadquarterBudgetAllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_HeadquarterBudgetAllocationHistories_HeadquarterId_FY",
                table: "HeadquarterBudgetAllocationHistories",
                columns: new[] { "HeadquarterId", "FY" });

            migrationBuilder.CreateIndex(
                name: "IX_RegionBudgetAllocationHistories_RegionBudgetAllocationId",
                table: "RegionBudgetAllocationHistories",
                column: "RegionBudgetAllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_RegionBudgetAllocationHistories_RegionId_FY",
                table: "RegionBudgetAllocationHistories",
                columns: new[] { "RegionId", "FY" });

            migrationBuilder.CreateIndex(
                name: "IX_StateBudgetAllocationHistories_StateBudgetAllocationId",
                table: "StateBudgetAllocationHistories",
                column: "StateBudgetAllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StateBudgetAllocationHistories_StateId_FY",
                table: "StateBudgetAllocationHistories",
                columns: new[] { "StateId", "FY" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HeadquarterBudgetAllocationHistories");

            migrationBuilder.DropTable(
                name: "RegionBudgetAllocationHistories");

            migrationBuilder.DropTable(
                name: "StateBudgetAllocationHistories");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "StateBudgetAllocations");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "StateBudgetAllocations");

            migrationBuilder.DropColumn(
                name: "ValidatedBy",
                table: "StateBudgetAllocations");

            migrationBuilder.DropColumn(
                name: "ValidatedDate",
                table: "StateBudgetAllocations");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "RegionBudgetAllocations");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "RegionBudgetAllocations");

            migrationBuilder.DropColumn(
                name: "ValidatedBy",
                table: "RegionBudgetAllocations");

            migrationBuilder.DropColumn(
                name: "ValidatedDate",
                table: "RegionBudgetAllocations");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "HeadquarterBudgetAllocations");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "HeadquarterBudgetAllocations");

            migrationBuilder.DropColumn(
                name: "ValidatedBy",
                table: "HeadquarterBudgetAllocations");

            migrationBuilder.DropColumn(
                name: "ValidatedDate",
                table: "HeadquarterBudgetAllocations");
        }
    }
}
