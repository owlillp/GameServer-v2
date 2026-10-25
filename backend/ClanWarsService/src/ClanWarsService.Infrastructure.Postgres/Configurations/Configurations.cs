using System.Text.Json;
using ClanWarsService.Domain.Battles;
using ClanWarsService.Domain.Rewards;
using ClanWarsService.Domain.Roster;
using ClanWarsService.Domain.Scores;
using ClanWarsService.Domain.Seasons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClanWarsService.Infrastructure.Postgres.Configurations;

internal sealed class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    private static readonly JsonSerializerOptions JSON = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.ToTable("seasons");

        builder.Property(season => season.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(season => season.Number).HasColumnName("number");
        builder.Property(season => season.Name).HasColumnName("name").HasMaxLength(Season.NAME_MAX_LENGTH);
        builder.Property(season => season.StartsAt).HasColumnName("starts_at");
        builder.Property(season => season.EndsAt).HasColumnName("ends_at");
        builder.Property(season => season.FinalizesAt).HasColumnName("finalizes_at");
        builder.Property(season => season.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16);
        builder.Property(season => season.ScoringRules)
            .HasColumnName("scoring_rules")
            .HasColumnType("jsonb")
            .HasConversion(
                rules => JsonSerializer.Serialize(rules, JSON),
                json => JsonSerializer.Deserialize<ScoringRules>(json, JSON) ?? ScoringRules.Default);
        builder.Property(season => season.RewardTableId).HasColumnName("reward_table_id");
        builder.Property(season => season.BattleWindowTemplate)
            .HasColumnName("battle_window_template")
            .HasColumnType("jsonb")
            .HasConversion(
                template => template == null ? null : JsonSerializer.Serialize(template, JSON),
                json => json == null ? null : JsonSerializer.Deserialize<BattleWindowTemplate>(json, JSON));
        builder.Property(season => season.CreatedAt).HasColumnName("created_at");
        builder.Property(season => season.PublishedAt).HasColumnName("published_at");
        builder.Property(season => season.StartedAt).HasColumnName("started_at");
        builder.Property(season => season.EndedAt).HasColumnName("ended_at");
        builder.Property(season => season.ClosedAt).HasColumnName("closed_at");

        builder.HasKey(season => season.Id);

        // Phase changes are conditional updates: a stale writer fails on the row version.
        builder.Property<uint>(Constants.ROW_VERSION).IsRowVersion();

        builder.HasIndex(season => season.Number).IsUnique().HasDatabaseName("uq_seasons_number");
        builder.HasIndex(season => new { season.Status, season.StartsAt }).HasDatabaseName("ix_seasons_status_starts_at");

        builder.Ignore(season => season.AcceptsScores);
    }
}

internal sealed class ClanProjectionConfiguration : IEntityTypeConfiguration<ClanProjection>
{
    public void Configure(EntityTypeBuilder<ClanProjection> builder)
    {
        builder.ToTable("clans");

        builder.Property(clan => clan.ClanId).HasColumnName("clan_id").ValueGeneratedNever();
        builder.Property(clan => clan.Tag).HasColumnName("tag").HasMaxLength(16);
        builder.Property(clan => clan.Name).HasColumnName("name").HasMaxLength(64);
        builder.Property(clan => clan.IsDisbanded).HasColumnName("is_disbanded");
        builder.Property(clan => clan.UpdatedAt).HasColumnName("updated_at");

        builder.HasKey(clan => clan.ClanId);
    }
}

internal sealed class RosterEntryConfiguration : IEntityTypeConfiguration<RosterEntry>
{
    public void Configure(EntityTypeBuilder<RosterEntry> builder)
    {
        builder.ToTable("clan_roster");

        builder.Property(entry => entry.AccountId).HasColumnName("account_id").ValueGeneratedNever();
        builder.Property(entry => entry.ClanId).HasColumnName("clan_id");
        builder.Property(entry => entry.Role).HasColumnName("role").HasMaxLength(16);
        builder.Property(entry => entry.EventAt).HasColumnName("event_at");

        builder.HasKey(entry => entry.AccountId);
        builder.Property<uint>(Constants.ROW_VERSION).IsRowVersion();
        builder.HasIndex(entry => entry.ClanId).HasDatabaseName("ix_clan_roster_clan_id");
    }
}

