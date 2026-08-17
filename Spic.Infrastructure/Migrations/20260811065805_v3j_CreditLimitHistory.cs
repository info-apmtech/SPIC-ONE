using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v3j_CreditLimitHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditLimitHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    CreditType = table.Column<int>(type: "integer", nullable: true),
                    ExistingCreditLimit = table.Column<decimal>(type: "numeric", nullable: true),
                    ExistingValidFrom = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExistingValidTo = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AdditionalCreditLimit = table.Column<decimal>(type: "numeric", nullable: true),
                    MORecommendedCreditLimit = table.Column<decimal>(type: "numeric", nullable: true),
                    RMApprovedCreditLimit = table.Column<decimal>(type: "numeric", nullable: true),
                    SMApprovedCreditLimit = table.Column<decimal>(type: "numeric", nullable: true),
                    AVPApprovedCreditLimit = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditLimitHistories", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditLimitHistories");
        }
    }
}
