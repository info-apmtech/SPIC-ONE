using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v1BudgetProgram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<int>(
            //    name: "StateId",
            //    table: "RakePointMasters",
            //    type: "integer",
            //    nullable: true);

            //migrationBuilder.AddColumn<int>(
            //    name: "StateId",
            //    table: "PVTMasters",
            //    type: "integer",
            //    nullable: true);

            migrationBuilder.CreateTable(
                name: "ProgramTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProgramMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ProgramTypeId = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramMasters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramMasters_ProgramTypes_ProgramTypeId",
                        column: x => x.ProgramTypeId,
                        principalTable: "ProgramTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BudgetPrograms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProgramId = table.Column<int>(type: "integer", nullable: false),
                    TotalBudget = table.Column<decimal>(type: "numeric", nullable: false),
                    April = table.Column<decimal>(type: "numeric", nullable: false),
                    May = table.Column<decimal>(type: "numeric", nullable: false),
                    June = table.Column<decimal>(type: "numeric", nullable: false),
                    July = table.Column<decimal>(type: "numeric", nullable: false),
                    August = table.Column<decimal>(type: "numeric", nullable: false),
                    September = table.Column<decimal>(type: "numeric", nullable: false),
                    October = table.Column<decimal>(type: "numeric", nullable: false),
                    November = table.Column<decimal>(type: "numeric", nullable: false),
                    December = table.Column<decimal>(type: "numeric", nullable: false),
                    January = table.Column<decimal>(type: "numeric", nullable: false),
                    February = table.Column<decimal>(type: "numeric", nullable: false),
                    March = table.Column<decimal>(type: "numeric", nullable: false),
                    FinancialYear = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetPrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetPrograms_ProgramMasters_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "ProgramMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPrograms_ProgramId",
                table: "BudgetPrograms",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramMasters_ProgramTypeId",
                table: "ProgramMasters",
                column: "ProgramTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetPrograms");

            migrationBuilder.DropTable(
                name: "ProgramMasters");

            migrationBuilder.DropTable(
                name: "ProgramTypes");

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "RakePointMasters");

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "PVTMasters");
        }
    }
}
