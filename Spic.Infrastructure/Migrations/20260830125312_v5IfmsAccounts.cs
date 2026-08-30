using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v5IfmsAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConsumedByAccountKey",
                table: "IfmsOtpMessages",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountKey",
                table: "IfmsChallengeRequests",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "IfmsChallengeRequests",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccountsSucceeded",
                table: "IfmsAutomationRuns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AccountsTotal",
                table: "IfmsAutomationRuns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AccountKey",
                table: "IfmsAutomationReportRuns",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IfmsPasswordChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ChangedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    VerifiedByLogin = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IfmsPasswordChanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IfmsPortalAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountKey = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UserName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ProtectedPassword = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    PasswordSetAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PasswordRotationDays = table.Column<int>(type: "integer", nullable: false),
                    PasswordExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpiryWarningSentAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastLoginSucceeded = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginMessage = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    OtpMobileNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IfmsPortalAccounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IfmsPasswordChanges_AccountId_ChangedAt",
                table: "IfmsPasswordChanges",
                columns: new[] { "AccountId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IfmsPortalAccounts_AccountKey",
                table: "IfmsPortalAccounts",
                column: "AccountKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "IfmsPasswordChanges");

            migrationBuilder.DropTable(
                name: "IfmsPortalAccounts");

            migrationBuilder.DropColumn(
                name: "ConsumedByAccountKey",
                table: "IfmsOtpMessages");

            migrationBuilder.DropColumn(
                name: "AccountKey",
                table: "IfmsChallengeRequests");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "IfmsChallengeRequests");

            migrationBuilder.DropColumn(
                name: "AccountsSucceeded",
                table: "IfmsAutomationRuns");

            migrationBuilder.DropColumn(
                name: "AccountsTotal",
                table: "IfmsAutomationRuns");

            migrationBuilder.DropColumn(
                name: "AccountKey",
                table: "IfmsAutomationReportRuns");
        }
    }
}
