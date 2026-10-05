using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSalesWholesalersSchemaAckThrough : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AckThrough",
                table: "SalesWholesalers");

            migrationBuilder.AddColumn<int>(
                name: "AckThroughId",
                table: "SalesWholesalers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AckThroughs",
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
                    table.PrimaryKey("PK_AckThroughs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AckThroughs");

            migrationBuilder.DropColumn(
                name: "AckThroughId",
                table: "SalesWholesalers");

            migrationBuilder.AddColumn<string>(
                name: "AckThrough",
                table: "SalesWholesalers",
                type: "text",
                nullable: true);
        }
    }
}
