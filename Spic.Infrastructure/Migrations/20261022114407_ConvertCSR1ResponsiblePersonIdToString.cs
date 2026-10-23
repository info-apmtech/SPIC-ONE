using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertCSR1ResponsiblePersonIdToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
    name: "ResponsiblePersonId",
    table: "CSR1",
    type: "character varying(100)",
    maxLength: 100,
    nullable: true,
    oldClrType: typeof(int),
    oldType: "integer",
    oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
    name: "ResponsiblePersonId",
    table: "CSR1",
    type: "integer",
    nullable: true,
    oldClrType: typeof(string),
    oldType: "character varying(100)",
    oldMaxLength: 100,
    oldNullable: true);
        }
    }
}
