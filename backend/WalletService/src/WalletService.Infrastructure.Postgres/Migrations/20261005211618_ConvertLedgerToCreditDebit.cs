using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WalletService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class ConvertLedgerToCreditDebit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Старые строковые значения enum LedgerDirection -> Credit/Debit.
            migrationBuilder.Sql(
                "UPDATE wallets.ledger_entries SET direction = 'Credit' WHERE direction = 'TOP_UP';");

            migrationBuilder.Sql(
                "UPDATE wallets.ledger_entries SET direction = 'Debit' WHERE direction = 'SPENT';");

            // Сид базовых валют убран из модели — список валют теперь полностью динамический.
            migrationBuilder.DeleteData(
                schema: "wallets",
                table: "currencies",
                keyColumn: "code",
                keyValue: "HARD");

            migrationBuilder.DeleteData(
                schema: "wallets",
                table: "currencies",
                keyColumn: "code",
                keyValue: "SOFT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE wallets.ledger_entries SET direction = 'TOP_UP' WHERE direction = 'Credit';");

            migrationBuilder.Sql(
                "UPDATE wallets.ledger_entries SET direction = 'SPENT' WHERE direction = 'Debit';");

            migrationBuilder.InsertData(
                schema: "wallets",
                table: "currencies",
                columns: new[] { "code", "created_at", "display_name", "is_active", "kind" },
                values: new object[,]
                {
                    { "HARD", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Hard currency", true, "Hard" },
                    { "SOFT", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Soft currency", true, "Soft" }
                });
        }
    }
}
