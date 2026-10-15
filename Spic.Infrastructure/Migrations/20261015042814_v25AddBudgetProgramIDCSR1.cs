using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v25AddBudgetProgramIDCSR1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BudgetProgramId",
                table: "CSR1",
                type: "integer",
                nullable: true);

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "Level1ApprovedAt",
            //    table: "CSR1",
            //    type: "timestamp without time zone",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "Level1ApprovedBy",
            //    table: "CSR1",
            //    type: "text",
            //    nullable: true);

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "Level2ApprovedAt",
            //    table: "CSR1",
            //    type: "timestamp without time zone",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "Level2ApprovedBy",
            //    table: "CSR1",
            //    type: "text",
            //    nullable: true);

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "Level3ApprovedAt",
            //    table: "CSR1",
            //    type: "timestamp without time zone",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "Level3ApprovedBy",
            //    table: "CSR1",
            //    type: "text",
            //    nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProgramMonth",
                table: "CSR1",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProgramYear",
                table: "CSR1",
                type: "integer",
                nullable: true);

            //migrationBuilder.CreateTable(
            //    name: "RMDHQMappings",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "integer", nullable: false)
            //            .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            //        EmployeeId = table.Column<int>(type: "integer", nullable: false),
            //        StateId = table.Column<int>(type: "integer", nullable: false),
            //        RegionId = table.Column<int>(type: "integer", nullable: false),
            //        HeadquartersIds = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
            //        CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
            //        CreatedBy = table.Column<string>(type: "text", nullable: true),
            //        UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
            //        UpdatedBy = table.Column<string>(type: "text", nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_RMDHQMappings", x => x.Id);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "SanctionNumberSequences",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "integer", nullable: false)
            //            .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            //        StateId = table.Column<int>(type: "integer", nullable: false),
            //        RegionId = table.Column<int>(type: "integer", nullable: true),
            //        StateSAPCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
            //        RegionSAPCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
            //        SanctionNumber = table.Column<int>(type: "integer", nullable: false),
            //        SanctionNumberState = table.Column<int>(type: "integer", nullable: false),
            //        CreatedBy = table.Column<string>(type: "text", nullable: true),
            //        CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
            //        UpdatedBy = table.Column<string>(type: "text", nullable: true),
            //        UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_SanctionNumberSequences", x => x.Id);
            //    });

            //migrationBuilder.CreateIndex(
            //    name: "IX_RMDHQMappings_EmployeeId_StateId_RegionId",
            //    table: "RMDHQMappings",
            //    columns: new[] { "EmployeeId", "StateId", "RegionId" },
            //    unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RMDHQMappings");

            migrationBuilder.DropTable(
                name: "SanctionNumberSequences");

            migrationBuilder.DropColumn(
                name: "BudgetProgramId",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level1ApprovedAt",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level1ApprovedBy",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level2ApprovedAt",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level2ApprovedBy",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level3ApprovedAt",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level3ApprovedBy",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "ProgramMonth",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "ProgramYear",
                table: "CSR1");
        }
    }
}
