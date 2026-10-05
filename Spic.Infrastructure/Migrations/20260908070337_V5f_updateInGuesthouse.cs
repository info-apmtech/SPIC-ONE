using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V5f_updateInGuesthouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualCheckInAt",
                table: "GuestHouseBookings",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualCheckOutAt",
                table: "GuestHouseBookings",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GuestHouseBills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BillNumber = table.Column<string>(type: "text", nullable: true),
                    GuestHouseBookingId = table.Column<int>(type: "integer", nullable: false),
                    BillDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BookingReference = table.Column<string>(type: "text", nullable: true),
                    GuestName = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    GuestHouseName = table.Column<string>(type: "text", nullable: true),
                    RoomNumber = table.Column<string>(type: "text", nullable: true),
                    RoomType = table.Column<string>(type: "text", nullable: true),
                    CheckInAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CheckOutAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NumberOfNights = table.Column<int>(type: "integer", nullable: true),
                    NumberOfRooms = table.Column<int>(type: "integer", nullable: true),
                    NumberOfPersons = table.Column<int>(type: "integer", nullable: true),
                    ExtraBeds = table.Column<int>(type: "integer", nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric", nullable: false),
                    CgstAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    SgstAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount = table.Column<decimal>(type: "numeric", nullable: false),
                    AdvancePayment = table.Column<decimal>(type: "numeric", nullable: false),
                    RoundOff = table.Column<decimal>(type: "numeric", nullable: false),
                    BalanceAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    PaymentStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestHouseBills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestHouseBills_GuestHouseBookings_GuestHouseBookingId",
                        column: x => x.GuestHouseBookingId,
                        principalTable: "GuestHouseBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestHouseBillLineItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuestHouseBillId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Rate = table.Column<decimal>(type: "numeric", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    CgstPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    SgstPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    CgstAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    SgstAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestHouseBillLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestHouseBillLineItems_GuestHouseBills_GuestHouseBillId",
                        column: x => x.GuestHouseBillId,
                        principalTable: "GuestHouseBills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseBillLineItems_GuestHouseBillId",
                table: "GuestHouseBillLineItems",
                column: "GuestHouseBillId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseBills_GuestHouseBookingId",
                table: "GuestHouseBills",
                column: "GuestHouseBookingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestHouseBillLineItems");

            migrationBuilder.DropTable(
                name: "GuestHouseBills");

            migrationBuilder.DropColumn(
                name: "ActualCheckInAt",
                table: "GuestHouseBookings");

            migrationBuilder.DropColumn(
                name: "ActualCheckOutAt",
                table: "GuestHouseBookings");
        }
    }
}
