using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.CreateTable(
                name: "grant_receipts",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    item_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grant_receipts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inventories",
                schema: "inventory",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventories", x => x.account_id);
                });

            migrationBuilder.CreateTable(
                name: "outfit_entries",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outfit_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_outfit_entries_inventories_account_id",
                        column: x => x.account_id,
                        principalSchema: "inventory",
                        principalTable: "inventories",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "owned_cars",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    car_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    plate = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_owned_cars", x => x.id);
                    table.ForeignKey(
                        name: "FK_owned_cars_inventories_account_id",
                        column: x => x.account_id,
                        principalSchema: "inventory",
                        principalTable: "inventories",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "owned_items",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    slot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    acquired_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_owned_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_owned_items_inventories_account_id",
                        column: x => x.account_id,
                        principalSchema: "inventory",
                        principalTable: "inventories",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "car_installations",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    car_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    part_item_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_car_installations", x => x.id);
                    table.ForeignKey(
                        name: "FK_car_installations_owned_cars_car_id",
                        column: x => x.car_id,
                        principalSchema: "inventory",
                        principalTable: "owned_cars",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "uq_car_installations_car_slot",
                schema: "inventory",
                table: "car_installations",
                columns: new[] { "car_id", "slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_grant_receipts_operation",
                schema: "inventory",
                table: "grant_receipts",
                column: "operation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_outfit_entries_account_slot",
                schema: "inventory",
                table: "outfit_entries",
                columns: new[] { "account_id", "slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_owned_cars_account",
                schema: "inventory",
                table: "owned_cars",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "uq_owned_items_account_item",
                schema: "inventory",
                table: "owned_items",
                columns: new[] { "account_id", "item_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "car_installations",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "grant_receipts",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "outfit_entries",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "owned_items",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "owned_cars",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "inventories",
                schema: "inventory");
        }
    }
}
