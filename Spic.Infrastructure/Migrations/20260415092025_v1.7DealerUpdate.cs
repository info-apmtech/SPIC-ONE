using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v17DealerUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DealerAssetMovables");

            migrationBuilder.DropTable(
                name: "DealerInfrastructures");

            migrationBuilder.DropTable(
                name: "DealerInvestments");

            migrationBuilder.AlterColumn<string>(
                name: "Occupation",
                table: "PartnerFamilyDetails",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<decimal>(
                name: "AssetValue",
                table: "DealerRegistrations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CapitalInvestment",
                table: "DealerRegistrations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CapitalInvestmentRemarks",
                table: "DealerRegistrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CashCreditLimit",
                table: "DealerRegistrations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CashCreditLimitRrmarks",
                table: "DealerRegistrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OwnGodownCapacity",
                table: "DealerRegistrations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "DealerRegistrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RentGodownCapacity",
                table: "DealerRegistrations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssetValue",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "CapitalInvestment",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "CapitalInvestmentRemarks",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "CashCreditLimit",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "CashCreditLimitRrmarks",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "OwnGodownCapacity",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "RentGodownCapacity",
                table: "DealerRegistrations");

            migrationBuilder.AlterColumn<string>(
                name: "Occupation",
                table: "PartnerFamilyDetails",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "DealerAssetMovables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssetValue = table.Column<decimal>(type: "numeric", nullable: false),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerAssetMovables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerInfrastructures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    OwnGodownCapacity = table.Column<decimal>(type: "numeric", nullable: false),
                    RentGodownCapacity = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerInfrastructures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerInvestments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CapitalInvestment = table.Column<decimal>(type: "numeric", nullable: false),
                    CapitalInvestmentRemarks = table.Column<string>(type: "text", nullable: true),
                    CashCreditLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    CashCreditLimitRrmarks = table.Column<string>(type: "text", nullable: true),
                    DealerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerInvestments", x => x.Id);
                });
        }
    }
}
