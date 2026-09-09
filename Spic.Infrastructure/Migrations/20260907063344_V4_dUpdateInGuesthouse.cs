using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V4_dUpdateInGuesthouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuestHouseImage_GuestHouses_GuestHouseId",
                table: "GuestHouseImage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GuestHouseImage",
                table: "GuestHouseImage");

            migrationBuilder.RenameTable(
                name: "GuestHouseImage",
                newName: "GuestHouseImages");

            migrationBuilder.RenameIndex(
                name: "IX_GuestHouseImage_GuestHouseId",
                table: "GuestHouseImages",
                newName: "IX_GuestHouseImages_GuestHouseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuestHouseImages",
                table: "GuestHouseImages",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GuestHouseImages_GuestHouses_GuestHouseId",
                table: "GuestHouseImages",
                column: "GuestHouseId",
                principalTable: "GuestHouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuestHouseImages_GuestHouses_GuestHouseId",
                table: "GuestHouseImages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GuestHouseImages",
                table: "GuestHouseImages");

            migrationBuilder.RenameTable(
                name: "GuestHouseImages",
                newName: "GuestHouseImage");

            migrationBuilder.RenameIndex(
                name: "IX_GuestHouseImages_GuestHouseId",
                table: "GuestHouseImage",
                newName: "IX_GuestHouseImage_GuestHouseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuestHouseImage",
                table: "GuestHouseImage",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GuestHouseImage_GuestHouses_GuestHouseId",
                table: "GuestHouseImage",
                column: "GuestHouseId",
                principalTable: "GuestHouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
