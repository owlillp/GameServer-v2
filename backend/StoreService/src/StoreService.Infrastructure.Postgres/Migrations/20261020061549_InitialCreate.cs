using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "store");

            migrationBuilder.CreateTable(
                name: "offers",
                schema: "store",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    price_currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    price_amount = table.Column<long>(type: "bigint", nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_offers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                schema: "store",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    offer_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    total_amount = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    hold_id = table.Column<Guid>(type: "uuid", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "offer_items",
                schema: "store",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    slot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_offer_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_offer_items_offers_offer_id",
                        column: x => x.offer_id,
                        principalSchema: "store",
                        principalTable: "offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                schema: "store",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    slot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_items_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "store",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_offer_items_offer_id",
                schema: "store",
                table: "offer_items",
                column: "offer_id");

            migrationBuilder.CreateIndex(
                name: "uq_offers_code",
                schema: "store",
                table: "offers",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_items_order_id",
                schema: "store",
                table: "order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_account_created_at",
                schema: "store",
                table: "orders",
                columns: new[] { "account_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_orders_status_created_at",
                schema: "store",
                table: "orders",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "uq_orders_account_idempotency_key",
                schema: "store",
                table: "orders",
                columns: new[] { "account_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "offer_items",
                schema: "store");

            migrationBuilder.DropTable(
                name: "order_items",
                schema: "store");

            migrationBuilder.DropTable(
                name: "offers",
                schema: "store");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "store");
        }
    }
}
