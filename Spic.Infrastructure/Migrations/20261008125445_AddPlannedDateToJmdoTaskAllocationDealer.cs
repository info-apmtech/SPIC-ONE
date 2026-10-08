using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlannedDateToJmdoTaskAllocationDealer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The auto-generated diff also wanted to overwrite "Pages" rows
            // 112-124 with different Key/Name values and insert 125-134 - that
            // reflects the code's Pages seed data having drifted far from what's
            // actually live in the DB (e.g. live id=112 is "TaskMonitoring", but
            // the seed data assumes "AllocationRequestReview"). Applying that
            // blindly would corrupt real, in-use page-catalog rows, and it's
            // unrelated pre-existing drift (same situation as the PlannedDay
            // migration) - so only the genuine new column is kept here.
            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedDate",
                table: "JmdoTaskAllocationDealers",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlannedDate",
                table: "JmdoTaskAllocationDealers");
        }
    }
}
