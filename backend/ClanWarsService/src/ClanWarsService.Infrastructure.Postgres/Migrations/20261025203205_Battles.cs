using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClanWarsService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class Battles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "battle_window_template",
                schema: "clanwars",
                table: "seasons",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "battle_queue",
                schema: "clanwars",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    squad = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    rating = table.Column<long>(type: "bigint", nullable: false),
                    queued_by = table.Column<Guid>(type: "uuid", nullable: false),
                    queued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    battle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_battle_queue", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "battle_windows",
                schema: "clanwars",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_battle_windows", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "battles",
                schema: "clanwars",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clan_a_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clan_b_id = table.Column<Guid>(type: "uuid", nullable: false),
                    squad_a = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    squad_b = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: true),
                    winner_clan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    points_a = table.Column<long>(type: "bigint", nullable: false),
                    points_b = table.Column<long>(type: "bigint", nullable: false),
                    cancel_reason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_battles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_battle_queue_season_status",
                schema: "clanwars",
                table: "battle_queue",
                columns: new[] { "season_id", "status", "queued_at" });

            migrationBuilder.CreateIndex(
                name: "uq_battle_queue_clan_queued",
                schema: "clanwars",
                table: "battle_queue",
                column: "clan_id",
                unique: true,
                filter: "status = 'Queued'");

            migrationBuilder.CreateIndex(
                name: "uq_battle_windows_season_start",
                schema: "clanwars",
                table: "battle_windows",
                columns: new[] { "season_id", "starts_at" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_battles_clan_a",
                schema: "clanwars",
                table: "battles",
                columns: new[] { "clan_a_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_battles_clan_b",
                schema: "clanwars",
                table: "battles",
                columns: new[] { "clan_b_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_battles_season_id",
                schema: "clanwars",
                table: "battles",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_battles_status_created",
                schema: "clanwars",
                table: "battles",
                columns: new[] { "status", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "battle_queue",
                schema: "clanwars");

            migrationBuilder.DropTable(
                name: "battle_windows",
                schema: "clanwars");

            migrationBuilder.DropTable(
                name: "battles",
                schema: "clanwars");

            migrationBuilder.DropColumn(
                name: "battle_window_template",
                schema: "clanwars",
                table: "seasons");
        }
    }
}
