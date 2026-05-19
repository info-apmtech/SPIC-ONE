using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v22Dealercreditlimitsalesdataupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExistingCreditLimitTo",
                table: "DealerCreditLimitProposals",
                newName: "SpicExistingCreditLimitTo");

            migrationBuilder.RenameColumn(
                name: "ExistingCreditLimitFrom",
                table: "DealerCreditLimitProposals",
                newName: "SpicExistingCreditLimitFrom");

            migrationBuilder.RenameColumn(
                name: "ExistingCreditLimitAmount",
                table: "DealerCreditLimitProposals",
                newName: "SpicExistingCreditLimitAmount");

            migrationBuilder.AddColumn<decimal>(
                name: "GSExistingCreditLimitAmount",
                table: "DealerCreditLimitProposals",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "GSExistingCreditLimitFrom",
                table: "DealerCreditLimitProposals",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "GSExistingCreditLimitTo",
                table: "DealerCreditLimitProposals",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "DealerCreditLimitSalesData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    State = table.Column<string>(type: "text", nullable: false),
                    CustomerNumber = table.Column<string>(type: "text", nullable: false),
                    CustomerName = table.Column<string>(type: "text", nullable: false),
                    SubGroup = table.Column<string>(type: "text", nullable: false),
                    ProductGroup = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<string>(type: "text", nullable: false),
                    GrossAmount = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerCreditLimitSalesData", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DealerCreditLimitSalesData");

            migrationBuilder.DropColumn(
                name: "GSExistingCreditLimitAmount",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GSExistingCreditLimitFrom",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GSExistingCreditLimitTo",
                table: "DealerCreditLimitProposals");

            migrationBuilder.RenameColumn(
                name: "SpicExistingCreditLimitTo",
                table: "DealerCreditLimitProposals",
                newName: "ExistingCreditLimitTo");

            migrationBuilder.RenameColumn(
                name: "SpicExistingCreditLimitFrom",
                table: "DealerCreditLimitProposals",
                newName: "ExistingCreditLimitFrom");

            migrationBuilder.RenameColumn(
                name: "SpicExistingCreditLimitAmount",
                table: "DealerCreditLimitProposals",
                newName: "ExistingCreditLimitAmount");
        }
    }
}
