using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJmdoDealerVisitPostponeRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JmdoDealerVisitPostponeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerRowId = table.Column<int>(type: "integer", nullable: false),
                    OriginalAllocationId = table.Column<int>(type: "integer", nullable: false),
                    OriginalPlannedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    OriginalPlannedDay = table.Column<int>(type: "integer", nullable: true),
                    RequestedByUserId = table.Column<string>(type: "text", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReviewRemarks = table.Column<string>(type: "text", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ResultingAllocationId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JmdoDealerVisitPostponeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JmdoDealerVisitPostponeRequests_JmdoTaskAllocationDealers_D~",
                        column: x => x.DealerRowId,
                        principalTable: "JmdoTaskAllocationDealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JmdoDealerVisitPostponeRequests_DealerRowId",
                table: "JmdoDealerVisitPostponeRequests",
                column: "DealerRowId",
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_JmdoDealerVisitPostponeRequests_RequestedByUserId",
                table: "JmdoDealerVisitPostponeRequests",
                column: "RequestedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JmdoDealerVisitPostponeRequests");
        }
    }
}
