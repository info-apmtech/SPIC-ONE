using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJmdoTrackingOwnershipAndTaskLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JMDOTrackingSessions_UserId",
                table: "JMDOTrackingSessions");

            migrationBuilder.AddColumn<int>(
                name: "Ownership",
                table: "JMDOTrackingSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "JmdoTaskTrackingLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    AllocationId = table.Column<int>(type: "integer", nullable: false),
                    TaskKey = table.Column<string>(type: "text", nullable: false),
                    TrackingSessionId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JmdoTaskTrackingLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JmdoTaskTrackingLinks_JMDOTrackingSessions_TrackingSessionId",
                        column: x => x.TrackingSessionId,
                        principalTable: "JMDOTrackingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JMDOTrackingSessions_UserId",
                table: "JMDOTrackingSessions",
                column: "UserId",
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_JmdoTaskTrackingLinks_AllocationId_TaskKey",
                table: "JmdoTaskTrackingLinks",
                columns: new[] { "AllocationId", "TaskKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JmdoTaskTrackingLinks_TrackingSessionId",
                table: "JmdoTaskTrackingLinks",
                column: "TrackingSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JmdoTaskTrackingLinks");

            migrationBuilder.DropIndex(
                name: "IX_JMDOTrackingSessions_UserId",
                table: "JMDOTrackingSessions");

            migrationBuilder.DropColumn(
                name: "Ownership",
                table: "JMDOTrackingSessions");

            migrationBuilder.CreateIndex(
                name: "IX_JMDOTrackingSessions_UserId",
                table: "JMDOTrackingSessions",
                column: "UserId");
        }
    }
}
