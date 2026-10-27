using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CSR2AddedNewColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CSR2Expenses_CSR2Id_CategoryKey",
                table: "CSR2Expenses");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "CSR2Participants");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "CSR2Participants");

            migrationBuilder.DropColumn(
                name: "GroupNumber",
                table: "CSR2FocusProduct");

            migrationBuilder.DropColumn(
                name: "CategoryKey",
                table: "CSR2Expenses");

            migrationBuilder.DropColumn(
                name: "CategoryName",
                table: "CSR2Expenses");

            migrationBuilder.DropColumn(
                name: "EmployeeName",
                table: "CSR2Employees");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "CSR2Employees");

            migrationBuilder.AlterColumn<string>(
                name: "EventCoordinatorEmployeeId",
                table: "CSR2s",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParticipantSource",
                table: "CSR2Participants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParticipantSourceId",
                table: "CSR2Participants",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpenseInformationId",
                table: "CSR2Expenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CSR2Expenses_CSR2Id_ExpenseInformationId",
                table: "CSR2Expenses",
                columns: new[] { "CSR2Id", "ExpenseInformationId" });

            migrationBuilder.CreateIndex(
                name: "IX_CSR2Expenses_ExpenseInformationId",
                table: "CSR2Expenses",
                column: "ExpenseInformationId");

            migrationBuilder.AddForeignKey(
                name: "FK_CSR2Expenses_ExpenseInformations_ExpenseInformationId",
                table: "CSR2Expenses",
                column: "ExpenseInformationId",
                principalTable: "ExpenseInformations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CSR2Expenses_ExpenseInformations_ExpenseInformationId",
                table: "CSR2Expenses");

            migrationBuilder.DropIndex(
                name: "IX_CSR2Expenses_CSR2Id_ExpenseInformationId",
                table: "CSR2Expenses");

            migrationBuilder.DropIndex(
                name: "IX_CSR2Expenses_ExpenseInformationId",
                table: "CSR2Expenses");

            migrationBuilder.DropColumn(
                name: "ParticipantSource",
                table: "CSR2Participants");

            migrationBuilder.DropColumn(
                name: "ParticipantSourceId",
                table: "CSR2Participants");

            migrationBuilder.DropColumn(
                name: "ExpenseInformationId",
                table: "CSR2Expenses");

            migrationBuilder.AlterColumn<string>(
                name: "EventCoordinatorEmployeeId",
                table: "CSR2s",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "CSR2Participants",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "CSR2Participants",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GroupNumber",
                table: "CSR2FocusProduct",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CategoryKey",
                table: "CSR2Expenses",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CategoryName",
                table: "CSR2Expenses",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeName",
                table: "CSR2Employees",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "CSR2Employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CSR2Expenses_CSR2Id_CategoryKey",
                table: "CSR2Expenses",
                columns: new[] { "CSR2Id", "CategoryKey" },
                unique: true);
        }
    }
}
