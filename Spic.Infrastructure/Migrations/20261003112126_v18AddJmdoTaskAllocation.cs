using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v18AddJmdoTaskAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ValidateBy",
                table: "BudgetProgramMains",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "BudgetProgramMains",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ApprovedBy",
                table: "BudgetProgramMains",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "RegionId",
                table: "BudgetProgramMains",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JmdoTaskAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AllocationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SubmittedByUserId = table.Column<string>(type: "text", nullable: false),
                    SubmittedByRole = table.Column<string>(type: "text", nullable: true),
                    HeadquarterId = table.Column<int>(type: "integer", nullable: true),
                    RegionId = table.Column<int>(type: "integer", nullable: true),
                    StateId = table.Column<int>(type: "integer", nullable: true),
                    SpcmTarget = table.Column<int>(type: "integer", nullable: true),
                    SoilSampleTarget = table.Column<int>(type: "integer", nullable: true),
                    UreaTarget = table.Column<int>(type: "integer", nullable: true),
                    DapTarget = table.Column<int>(type: "integer", nullable: true),
                    NpsTarget = table.Column<int>(type: "integer", nullable: true),
                    OthersTarget = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReviewRemarks = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JmdoTaskAllocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JmdoTaskAllocationDealers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AllocationId = table.Column<int>(type: "integer", nullable: false),
                    SubDealerId = table.Column<int>(type: "integer", nullable: false),
                    DealerName = table.Column<string>(type: "text", nullable: false),
                    DealerCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JmdoTaskAllocationDealers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JmdoTaskAllocationDealers_JmdoTaskAllocations_AllocationId",
                        column: x => x.AllocationId,
                        principalTable: "JmdoTaskAllocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JmdoTaskAllocationPrograms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AllocationId = table.Column<int>(type: "integer", nullable: false),
                    Csr1Id = table.Column<int>(type: "integer", nullable: false),
                    ProgramName = table.Column<string>(type: "text", nullable: false),
                    Budget = table.Column<decimal>(type: "numeric(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JmdoTaskAllocationPrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JmdoTaskAllocationPrograms_JmdoTaskAllocations_AllocationId",
                        column: x => x.AllocationId,
                        principalTable: "JmdoTaskAllocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // NOTE: the InsertData("Pages", ...) block EF scaffolded here is deliberately
            // dropped - its reflection-based IDs (109-111) collide with already-seeded rows
            // (Metrics/AnnualBudgeting) for unrelated, pre-existing reasons. FieldDashboard/
            // MarkAttendance/TasksAllocation are gated by LoginState.UserRole today, not
            // PagePermission, so nothing depends on these Page rows existing yet.

            migrationBuilder.CreateIndex(
                name: "IX_JmdoTaskAllocationDealers_AllocationId",
                table: "JmdoTaskAllocationDealers",
                column: "AllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_JmdoTaskAllocationPrograms_AllocationId",
                table: "JmdoTaskAllocationPrograms",
                column: "AllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_JmdoTaskAllocations_AllocationDate",
                table: "JmdoTaskAllocations",
                column: "AllocationDate");

            migrationBuilder.CreateIndex(
                name: "IX_JmdoTaskAllocations_Status",
                table: "JmdoTaskAllocations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JmdoTaskAllocations_SubmittedByUserId",
                table: "JmdoTaskAllocations",
                column: "SubmittedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JmdoTaskAllocationDealers");

            migrationBuilder.DropTable(
                name: "JmdoTaskAllocationPrograms");

            migrationBuilder.DropTable(
                name: "JmdoTaskAllocations");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "BudgetProgramMains");

            migrationBuilder.AlterColumn<string>(
                name: "ValidateBy",
                table: "BudgetProgramMains",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "BudgetProgramMains",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApprovedBy",
                table: "BudgetProgramMains",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
