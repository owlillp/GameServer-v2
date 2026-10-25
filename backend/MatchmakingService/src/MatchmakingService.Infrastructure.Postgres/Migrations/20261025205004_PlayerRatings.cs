using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchmakingService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class PlayerRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "rating",
                schema: "matchmaking",
                table: "queue_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 1000);

            migrationBuilder.CreateTable(
                name: "player_ratings",
                schema: "matchmaking",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    games = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_ratings", x => x.account_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_ratings",
                schema: "matchmaking");

            migrationBuilder.DropColumn(
                name: "rating",
                schema: "matchmaking",
                table: "queue_tickets");
        }
    }
}
