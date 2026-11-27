using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V35_FarmerPortalConnect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FarmerContactRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FarmerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    StaffUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    StaffRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Topic = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PreferredTime = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StaffNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmerContactRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FarmerContactRequests_FarmerUserId_CreatedAtUtc",
                table: "FarmerContactRequests",
                columns: new[] { "FarmerUserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FarmerContactRequests_StaffUserId_Status_CreatedAtUtc",
                table: "FarmerContactRequests",
                columns: new[] { "StaffUserId", "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FarmerContactRequests");
        }
    }
}