internal sealed class SeasonScoreConfiguration : IEntityTypeConfiguration<SeasonScore>
{
    public void Configure(EntityTypeBuilder<SeasonScore> builder)
    {
        builder.ToTable("season_scores");

        builder.Property(score => score.SeasonId).HasColumnName("season_id");
        builder.Property(score => score.ClanId).HasColumnName("clan_id");
        builder.Property(score => score.Points).HasColumnName("points");
        builder.Property(score => score.Battles).HasColumnName("battles");
        builder.Property(score => score.Wins).HasColumnName("wins");
        builder.Property(score => score.UpdatedAt).HasColumnName("updated_at");

        builder.HasKey(score => new { score.SeasonId, score.ClanId });
        builder.HasIndex(score => new { score.SeasonId, score.Points, score.UpdatedAt })
            .IsDescending(false, true, false)
            .HasDatabaseName("ix_season_scores_ranking");
    }
}

internal sealed class ScoreReceiptConfiguration : IEntityTypeConfiguration<ScoreReceipt>
{
    public void Configure(EntityTypeBuilder<ScoreReceipt> builder)
    {
        builder.ToTable("score_receipts");

        builder.Property(receipt => receipt.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(receipt => receipt.SeasonId).HasColumnName("season_id");
        builder.Property(receipt => receipt.ClanId).HasColumnName("clan_id");
        builder.Property(receipt => receipt.SourceId).HasColumnName("source_id").HasMaxLength(ScoreReceipt.SOURCE_MAX_LENGTH);
        builder.Property(receipt => receipt.Amount).HasColumnName("amount");
        builder.Property(receipt => receipt.Reason).HasColumnName("reason").HasMaxLength(ScoreReceipt.REASON_MAX_LENGTH);
        builder.Property(receipt => receipt.ActorId).HasColumnName("actor_id");
        builder.Property(receipt => receipt.CreatedAt).HasColumnName("created_at");

        builder.HasKey(receipt => receipt.Id);
        builder.HasIndex(receipt => new { receipt.SeasonId, receipt.ClanId, receipt.SourceId })
            .IsUnique()
            .HasDatabaseName("uq_score_receipts_source");
    }
}

internal sealed class MemberSeasonStatsConfiguration : IEntityTypeConfiguration<MemberSeasonStats>
{
    public void Configure(EntityTypeBuilder<MemberSeasonStats> builder)
    {
        builder.ToTable("member_season_stats");

        builder.Property(stats => stats.SeasonId).HasColumnName("season_id");
        builder.Property(stats => stats.AccountId).HasColumnName("account_id");
        builder.Property(stats => stats.ClanId).HasColumnName("clan_id");
        builder.Property(stats => stats.Points).HasColumnName("points");
        builder.Property(stats => stats.Battles).HasColumnName("battles");
        builder.Property(stats => stats.Wins).HasColumnName("wins");

        builder.HasKey(stats => new { stats.SeasonId, stats.AccountId, stats.ClanId });
        builder.HasIndex(stats => new { stats.SeasonId, stats.ClanId }).HasDatabaseName("ix_member_season_stats_clan");
    }
}

internal sealed class SeasonResultConfiguration : IEntityTypeConfiguration<SeasonResult>
{
    public void Configure(EntityTypeBuilder<SeasonResult> builder)
    {
        builder.ToTable("season_results");

        builder.Property(result => result.SeasonId).HasColumnName("season_id");
        builder.Property(result => result.ClanId).HasColumnName("clan_id");
        builder.Property(result => result.Rank).HasColumnName("rank");
        builder.Property(result => result.Points).HasColumnName("points");
        builder.Property(result => result.Battles).HasColumnName("battles");
        builder.Property(result => result.Wins).HasColumnName("wins");

        builder.HasKey(result => new { result.SeasonId, result.ClanId });
        builder.HasIndex(result => new { result.SeasonId, result.Rank }).HasDatabaseName("ix_season_results_rank");
    }
}

internal sealed class BattleWindowConfiguration : IEntityTypeConfiguration<BattleWindow>
{
    public void Configure(EntityTypeBuilder<BattleWindow> builder)
    {
        builder.ToTable("battle_windows");

        builder.Property(window => window.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(window => window.SeasonId).HasColumnName("season_id");
        builder.Property(window => window.StartsAt).HasColumnName("starts_at");
        builder.Property(window => window.EndsAt).HasColumnName("ends_at");

        builder.HasKey(window => window.Id);
        builder.HasIndex(window => new { window.SeasonId, window.StartsAt })
            .IsUnique()
            .HasDatabaseName("uq_battle_windows_season_start");
    }
}

internal sealed class BattleQueueEntryConfiguration : IEntityTypeConfiguration<BattleQueueEntry>
{
    public void Configure(EntityTypeBuilder<BattleQueueEntry> builder)
    {
        builder.ToTable("battle_queue");

        builder.Property(entry => entry.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(entry => entry.SeasonId).HasColumnName("season_id");
        builder.Property(entry => entry.ClanId).HasColumnName("clan_id");
        builder.Property(entry => entry.Squad).HasColumnName("squad").HasColumnType("uuid[]");
        builder.Property(entry => entry.Rating).HasColumnName("rating");
        builder.Property(entry => entry.QueuedBy).HasColumnName("queued_by");
        builder.Property(entry => entry.QueuedAt).HasColumnName("queued_at");
        builder.Property(entry => entry.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16);
        builder.Property(entry => entry.BattleId).HasColumnName("battle_id");
        builder.Property(entry => entry.ResolvedAt).HasColumnName("resolved_at");

        builder.HasKey(entry => entry.Id);
        builder.Property<uint>(Constants.ROW_VERSION).IsRowVersion();

        // One squad in the queue per clan.
        builder.HasIndex(entry => entry.ClanId)
            .IsUnique()
            .HasFilter("status = 'Queued'")
            .HasDatabaseName("uq_battle_queue_clan_queued");
        builder.HasIndex(entry => new { entry.SeasonId, entry.Status, entry.QueuedAt })
            .HasDatabaseName("ix_battle_queue_season_status");
    }
}

internal sealed class BattleConfiguration : IEntityTypeConfiguration<Battle>
{
    public void Configure(EntityTypeBuilder<Battle> builder)
    {
        builder.ToTable("battles");

        builder.Property(battle => battle.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(battle => battle.SeasonId).HasColumnName("season_id");
        builder.Property(battle => battle.ClanAId).HasColumnName("clan_a_id");
        builder.Property(battle => battle.ClanBId).HasColumnName("clan_b_id");
        builder.Property(battle => battle.SquadA).HasColumnName("squad_a").HasColumnType("uuid[]");
        builder.Property(battle => battle.SquadB).HasColumnName("squad_b").HasColumnType("uuid[]");
        builder.Property(battle => battle.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16);
        builder.Property(battle => battle.MatchId).HasColumnName("match_id");
        builder.Property(battle => battle.WinnerClanId).HasColumnName("winner_clan_id");
        builder.Property(battle => battle.PointsA).HasColumnName("points_a");
        builder.Property(battle => battle.PointsB).HasColumnName("points_b");
        builder.Property(battle => battle.CancelReason).HasColumnName("cancel_reason").HasMaxLength(64);
        builder.Property(battle => battle.CreatedAt).HasColumnName("created_at");
        builder.Property(battle => battle.StartedAt).HasColumnName("started_at");
        builder.Property(battle => battle.FinishedAt).HasColumnName("finished_at");

        builder.HasKey(battle => battle.Id);
        builder.HasIndex(battle => new { battle.Status, battle.CreatedAt }).HasDatabaseName("ix_battles_status_created");
        builder.HasIndex(battle => new { battle.ClanAId, battle.CreatedAt }).HasDatabaseName("ix_battles_clan_a");
        builder.HasIndex(battle => new { battle.ClanBId, battle.CreatedAt }).HasDatabaseName("ix_battles_clan_b");
        builder.HasIndex(battle => battle.SeasonId).HasDatabaseName("ix_battles_season_id");

        builder.Ignore(battle => battle.ExternalRef);
        builder.Ignore(battle => battle.IsOpen);
    }
}

internal static class ItemsJson
{
    private static readonly JsonSerializerOptions JSON = new(JsonSerializerDefaults.Web);

