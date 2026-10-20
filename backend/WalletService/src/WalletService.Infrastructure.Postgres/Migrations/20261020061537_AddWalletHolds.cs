using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalletService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletHolds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "holds",
                schema: "wallets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    reference_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_holds", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_holds_expires_at",
                schema: "wallets",
                table: "holds",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "uq_holds_wallet_operation",
                schema: "wallets",
                table: "holds",
                columns: new[] { "wallet_id", "operation_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "holds",
                schema: "wallets");
        }
    }
}
