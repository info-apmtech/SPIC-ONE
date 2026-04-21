using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
	public partial class v18_idupdateinemployeelogin : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropPrimaryKey(
				name: "PK_Employeelogins",
				table: "Employeelogins");

			migrationBuilder.DropColumn(
				name: "Id",
				table: "Employeelogins");

			migrationBuilder.AddColumn<int>(
				name: "Id",
				table: "Employeelogins",
				type: "integer",
				nullable: false,
				defaultValue: 0)
				.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

			migrationBuilder.AddPrimaryKey(
				name: "PK_Employeelogins",
				table: "Employeelogins",
				column: "Id");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropPrimaryKey(
				name: "PK_Employeelogins",
				table: "Employeelogins");

			migrationBuilder.DropColumn(
				name: "Id",
				table: "Employeelogins");

			migrationBuilder.AddColumn<string>(
				name: "Id",
				table: "Employeelogins",
				type: "text",
				nullable: false,
				defaultValue: "");

			migrationBuilder.AddPrimaryKey(
				name: "PK_Employeelogins",
				table: "Employeelogins",
				column: "Id");
		}
	}
}