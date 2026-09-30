using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v6AddCSR1Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CSR1",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProgramTypeId = table.Column<int>(type: "integer", nullable: false),
                    ProgramId = table.Column<int>(type: "integer", nullable: false),
                    NumberOfPrograms = table.Column<int>(type: "integer", nullable: false),
                    Budget = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    HeadquarterId = table.Column<int>(type: "integer", nullable: true),
                    LocationId = table.Column<int>(type: "integer", nullable: true),
                    ResponsiblePersonId = table.Column<int>(type: "integer", nullable: true),
                    FocusCrop1Id = table.Column<int>(type: "integer", nullable: true),
                    FocusProduct1Id = table.Column<int>(type: "integer", nullable: true),
                    FocusCrop2Id = table.Column<int>(type: "integer", nullable: true),
                    FocusProduct2Id = table.Column<int>(type: "integer", nullable: true),
                    FocusCrop3Id = table.Column<int>(type: "integer", nullable: true),
                    FocusProduct3Id = table.Column<int>(type: "integer", nullable: true),
                    PrintingAndStationery = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PublicityMaterial = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StageArrangements = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ServiceChargesLCA = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TransportRent = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    JeepRunningExpenses = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Refreshments = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Inputs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Photography = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Compliments = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Others = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CSR1", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CSR1");
        }
    }
}
