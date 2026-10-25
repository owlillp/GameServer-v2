using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SessionService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sessions");

            migrationBuilder.CreateTable(
                name: "game_servers",
                schema: "sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    address = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    region = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    modes = table.Column<string[]>(type: "text[]", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_heartbeat_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_servers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                schema: "sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    server_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    region = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    max_players = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: true),
                    player_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    close_reason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    liveness_checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_sessions_game_servers_server_id",
                        column: x => x.server_id,
                        principalSchema: "sessions",
                        principalTable: "game_servers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_reports",
                schema: "sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    server_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    flags = table.Column<string[]>(type: "text[]", nullable: false),
                    is_suspicious = table.Column<bool>(type: "boolean", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_reports", x => x.id);
                    table.ForeignKey(
                        name: "FK_raw_reports_sessions_session_id",
                        column: x => x.session_id,
                        principalSchema: "sessions",
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "session_players",
                schema: "sessions",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    left_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_players", x => new { x.session_id, x.account_id });
                    table.ForeignKey(
                        name: "FK_session_players_sessions_session_id",
                        column: x => x.session_id,
                        principalSchema: "sessions",
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_game_servers_region_status_heartbeat",
                schema: "sessions",
                table: "game_servers",
                columns: new[] { "region", "status", "last_heartbeat_at" });

            migrationBuilder.CreateIndex(
                name: "ix_raw_reports_session_id",
                schema: "sessions",
                table: "raw_reports",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_raw_reports_suspicious",
                schema: "sessions",
                table: "raw_reports",
                column: "received_at",
                filter: "is_suspicious");

            migrationBuilder.CreateIndex(
                name: "uq_raw_reports_activity_id",
                schema: "sessions",
                table: "raw_reports",
                column: "activity_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_session_players_present_account",
                schema: "sessions",
                table: "session_players",
                column: "account_id",
                filter: "left_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_joinable",
                schema: "sessions",
                table: "sessions",
                columns: new[] { "kind", "region", "mode", "status", "player_count" });

            migrationBuilder.CreateIndex(
                name: "ix_sessions_server_id",
                schema: "sessions",
                table: "sessions",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_status_liveness",
                schema: "sessions",
                table: "sessions",
                columns: new[] { "status", "liveness_checked_at" });

            migrationBuilder.CreateIndex(
                name: "uq_sessions_match_id",
                schema: "sessions",
                table: "sessions",
                column: "match_id",
                unique: true,
                filter: "match_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "raw_reports",
                schema: "sessions");

            migrationBuilder.DropTable(
                name: "session_players",
                schema: "sessions");

            migrationBuilder.DropTable(
                name: "sessions",
                schema: "sessions");

            migrationBuilder.DropTable(
                name: "game_servers",
                schema: "sessions");
        }
    }
}
