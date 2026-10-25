using ClanService.Domain;
using ClanService.Domain.Clans;
using ClanService.Domain.Members;
using ClanService.Domain.Points;
using ClanService.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClanService.Infrastructure.Postgres.Configurations;

internal sealed class ClanConfiguration : IEntityTypeConfiguration<Clan>
{
    public void Configure(EntityTypeBuilder<Clan> builder)
    {
        builder.ToTable("clans");

        builder.Property(clan => clan.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(clan => clan.Tag).HasColumnName("tag").HasMaxLength(ClanConstants.TAG_MAX_LENGTH);
        builder.Property(clan => clan.Name).HasColumnName("name").HasMaxLength(ClanConstants.NAME_MAX_LENGTH);
        builder.Property(clan => clan.Description).HasColumnName("description").HasMaxLength(ClanConstants.DESCRIPTION_MAX_LENGTH);
        builder.Property(clan => clan.LeaderAccountId).HasColumnName("leader_account_id");
        builder.Property(clan => clan.MemberCount).HasColumnName("member_count");
        builder.Property(clan => clan.ClanPoints).HasColumnName("clan_points");
        builder.Property(clan => clan.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16);
        builder.Property(clan => clan.CreatedAt).HasColumnName("created_at");
        builder.Property(clan => clan.DisbandedAt).HasColumnName("disbanded_at");
        builder.Property(clan => clan.FeeHoldId).HasColumnName("fee_hold_id");
        builder.Property(clan => clan.FeeCommittedAt).HasColumnName("fee_committed_at");

        builder.HasKey(clan => clan.Id);
        builder.Property<uint>(Constants.ROW_VERSION).IsRowVersion();

        builder.HasIndex(clan => clan.Tag).IsUnique().HasDatabaseName("uq_clans_tag");
        builder.HasIndex(clan => clan.Name).HasDatabaseName("ix_clans_name");
        builder.HasIndex(clan => clan.FeeHoldId)
            .HasFilter("fee_hold_id IS NOT NULL AND fee_committed_at IS NULL")
            .HasDatabaseName("ix_clans_fee_pending");

        builder.Ignore(clan => clan.IsActive);
    }
}

internal sealed class ClanMemberConfiguration : IEntityTypeConfiguration<ClanMember>
{
    public void Configure(EntityTypeBuilder<ClanMember> builder)
    {
        builder.ToTable("clan_members");

        builder.Property(member => member.AccountId).HasColumnName("account_id").ValueGeneratedNever();
        builder.Property(member => member.ClanId).HasColumnName("clan_id");
        builder.Property(member => member.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(16);
        builder.Property(member => member.JoinedAt).HasColumnName("joined_at");

        // One clan per player: the account id is the key.
        builder.HasKey(member => member.AccountId).HasName("pk_clan_members");
        builder.Property<uint>(Constants.ROW_VERSION).IsRowVersion();

        builder.HasOne<Clan>()
            .WithMany()
            .HasForeignKey(member => member.ClanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(member => member.ClanId).HasDatabaseName("ix_clan_members_clan_id");
    }
}

internal sealed class ClanMemberHistoryConfiguration : IEntityTypeConfiguration<ClanMemberHistory>
{
    public void Configure(EntityTypeBuilder<ClanMemberHistory> builder)
    {
        builder.ToTable("clan_member_history");

        builder.Property(history => history.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(history => history.ClanId).HasColumnName("clan_id");
        builder.Property(history => history.AccountId).HasColumnName("account_id");
        builder.Property(history => history.JoinedAt).HasColumnName("joined_at");
        builder.Property(history => history.LeftAt).HasColumnName("left_at");
        builder.Property(history => history.Reason).HasColumnName("reason").HasConversion<string>().HasMaxLength(16);

        builder.HasKey(history => history.Id);
        builder.HasIndex(history => new { history.AccountId, history.LeftAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_clan_member_history_account_left");

        builder.Ignore(history => history.StartsCooldown);
    }
}

internal sealed class ClanJoinRequestConfiguration : IEntityTypeConfiguration<ClanJoinRequest>
{
    public void Configure(EntityTypeBuilder<ClanJoinRequest> builder)
    {
        builder.ToTable("clan_join_requests");

        builder.Property(request => request.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(request => request.ClanId).HasColumnName("clan_id");
        builder.Property(request => request.AccountId).HasColumnName("account_id");
        builder.Property(request => request.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(16);
        builder.Property(request => request.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16);
        builder.Property(request => request.CreatedBy).HasColumnName("created_by");
        builder.Property(request => request.CreatedAt).HasColumnName("created_at");
        builder.Property(request => request.ResolvedBy).HasColumnName("resolved_by");
        builder.Property(request => request.ResolvedAt).HasColumnName("resolved_at");

        builder.HasKey(request => request.Id);

        // Two officers accepting the same request: the second one fails on the row version.
        builder.Property<uint>(Constants.ROW_VERSION).IsRowVersion();

        builder.HasOne<Clan>()
            .WithMany()
            .HasForeignKey(request => request.ClanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(request => new { request.ClanId, request.AccountId })
            .IsUnique()
            .HasFilter("status = 'Pending'")
            .HasDatabaseName("uq_clan_join_requests_pending");
        builder.HasIndex(request => new { request.AccountId, request.Status })
            .HasDatabaseName("ix_clan_join_requests_account_status");

        builder.Ignore(request => request.IsPending);
        builder.Ignore(request => request.IsDecidedByPlayer);
    }
}

internal sealed class ClanPointsEntryConfiguration : IEntityTypeConfiguration<ClanPointsEntry>
{
    public void Configure(EntityTypeBuilder<ClanPointsEntry> builder)
    {
        builder.ToTable("clan_points_ledger");

        builder.Property(entry => entry.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(entry => entry.ClanId).HasColumnName("clan_id");
        builder.Property(entry => entry.OperationId).HasColumnName("operation_id");
        builder.Property(entry => entry.Amount).HasColumnName("amount");
        builder.Property(entry => entry.Reason).HasColumnName("reason").HasMaxLength(ClanConstants.REASON_MAX_LENGTH);
        builder.Property(entry => entry.ActorId).HasColumnName("actor_id");
        builder.Property(entry => entry.Comment).HasColumnName("comment").HasMaxLength(ClanConstants.COMMENT_MAX_LENGTH);
        builder.Property(entry => entry.CreatedAt).HasColumnName("created_at");

        builder.HasKey(entry => entry.Id);

        builder.HasOne<Clan>()
            .WithMany()
            .HasForeignKey(entry => entry.ClanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entry => entry.OperationId).IsUnique().HasDatabaseName("uq_clan_points_ledger_operation");
        builder.HasIndex(entry => new { entry.ClanId, entry.CreatedAt }).HasDatabaseName("ix_clan_points_ledger_clan_created");
    }
}
