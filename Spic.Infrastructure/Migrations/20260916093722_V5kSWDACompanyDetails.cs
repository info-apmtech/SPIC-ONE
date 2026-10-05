using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V5kSWDACompanyDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GstinNumber",
                table: "GuestHouseBookingGuests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SdwaCompanyId",
                table: "GuestHouseBookingGuests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "GuestHouseBills",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GstinNumber",
                table: "GuestHouseBills",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SdwaCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    ShortCode = table.Column<string>(type: "text", nullable: true),
                    GSTIN = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SdwaCompanies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SdwaCompanyGuestHouses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SdwaCompanyId = table.Column<int>(type: "integer", nullable: false),
                    GuestHouseId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SdwaCompanyGuestHouses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SdwaCompanyGuestHouses_GuestHouses_GuestHouseId",
                        column: x => x.GuestHouseId,
                        principalTable: "GuestHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SdwaCompanyGuestHouses_SdwaCompanies_SdwaCompanyId",
                        column: x => x.SdwaCompanyId,
                        principalTable: "SdwaCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseBookingGuests_SdwaCompanyId",
                table: "GuestHouseBookingGuests",
                column: "SdwaCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SdwaCompanyGuestHouses_GuestHouseId",
                table: "SdwaCompanyGuestHouses",
                column: "GuestHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_SdwaCompanyGuestHouses_SdwaCompanyId",
                table: "SdwaCompanyGuestHouses",
                column: "SdwaCompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_GuestHouseBookingGuests_SdwaCompanies_SdwaCompanyId",
                table: "GuestHouseBookingGuests",
                column: "SdwaCompanyId",
                principalTable: "SdwaCompanies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuestHouseBookingGuests_SdwaCompanies_SdwaCompanyId",
                table: "GuestHouseBookingGuests");

            migrationBuilder.DropTable(
                name: "SdwaCompanyGuestHouses");

            migrationBuilder.DropTable(
                name: "SdwaCompanies");

            migrationBuilder.DropIndex(
                name: "IX_GuestHouseBookingGuests_SdwaCompanyId",
                table: "GuestHouseBookingGuests");

            migrationBuilder.DropColumn(
                name: "GstinNumber",
                table: "GuestHouseBookingGuests");

            migrationBuilder.DropColumn(
                name: "SdwaCompanyId",
                table: "GuestHouseBookingGuests");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "GuestHouseBills");

            migrationBuilder.DropColumn(
                name: "GstinNumber",
                table: "GuestHouseBills");
        }
    }
}
