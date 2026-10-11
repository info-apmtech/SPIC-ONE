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
            migrationBuilder.InsertData(
                table: "Pages",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "HasActions", "IsActive", "Key", "Module", "Name", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 125, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "ProgramMaster", null, "Program Master", 124, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 126, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "Activities", null, "Activities", 125, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 127, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "Farmers", null, "Farmers", 126, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 128, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "Alerts", null, "Alerts", 127, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 129, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "StartDocumentation", null, "Start Documentation", 128, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 130, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "DemoDetails", null, "Demo Details", 129, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 131, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "TreatmentDetails", null, "Treatment Details", 130, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 132, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "Treatment01", null, "Treatment01", 131, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 133, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "Treatment02", null, "Treatment02", 132, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 134, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "TreatmentDemoDetails", null, "Treatment Demo Details", 133, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 135, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "GHAdmin", null, "GHAdmin", 134, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" }
                });

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

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 125);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 126);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 127);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 128);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 129);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 130);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 131);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 132);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 133);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 134);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 135);

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
