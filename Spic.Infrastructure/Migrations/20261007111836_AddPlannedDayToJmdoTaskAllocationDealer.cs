using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlannedDayToJmdoTaskAllocationDealer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 2026-10-11 (pre-deploy dry run): the scaffolded InsertData / DeleteData of "Pages" rows by
            // enum-index id were removed - those ids are already taken on the live site (v19 / v20 insert
            // by Key with generated ids) and would fail with a duplicate key. The page rows are inserted
            // by Key in v22PageCatalogueNoSeedAndMenuKeys.
            migrationBuilder.AddColumn<int>(
                name: "PlannedDay",
                table: "JmdoTaskAllocationDealers",
                type: "integer",
                nullable: true);

            // JMDOAttendances/JMDOTrackingSessions/JMDOTrackingPoints (+ indexes) and Pages rows
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropColumn(
                name: "PlannedDay",
                table: "JmdoTaskAllocationDealers");
        }
    }
}
