using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v8AddCSR1Products : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FocusCrop1Id",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "FocusCrop2Id",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "FocusCrop3Id",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "FocusProduct1Id",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "FocusProduct2Id",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "FocusProduct3Id",
                table: "CSR1");

            migrationBuilder.CreateTable(
                name: "CSR1Products1",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CSR1Id = table.Column<int>(type: "integer", nullable: false),
                    CropId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CSR1Products1", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CSRProducts2",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CSR1Id = table.Column<int>(type: "integer", nullable: false),
                    CropId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CSRProducts2", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CSRProducts3",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CSR1Id = table.Column<int>(type: "integer", nullable: false),
                    CropId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CSRProducts3", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CSR1Products1");

            migrationBuilder.DropTable(
                name: "CSRProducts2");

            migrationBuilder.DropTable(
                name: "CSRProducts3");

            migrationBuilder.AddColumn<int>(
                name: "FocusCrop1Id",
                table: "CSR1",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FocusCrop2Id",
                table: "CSR1",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FocusCrop3Id",
                table: "CSR1",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FocusProduct1Id",
                table: "CSR1",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FocusProduct2Id",
                table: "CSR1",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FocusProduct3Id",
                table: "CSR1",
                type: "integer",
                nullable: true);
        }
    }
}