    public static PropertyBuilder<IReadOnlyList<RewardItem>> AsItemsJson(this PropertyBuilder<IReadOnlyList<RewardItem>> property) =>
        property
            .HasColumnType("jsonb")
            .HasConversion(
                items => JsonSerializer.Serialize(items, JSON),
                json => JsonSerializer.Deserialize<List<RewardItem>>(json, JSON) ?? new List<RewardItem>(),
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<RewardItem>>(
                    (left, right) => left!.SequenceEqual(right!),
                    items => items.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    items => items.ToList()));
}

internal sealed class RewardTableConfiguration : IEntityTypeConfiguration<RewardTable>
{
    public void Configure(EntityTypeBuilder<RewardTable> builder)
    {
        builder.ToTable("reward_tables");

        builder.Property(table => table.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(table => table.Name).HasColumnName("name").HasMaxLength(RewardTable.NAME_MAX_LENGTH);
        builder.Property(table => table.CreatedAt).HasColumnName("created_at");

        builder.HasKey(table => table.Id);
        builder.HasMany(table => table.Tiers).WithOne().HasForeignKey(tier => tier.TableId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(table => table.Tiers).AutoInclude();
    }
}

internal sealed class RewardTierConfiguration : IEntityTypeConfiguration<RewardTier>
{
    public void Configure(EntityTypeBuilder<RewardTier> builder)
    {
        builder.ToTable("reward_tiers");

        builder.Property(tier => tier.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(tier => tier.TableId).HasColumnName("table_id");
        builder.Property(tier => tier.FromRank).HasColumnName("from_rank");
        builder.Property(tier => tier.ToRank).HasColumnName("to_rank");
        builder.Property(tier => tier.ClanPoints).HasColumnName("clan_points");
        builder.Property(tier => tier.CurrencyCode).HasColumnName("currency_code").HasMaxLength(16);
        builder.Property(tier => tier.CurrencyAmount).HasColumnName("currency_amount");
        builder.Property(tier => tier.Items).HasColumnName("items").AsItemsJson();
        builder.Property(tier => tier.MinBattles).HasColumnName("min_battles");

        builder.HasKey(tier => tier.Id);
        builder.Ignore(tier => tier.HasMemberReward);
    }
}

internal sealed class SeasonRewardGrantConfiguration : IEntityTypeConfiguration<SeasonRewardGrant>
{
    public void Configure(EntityTypeBuilder<SeasonRewardGrant> builder)
    {
        builder.ToTable("season_reward_grants");

        builder.Property(grant => grant.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(grant => grant.SeasonId).HasColumnName("season_id");
        builder.Property(grant => grant.ClanId).HasColumnName("clan_id");
        builder.Property(grant => grant.AccountId).HasColumnName("account_id");
        builder.Property(grant => grant.Rank).HasColumnName("rank");
        builder.Property(grant => grant.ClanPoints).HasColumnName("clan_points");
        builder.Property(grant => grant.CurrencyCode).HasColumnName("currency_code").HasMaxLength(16);
        builder.Property(grant => grant.CurrencyAmount).HasColumnName("currency_amount");
        builder.Property(grant => grant.Items).HasColumnName("items").AsItemsJson();
        builder.Property(grant => grant.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16);
        builder.Property(grant => grant.Attempts).HasColumnName("attempts");
        builder.Property(grant => grant.NextAttemptAt).HasColumnName("next_attempt_at");
        builder.Property(grant => grant.LastError).HasColumnName("last_error").HasMaxLength(512);
        builder.Property(grant => grant.PointsPublishedAt).HasColumnName("points_published_at");
        builder.Property(grant => grant.WalletCreditedAt).HasColumnName("wallet_credited_at");
        builder.Property(grant => grant.ItemsPublishedAt).HasColumnName("items_published_at");
        builder.Property(grant => grant.ItemsGrantedAt).HasColumnName("items_granted_at");
        builder.Property(grant => grant.CreatedAt).HasColumnName("created_at");
        builder.Property(grant => grant.CompletedAt).HasColumnName("completed_at");

        builder.HasKey(grant => grant.Id);
        builder.Property<uint>(Constants.ROW_VERSION).IsRowVersion();

        // One clan grant (account null) and one grant per member per season: closing twice cannot double.
        builder.HasIndex(grant => new { grant.SeasonId, grant.ClanId, grant.AccountId })
            .IsUnique()
            .AreNullsDistinct(false)
            .HasDatabaseName("uq_season_reward_grants_recipient");
        builder.HasIndex(grant => new { grant.Status, grant.NextAttemptAt })
            .HasFilter("status = 'Pending'")
            .HasDatabaseName("ix_season_reward_grants_due");
        builder.HasIndex(grant => new { grant.SeasonId, grant.Status }).HasDatabaseName("ix_season_reward_grants_season_status");
        builder.HasIndex(grant => new { grant.SeasonId, grant.AccountId }).HasDatabaseName("ix_season_reward_grants_account");

        builder.Ignore(grant => grant.NeedsPoints);
        builder.Ignore(grant => grant.NeedsWallet);
        builder.Ignore(grant => grant.NeedsItemsPublish);
    }
}
