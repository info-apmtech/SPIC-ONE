using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v4_warehouseupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RackPoints_Districts_DistrictId",
                table: "RackPoints");

            migrationBuilder.DropForeignKey(
                name: "FK_RackPoints_States_StateId",
                table: "RackPoints");

            migrationBuilder.AlterColumn<int>(
                name: "StateId",
                table: "Warehouses",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "DistrictId",
                table: "Warehouses",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "StateId",
                table: "RackPoints",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "DistrictId",
                table: "RackPoints",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_RackPoints_Districts_DistrictId",
                table: "RackPoints",
                column: "DistrictId",
                principalTable: "Districts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RackPoints_States_StateId",
                table: "RackPoints",
                column: "StateId",
                principalTable: "States",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RackPoints_Districts_DistrictId",
                table: "RackPoints");

            migrationBuilder.DropForeignKey(
                name: "FK_RackPoints_States_StateId",
                table: "RackPoints");

            migrationBuilder.AlterColumn<int>(
                name: "StateId",
                table: "Warehouses",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DistrictId",
                table: "Warehouses",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "StateId",
                table: "RackPoints",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DistrictId",
                table: "RackPoints",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RackPoints_Districts_DistrictId",
                table: "RackPoints",
                column: "DistrictId",
                principalTable: "Districts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RackPoints_States_StateId",
                table: "RackPoints",
                column: "StateId",
                principalTable: "States",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
