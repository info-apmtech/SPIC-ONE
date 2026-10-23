using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBudgetFinancialYearMappingsinStateRegionHq : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StateBudgetSummaryHistories_FY",
                table: "StateBudgetSummaryHistories");

            migrationBuilder.DropIndex(
                name: "IX_StateBudgetAllocations_StateId_FY",
                table: "StateBudgetAllocations");

            migrationBuilder.DropIndex(
                name: "IX_StateBudgetAllocationHistories_StateId_FY",
                table: "StateBudgetAllocationHistories");

            migrationBuilder.DropIndex(
                name: "IX_RegionBudgetSummaryHistories_StateId_FY",
                table: "RegionBudgetSummaryHistories");

            migrationBuilder.DropIndex(
                name: "IX_RegionBudgetAllocations_RegionId_FY",
                table: "RegionBudgetAllocations");

            migrationBuilder.DropIndex(
                name: "IX_RegionBudgetAllocationHistories_RegionId_FY",
                table: "RegionBudgetAllocationHistories");

            migrationBuilder.DropIndex(
                name: "IX_HeadquarterBudgetSummaryHistories_RegionId_FY",
                table: "HeadquarterBudgetSummaryHistories");

            migrationBuilder.DropIndex(
                name: "IX_HeadquarterBudgetAllocations_HeadquarterId_FY",
                table: "HeadquarterBudgetAllocations");

            migrationBuilder.DropIndex(
                name: "IX_HeadquarterBudgetAllocationHistories_HeadquarterId_FY",
                table: "HeadquarterBudgetAllocationHistories");

            migrationBuilder.DropColumn(
                name: "FY",
                table: "StateBudgetSummaryHistories");

            migrationBuilder.DropColumn(
                name: "FY",
                table: "StateBudgetAllocations");

            migrationBuilder.DropColumn(
                name: "FY",
                table: "StateBudgetAllocationHistories");

            migrationBuilder.DropColumn(
                name: "FY",
                table: "RegionBudgetSummaryHistories");

            migrationBuilder.DropColumn(
                name: "FY",
                table: "RegionBudgetAllocations");

            migrationBuilder.DropColumn(
                name: "FY",
                table: "RegionBudgetAllocationHistories");

            migrationBuilder.DropColumn(
                name: "FY",
                table: "HeadquarterBudgetSummaryHistories");

            migrationBuilder.DropColumn(
                name: "FY",
                table: "HeadquarterBudgetAllocations");

            migrationBuilder.DropColumn(
                name: "FY",
                table: "HeadquarterBudgetAllocationHistories");

            // Convert existing FY names (for example "2026-2027") to FinancialYears.Id
            // before changing the PostgreSQL column type from text to integer.
            migrationBuilder.Sql(@"
                UPDATE ""StateBudgetSummaries"" AS s
                SET ""FY"" = fy.""Id""::text
                FROM ""FinancialYears"" AS fy
                WHERE BTRIM(s.""FY"") = BTRIM(fy.""Name"");
            ");

            migrationBuilder.Sql(@"
                UPDATE ""RegionBudgetSummaries"" AS s
                SET ""FY"" = fy.""Id""::text
                FROM ""FinancialYears"" AS fy
                WHERE BTRIM(s.""FY"") = BTRIM(fy.""Name"");
            ");

            migrationBuilder.Sql(@"
                UPDATE ""HeadquarterBudgetSummaries"" AS s
                SET ""FY"" = fy.""Id""::text
                FROM ""FinancialYears"" AS fy
                WHERE BTRIM(s.""FY"") = BTRIM(fy.""Name"");
            ");

            // Abort instead of silently corrupting data if any FY name did not match
            // FinancialYears.Name.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM ""StateBudgetSummaries""
                        WHERE ""FY"" IS NULL OR ""FY"" !~ '^[0-9]+$'
                    ) THEN
                        RAISE EXCEPTION 'StateBudgetSummaries contains FY values that do not match FinancialYears.Name.';
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM ""RegionBudgetSummaries""
                        WHERE ""FY"" IS NULL OR ""FY"" !~ '^[0-9]+$'
                    ) THEN
                        RAISE EXCEPTION 'RegionBudgetSummaries contains FY values that do not match FinancialYears.Name.';
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM ""HeadquarterBudgetSummaries""
                        WHERE ""FY"" IS NULL OR ""FY"" !~ '^[0-9]+$'
                    ) THEN
                        RAISE EXCEPTION 'HeadquarterBudgetSummaries contains FY values that do not match FinancialYears.Name.';
                    END IF;
                END $$;
            ");

            // Npgsql cannot safely infer text -> integer for existing PostgreSQL data,
            // so use explicit PostgreSQL USING casts.
            migrationBuilder.Sql(@"
                ALTER TABLE ""StateBudgetSummaries""
                ALTER COLUMN ""FY"" TYPE integer
                USING ""FY""::integer;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""RegionBudgetSummaries""
                ALTER COLUMN ""FY"" TYPE integer
                USING ""FY""::integer;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""HeadquarterBudgetSummaries""
                ALTER COLUMN ""FY"" TYPE integer
                USING ""FY""::integer;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_StateBudgetAllocations_StateId_StateBudgetSummaryId",
                table: "StateBudgetAllocations",
                columns: new[] { "StateId", "StateBudgetSummaryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateBudgetAllocationHistories_StateId",
                table: "StateBudgetAllocationHistories",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_RegionBudgetSummaryHistories_StateId",
                table: "RegionBudgetSummaryHistories",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_RegionBudgetAllocations_RegionId_RegionBudgetSummaryId",
                table: "RegionBudgetAllocations",
                columns: new[] { "RegionId", "RegionBudgetSummaryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegionBudgetAllocationHistories_RegionId",
                table: "RegionBudgetAllocationHistories",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_HeadquarterBudgetSummaryHistories_RegionId",
                table: "HeadquarterBudgetSummaryHistories",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_HeadquarterBudgetAllocations_HeadquarterId_HeadquarterBudge~",
                table: "HeadquarterBudgetAllocations",
                columns: new[] { "HeadquarterId", "HeadquarterBudgetSummaryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HeadquarterBudgetAllocationHistories_HeadquarterId",
                table: "HeadquarterBudgetAllocationHistories",
                column: "HeadquarterId");

            migrationBuilder.AddForeignKey(
                name: "FK_StateBudgetSummaries_FinancialYears_FY",
                table: "StateBudgetSummaries",
                column: "FY",
                principalTable: "FinancialYears",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RegionBudgetSummaries_FinancialYears_FY",
                table: "RegionBudgetSummaries",
                column: "FY",
                principalTable: "FinancialYears",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HeadquarterBudgetSummaries_FinancialYears_FY",
                table: "HeadquarterBudgetSummaries",
                column: "FY",
                principalTable: "FinancialYears",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StateBudgetSummaries_FinancialYears_FY",
                table: "StateBudgetSummaries");

            migrationBuilder.DropForeignKey(
                name: "FK_RegionBudgetSummaries_FinancialYears_FY",
                table: "RegionBudgetSummaries");

            migrationBuilder.DropForeignKey(
                name: "FK_HeadquarterBudgetSummaries_FinancialYears_FY",
                table: "HeadquarterBudgetSummaries");

            migrationBuilder.DropIndex(
                name: "IX_StateBudgetAllocations_StateId_StateBudgetSummaryId",
                table: "StateBudgetAllocations");

            migrationBuilder.DropIndex(
                name: "IX_StateBudgetAllocationHistories_StateId",
                table: "StateBudgetAllocationHistories");

            migrationBuilder.DropIndex(
                name: "IX_RegionBudgetSummaryHistories_StateId",
                table: "RegionBudgetSummaryHistories");

            migrationBuilder.DropIndex(
                name: "IX_RegionBudgetAllocations_RegionId_RegionBudgetSummaryId",
                table: "RegionBudgetAllocations");

            migrationBuilder.DropIndex(
                name: "IX_RegionBudgetAllocationHistories_RegionId",
                table: "RegionBudgetAllocationHistories");

            migrationBuilder.DropIndex(
                name: "IX_HeadquarterBudgetSummaryHistories_RegionId",
                table: "HeadquarterBudgetSummaryHistories");

            migrationBuilder.DropIndex(
                name: "IX_HeadquarterBudgetAllocations_HeadquarterId_HeadquarterBudge~",
                table: "HeadquarterBudgetAllocations");

            migrationBuilder.DropIndex(
                name: "IX_HeadquarterBudgetAllocationHistories_HeadquarterId",
                table: "HeadquarterBudgetAllocationHistories");

            migrationBuilder.AddColumn<string>(
                name: "FY",
                table: "StateBudgetSummaryHistories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                ALTER TABLE ""StateBudgetSummaries""
                ALTER COLUMN ""FY"" TYPE text
                USING ""FY""::text;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""StateBudgetSummaries"" AS s
                SET ""FY"" = fy.""Name""
                FROM ""FinancialYears"" AS fy
                WHERE s.""FY"" = fy.""Id""::text;
            ");

            migrationBuilder.AddColumn<string>(
                name: "FY",
                table: "StateBudgetAllocations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FY",
                table: "StateBudgetAllocationHistories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FY",
                table: "RegionBudgetSummaryHistories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                ALTER TABLE ""RegionBudgetSummaries""
                ALTER COLUMN ""FY"" TYPE text
                USING ""FY""::text;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""RegionBudgetSummaries"" AS s
                SET ""FY"" = fy.""Name""
                FROM ""FinancialYears"" AS fy
                WHERE s.""FY"" = fy.""Id""::text;
            ");

            migrationBuilder.AddColumn<string>(
                name: "FY",
                table: "RegionBudgetAllocations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FY",
                table: "RegionBudgetAllocationHistories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FY",
                table: "HeadquarterBudgetSummaryHistories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                ALTER TABLE ""HeadquarterBudgetSummaries""
                ALTER COLUMN ""FY"" TYPE text
                USING ""FY""::text;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""HeadquarterBudgetSummaries"" AS s
                SET ""FY"" = fy.""Name""
                FROM ""FinancialYears"" AS fy
                WHERE s.""FY"" = fy.""Id""::text;
            ");

            migrationBuilder.AddColumn<string>(
                name: "FY",
                table: "HeadquarterBudgetAllocations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FY",
                table: "HeadquarterBudgetAllocationHistories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_StateBudgetSummaryHistories_FY",
                table: "StateBudgetSummaryHistories",
                column: "FY");

            migrationBuilder.CreateIndex(
                name: "IX_StateBudgetAllocations_StateId_FY",
                table: "StateBudgetAllocations",
                columns: new[] { "StateId", "FY" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateBudgetAllocationHistories_StateId_FY",
                table: "StateBudgetAllocationHistories",
                columns: new[] { "StateId", "FY" });

            migrationBuilder.CreateIndex(
                name: "IX_RegionBudgetSummaryHistories_StateId_FY",
                table: "RegionBudgetSummaryHistories",
                columns: new[] { "StateId", "FY" });

            migrationBuilder.CreateIndex(
                name: "IX_RegionBudgetAllocations_RegionId_FY",
                table: "RegionBudgetAllocations",
                columns: new[] { "RegionId", "FY" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegionBudgetAllocationHistories_RegionId_FY",
                table: "RegionBudgetAllocationHistories",
                columns: new[] { "RegionId", "FY" });

            migrationBuilder.CreateIndex(
                name: "IX_HeadquarterBudgetSummaryHistories_RegionId_FY",
                table: "HeadquarterBudgetSummaryHistories",
                columns: new[] { "RegionId", "FY" });

            migrationBuilder.CreateIndex(
                name: "IX_HeadquarterBudgetAllocations_HeadquarterId_FY",
                table: "HeadquarterBudgetAllocations",
                columns: new[] { "HeadquarterId", "FY" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HeadquarterBudgetAllocationHistories_HeadquarterId_FY",
                table: "HeadquarterBudgetAllocationHistories",
                columns: new[] { "HeadquarterId", "FY" });
        }
    }
}
