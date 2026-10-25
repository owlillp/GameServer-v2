using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchmakingService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class RowVersionConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "matchmaking",
                table: "queue_tickets",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "matchmaking",
                table: "parties",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "matchmaking",
                table: "matches",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "matchmaking",
                table: "match_reward_grants",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "matchmaking",
                table: "queue_tickets");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "matchmaking",
                table: "parties");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "matchmaking",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "matchmaking",
                table: "match_reward_grants");
        }
    }
}
