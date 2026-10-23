using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    public partial class FixCSR1ProductArrays : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // =========================================================
            // CSR1Products1
            // =========================================================

            migrationBuilder.DropColumn(
                name: "CropId",
                table: "CSR1Products1");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "CSR1Products1");

            migrationBuilder.AddColumn<int[]>(
                name: "CropId",
                table: "CSR1Products1",
                type: "integer[]",
                nullable: false,
                defaultValue: Array.Empty<int>());

            migrationBuilder.AddColumn<int[]>(
                name: "ProductId",
                table: "CSR1Products1",
                type: "integer[]",
                nullable: false,
                defaultValue: Array.Empty<int>());


            // =========================================================
            // CSR1Products2
            // =========================================================

            migrationBuilder.DropColumn(
                name: "CropId",
                table: "CSR1Products2");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "CSR1Products2");

            migrationBuilder.AddColumn<int[]>(
                name: "CropId",
                table: "CSR1Products2",
                type: "integer[]",
                nullable: false,
                defaultValue: Array.Empty<int>());

            migrationBuilder.AddColumn<int[]>(
                name: "ProductId",
                table: "CSR1Products2",
                type: "integer[]",
                nullable: false,
                defaultValue: Array.Empty<int>());


            // =========================================================
            // CSR1Products3
            // =========================================================

            migrationBuilder.DropColumn(
                name: "CropId",
                table: "CSR1Products3");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "CSR1Products3");

            migrationBuilder.AddColumn<int[]>(
                name: "CropId",
                table: "CSR1Products3",
                type: "integer[]",
                nullable: false,
                defaultValue: Array.Empty<int>());

            migrationBuilder.AddColumn<int[]>(
                name: "ProductId",
                table: "CSR1Products3",
                type: "integer[]",
                nullable: false,
                defaultValue: Array.Empty<int>());
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // =========================================================
            // CSR1Products1
            // =========================================================

            migrationBuilder.DropColumn(
                name: "CropId",
                table: "CSR1Products1");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "CSR1Products1");

            migrationBuilder.AddColumn<int>(
                name: "CropId",
                table: "CSR1Products1",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "CSR1Products1",
                type: "integer",
                nullable: false,
                defaultValue: 0);


            // =========================================================
            // CSR1Products2
            // =========================================================

            migrationBuilder.DropColumn(
                name: "CropId",
                table: "CSR1Products2");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "CSR1Products2");

            migrationBuilder.AddColumn<int>(
                name: "CropId",
                table: "CSR1Products2",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "CSR1Products2",
                type: "integer",
                nullable: false,
                defaultValue: 0);


            // =========================================================
            // CSR1Products3
            // =========================================================

            migrationBuilder.DropColumn(
                name: "CropId",
                table: "CSR1Products3");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "CSR1Products3");

            migrationBuilder.AddColumn<int>(
                name: "CropId",
                table: "CSR1Products3",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "CSR1Products3",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}