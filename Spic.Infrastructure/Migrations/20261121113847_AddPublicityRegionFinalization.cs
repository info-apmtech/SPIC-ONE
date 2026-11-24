using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicityRegionFinalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SmdReviewedAt",
                table: "PublicityRequests",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmdReviewedByName",
                table: "PublicityRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PublicityRegionFinalizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StateId = table.Column<int>(type: "integer", nullable: false),
                    RegionId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FinalizedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FinalizedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicityRegionFinalizations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicityRegionFinalizations_StateId_RegionId",
                table: "PublicityRegionFinalizations",
                columns: new[] { "StateId", "RegionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicityRegionFinalizations");

            migrationBuilder.DropColumn(
                name: "SmdReviewedAt",
                table: "PublicityRequests");

            migrationBuilder.DropColumn(
                name: "SmdReviewedByName",
                table: "PublicityRequests");
        }
    }
}
