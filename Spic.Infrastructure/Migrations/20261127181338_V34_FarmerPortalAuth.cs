using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V34_FarmerPortalAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FarmerOtpChallenges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Phone = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RequestIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmerOtpChallenges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FarmerProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    StateId = table.Column<int>(type: "integer", nullable: false),
                    DistrictId = table.Column<int>(type: "integer", nullable: false),
                    Village = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Taluk = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PinCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    PrimaryCrops = table.Column<string>(type: "character varying(700)", maxLength: 700, nullable: true),
                    LandAcres = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    IrrigationType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PreferredLanguage = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmerProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FarmerOtpChallenges_CreatedAtUtc",
                table: "FarmerOtpChallenges",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FarmerOtpChallenges_Phone_CreatedAtUtc",
                table: "FarmerOtpChallenges",
                columns: new[] { "Phone", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FarmerOtpChallenges_RequestIp_CreatedAtUtc",
                table: "FarmerOtpChallenges",
                columns: new[] { "RequestIp", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FarmerProfiles_DistrictId",
                table: "FarmerProfiles",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmerProfiles_UserId",
                table: "FarmerProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FarmerOtpChallenges");

            migrationBuilder.DropTable(
                name: "FarmerProfiles");
        }
    }
}
