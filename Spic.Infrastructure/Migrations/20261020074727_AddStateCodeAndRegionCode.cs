using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStateCodeAndRegionCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StateCode",
                table: "States",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegionCode",
                table: "Regions",
                type: "text",
                nullable: true);

            //migrationBuilder.CreateTable(
            //    name: "CSR2FocusProduct",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "integer", nullable: false)
            //            .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            //        CSR2Id = table.Column<int>(type: "integer", nullable: false),
            //        GroupNumber = table.Column<int>(type: "integer", nullable: false),
            //        CropId = table.Column<int>(type: "integer", nullable: false),
            //        ProductId = table.Column<int>(type: "integer", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_CSR2FocusProduct", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_CSR2FocusProduct_CSR2s_CSR2Id",
            //            column: x => x.CSR2Id,
            //            principalTable: "CSR2s",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    });

            //migrationBuilder.CreateIndex(
            //    name: "IX_CSR2FocusProduct_CSR2Id",
            //    table: "CSR2FocusProduct",
            //    column: "CSR2Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CSR2FocusProduct");

            migrationBuilder.DropColumn(
                name: "StateCode",
                table: "States");

            migrationBuilder.DropColumn(
                name: "RegionCode",
                table: "Regions");
        }
    }
}
