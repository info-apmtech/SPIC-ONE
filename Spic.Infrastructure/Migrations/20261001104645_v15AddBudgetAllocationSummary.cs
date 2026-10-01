using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v15AddBudgetAllocationSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HeadquarterBudgetSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegionId = table.Column<int>(type: "integer", nullable: false),
                    FY = table.Column<string>(type: "text", nullable: false),
                    TotalBudget = table.Column<decimal>(type: "numeric", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Draft"),
                    ValidatedBy = table.Column<string>(type: "text", nullable: true),
                    ValidatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeadquarterBudgetSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HeadquarterBudgetSummaries_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegionBudgetSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StateId = table.Column<int>(type: "integer", nullable: false),
                    FY = table.Column<string>(type: "text", nullable: false),
                    TotalBudget = table.Column<decimal>(type: "numeric", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Draft"),
                    ValidatedBy = table.Column<string>(type: "text", nullable: true),
                    ValidatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionBudgetSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegionBudgetSummaries_States_StateId",
                        column: x => x.StateId,
                        principalTable: "States",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StateBudgetSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FY = table.Column<string>(type: "text", nullable: false),
                    TotalBudget = table.Column<decimal>(type: "numeric", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Draft"),
                    ValidatedBy = table.Column<string>(type: "text", nullable: true),
                    ValidatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateBudgetSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HeadquarterBudgetSummaryHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HeadquarterBudgetSummaryId = table.Column<int>(type: "integer", nullable: false),
                    RegionId = table.Column<int>(type: "integer", nullable: false),
                    FY = table.Column<string>(type: "text", nullable: false),
                    TotalBudget = table.Column<decimal>(type: "numeric", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "numeric", nullable: false),
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
                    table.PrimaryKey("PK_HeadquarterBudgetSummaryHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HeadquarterBudgetSummaryHistories_HeadquarterBudgetSummarie~",
                        column: x => x.HeadquarterBudgetSummaryId,
                        principalTable: "HeadquarterBudgetSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegionBudgetSummaryHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegionBudgetSummaryId = table.Column<int>(type: "integer", nullable: false),
                    StateId = table.Column<int>(type: "integer", nullable: false),
                    FY = table.Column<string>(type: "text", nullable: false),
                    TotalBudget = table.Column<decimal>(type: "numeric", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "numeric", nullable: false),
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
                    table.PrimaryKey("PK_RegionBudgetSummaryHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegionBudgetSummaryHistories_RegionBudgetSummaries_RegionBu~",
                        column: x => x.RegionBudgetSummaryId,
                        principalTable: "RegionBudgetSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StateBudgetSummaryHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StateBudgetSummaryId = table.Column<int>(type: "integer", nullable: false),
                    FY = table.Column<string>(type: "text", nullable: false),
                    TotalBudget = table.Column<decimal>(type: "numeric", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "numeric", nullable: false),
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
                    table.PrimaryKey("PK_StateBudgetSummaryHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StateBudgetSummaryHistories_StateBudgetSummaries_StateBudge~",
                        column: x => x.StateBudgetSummaryId,
                        principalTable: "StateBudgetSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HeadquarterBudgetSummaries_RegionId_FY",
                table: "HeadquarterBudgetSummaries",
                columns: new[] { "RegionId", "FY" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HeadquarterBudgetSummaryHistories_HeadquarterBudgetSummaryId",
                table: "HeadquarterBudgetSummaryHistories",
                column: "HeadquarterBudgetSummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_HeadquarterBudgetSummaryHistories_RegionId_FY",
                table: "HeadquarterBudgetSummaryHistories",
                columns: new[] { "RegionId", "FY" });

            migrationBuilder.CreateIndex(
                name: "IX_RegionBudgetSummaries_StateId_FY",
                table: "RegionBudgetSummaries",
                columns: new[] { "StateId", "FY" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegionBudgetSummaryHistories_RegionBudgetSummaryId",
                table: "RegionBudgetSummaryHistories",
                column: "RegionBudgetSummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_RegionBudgetSummaryHistories_StateId_FY",
                table: "RegionBudgetSummaryHistories",
                columns: new[] { "StateId", "FY" });

            migrationBuilder.CreateIndex(
                name: "IX_StateBudgetSummaries_FY",
                table: "StateBudgetSummaries",
                column: "FY",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateBudgetSummaryHistories_FY",
                table: "StateBudgetSummaryHistories",
                column: "FY");

            migrationBuilder.CreateIndex(
                name: "IX_StateBudgetSummaryHistories_StateBudgetSummaryId",
                table: "StateBudgetSummaryHistories",
                column: "StateBudgetSummaryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HeadquarterBudgetSummaryHistories");

            migrationBuilder.DropTable(
                name: "RegionBudgetSummaryHistories");

            migrationBuilder.DropTable(
                name: "StateBudgetSummaryHistories");

            migrationBuilder.DropTable(
                name: "HeadquarterBudgetSummaries");

            migrationBuilder.DropTable(
                name: "RegionBudgetSummaries");

            migrationBuilder.DropTable(
                name: "StateBudgetSummaries");
        }
    }
}
