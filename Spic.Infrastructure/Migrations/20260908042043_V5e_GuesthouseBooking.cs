using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V5e_GuesthouseBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuestHouseBooking_GuestHouseRooms_GuestHouseRoomId",
                table: "GuestHouseBooking");

            migrationBuilder.DropForeignKey(
                name: "FK_GuestHouseBooking_GuestHouses_GuestHouseId",
                table: "GuestHouseBooking");

            migrationBuilder.DropForeignKey(
                name: "FK_GuestHouseBookingCancellation_GuestHouseBooking_GuestHouseB~",
                table: "GuestHouseBookingCancellation");

            migrationBuilder.DropForeignKey(
                name: "FK_GuestHouseBookingDocument_GuestHouseBooking_GuestHouseBooki~",
                table: "GuestHouseBookingDocument");

            migrationBuilder.DropForeignKey(
                name: "FK_GuestHouseBookingGuest_GuestHouseBooking_GuestHouseBookingId",
                table: "GuestHouseBookingGuest");

            migrationBuilder.DropForeignKey(
                name: "FK_GuestHouseBookingPayment_GuestHouseBooking_GuestHouseBookin~",
                table: "GuestHouseBookingPayment");

            migrationBuilder.DropForeignKey(
                name: "FK_GuestHouseBookingRefund_GuestHouseBooking_GuestHouseBooking~",
                table: "GuestHouseBookingRefund");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GuestHouseBookingPayment",
                table: "GuestHouseBookingPayment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GuestHouseBookingGuest",
                table: "GuestHouseBookingGuest");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GuestHouseBooking",
                table: "GuestHouseBooking");

            migrationBuilder.RenameTable(
                name: "GuestHouseBookingPayment",
                newName: "GuestHouseBookingPayments");

            migrationBuilder.RenameTable(
                name: "GuestHouseBookingGuest",
                newName: "GuestHouseBookingGuests");

            migrationBuilder.RenameTable(
                name: "GuestHouseBooking",
                newName: "GuestHouseBookings");

            migrationBuilder.RenameIndex(
                name: "IX_GuestHouseBookingPayment_GuestHouseBookingId",
                table: "GuestHouseBookingPayments",
                newName: "IX_GuestHouseBookingPayments_GuestHouseBookingId");

            migrationBuilder.RenameIndex(
                name: "IX_GuestHouseBookingGuest_GuestHouseBookingId",
                table: "GuestHouseBookingGuests",
                newName: "IX_GuestHouseBookingGuests_GuestHouseBookingId");

            migrationBuilder.RenameIndex(
                name: "IX_GuestHouseBooking_GuestHouseRoomId",
                table: "GuestHouseBookings",
                newName: "IX_GuestHouseBookings_GuestHouseRoomId");

            migrationBuilder.RenameIndex(
                name: "IX_GuestHouseBooking_GuestHouseId",
                table: "GuestHouseBookings",
                newName: "IX_GuestHouseBookings_GuestHouseId");

            migrationBuilder.AddColumn<int>(
                name: "NumberOfRooms",
                table: "GuestHouseBookings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuestHouseBookingPayments",
                table: "GuestHouseBookingPayments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuestHouseBookingGuests",
                table: "GuestHouseBookingGuests",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuestHouseBookings",
                table: "GuestHouseBookings",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GuestHouseBookingCancellation_GuestHouseBookings_GuestHouse~",
                table: "GuestHouseBookingCancellation",
                column: "GuestHouseBookingId",
                principalTable: "GuestHouseBookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GuestHouseBookingDocument_GuestHouseBookings_GuestHouseBook~",
                table: "GuestHouseBookingDocument",
                column: "GuestHouseBookingId",
                principalTable: "GuestHouseBookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GuestHouseBookingGuests_GuestHouseBookings_GuestHouseBookin~",
                table: "GuestHouseBookingGuests",
                column: "GuestHouseBookingId",
                principalTable: "GuestHouseBookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GuestHouseBookingPayments_GuestHouseBookings_GuestHouseBook~",
                table: "GuestHouseBookingPayments",
                column: "GuestHouseBookingId",
                principalTable: "GuestHouseBookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GuestHouseBookingRefund_GuestHouseBookings_GuestHouseBookin~",
                table: "GuestHouseBookingRefund",
                column: "GuestHouseBookingId",
                principalTable: "GuestHouseBookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GuestHouseBookings_GuestHouseRooms_GuestHouseRoomId",
                table: "GuestHouseBookings",
                column: "GuestHouseRoomId",
                principalTable: "GuestHouseRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GuestHouseBookings_GuestHouses_GuestHouseId",
                table: "GuestHouseBookings",
                column: "GuestHouseId",
                principalTable: "GuestHouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuestHouseBookingCancellation_GuestHouseBookings_GuestHouse~",
                table: "GuestHouseBookingCancellation");

            migrationBuilder.DropForeignKey(
                name: "FK_GuestHouseBookingDocument_GuestHouseBookings_GuestHouseBook~",
                table: "GuestHouseBookingDocument");

            migrationBuilder.DropForeignKey(
                name: "FK_GuestHouseBookingGuests_GuestHouseBookings_GuestHouseBookin~",
                table: "GuestHouseBookingGuests");

            migrationBuilder.DropForeignKey(
                name: "FK_GuestHouseBookingPayments_GuestHouseBookings_GuestHouseBook~",
                table: "GuestHouseBookingPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_GuestHouseBookingRefund_GuestHouseBookings_GuestHouseBookin~",
                table: "GuestHouseBookingRefund");

            migrationBuilder.DropForeignKey(
                name: "FK_GuestHouseBookings_GuestHouseRooms_GuestHouseRoomId",
                table: "GuestHouseBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_GuestHouseBookings_GuestHouses_GuestHouseId",
                table: "GuestHouseBookings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GuestHouseBookings",
                table: "GuestHouseBookings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GuestHouseBookingPayments",
                table: "GuestHouseBookingPayments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GuestHouseBookingGuests",
                table: "GuestHouseBookingGuests");

            migrationBuilder.DropColumn(
                name: "NumberOfRooms",
                table: "GuestHouseBookings");

            migrationBuilder.RenameTable(
                name: "GuestHouseBookings",
                newName: "GuestHouseBooking");

            migrationBuilder.RenameTable(
                name: "GuestHouseBookingPayments",
                newName: "GuestHouseBookingPayment");

            migrationBuilder.RenameTable(
                name: "GuestHouseBookingGuests",
                newName: "GuestHouseBookingGuest");

            migrationBuilder.RenameIndex(
                name: "IX_GuestHouseBookings_GuestHouseRoomId",
                table: "GuestHouseBooking",
                newName: "IX_GuestHouseBooking_GuestHouseRoomId");

            migrationBuilder.RenameIndex(
                name: "IX_GuestHouseBookings_GuestHouseId",
                table: "GuestHouseBooking",
                newName: "IX_GuestHouseBooking_GuestHouseId");

            migrationBuilder.RenameIndex(
                name: "IX_GuestHouseBookingPayments_GuestHouseBookingId",
                table: "GuestHouseBookingPayment",
                newName: "IX_GuestHouseBookingPayment_GuestHouseBookingId");

            migrationBuilder.RenameIndex(
                name: "IX_GuestHouseBookingGuests_GuestHouseBookingId",
                table: "GuestHouseBookingGuest",
                newName: "IX_GuestHouseBookingGuest_GuestHouseBookingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuestHouseBooking",
                table: "GuestHouseBooking",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuestHouseBookingPayment",
                table: "GuestHouseBookingPayment",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuestHouseBookingGuest",
                table: "GuestHouseBookingGuest",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GuestHouseBooking_GuestHouseRooms_GuestHouseRoomId",
                table: "GuestHouseBooking",
                column: "GuestHouseRoomId",
                principalTable: "GuestHouseRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GuestHouseBooking_GuestHouses_GuestHouseId",
                table: "GuestHouseBooking",
                column: "GuestHouseId",
                principalTable: "GuestHouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GuestHouseBookingCancellation_GuestHouseBooking_GuestHouseB~",
                table: "GuestHouseBookingCancellation",
                column: "GuestHouseBookingId",
                principalTable: "GuestHouseBooking",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GuestHouseBookingDocument_GuestHouseBooking_GuestHouseBooki~",
                table: "GuestHouseBookingDocument",
                column: "GuestHouseBookingId",
                principalTable: "GuestHouseBooking",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GuestHouseBookingGuest_GuestHouseBooking_GuestHouseBookingId",
                table: "GuestHouseBookingGuest",
                column: "GuestHouseBookingId",
                principalTable: "GuestHouseBooking",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GuestHouseBookingPayment_GuestHouseBooking_GuestHouseBookin~",
                table: "GuestHouseBookingPayment",
                column: "GuestHouseBookingId",
                principalTable: "GuestHouseBooking",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GuestHouseBookingRefund_GuestHouseBooking_GuestHouseBooking~",
                table: "GuestHouseBookingRefund",
                column: "GuestHouseBookingId",
                principalTable: "GuestHouseBooking",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
