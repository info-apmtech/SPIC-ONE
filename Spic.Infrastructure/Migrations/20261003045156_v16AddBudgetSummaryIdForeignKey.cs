using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v16AddBudgetSummaryIdForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StateBudgetSummaryId",
                table: "StateBudgetAllocations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegionBudgetSummaryId",
                table: "RegionBudgetAllocations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HeadquarterBudgetSummaryId",
                table: "HeadquarterBudgetAllocations",
                type: "integer",
                nullable: true);

            // Backfill the new FK on existing rows from the same match the app already used
            // (FY, plus StateId/RegionId scope at levels 2/3) - no amounts are touched or
            // invented, this only links a detail row to the one summary row it already
            // corresponded to. Rows with no matching summary are left NULL (nullable FK) and
            // should be reviewed manually.
            migrationBuilder.Sql(@"
                UPDATE ""StateBudgetAllocations"" a
                SET ""StateBudgetSummaryId"" = s.""Id""
                FROM ""StateBudgetSummaries"" s
                WHERE s.""FY"" = a.""FY"" AND a.""StateBudgetSummaryId"" IS NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""RegionBudgetAllocations"" a
                SET ""RegionBudgetSummaryId"" = s.""Id""
                FROM ""RegionBudgetSummaries"" s
                WHERE s.""StateId"" = a.""StateId"" AND s.""FY"" = a.""FY"" AND a.""RegionBudgetSummaryId"" IS NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""HeadquarterBudgetAllocations"" a
                SET ""HeadquarterBudgetSummaryId"" = s.""Id""
                FROM ""HeadquarterBudgetSummaries"" s
                WHERE s.""RegionId"" = a.""RegionId"" AND s.""FY"" = a.""FY"" AND a.""HeadquarterBudgetSummaryId"" IS NULL;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_StateBudgetAllocations_StateBudgetSummaryId",
                table: "StateBudgetAllocations",
                column: "StateBudgetSummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_RegionBudgetAllocations_RegionBudgetSummaryId",
                table: "RegionBudgetAllocations",
                column: "RegionBudgetSummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_HeadquarterBudgetAllocations_HeadquarterBudgetSummaryId",
                table: "HeadquarterBudgetAllocations",
                column: "HeadquarterBudgetSummaryId");

            migrationBuilder.AddForeignKey(
                name: "FK_HeadquarterBudgetAllocations_HeadquarterBudgetSummaries_Hea~",
                table: "HeadquarterBudgetAllocations",
                column: "HeadquarterBudgetSummaryId",
                principalTable: "HeadquarterBudgetSummaries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RegionBudgetAllocations_RegionBudgetSummaries_RegionBudgetS~",
                table: "RegionBudgetAllocations",
                column: "RegionBudgetSummaryId",
                principalTable: "RegionBudgetSummaries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StateBudgetAllocations_StateBudgetSummaries_StateBudgetSumm~",
                table: "StateBudgetAllocations",
                column: "StateBudgetSummaryId",
                principalTable: "StateBudgetSummaries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HeadquarterBudgetAllocations_HeadquarterBudgetSummaries_Hea~",
                table: "HeadquarterBudgetAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_RegionBudgetAllocations_RegionBudgetSummaries_RegionBudgetS~",
                table: "RegionBudgetAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_StateBudgetAllocations_StateBudgetSummaries_StateBudgetSumm~",
                table: "StateBudgetAllocations");

            migrationBuilder.DropIndex(
                name: "IX_StateBudgetAllocations_StateBudgetSummaryId",
                table: "StateBudgetAllocations");

            migrationBuilder.DropIndex(
                name: "IX_RegionBudgetAllocations_RegionBudgetSummaryId",
                table: "RegionBudgetAllocations");

            migrationBuilder.DropIndex(
                name: "IX_HeadquarterBudgetAllocations_HeadquarterBudgetSummaryId",
                table: "HeadquarterBudgetAllocations");

            migrationBuilder.DropColumn(
                name: "StateBudgetSummaryId",
                table: "StateBudgetAllocations");

            migrationBuilder.DropColumn(
                name: "RegionBudgetSummaryId",
                table: "RegionBudgetAllocations");

            migrationBuilder.DropColumn(
                name: "HeadquarterBudgetSummaryId",
                table: "HeadquarterBudgetAllocations");
        }
    }
}
