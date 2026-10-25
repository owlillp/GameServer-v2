using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClanWarsService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AntiAbuse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "joined_at",
                schema: "clanwars",
                table: "clan_roster",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Existing members keep their known join time instead of the column default.
            migrationBuilder.Sql("UPDATE clanwars.clan_roster SET joined_at = event_at");

            migrationBuilder.AddColumn<long>(
                name: "rating_a",
                schema: "clanwars",
                table: "battles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "rating_b",
                schema: "clanwars",
                table: "battles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "abuse_signals",
                schema: "clanwars",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clan_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    battle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    details = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    detected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abuse_signals", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_abuse_signals_season",
                schema: "clanwars",
                table: "abuse_signals",
                columns: new[] { "season_id", "detected_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abuse_signals",
                schema: "clanwars");

            migrationBuilder.DropColumn(
                name: "joined_at",
                schema: "clanwars",
                table: "clan_roster");

            migrationBuilder.DropColumn(
                name: "rating_a",
                schema: "clanwars",
                table: "battles");

            migrationBuilder.DropColumn(
                name: "rating_b",
                schema: "clanwars",
                table: "battles");
        }
    }
}
