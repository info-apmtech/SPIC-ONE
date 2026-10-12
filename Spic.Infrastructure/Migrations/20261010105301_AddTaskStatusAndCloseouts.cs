using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskStatusAndCloseouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 2026-10-11 (pre-deploy dry run): the scaffolded InsertData / DeleteData of "Pages" rows by
            // enum-index id were removed - those ids are already taken on the live site (v19 / v20 insert
            // by Key with generated ids) and would fail with a duplicate key. The page rows are inserted
            // by Key in v22PageCatalogueNoSeedAndMenuKeys.
            migrationBuilder.AddColumn<int>(
                name: "PosLiquidationStatus",
                table: "JmdoTaskAllocations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SoilSampleStatus",
                table: "JmdoTaskAllocations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SpcmStatus",
                table: "JmdoTaskAllocations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "JmdoTaskAllocationPrograms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "JmdoTaskAllocationDealers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "JmdoTaskCloseouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AllocationId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    SubDealerId = table.Column<int>(type: "integer", nullable: true),
                    Csr1Id = table.Column<int>(type: "integer", nullable: true),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    AchievedTarget = table.Column<int>(type: "integer", nullable: true),
                    Urea = table.Column<int>(type: "integer", nullable: true),
                    Nps = table.Column<int>(type: "integer", nullable: true),
                    Dap = table.Column<int>(type: "integer", nullable: true),
                    Other = table.Column<int>(type: "integer", nullable: true),
                    FilePath = table.Column<string>(type: "text", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JmdoTaskCloseouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JmdoTaskCloseouts_JmdoTaskAllocations_AllocationId",
                        column: x => x.AllocationId,
                        principalTable: "JmdoTaskAllocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JmdoTaskCloseoutProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CloseoutId = table.Column<int>(type: "integer", nullable: false),
                    ProductGroupName = table.Column<string>(type: "text", nullable: false),
                    ProductName = table.Column<string>(type: "text", nullable: false),
                    StockText = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JmdoTaskCloseoutProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JmdoTaskCloseoutProducts_JmdoTaskCloseouts_CloseoutId",
                        column: x => x.CloseoutId,
                        principalTable: "JmdoTaskCloseouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // The "Pages" page-permission catalog is seeded from the live PagePermission
            // enum's current order (see AppDbContext.OnModelCreating) - these 11 rows
            // (ProgramMaster..GHAdmin) are enum members added since the catalog was last
            // migrated and are genuinely missing from the live DB (verified directly),
            // not pre-existing drift. GuestHouseRoomTypes/RoomTypeId (also part of the
            // current model) is NOT in this migration - it's already shipped by the
            // separate, already-applied 20261009124126_AddGuestHouseRoomTypes migration.

            migrationBuilder.CreateIndex(
                name: "IX_JmdoTaskCloseoutProducts_CloseoutId",
                table: "JmdoTaskCloseoutProducts",
                column: "CloseoutId");

            migrationBuilder.CreateIndex(
                name: "IX_JmdoTaskCloseouts_AllocationId",
                table: "JmdoTaskCloseouts",
                column: "AllocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JmdoTaskCloseoutProducts");

            migrationBuilder.DropTable(
                name: "JmdoTaskCloseouts");

            migrationBuilder.DropColumn(
                name: "PosLiquidationStatus",
                table: "JmdoTaskAllocations");

            migrationBuilder.DropColumn(
                name: "SoilSampleStatus",
                table: "JmdoTaskAllocations");

            migrationBuilder.DropColumn(
                name: "SpcmStatus",
                table: "JmdoTaskAllocations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "JmdoTaskAllocationPrograms");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "JmdoTaskAllocationDealers");
        }
    }
}
