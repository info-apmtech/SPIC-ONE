using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations.Ifms
{
    /// <inheritdoc />
    public partial class AlertSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IfmsAlertSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SmtpHost = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SmtpPort = table.Column<int>(type: "integer", nullable: false),
                    UseStartTls = table.Column<bool>(type: "boolean", nullable: false),
                    UserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProtectedPassword = table.Column<string>(type: "text", nullable: true),
                    FromAddress = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FromName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ToAddresses = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CcAddresses = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    FailuresOnly = table.Column<bool>(type: "boolean", nullable: false),
                    AttachReports = table.Column<bool>(type: "boolean", nullable: false),
                    TestRequestedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TestRequestedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    LastTestAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastTestResult = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LastSentAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastSendResult = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IfmsAlertSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IfmsAlertSettings");
        }
    }
}
