using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClanService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "clans");

            migrationBuilder.CreateTable(
                name: "clan_member_history",
                schema: "clans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    clan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    left_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clan_member_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clans",
                schema: "clans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    leader_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_count = table.Column<int>(type: "integer", nullable: false),
                    clan_points = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    disbanded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fee_hold_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fee_committed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clan_join_requests",
                schema: "clans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    clan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clan_join_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_clan_join_requests_clans_clan_id",
                        column: x => x.clan_id,
                        principalSchema: "clans",
                        principalTable: "clans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "clan_members",
                schema: "clans",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clan_members", x => x.account_id);
                    table.ForeignKey(
                        name: "FK_clan_members_clans_clan_id",
                        column: x => x.clan_id,
                        principalSchema: "clans",
                        principalTable: "clans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "clan_points_ledger",
                schema: "clans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    clan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    comment = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clan_points_ledger", x => x.id);
                    table.ForeignKey(
                        name: "FK_clan_points_ledger_clans_clan_id",
                        column: x => x.clan_id,
                        principalSchema: "clans",
                        principalTable: "clans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_clan_join_requests_account_status",
                schema: "clans",
                table: "clan_join_requests",
                columns: new[] { "account_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uq_clan_join_requests_pending",
                schema: "clans",
                table: "clan_join_requests",
                columns: new[] { "clan_id", "account_id" },
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ix_clan_member_history_account_left",
                schema: "clans",
                table: "clan_member_history",
                columns: new[] { "account_id", "left_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_clan_members_clan_id",
                schema: "clans",
                table: "clan_members",
                column: "clan_id");

            migrationBuilder.CreateIndex(
                name: "ix_clan_points_ledger_clan_created",
                schema: "clans",
                table: "clan_points_ledger",
                columns: new[] { "clan_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "uq_clan_points_ledger_operation",
                schema: "clans",
                table: "clan_points_ledger",
                column: "operation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_clans_fee_pending",
                schema: "clans",
                table: "clans",
                column: "fee_hold_id",
                filter: "fee_hold_id IS NOT NULL AND fee_committed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_clans_name",
                schema: "clans",
                table: "clans",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "uq_clans_tag",
                schema: "clans",
                table: "clans",
                column: "tag",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clan_join_requests",
                schema: "clans");

            migrationBuilder.DropTable(
                name: "clan_member_history",
                schema: "clans");

            migrationBuilder.DropTable(
                name: "clan_members",
                schema: "clans");

            migrationBuilder.DropTable(
                name: "clan_points_ledger",
                schema: "clans");

            migrationBuilder.DropTable(
                name: "clans",
                schema: "clans");
        }
    }
}
