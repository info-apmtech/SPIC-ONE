using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    public partial class ConvertCSR1ProductCropAndProductIdsToArrays : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CSR1Products1
            migrationBuilder.AlterColumn<int[]>(
                name: "CropId",
                table: "CSR1Products1",
                type: "integer[]",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int[]>(
                name: "ProductId",
                table: "CSR1Products1",
                type: "integer[]",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");


            // CSR1Products2
            migrationBuilder.AlterColumn<int[]>(
                name: "CropId",
                table: "CSR1Products2",
                type: "integer[]",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int[]>(
                name: "ProductId",
                table: "CSR1Products2",
                type: "integer[]",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");


            // CSR1Products3
            migrationBuilder.AlterColumn<int[]>(
                name: "CropId",
                table: "CSR1Products3",
                type: "integer[]",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int[]>(
                name: "ProductId",
                table: "CSR1Products3",
                type: "integer[]",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // CSR1Products1
            migrationBuilder.AlterColumn<int>(
                name: "CropId",
                table: "CSR1Products1",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int[]),
                oldType: "integer[]");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "CSR1Products1",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int[]),
                oldType: "integer[]");


            // CSR1Products2
            migrationBuilder.AlterColumn<int>(
                name: "CropId",
                table: "CSR1Products2",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int[]),
                oldType: "integer[]");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "CSR1Products2",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int[]),
                oldType: "integer[]");


            // CSR1Products3
            migrationBuilder.AlterColumn<int>(
                name: "CropId",
                table: "CSR1Products3",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int[]),
                oldType: "integer[]");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "CSR1Products3",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int[]),
                oldType: "integer[]");
        }
    }
}