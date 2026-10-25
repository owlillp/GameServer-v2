using System.Text.Json;
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
