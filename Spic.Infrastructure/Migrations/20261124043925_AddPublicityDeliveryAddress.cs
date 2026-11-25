using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicityDeliveryAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryAddressId",
                table: "PublicityDispatches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PublicityDeliveryAddresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AddressLabel = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactPerson = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    MobileNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    AddressLine2 = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Landmark = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    StateId = table.Column<int>(type: "integer", nullable: false),
                    DistrictId = table.Column<int>(type: "integer", nullable: true),
                    City = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PinCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicityDeliveryAddresses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicityDispatches_DeliveryAddressId",
                table: "PublicityDispatches",
                column: "DeliveryAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicityDeliveryAddresses_IsActive",
                table: "PublicityDeliveryAddresses",
                column: "IsActive");

            migrationBuilder.AddForeignKey(
                name: "FK_PublicityDispatches_PublicityDeliveryAddresses_DeliveryAddr~",
                table: "PublicityDispatches",
                column: "DeliveryAddressId",
                principalTable: "PublicityDeliveryAddresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PublicityDispatches_PublicityDeliveryAddresses_DeliveryAddr~",
                table: "PublicityDispatches");

            migrationBuilder.DropTable(
                name: "PublicityDeliveryAddresses");

            migrationBuilder.DropIndex(
                name: "IX_PublicityDispatches_DeliveryAddressId",
                table: "PublicityDispatches");

            migrationBuilder.DropColumn(
                name: "DeliveryAddressId",
                table: "PublicityDispatches");
        }
    }
}
