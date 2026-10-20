using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "events");

            migrationBuilder.CreateTable(
                name: "event_match_receipts",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_match_receipts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_progress",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    points_balance = table.Column<int>(type: "integer", nullable: false),
                    total_points_earned = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_progress", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_quest_progress",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    progress = table.Column<int>(type: "integer", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_quest_progress", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_reward_claims",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reward_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reward_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    points_spent = table.Column<int>(type: "integer", nullable: false),
                    is_repeatable = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_reward_claims", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "game_events",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    announce_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    grace_ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_notified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ended_notified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_quests",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    metric = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    target = table.Column<int>(type: "integer", nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_quests", x => x.id);
                    table.ForeignKey(
                        name: "FK_event_quests_game_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "events",
                        principalTable: "game_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_rewards",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    points_cost = table.Column<int>(type: "integer", nullable: false),
                    catalog_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    item_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    slot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_repeatable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_rewards", x => x.id);
                    table.ForeignKey(
                        name: "FK_event_rewards_game_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "events",
                        principalTable: "game_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "uq_event_match_receipts_event_account_match",
                schema: "events",
                table: "event_match_receipts",
                columns: new[] { "event_id", "account_id", "match_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_event_progress_event_account",
                schema: "events",
                table: "event_progress",
                columns: new[] { "event_id", "account_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_quest_progress_event_account",
                schema: "events",
                table: "event_quest_progress",
                columns: new[] { "event_id", "account_id" });

            migrationBuilder.CreateIndex(
                name: "uq_event_quest_progress_quest_account_period",
                schema: "events",
                table: "event_quest_progress",
                columns: new[] { "quest_id", "account_id", "period_start" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_event_quests_event_code",
                schema: "events",
                table: "event_quests",
                columns: new[] { "event_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_reward_claims_account_created_at",
                schema: "events",
                table: "event_reward_claims",
                columns: new[] { "account_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "uq_event_reward_claims_account_idempotency_key",
                schema: "events",
                table: "event_reward_claims",
                columns: new[] { "account_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_event_reward_claims_account_reward",
                schema: "events",
                table: "event_reward_claims",
                columns: new[] { "account_id", "reward_id" },
                unique: true,
                filter: "is_repeatable = false AND status <> 'FAILED'");

            migrationBuilder.CreateIndex(
                name: "uq_event_rewards_event_code",
                schema: "events",
                table: "event_rewards",
                columns: new[] { "event_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_events_status_schedule",
                schema: "events",
                table: "game_events",
                columns: new[] { "status", "starts_at", "grace_ends_at" });

            migrationBuilder.CreateIndex(
                name: "uq_game_events_code",
                schema: "events",
                table: "game_events",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_match_receipts",
                schema: "events");

            migrationBuilder.DropTable(
                name: "event_progress",
                schema: "events");

            migrationBuilder.DropTable(
                name: "event_quest_progress",
                schema: "events");

            migrationBuilder.DropTable(
                name: "event_quests",
                schema: "events");

            migrationBuilder.DropTable(
                name: "event_reward_claims",
                schema: "events");

            migrationBuilder.DropTable(
                name: "event_rewards",
                schema: "events");

            migrationBuilder.DropTable(
                name: "game_events",
                schema: "events");
        }
    }
}
