using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClanWarsService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "clanwars");

            migrationBuilder.CreateTable(
                name: "clan_roster",
                schema: "clanwars",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    event_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clan_roster", x => x.account_id);
                });

            migrationBuilder.CreateTable(
                name: "clans",
                schema: "clanwars",
                columns: table => new
                {
                    clan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_disbanded = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clans", x => x.clan_id);
                });

            migrationBuilder.CreateTable(
                name: "member_season_stats",
                schema: "clanwars",
                columns: table => new
                {
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    points = table.Column<long>(type: "bigint", nullable: false),
                    battles = table.Column<int>(type: "integer", nullable: false),
                    wins = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member_season_stats", x => new { x.season_id, x.account_id, x.clan_id });
                });

            migrationBuilder.CreateTable(
                name: "score_receipts",
                schema: "clanwars",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_score_receipts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "season_results",
                schema: "clanwars",
                columns: table => new
                {
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    points = table.Column<long>(type: "bigint", nullable: false),
                    battles = table.Column<int>(type: "integer", nullable: false),
                    wins = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_season_results", x => new { x.season_id, x.clan_id });
                });

            migrationBuilder.CreateTable(
                name: "season_scores",
                schema: "clanwars",
                columns: table => new
                {
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    points = table.Column<long>(type: "bigint", nullable: false),
                    battles = table.Column<int>(type: "integer", nullable: false),
                    wins = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_season_scores", x => new { x.season_id, x.clan_id });
                });

            migrationBuilder.CreateTable(
                name: "seasons",
                schema: "clanwars",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    finalizes_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    scoring_rules = table.Column<string>(type: "jsonb", nullable: false),
                    reward_table_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seasons", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_clan_roster_clan_id",
                schema: "clanwars",
                table: "clan_roster",
                column: "clan_id");

            migrationBuilder.CreateIndex(
                name: "ix_member_season_stats_clan",
                schema: "clanwars",
                table: "member_season_stats",
                columns: new[] { "season_id", "clan_id" });

            migrationBuilder.CreateIndex(
                name: "uq_score_receipts_source",
                schema: "clanwars",
                table: "score_receipts",
                columns: new[] { "season_id", "clan_id", "source_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_season_results_rank",
                schema: "clanwars",
                table: "season_results",
                columns: new[] { "season_id", "rank" });

            migrationBuilder.CreateIndex(
                name: "ix_season_scores_ranking",
                schema: "clanwars",
                table: "season_scores",
                columns: new[] { "season_id", "points", "updated_at" },
                descending: new[] { false, true, false });

            migrationBuilder.CreateIndex(
                name: "ix_seasons_status_starts_at",
                schema: "clanwars",
                table: "seasons",
                columns: new[] { "status", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "uq_seasons_number",
                schema: "clanwars",
                table: "seasons",
                column: "number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clan_roster",
                schema: "clanwars");

            migrationBuilder.DropTable(
                name: "clans",
                schema: "clanwars");

            migrationBuilder.DropTable(
                name: "member_season_stats",
                schema: "clanwars");

            migrationBuilder.DropTable(
                name: "score_receipts",
                schema: "clanwars");

            migrationBuilder.DropTable(
                name: "season_results",
                schema: "clanwars");

            migrationBuilder.DropTable(
                name: "season_scores",
                schema: "clanwars");

            migrationBuilder.DropTable(
                name: "seasons",
                schema: "clanwars");
        }
    }
}
