using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v4c_GuestHouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuestHouses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestHouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuestHouseCancellationPolicy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuestHouseId = table.Column<int>(type: "integer", nullable: false),
                    PolicyName = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    HoursBeforeCheckIn = table.Column<int>(type: "integer", nullable: true),
                    RefundPercentage = table.Column<decimal>(type: "numeric", nullable: true),
                    CancellationChargePercentage = table.Column<decimal>(type: "numeric", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestHouseCancellationPolicy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestHouseCancellationPolicy_GuestHouses_GuestHouseId",
                        column: x => x.GuestHouseId,
                        principalTable: "GuestHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestHouseImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuestHouseId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    FilePath = table.Column<string>(type: "text", nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestHouseImage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestHouseImage_GuestHouses_GuestHouseId",
                        column: x => x.GuestHouseId,
                        principalTable: "GuestHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestHouseRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuestHouseId = table.Column<int>(type: "integer", nullable: false),
                    RoomType = table.Column<string>(type: "text", nullable: true),
                    RoomNumber = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: true),
                    NumberOfAdults = table.Column<int>(type: "integer", nullable: true),
                    NumberOfChildren = table.Column<int>(type: "integer", nullable: true),
                    PricePerNight = table.Column<decimal>(type: "numeric", nullable: false),
                    ExtraCotPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    AvailableQuantity = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestHouseRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestHouseRooms_GuestHouses_GuestHouseId",
                        column: x => x.GuestHouseId,
                        principalTable: "GuestHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestHouseBooking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookingReference = table.Column<string>(type: "text", nullable: true),
                    GuestHouseId = table.Column<int>(type: "integer", nullable: false),
                    GuestHouseRoomId = table.Column<int>(type: "integer", nullable: false),
                    CheckInDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CheckInTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    CheckOutDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CheckOutTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    NumberOfNights = table.Column<int>(type: "integer", nullable: true),
                    NumberOfPersons = table.Column<int>(type: "integer", nullable: true),
                    NumberOfAdults = table.Column<int>(type: "integer", nullable: true),
                    NumberOfChildren = table.Column<int>(type: "integer", nullable: true),
                    ExtraCotQuantity = table.Column<int>(type: "integer", nullable: true),
                    RoomPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    ExtraCotPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    SubTotal = table.Column<decimal>(type: "numeric", nullable: true),
                    TaxAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    BookingStatus = table.Column<int>(type: "integer", nullable: false),
                    PaymentStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestHouseBooking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestHouseBooking_GuestHouseRooms_GuestHouseRoomId",
                        column: x => x.GuestHouseRoomId,
                        principalTable: "GuestHouseRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuestHouseBooking_GuestHouses_GuestHouseId",
                        column: x => x.GuestHouseId,
                        principalTable: "GuestHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestHouseRoomAmenity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuestHouseRoomId = table.Column<int>(type: "integer", nullable: false),
                    AmenityName = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestHouseRoomAmenity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestHouseRoomAmenity_GuestHouseRooms_GuestHouseRoomId",
                        column: x => x.GuestHouseRoomId,
                        principalTable: "GuestHouseRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestHouseRoomAvailabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuestHouseRoomId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TotalRooms = table.Column<int>(type: "integer", nullable: false),
                    AvailableRooms = table.Column<int>(type: "integer", nullable: false),
                    BookedRooms = table.Column<int>(type: "integer", nullable: false),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestHouseRoomAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestHouseRoomAvailabilities_GuestHouseRooms_GuestHouseRoom~",
                        column: x => x.GuestHouseRoomId,
                        principalTable: "GuestHouseRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestHouseRoomImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuestHouseRoomId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    FilePath = table.Column<string>(type: "text", nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestHouseRoomImage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestHouseRoomImage_GuestHouseRooms_GuestHouseRoomId",
                        column: x => x.GuestHouseRoomId,
                        principalTable: "GuestHouseRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestHouseBookingCancellation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CancellationReference = table.Column<string>(type: "text", nullable: true),
                    GuestHouseBookingId = table.Column<int>(type: "integer", nullable: false),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    CancelledBy = table.Column<string>(type: "text", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CancellationCharge = table.Column<decimal>(type: "numeric", nullable: true),
                    TaxAdjustment = table.Column<decimal>(type: "numeric", nullable: true),
                    RefundAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    RefundMethod = table.Column<string>(type: "text", nullable: true),
                    RefundStatus = table.Column<int>(type: "integer", nullable: false),
                    EstimatedRefundDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestHouseBookingCancellation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestHouseBookingCancellation_GuestHouseBooking_GuestHouseB~",
                        column: x => x.GuestHouseBookingId,
                        principalTable: "GuestHouseBooking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestHouseBookingDocument",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuestHouseBookingId = table.Column<int>(type: "integer", nullable: false),
                    DocumentType = table.Column<string>(type: "text", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    FilePath = table.Column<string>(type: "text", nullable: true),
                    ContentType = table.Column<string>(type: "text", nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    UploadedBy = table.Column<string>(type: "text", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestHouseBookingDocument", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestHouseBookingDocument_GuestHouseBooking_GuestHouseBooki~",
                        column: x => x.GuestHouseBookingId,
                        principalTable: "GuestHouseBooking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestHouseBookingGuest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuestHouseBookingId = table.Column<int>(type: "integer", nullable: false),
                    EmployeeOrDealerCode = table.Column<string>(type: "text", nullable: true),
                    GuestName = table.Column<string>(type: "text", nullable: true),
                    CompanyName = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    AadhaarOrPassportNumber = table.Column<string>(type: "text", nullable: true),
                    Nationality = table.Column<string>(type: "text", nullable: true),
                    NumberOfPersons = table.Column<int>(type: "integer", nullable: true),
                    NumberOfAdults = table.Column<int>(type: "integer", nullable: true),
                    NumberOfChildren = table.Column<int>(type: "integer", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestHouseBookingGuest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestHouseBookingGuest_GuestHouseBooking_GuestHouseBookingId",
                        column: x => x.GuestHouseBookingId,
                        principalTable: "GuestHouseBooking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestHouseBookingPayment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuestHouseBookingId = table.Column<int>(type: "integer", nullable: false),
                    PaymentReference = table.Column<string>(type: "text", nullable: true),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    PaymentStatus = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    TransactionId = table.Column<string>(type: "text", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GatewayResponse = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestHouseBookingPayment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestHouseBookingPayment_GuestHouseBooking_GuestHouseBookin~",
                        column: x => x.GuestHouseBookingId,
                        principalTable: "GuestHouseBooking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestHouseBookingRefund",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuestHouseBookingId = table.Column<int>(type: "integer", nullable: false),
                    GuestHouseBookingCancellationId = table.Column<int>(type: "integer", nullable: true),
                    OriginalAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    CancellationCharge = table.Column<decimal>(type: "numeric", nullable: true),
                    TaxAdjustment = table.Column<decimal>(type: "numeric", nullable: true),
                    RefundAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    RefundMethod = table.Column<string>(type: "text", nullable: true),
                    RefundStatus = table.Column<int>(type: "integer", nullable: false),
                    RefundReference = table.Column<string>(type: "text", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestHouseBookingRefund", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestHouseBookingRefund_GuestHouseBookingCancellation_Guest~",
                        column: x => x.GuestHouseBookingCancellationId,
                        principalTable: "GuestHouseBookingCancellation",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GuestHouseBookingRefund_GuestHouseBooking_GuestHouseBooking~",
                        column: x => x.GuestHouseBookingId,
                        principalTable: "GuestHouseBooking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseBooking_GuestHouseId",
                table: "GuestHouseBooking",
                column: "GuestHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseBooking_GuestHouseRoomId",
                table: "GuestHouseBooking",
                column: "GuestHouseRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseBookingCancellation_GuestHouseBookingId",
                table: "GuestHouseBookingCancellation",
                column: "GuestHouseBookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseBookingDocument_GuestHouseBookingId",
                table: "GuestHouseBookingDocument",
                column: "GuestHouseBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseBookingGuest_GuestHouseBookingId",
                table: "GuestHouseBookingGuest",
                column: "GuestHouseBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseBookingPayment_GuestHouseBookingId",
                table: "GuestHouseBookingPayment",
                column: "GuestHouseBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseBookingRefund_GuestHouseBookingCancellationId",
                table: "GuestHouseBookingRefund",
                column: "GuestHouseBookingCancellationId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseBookingRefund_GuestHouseBookingId",
                table: "GuestHouseBookingRefund",
                column: "GuestHouseBookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseCancellationPolicy_GuestHouseId",
                table: "GuestHouseCancellationPolicy",
                column: "GuestHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseImage_GuestHouseId",
                table: "GuestHouseImage",
                column: "GuestHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseRoomAmenity_GuestHouseRoomId",
                table: "GuestHouseRoomAmenity",
                column: "GuestHouseRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseRoomAvailabilities_GuestHouseRoomId",
                table: "GuestHouseRoomAvailabilities",
                column: "GuestHouseRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseRoomImage_GuestHouseRoomId",
                table: "GuestHouseRoomImage",
                column: "GuestHouseRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestHouseRooms_GuestHouseId",
                table: "GuestHouseRooms",
                column: "GuestHouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestHouseBookingDocument");

            migrationBuilder.DropTable(
                name: "GuestHouseBookingGuest");

            migrationBuilder.DropTable(
                name: "GuestHouseBookingPayment");

            migrationBuilder.DropTable(
                name: "GuestHouseBookingRefund");

            migrationBuilder.DropTable(
                name: "GuestHouseCancellationPolicy");

            migrationBuilder.DropTable(
                name: "GuestHouseImage");

            migrationBuilder.DropTable(
                name: "GuestHouseRoomAmenity");

            migrationBuilder.DropTable(
                name: "GuestHouseRoomAvailabilities");

            migrationBuilder.DropTable(
                name: "GuestHouseRoomImage");

            migrationBuilder.DropTable(
                name: "GuestHouseBookingCancellation");

            migrationBuilder.DropTable(
                name: "GuestHouseBooking");

            migrationBuilder.DropTable(
                name: "GuestHouseRooms");

            migrationBuilder.DropTable(
                name: "GuestHouses");
        }
    }
}
