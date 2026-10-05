using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v3welfareupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastResubmittedAt",
                table: "WelfareApplications",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastResubmittedBy",
                table: "WelfareApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResubmissionCount",
                table: "WelfareApplications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "WelfareApplicationActionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WelfareApplicationId = table.Column<int>(type: "integer", nullable: false),
                    ActorLevel = table.Column<int>(type: "integer", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    ActorName = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WelfareApplicationActionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WelfareApplicationActionLogs_WelfareApplications_WelfareApp~",
                        column: x => x.WelfareApplicationId,
                        principalTable: "WelfareApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WelfareApplicationActionLogs_WelfareApplicationId",
                table: "WelfareApplicationActionLogs",
                column: "WelfareApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WelfareApplicationActionLogs");

            migrationBuilder.DropColumn(
                name: "LastResubmittedAt",
                table: "WelfareApplications");

            migrationBuilder.DropColumn(
                name: "LastResubmittedBy",
                table: "WelfareApplications");

            migrationBuilder.DropColumn(
                name: "ResubmissionCount",
                table: "WelfareApplications");
        }
    }
}
