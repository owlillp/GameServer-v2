using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClanWarsService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class SeasonRewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reward_tables",
                schema: "clanwars",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reward_tables", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "season_reward_grants",
                schema: "clanwars",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    clan_points = table.Column<long>(type: "bigint", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    currency_amount = table.Column<long>(type: "bigint", nullable: false),
                    items = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_error = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    points_published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    wallet_credited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    items_published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    items_granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_season_reward_grants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reward_tiers",
                schema: "clanwars",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    table_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_rank = table.Column<int>(type: "integer", nullable: false),
                    to_rank = table.Column<int>(type: "integer", nullable: false),
                    clan_points = table.Column<long>(type: "bigint", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    currency_amount = table.Column<long>(type: "bigint", nullable: false),
                    items = table.Column<string>(type: "jsonb", nullable: false),
                    min_battles = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reward_tiers", x => x.id);
                    table.ForeignKey(
                        name: "FK_reward_tiers_reward_tables_table_id",
                        column: x => x.table_id,
                        principalSchema: "clanwars",
                        principalTable: "reward_tables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reward_tiers_table_id",
                schema: "clanwars",
                table: "reward_tiers",
                column: "table_id");

            migrationBuilder.CreateIndex(
                name: "ix_season_reward_grants_account",
                schema: "clanwars",
                table: "season_reward_grants",
                columns: new[] { "season_id", "account_id" });

            migrationBuilder.CreateIndex(
                name: "ix_season_reward_grants_due",
                schema: "clanwars",
                table: "season_reward_grants",
                columns: new[] { "status", "next_attempt_at" },
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ix_season_reward_grants_season_status",
                schema: "clanwars",
                table: "season_reward_grants",
                columns: new[] { "season_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uq_season_reward_grants_recipient",
                schema: "clanwars",
                table: "season_reward_grants",
                columns: new[] { "season_id", "clan_id", "account_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reward_tiers",
                schema: "clanwars");

            migrationBuilder.DropTable(
                name: "season_reward_grants",
                schema: "clanwars");

            migrationBuilder.DropTable(
                name: "reward_tables",
                schema: "clanwars");
        }
    }
}
