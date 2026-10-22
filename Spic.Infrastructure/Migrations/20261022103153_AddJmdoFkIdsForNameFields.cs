using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJmdoFkIdsForNameFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductGroupId",
                table: "JmdoTaskCloseoutProducts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "JmdoTaskCloseoutProducts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProgramMasterId",
                table: "JmdoTaskAllocationPrograms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill existing rows from the already-linked CSR1.ProgramId, instead of leaving the
            // just-added default of 0 (not a real ProgramMasters.Id) on every historical row.
            migrationBuilder.Sql(@"
                UPDATE ""JmdoTaskAllocationPrograms"" AS jtap
                SET ""ProgramMasterId"" = csr1.""ProgramId""
                FROM ""CSR1"" AS csr1
                WHERE jtap.""Csr1Id"" = csr1.""Id"";
            ");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "JMDOAttendances",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "JMDOAttendances",
                type: "text",
                nullable: true);

            // Backfill: a JMDO can only ever write their own attendance row (CurrentUserId() gate in
            // JMDOAttendanceController), so UserId already reliably IS the actor for every existing row.
            migrationBuilder.Sql(@"
                UPDATE ""JMDOAttendances""
                SET ""CreatedByUserId"" = ""UserId"",
                    ""UpdatedByUserId"" = ""UserId"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductGroupId",
                table: "JmdoTaskCloseoutProducts");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "JmdoTaskCloseoutProducts");

            migrationBuilder.DropColumn(
                name: "ProgramMasterId",
                table: "JmdoTaskAllocationPrograms");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "JMDOAttendances");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "JMDOAttendances");
        }
    }
}
