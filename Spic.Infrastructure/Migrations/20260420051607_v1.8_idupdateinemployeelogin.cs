using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
	public partial class v18_idupdateinemployeelogin : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "Employeelogins",
				columns: table => new
				{
					Id = table.Column<int>(type: "integer", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					UserId = table.Column<string>(type: "text", nullable: false),
					Role = table.Column<int>(type: "integer", nullable: false),
					ZoneId = table.Column<int>(type: "integer", nullable: false),
					StateId = table.Column<int>(type: "integer", nullable: false),
					RegionId = table.Column<int>(type: "integer", nullable: false),
					HeadquartersId = table.Column<int>(type: "integer", nullable: false),
					IsActive = table.Column<bool>(type: "boolean", nullable: false),
					EmployeeInformationID = table.Column<int>(type: "integer", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Employeelogins", x => x.Id);
				});
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "Employeelogins");
		}
	}
}