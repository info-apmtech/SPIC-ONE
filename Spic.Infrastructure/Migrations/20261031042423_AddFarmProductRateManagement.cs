using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFarmProductRateManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FarmProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Group = table.Column<int>(type: "integer", nullable: false),
                    Uom = table.Column<string>(type: "text", nullable: false),
                    CurrentRate = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FarmProductRateRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestType = table.Column<int>(type: "integer", nullable: false),
                    FarmProductId = table.Column<int>(type: "integer", nullable: true),
                    ProposedProductName = table.Column<string>(type: "text", nullable: true),
                    ProposedGroup = table.Column<int>(type: "integer", nullable: true),
                    ProposedUom = table.Column<string>(type: "text", nullable: false),
                    CurrentRateAtRequestTime = table.Column<decimal>(type: "numeric", nullable: true),
                    RequestedRate = table.Column<decimal>(type: "numeric", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedBy = table.Column<string>(type: "text", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ApprovedPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    ApprovedUom = table.Column<string>(type: "text", nullable: true),
                    AdminRemarks = table.Column<string>(type: "text", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    DecidedBy = table.Column<string>(type: "text", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmProductRateRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmProductRateRequests_FarmProducts_FarmProductId",
                        column: x => x.FarmProductId,
                        principalTable: "FarmProducts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FarmProductRateRequests_FarmProductId",
                table: "FarmProductRateRequests",
                column: "FarmProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FarmProductRateRequests");

            migrationBuilder.DropTable(
                name: "FarmProducts");
        }
    }
}
