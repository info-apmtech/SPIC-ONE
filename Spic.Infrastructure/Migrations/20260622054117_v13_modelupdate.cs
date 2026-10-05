using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v13_modelupdate : Migration
    {
        /// <inheritdoc />
        //protected override void Up(MigrationBuilder migrationBuilder)
        //{
        //    migrationBuilder.RenameColumn(
        //        name: "productId",
        //        table: "Products",
        //        newName: "ProductGroupId");

        //    migrationBuilder.AddColumn<DateTime>(
        //        name: "CreatedAt",
        //        table: "Designations",
        //        type: "timestamp without time zone",
        //        nullable: false,
        //        defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        //    migrationBuilder.AddColumn<string>(
        //        name: "CreatedBy",
        //        table: "Designations",
        //        type: "text",
        //        nullable: true);

        //    migrationBuilder.AddColumn<bool>(
        //        name: "IsActive",
        //        table: "Designations",
        //        type: "boolean",
        //        nullable: false,
        //        defaultValue: false);

        //    migrationBuilder.AddColumn<string>(
        //        name: "RoleAccess",
        //        table: "Designations",
        //        type: "text",
        //        nullable: true);

        //    migrationBuilder.AddColumn<DateTime>(
        //        name: "UpdatedAt",
        //        table: "Designations",
        //        type: "timestamp without time zone",
        //        nullable: false,
        //        defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        //    migrationBuilder.AddColumn<string>(
        //        name: "UpdatedBy",
        //        table: "Designations",
        //        type: "text",
        //        nullable: true);

        //    migrationBuilder.CreateIndex(
        //        name: "IX_Products_ProductGroupId",
        //        table: "Products",
        //        column: "ProductGroupId");

        //    migrationBuilder.AddForeignKey(
        //        name: "FK_Products_ProductGroups_ProductGroupId",
        //        table: "Products",
        //        column: "ProductGroupId",
        //        principalTable: "ProductGroups",
        //        principalColumn: "Id");
        //}
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "productId",
                table: "Products",
                newName: "ProductGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductGroupId",
                table: "Products",
                column: "ProductGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ProductGroups_ProductGroupId",
                table: "Products",
                column: "ProductGroupId",
                principalTable: "ProductGroups",
                principalColumn: "Id");
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_Products_ProductGroups_ProductGroupId",
            //    table: "Products");

            //migrationBuilder.DropIndex(
            //    name: "IX_Products_ProductGroupId",
            //    table: "Products");

            //migrationBuilder.DropColumn(
            //    name: "CreatedAt",
            //    table: "Designations");

            //migrationBuilder.DropColumn(
            //    name: "CreatedBy",
            //    table: "Designations");

            //migrationBuilder.DropColumn(
            //    name: "IsActive",
            //    table: "Designations");

            //migrationBuilder.DropColumn(
            //    name: "RoleAccess",
            //    table: "Designations");

            //migrationBuilder.DropColumn(
            //    name: "UpdatedAt",
            //    table: "Designations");

            //migrationBuilder.DropColumn(
            //    name: "UpdatedBy",
            //    table: "Designations");

            //migrationBuilder.RenameColumn(
            //    name: "ProductGroupId",
            //    table: "Products",
            //    newName: "productId");
            migrationBuilder.DropForeignKey(
    name: "FK_Products_ProductGroups_ProductGroupId",
    table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProductGroupId",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "ProductGroupId",
                table: "Products",
                newName: "productId");
        }
    }
}
