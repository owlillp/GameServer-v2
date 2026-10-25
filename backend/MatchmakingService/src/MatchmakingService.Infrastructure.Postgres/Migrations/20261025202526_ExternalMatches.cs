using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchmakingService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class ExternalMatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "external_ref",
                schema: "matchmaking",
                table: "matches",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "uq_matches_external_ref",
                schema: "matchmaking",
                table: "matches",
                column: "external_ref",
                unique: true,
                filter: "external_ref IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_matches_external_ref",
                schema: "matchmaking",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "external_ref",
                schema: "matchmaking",
                table: "matches");
        }
    }
}
