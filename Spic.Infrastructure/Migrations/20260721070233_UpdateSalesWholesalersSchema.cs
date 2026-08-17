using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSalesWholesalersSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DealerNature",
                table: "SalesWholesalers");

            migrationBuilder.DropColumn(
                name: "Manufacturer",
                table: "SalesWholesalers");

            migrationBuilder.DropColumn(
                name: "Marketer",
                table: "SalesWholesalers");

            migrationBuilder.DropColumn(
                name: "TxnType",
                table: "SalesWholesalers");

            migrationBuilder.DropColumn(
                name: "WholesalerNature",
                table: "SalesWholesalers");

            migrationBuilder.RenameColumn(
                name: "DealerRegistrationId",
                table: "SalesWholesalers",
                newName: "WholesalerNatureId");

            migrationBuilder.Sql("ALTER TABLE \"SalesWholesalers\" ALTER COLUMN \"WholesalerId\" TYPE integer USING \"WholesalerId\"::integer;");
            
            migrationBuilder.Sql("ALTER TABLE \"SalesWholesalers\" ALTER COLUMN \"DealerId\" TYPE integer USING \"DealerId\"::integer;");

            migrationBuilder.AddColumn<int>(
                name: "DealerNatureId",
                table: "SalesWholesalers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IfmsWholesalerId",
                table: "SalesWholesalers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ManufacturerId",
                table: "SalesWholesalers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MarketerId",
                table: "SalesWholesalers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TxnTypeId",
                table: "SalesWholesalers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TxnTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TxnTypes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TxnTypes");

            migrationBuilder.DropColumn(
                name: "DealerNatureId",
                table: "SalesWholesalers");

            migrationBuilder.DropColumn(
                name: "IfmsWholesalerId",
                table: "SalesWholesalers");

            migrationBuilder.DropColumn(
                name: "ManufacturerId",
                table: "SalesWholesalers");

            migrationBuilder.DropColumn(
                name: "MarketerId",
                table: "SalesWholesalers");

            migrationBuilder.DropColumn(
                name: "TxnTypeId",
                table: "SalesWholesalers");

            migrationBuilder.RenameColumn(
                name: "WholesalerNatureId",
                table: "SalesWholesalers",
                newName: "DealerRegistrationId");

            migrationBuilder.AlterColumn<string>(
                name: "WholesalerId",
                table: "SalesWholesalers",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DealerId",
                table: "SalesWholesalers",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DealerNature",
                table: "SalesWholesalers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "SalesWholesalers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Marketer",
                table: "SalesWholesalers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TxnType",
                table: "SalesWholesalers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WholesalerNature",
                table: "SalesWholesalers",
                type: "text",
                nullable: true);
        }
    }
}
