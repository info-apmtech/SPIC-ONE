using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpcmBookCollectionPaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "JmdoTaskCloseouts",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "JmdoTaskCloseouts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BooksAccountedInFavourOf",
                table: "JmdoTaskCloseouts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentByName",
                table: "JmdoTaskCloseouts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDate",
                table: "JmdoTaskCloseouts",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptNumber",
                table: "JmdoTaskCloseouts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionNumber",
                table: "JmdoTaskCloseouts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "JmdoTaskCloseouts");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "JmdoTaskCloseouts");

            migrationBuilder.DropColumn(
                name: "BooksAccountedInFavourOf",
                table: "JmdoTaskCloseouts");

            migrationBuilder.DropColumn(
                name: "PaymentByName",
                table: "JmdoTaskCloseouts");

            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "JmdoTaskCloseouts");

            migrationBuilder.DropColumn(
                name: "ReceiptNumber",
                table: "JmdoTaskCloseouts");

            migrationBuilder.DropColumn(
                name: "TransactionNumber",
                table: "JmdoTaskCloseouts");
        }
    }
}
