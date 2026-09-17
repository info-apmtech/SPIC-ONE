using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V5_hUpdateinGuestHouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuestHouseRoomAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuestHouseBookingId = table.Column<int>(type: "integer", nullable: false),
                    GuestHouseId = table.Column<int>(type: "integer", nullable: false),
                    GuestHouseRoomId = table.Column<int>(type: "integer", nullable: false),
                    RoomNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CheckInDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CheckOutDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AssignedBy = table.Column<string>(type: "text", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestHouseRoomAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestHouseRoomAllocations_GuestHouseBookings_GuestHouseBook~",
                        column: x => x.GuestHouseBookingId,
                        principalTable: "GuestHouseBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuestHouseRoomAllocations_GuestHouseRooms_GuestHouseRoomId",
                        column: x => x.GuestHouseRoomId,
                        principalTable: "GuestHouseRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuestHouseRoomAllocations_GuestHouses_GuestHouseId",
                        column: x => x.GuestHouseId,
                        principalTable: "GuestHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseRoomAllocations_GuestHouseBookingId",
                table: "GuestHouseRoomAllocations",
                column: "GuestHouseBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseRoomAllocations_GuestHouseId",
                table: "GuestHouseRoomAllocations",
                column: "GuestHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseRoomAllocations_GuestHouseRoomId_RoomNumber",
                table: "GuestHouseRoomAllocations",
                columns: new[] { "GuestHouseRoomId", "RoomNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestHouseRoomAllocations");
        }
    }
}
