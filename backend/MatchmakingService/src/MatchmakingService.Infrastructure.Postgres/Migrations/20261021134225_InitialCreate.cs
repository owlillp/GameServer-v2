using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchmakingService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "matchmaking");

            migrationBuilder.CreateTable(
                name: "leaderboard_entries",
                schema: "matchmaking",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    points = table.Column<long>(type: "bigint", nullable: false),
                    wins = table.Column<int>(type: "integer", nullable: false),
                    matches_played = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leaderboard_entries", x => x.account_id);
                });

            migrationBuilder.CreateTable(
                name: "match_reward_grants",
                schema: "matchmaking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    wallet_operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_credited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    items_published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    items_granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_reward_grants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "matches",
                schema: "matchmaking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    winner_party_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "parties",
                schema: "matchmaking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    leader_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parties", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "queue_tickets",
                schema: "matchmaking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leader_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_size = table.Column<int>(type: "integer", nullable: false),
                    mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: true),
                    enqueued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    matched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_queue_tickets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "match_reward_grant_items",
                schema: "matchmaking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    slot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_reward_grant_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_match_reward_grant_items_match_reward_grants_grant_id",
                        column: x => x.grant_id,
                        principalSchema: "matchmaking",
                        principalTable: "match_reward_grants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "match_participants",
                schema: "matchmaking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_participants", x => x.id);
                    table.ForeignKey(
                        name: "FK_match_participants_matches_match_id",
                        column: x => x.match_id,
                        principalSchema: "matchmaking",
                        principalTable: "matches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "match_parties",
                schema: "matchmaking",
                columns: table => new
                {
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    side = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_parties", x => new { x.match_id, x.party_id });
                    table.ForeignKey(
                        name: "FK_match_parties_matches_match_id",
                        column: x => x.match_id,
                        principalSchema: "matchmaking",
                        principalTable: "matches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "match_results",
                schema: "matchmaking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<long>(type: "bigint", nullable: false),
                    kills = table.Column<int>(type: "integer", nullable: false),
                    is_win = table.Column<bool>(type: "boolean", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_match_results_matches_match_id",
                        column: x => x.match_id,
                        principalSchema: "matchmaking",
                        principalTable: "matches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "match_reward_definitions",
                schema: "matchmaking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    participation_amount = table.Column<long>(type: "bigint", nullable: false),
                    win_bonus_amount = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_reward_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_match_reward_definitions_matches_match_id",
                        column: x => x.match_id,
                        principalSchema: "matchmaking",
                        principalTable: "matches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "party_members",
                schema: "matchmaking",
                columns: table => new
                {
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_ready = table.Column<bool>(type: "boolean", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party_members", x => new { x.party_id, x.account_id });
                    table.ForeignKey(
                        name: "FK_party_members_parties_party_id",
                        column: x => x.party_id,
                        principalSchema: "matchmaking",
                        principalTable: "parties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "match_reward_definition_items",
                schema: "matchmaking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    slot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_reward_definition_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_match_reward_definition_items_match_reward_definitions_defi~",
                        column: x => x.definition_id,
                        principalSchema: "matchmaking",
                        principalTable: "match_reward_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_leaderboard_entries_points",
                schema: "matchmaking",
                table: "leaderboard_entries",
                column: "points",
                descending: [true]);

            migrationBuilder.CreateIndex(
                name: "ix_match_participants_account_id",
                schema: "matchmaking",
                table: "match_participants",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "uq_match_participants_match_account",
                schema: "matchmaking",
                table: "match_participants",
                columns: new[] { "match_id", "account_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_match_parties_party_id",
                schema: "matchmaking",
                table: "match_parties",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "uq_match_results_match_account",
                schema: "matchmaking",
                table: "match_results",
                columns: new[] { "match_id", "account_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_match_reward_definition_items_definition_id",
                schema: "matchmaking",
                table: "match_reward_definition_items",
                column: "definition_id");

            migrationBuilder.CreateIndex(
                name: "uq_match_reward_definitions_match_id",
                schema: "matchmaking",
                table: "match_reward_definitions",
                column: "match_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_match_reward_grant_items_grant_id",
                schema: "matchmaking",
                table: "match_reward_grant_items",
                column: "grant_id");

            migrationBuilder.CreateIndex(
                name: "ix_match_reward_grants_status",
                schema: "matchmaking",
                table: "match_reward_grants",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "uq_match_reward_grants_match_account",
                schema: "matchmaking",
                table: "match_reward_grants",
                columns: new[] { "match_id", "account_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_matches_status_ends_at",
                schema: "matchmaking",
                table: "matches",
                columns: new[] { "status", "ends_at" });

            migrationBuilder.CreateIndex(
                name: "ix_matches_winner_party_id",
                schema: "matchmaking",
                table: "matches",
                column: "winner_party_id");

            migrationBuilder.CreateIndex(
                name: "uq_parties_code",
                schema: "matchmaking",
                table: "parties",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_party_members_account_id",
                schema: "matchmaking",
                table: "party_members",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_queue_tickets_party_id",
                schema: "matchmaking",
                table: "queue_tickets",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "ix_queue_tickets_status_enqueued_at",
                schema: "matchmaking",
                table: "queue_tickets",
                columns: new[] { "status", "enqueued_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "leaderboard_entries",
                schema: "matchmaking");

            migrationBuilder.DropTable(
                name: "match_participants",
                schema: "matchmaking");

            migrationBuilder.DropTable(
                name: "match_parties",
                schema: "matchmaking");

            migrationBuilder.DropTable(
                name: "match_results",
                schema: "matchmaking");

            migrationBuilder.DropTable(
                name: "match_reward_definition_items",
                schema: "matchmaking");

            migrationBuilder.DropTable(
                name: "match_reward_grant_items",
                schema: "matchmaking");

            migrationBuilder.DropTable(
                name: "party_members",
                schema: "matchmaking");

            migrationBuilder.DropTable(
                name: "queue_tickets",
                schema: "matchmaking");

            migrationBuilder.DropTable(
                name: "match_reward_definitions",
                schema: "matchmaking");

            migrationBuilder.DropTable(
                name: "match_reward_grants",
                schema: "matchmaking");

            migrationBuilder.DropTable(
                name: "parties",
                schema: "matchmaking");

            migrationBuilder.DropTable(
                name: "matches",
                schema: "matchmaking");
        }
    }
}
