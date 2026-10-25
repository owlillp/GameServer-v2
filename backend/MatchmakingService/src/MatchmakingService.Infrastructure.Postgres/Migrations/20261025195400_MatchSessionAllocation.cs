using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchmakingService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class MatchSessionAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attempt",
                schema: "matchmaking",
                table: "queue_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_priority",
                schema: "matchmaking",
                table: "queue_tickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "region",
                schema: "matchmaking",
                table: "queue_tickets",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "eu-west");

            migrationBuilder.AddColumn<DateTime>(
                name: "allocated_at",
                schema: "matchmaking",
                table: "matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "allocation_requested_at",
                schema: "matchmaking",
                table: "matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "region",
                schema: "matchmaking",
                table: "matches",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "eu-west");

            migrationBuilder.AddColumn<string>(
                name: "server_address",
                schema: "matchmaking",
                table: "matches",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "session_id",
                schema: "matchmaking",
                table: "matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_queue_tickets_match_id",
                schema: "matchmaking",
                table: "queue_tickets",
                column: "match_id");

            migrationBuilder.CreateIndex(
                name: "ix_matches_waiting_for_server",
                schema: "matchmaking",
                table: "matches",
                columns: new[] { "status", "allocation_requested_at" },
                filter: "allocated_at IS NULL AND allocation_requested_at IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_queue_tickets_match_id",
                schema: "matchmaking",
                table: "queue_tickets");

            migrationBuilder.DropIndex(
                name: "ix_matches_waiting_for_server",
                schema: "matchmaking",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "attempt",
                schema: "matchmaking",
                table: "queue_tickets");

            migrationBuilder.DropColumn(
                name: "is_priority",
                schema: "matchmaking",
                table: "queue_tickets");

            migrationBuilder.DropColumn(
                name: "region",
                schema: "matchmaking",
                table: "queue_tickets");

            migrationBuilder.DropColumn(
                name: "allocated_at",
                schema: "matchmaking",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "allocation_requested_at",
                schema: "matchmaking",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "region",
                schema: "matchmaking",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "server_address",
                schema: "matchmaking",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "session_id",
                schema: "matchmaking",
                table: "matches");
        }
    }
}
