using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "catalog_versions",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<long>(type: "bigint", nullable: false),
                    note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    published_by = table.Column<Guid>(type: "uuid", nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "items",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    rarity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    model_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    slot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    tags = table.Column<string[]>(type: "text[]", nullable: false),
                    fit_tags = table.Column<string[]>(type: "text[]", nullable: false),
                    last_published_version = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_version_items",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    snapshot = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_version_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_catalog_version_items_catalog_versions_version_id",
                        column: x => x.version_id,
                        principalSchema: "catalog",
                        principalTable: "catalog_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "availability_windows",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_availability_windows", x => x.id);
                    table.ForeignKey(
                        name: "FK_availability_windows_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "catalog",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_prices",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_prices", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_prices_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "catalog",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_stats",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_stats", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_stats_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "catalog",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_availability_windows_item",
                schema: "catalog",
                table: "availability_windows",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_version_items_version_item",
                schema: "catalog",
                table: "catalog_version_items",
                columns: new[] { "version_id", "item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_catalog_versions_number",
                schema: "catalog",
                table: "catalog_versions",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_item_prices_item_currency",
                schema: "catalog",
                table: "item_prices",
                columns: new[] { "item_id", "currency_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_item_stats_item_key",
                schema: "catalog",
                table: "item_stats",
                columns: new[] { "item_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_items_kind",
                schema: "catalog",
                table: "items",
                column: "kind");

            migrationBuilder.CreateIndex(
                name: "ix_items_status",
                schema: "catalog",
                table: "items",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "uq_items_code",
                schema: "catalog",
                table: "items",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "availability_windows",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "catalog_version_items",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "item_prices",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "item_stats",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "catalog_versions",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "items",
                schema: "catalog");
        }
    }
}
