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
            migrationBuilder.AddColumn<int>(
                name: "PlannedDay",
                table: "JmdoTaskAllocationDealers",
                type: "integer",
                nullable: true);

            // JMDOAttendances/JMDOTrackingSessions/JMDOTrackingPoints (+ indexes) and Pages rows
            // 109-110 already exist in the database from unrelated, previously-applied-out-of-band
            // work that was never recorded in __EFMigrationsHistory - only the still-missing Pages
            // rows (111-121) are inserted here so this migration stays scoped to PlannedDay plus
            // that one genuine gap, instead of re-creating tables/rows that are already there.
            migrationBuilder.InsertData(
                table: "Pages",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "HasActions", "IsActive", "Key", "Module", "Name", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 111, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "AllocationRequests", null, "Allocation Requests", 110, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 112, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "TaskMonitoring", null, "Task Monitoring", 111, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 113, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "PerformanceTracker", null, "Performance Tracker", 112, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 114, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "TargetAchievement", null, "Target Achievement", 113, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 115, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "FarmerData", null, "Farmer Data", 114, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 116, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "MdoReports", null, "Mdo Reports", 115, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 117, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "SdwaCompanyMaster", null, "Sdwa Company Master", 116, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 118, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "GuestHouseCancellations", null, "Guest House Cancellations", 117, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 119, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "DemoDocumentation", null, "Demo Documentation", 118, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 120, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "SalesAudit", null, "Sales Audit", 119, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
                    { 121, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "ExtensionRequests", null, "Extension Requests", 120, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 111);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 112);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 113);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 114);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 115);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 116);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 117);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 118);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 119);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 120);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 121);

            migrationBuilder.DropColumn(
                name: "PlannedDay",
                table: "JmdoTaskAllocationDealers");
        }
    }
}
