using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicityRequestItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequestItemId",
                table: "PublicityFiles",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PublicityRequestItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    OriginalQuantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicityRequestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicityRequestItems_PublicityRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "PublicityRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PublicityDispatchItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DispatchId = table.Column<int>(type: "integer", nullable: false),
                    RequestItemId = table.Column<int>(type: "integer", nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    DispatchedQty = table.Column<int>(type: "integer", nullable: false),
                    ReceivedQty = table.Column<int>(type: "integer", nullable: false),
                    DamagedQty = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicityDispatchItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicityDispatchItems_PublicityDispatches_DispatchId",
                        column: x => x.DispatchId,
                        principalTable: "PublicityDispatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PublicityDispatchItems_PublicityRequestItems_RequestItemId",
                        column: x => x.RequestItemId,
                        principalTable: "PublicityRequestItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicityFiles_RequestItemId",
                table: "PublicityFiles",
                column: "RequestItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicityDispatchItems_DispatchId",
                table: "PublicityDispatchItems",
                column: "DispatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicityDispatchItems_RequestItemId",
                table: "PublicityDispatchItems",
                column: "RequestItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicityRequestItems_ProductId",
                table: "PublicityRequestItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicityRequestItems_RequestId",
                table: "PublicityRequestItems",
                column: "RequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicityDispatchItems");

            migrationBuilder.DropTable(
                name: "PublicityRequestItems");

            migrationBuilder.DropIndex(
                name: "IX_PublicityFiles_RequestItemId",
                table: "PublicityFiles");

            migrationBuilder.DropColumn(
                name: "RequestItemId",
                table: "PublicityFiles");
        }
    }
}
