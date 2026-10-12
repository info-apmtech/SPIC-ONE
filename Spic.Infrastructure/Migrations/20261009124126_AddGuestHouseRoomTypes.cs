using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestHouseRoomTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoomTypeId",
                table: "GuestHouseRooms",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GuestHouseRoomTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestHouseRoomTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseRooms_RoomTypeId",
                table: "GuestHouseRooms",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseRoomTypes_Name",
                table: "GuestHouseRoomTypes",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_GuestHouseRooms_GuestHouseRoomTypes_RoomTypeId",
                table: "GuestHouseRooms",
                column: "RoomTypeId",
                principalTable: "GuestHouseRoomTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuestHouseRooms_GuestHouseRoomTypes_RoomTypeId",
                table: "GuestHouseRooms");

            migrationBuilder.DropTable(
                name: "GuestHouseRoomTypes");

            migrationBuilder.DropIndex(
                name: "IX_GuestHouseRooms_RoomTypeId",
                table: "GuestHouseRooms");

            migrationBuilder.DropColumn(
                name: "RoomTypeId",
                table: "GuestHouseRooms");
        }
    }
}
